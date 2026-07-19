using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // 交貨 B 方案：用原生交易界面挑貨，交易後由 Harmony postfix 偵測賣出的武器、判定委託 band。
    // ------------------------------------------------------------------
    // 相容評估（見 6b §6.8）：postfix 唯讀觀察、不改交易/價格 → 與交易 UI/定價 mod 相容；
    //   只怕「完全取代 vanilla 交易流程」的 mod（少見）。偵測不到就維持委託開啟＝退場安全網。
    // 與 C（自製視窗）差異：B 走原生交易，玩家已從交易拿到市值白銀，故 band 只補「名聲＋額外物＋訊息」，不再補 band.silver（避免雙重付款）。

    // 待結算的交易委託（key＝trader pawn thingIDNumber）。靜態、非存檔持久：交易同回合完成，
    // 中途存檔的極端邊界不保證（B＝備選路線，C 才是持久主力）。
    public class PendingForgeTrade
    {
        public int countMin;
        public int countMax;
        public QualityCategory minQuality;
        public string weaponFilter;
        public float baseThreshold;
        public List<ForgeDeliveryBand> bands;
    }

    public static class ForgeTradeRegistry
    {
        private static readonly Dictionary<int, PendingForgeTrade> pending = new Dictionary<int, PendingForgeTrade>();

        public static void Register(int traderId, PendingForgeTrade p)
        {
            pending[traderId] = p;
        }

        public static PendingForgeTrade Get(int traderId)
        {
            PendingForgeTrade p;
            pending.TryGetValue(traderId, out p);
            return p;
        }

        public static void Clear(int traderId)
        {
            pending.Remove(traderId);
        }
    }

    // 對話「交貨（開交易界面）」選項：把訪客設為 trader ＋ 登記委託 ＋ 開原生交易。
    public class CQFAction_OpenTradeDelivery : CQFAction_Target
    {
        public IntRange count = new IntRange(1, 1);
        public QualityCategory minQuality = QualityCategory.Awful;
        public string weaponFilter;
        public float minTotalValue = -1f;
        public ThingDef refThing;
        public ThingDef refStuff;
        public QualityCategory refQuality = QualityCategory.Good;
        public int refCount = 4;
        public List<ForgeDeliveryBand> bands = new List<ForgeDeliveryBand>();
        public string traderKindDef = "Visitor_Outlander_Standard";

        private float BaseThreshold()
        {
            if (minTotalValue >= 0f)
            {
                return minTotalValue;
            }
            if (refThing != null)
            {
                return refCount * WeaponValueUtil.BaselineUnitValue(refThing, refStuff, refQuality);
            }
            return 0f;
        }

        private static Pawn TargetPawn(Dictionary<string, TargetInfo> targets, string key)
        {
            TargetInfo ti;
            if (targets != null && targets.TryGetValue(key, out ti) && ti.HasThing)
            {
                return ti.Thing as Pawn;
            }
            return null;
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Pawn trader = TargetPawn(targets, "Interviewee");
            Pawn negotiator = TargetPawn(targets, "Interviewer");
            if (trader == null || negotiator == null || trader.Map == null)
            {
                return;
            }

            // 設為 trader（若還不是）：仿 vanilla IncidentWorker_VisitorGroup 轉小商販
            if (trader.trader == null || trader.trader.traderKind == null)
            {
                TraderKindDef tk = DefDatabase<TraderKindDef>.GetNamedSilentFail(traderKindDef);
                if (tk == null)
                {
                    return;
                }
                trader.mindState.wantsToTradeWithColony = true;
                PawnComponentsUtility.AddAndRemoveDynamicComponents(trader, true);
                if (trader.trader != null)
                {
                    trader.trader.traderKind = tk;
                }
                GenerateStock(trader, tk);
            }

            ForgeTradeRegistry.Register(trader.thingIDNumber, new PendingForgeTrade
            {
                countMin = count.min,
                countMax = count.max,
                minQuality = minQuality,
                weaponFilter = weaponFilter,
                baseThreshold = BaseThreshold(),
                bands = bands,
            });

            TradeSession.SetupWith(trader, negotiator, false);
            Find.WindowStack.Add(new Dialog_Trade(negotiator, trader, false));
        }

        private void GenerateStock(Pawn trader, TraderKindDef tk)
        {
            ThingSetMakerParams parms = new ThingSetMakerParams
            {
                traderDef = tk,
                tile = trader.Tile,
                makingFaction = trader.Faction,
            };
            foreach (Thing item in ThingSetMakerDefOf.TraderStock.root.Generate(parms))
            {
                Pawn p = item as Pawn;
                if (p != null)
                {
                    p.Destroy();   // 本委託交易不帶奴隸/動物，略過
                    continue;
                }
                if (!trader.inventory.innerContainer.TryAdd(item))
                {
                    item.Destroy();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count", new IntRange(1, 1));
            Scribe_Values.Look(ref minQuality, "minQuality", QualityCategory.Awful);
            Scribe_Values.Look(ref weaponFilter, "weaponFilter");
            Scribe_Values.Look(ref minTotalValue, "minTotalValue", -1f);
            Scribe_Defs.Look(ref refThing, "refThing");
            Scribe_Defs.Look(ref refStuff, "refStuff");
            Scribe_Values.Look(ref refQuality, "refQuality", QualityCategory.Good);
            Scribe_Values.Look(ref refCount, "refCount", 4);
            Scribe_Collections.Look(ref bands, "bands", LookMode.Deep);
            Scribe_Values.Look(ref traderKindDef, "traderKindDef", "Visitor_Outlander_Standard");
        }
    }

    [HarmonyPatch(typeof(TradeDeal), "TryExecute")]
    public static class Patch_TradeDeal_TryExecute
    {
        // ⚠ 為何 Prefix 快照：TradeDeal.TryExecute 成交後對每件 ResolveTrade() → 隨即 Reset()（tradeables.Clear()+AddAllTradeables()），
        //   才 return。若在 Postfix 讀 tradeables 已是清空重建、CountToTransfer 全 0（賣出的武器也已離開玩家）→ 偵測恆為 0、B 永不發獎。
        //   故在 Prefix（Reset 前）快照玩家意圖賣出的武器件數/價值，Postfix 僅在 __result && actuallyTraded 時消費快照。
        private static Pawn sTrader;
        private static int sCount;
        private static float sValue;

        public static void Prefix()
        {
            sTrader = null;
            sCount = 0;
            sValue = 0f;
            Pawn tp = TradeSession.trader as Pawn;
            if (tp == null || TradeSession.deal == null)
            {
                return;
            }
            PendingForgeTrade pend = ForgeTradeRegistry.Get(tp.thingIDNumber);
            if (pend == null)
            {
                return;
            }
            List<Tradeable> tradeables = TradeSession.deal.AllTradeables;
            for (int i = 0; i < tradeables.Count; i++)
            {
                Tradeable t = tradeables[i];
                if (t == null || t.ActionToDo != TradeAction.PlayerSells)
                {
                    continue;
                }
                Thing any = t.AnyThing;
                if (any == null || any.def == null || !any.def.IsWeapon)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(pend.weaponFilter))
                {
                    if (pend.weaponFilter == "Melee" && !any.def.IsMeleeWeapon)
                    {
                        continue;
                    }
                    if (pend.weaponFilter == "Ranged" && !any.def.IsRangedWeapon)
                    {
                        continue;
                    }
                }
                if (pend.minQuality > QualityCategory.Awful)
                {
                    CompQuality cq = any.TryGetComp<CompQuality>();
                    if (cq == null || (int)cq.Quality < (int)pend.minQuality)
                    {
                        continue;
                    }
                }
                int n = Math.Abs(t.CountToTransfer);
                sCount += n;
                sValue += any.MarketValue * n;
            }
            sTrader = tp;   // 有登記委託才記；供 Postfix 用（此時尚未 Reset，資料有效）
        }

        public static void Postfix(bool __result, bool actuallyTraded)
        {
            Pawn tp = sTrader;
            int soldCount = sCount;
            float soldValue = sValue;
            sTrader = null;
            sCount = 0;
            sValue = 0f;

            if (!__result || !actuallyTraded || tp == null)
            {
                return;   // 交易沒真的成交，或非本委託 pawn：快照作廢
            }
            PendingForgeTrade pend = ForgeTradeRegistry.Get(tp.thingIDNumber);
            if (pend == null)
            {
                return;
            }

            int band = -1;
            if (pend.bands != null && soldCount >= pend.countMin && soldValue >= pend.baseThreshold)
            {
                for (int i = 0; i < pend.bands.Count; i++)
                {
                    if (soldValue >= pend.bands[i].valueMultiple * pend.baseThreshold)
                    {
                        band = i;
                    }
                }
            }
            if (band < 0)
            {
                return;   // 未達門檻：委託留著，玩家可再交易補足
            }

            GrantBand(pend.bands[band], tp.MapHeld ?? Find.CurrentMap);
            ForgeTradeRegistry.Clear(tp.thingIDNumber);
            ForgeEnvoyUtil.SendEnvoyAway(tp);   // 交易達標 → 遣離訪客（移除對話＋走出地圖）
        }

        private static void GrantBand(ForgeDeliveryBand b, Map map)
        {
            GameComponent_Editor ed = GameComponent_Editor.Component;
            if (ed != null && b.evFlags != null)
            {
                foreach (string k in b.evFlags)
                {
                    if (!string.IsNullOrEmpty(k))
                    {
                        ed.SetBool(k, true);
                    }
                }
            }
            // B＝原生交易，市值白銀玩家已從交易取得，故不再補 band.silver，只補額外物＋名聲＋訊息（避免雙重付款）。
            if (b.extraThings != null && map != null)
            {
                List<Thing> reward = new List<Thing>();
                foreach (ThingDefCountClass tc in b.extraThings)
                {
                    if (tc == null || tc.thingDef == null || tc.count <= 0)
                    {
                        continue;
                    }
                    Thing rt = ThingMaker.MakeThing(tc.thingDef);
                    rt.stackCount = tc.count;
                    reward.Add(rt);
                }
                if (reward.Count > 0)
                {
                    IntVec3 spot = DropCellFinder.TradeDropSpot(map);
                    DropPodUtility.DropThingsNear(spot, map, reward, 110, false, false, true, true);
                }
            }
            if (!b.message.NullOrEmpty())
            {
                Messages.Message(b.message.Translate(), MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}
