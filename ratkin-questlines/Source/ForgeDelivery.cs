using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // 鐵匠屋交貨挑選視窗（C 方案）——讓玩家親手挑「要交哪些武器」，並用價值進度條讓分級後果一目了然。
    // ------------------------------------------------------------------
    // 為什麼自製而非原生交易：原生 Dialog_Trade 綁死 TradeSession/Tradeable，且易被交易 mod patch；
    //   自製 Window 完全不碰交易系統＝零相容風險，又能貼「N~M 把＋品質＋價值＋分級 band」的委託邏輯（見 6b §6.8）。
    // 借原版交易視窗的視覺排版（圖示/名稱/品質/市值欄＋捲動列表），後端自寫精簡版。
    //
    // 流程：對話「交貨」選項的 action＝CQFAction_OpenDeliveryWindow（帶需求＋band 設定）→ 開本視窗；
    //   玩家勾選武器（受 count.min~max 約束）→ 價值進度條顯示落在第幾 band → 確認：消費勾選、發該 band 獎勵、
    //   set 該 band 的 Ev_* 名聲旗標、跳訊息。

    // 一個分級後果 band：交付總值達 valueMultiple×基準門檻 → 此獎勵與名聲。bands 由低到高排。
    public class ForgeDeliveryBand : IExposable
    {
        public float valueMultiple = 1f;               // 1×/2×/3× 基準門檻
        public int silver = 0;                         // 白銀報酬
        public List<ThingDefCountClass> extraThings;   // 額外實體獎勵（口糧/材料…）
        public List<string> evFlags;                   // 達此 band 要 set 的全域名聲旗標（Ev_ForgeWellDone_*/Ev_WeaponDeal/Ev_Charity…）
        public string message;                         // 達此 band 的訊息（Keyed key）

        public void ExposeData()
        {
            Scribe_Values.Look(ref valueMultiple, "valueMultiple", 1f);
            Scribe_Values.Look(ref silver, "silver", 0);
            Scribe_Collections.Look(ref extraThings, "extraThings", LookMode.Deep);
            Scribe_Collections.Look(ref evFlags, "evFlags", LookMode.Value);
            Scribe_Values.Look(ref message, "message");
        }
    }

    // 對話「交貨」選項用：開啟交貨挑選視窗。需求與 bands 由 XML 設。
    public class CQFAction_OpenDeliveryWindow : CQFAction_Target
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
        public string titleKey = "RatkinQL_Forge_DeliverWindow_Title";

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

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Map map = QualityDeliveryUtil.MapFrom(targets);
            if (map == null)
            {
                return;
            }
            Thing envoy = null;
            TargetInfo ti;
            if (targets != null && targets.TryGetValue("Interviewee", out ti) && ti.HasThing)
            {
                envoy = ti.Thing;   // 交貨成功後遣離的對象（帶頭訪客）
            }
            Find.WindowStack.Add(new Dialog_ForgeDelivery(map, count, minQuality, weaponFilter, BaseThreshold(), bands, titleKey, envoy));
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
            Scribe_Values.Look(ref titleKey, "titleKey", "RatkinQL_Forge_DeliverWindow_Title");
        }
    }

    public class Dialog_ForgeDelivery : Window
    {
        private readonly Map map;
        private readonly IntRange count;
        private readonly QualityCategory minQuality;
        private readonly string weaponFilter;
        private readonly float baseThreshold;
        private readonly List<ForgeDeliveryBand> bands;
        private readonly string titleKey;
        private readonly Thing envoy;              // 帶頭訪客：獎勵放其腳下、交貨成功後遣離

        private readonly List<Thing> candidates;
        private readonly HashSet<Thing> selected = new HashSet<Thing>();
        private Vector2 scrollPos = Vector2.zero;

        public Dialog_ForgeDelivery(Map map, IntRange count, QualityCategory minQuality, string weaponFilter,
            float baseThreshold, List<ForgeDeliveryBand> bands, string titleKey, Thing envoy)
        {
            this.map = map;
            this.count = count;
            this.minQuality = minQuality;
            this.weaponFilter = weaponFilter;
            this.baseThreshold = baseThreshold;
            this.bands = bands;
            this.titleKey = titleKey;
            this.envoy = envoy;
            candidates = WeaponValueUtil.QualifyingWeapons(map, minQuality, weaponFilter);

            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            draggable = true;
        }

        public override Vector2 InitialSize => new Vector2(620f, 720f);

        private float SelectedValue()
        {
            float v = 0f;
            foreach (Thing t in selected)
            {
                v += t.MarketValue;
            }
            return v;
        }

        // 交付總值落在第幾 band（未達基準＝-1）。bands 由低到高，取滿足的最高者。
        private int AchievedBandIndex(float value, int selCount)
        {
            if (bands == null || bands.Count == 0 || selCount < count.min || value < baseThreshold)
            {
                return -1;
            }
            int idx = -1;
            for (int i = 0; i < bands.Count; i++)
            {
                if (value >= bands[i].valueMultiple * baseThreshold)
                {
                    idx = i;
                }
            }
            return idx;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 34f), titleKey.Translate());
            Text.Font = GameFont.Small;
            y += 40f;

            int selCount = selected.Count;
            float selValue = SelectedValue();
            int band = AchievedBandIndex(selValue, selCount);

            // 需求摘要
            string req = "RatkinQL_Forge_DeliverWindow_Req".Translate(count.min, count.max, minQuality.GetLabel(), Mathf.RoundToInt(baseThreshold));
            Widgets.Label(new Rect(0f, y, inRect.width, 44f), req);
            y += 46f;

            // 進度：目前選了幾件 / 總值，落在第幾 band
            float topMult = (bands != null && bands.Count > 0) ? bands[bands.Count - 1].valueMultiple : 1f;
            float fill = baseThreshold > 0f ? Mathf.Clamp01(selValue / (baseThreshold * topMult)) : 0f;
            Rect barRect = new Rect(0f, y, inRect.width, 24f);
            Widgets.FillableBar(barRect, fill);
            // band 刻度線
            if (bands != null && baseThreshold > 0f)
            {
                for (int i = 0; i < bands.Count; i++)
                {
                    float m = bands[i].valueMultiple / topMult;
                    float x = barRect.x + barRect.width * Mathf.Clamp01(m);
                    Widgets.DrawLineVertical(x, barRect.y, barRect.height);
                }
            }
            y += 26f;
            string bandLabel = band >= 0
                ? "RatkinQL_Forge_DeliverWindow_Band".Translate(selCount, Mathf.RoundToInt(selValue), band + 1)
                : "RatkinQL_Forge_DeliverWindow_Short".Translate(selCount, Mathf.RoundToInt(selValue));
            Widgets.Label(new Rect(0f, y, inRect.width, 24f), bandLabel);
            y += 30f;

            // 武器列表（可捲動、勾選）
            float listTop = y;
            float listBottom = inRect.height - 44f;
            Rect outRect = new Rect(0f, listTop, inRect.width, listBottom - listTop);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, candidates.Count * 30f + 4f);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            float ry = 0f;
            bool atMax = selCount >= count.max;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing t = candidates[i];
                Rect row = new Rect(0f, ry, viewRect.width, 28f);
                if (i % 2 == 1)
                {
                    Widgets.DrawLightHighlight(row);
                }
                Widgets.InfoCardButton(row.x + 2f, row.y + 2f, t);
                bool on = selected.Contains(t);
                string label = t.LabelCap + "  (" + Mathf.RoundToInt(t.MarketValue) + " " + "RatkinQL_Forge_DeliverWindow_SilverUnit".Translate() + ")";
                bool disabled = !on && atMax;               // 已達 max 時不能再勾新的
                bool newOn = on;
                Widgets.CheckboxLabeled(new Rect(row.x + 30f, row.y, row.width - 30f, row.height), label, ref newOn, disabled);
                if (newOn != on)
                {
                    if (newOn)
                    {
                        selected.Add(t);
                    }
                    else
                    {
                        selected.Remove(t);
                    }
                }
                ry += 30f;
            }
            Widgets.EndScrollView();

            // 底部按鈕
            float by = inRect.height - 38f;
            bool canConfirm = band >= 0;
            if (Widgets.ButtonText(new Rect(0f, by, inRect.width / 2f - 6f, 34f), "RatkinQL_Forge_DeliverWindow_Confirm".Translate(), true, true, canConfirm))
            {
                Execute(band);
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 6f, by, inRect.width / 2f - 6f, 34f), "RatkinQL_Forge_DeliverWindow_Cancel".Translate()))
            {
                Close();
            }
        }

        private void Execute(int bandIdx)
        {
            if (bandIdx < 0 || bands == null || bandIdx >= bands.Count)
            {
                return;
            }
            ForgeDeliveryBand b = bands[bandIdx];

            // 消費勾選的武器
            foreach (Thing t in selected)
            {
                if (t != null && !t.Destroyed)
                {
                    t.Destroy(DestroyMode.Vanish);
                }
            }

            // 名聲旗標
            GameComponent_Editor ed = GameComponent_Editor.Component;
            if (ed != null && b.evFlags != null)
            {
                foreach (string key in b.evFlags)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        ed.SetBool(key, true);
                    }
                }
            }

            // 獎勵（白銀＋額外物）──面對面交貨，直接放在訪客腳下（像普通交易對方遞貨），不空投。
            //   放置點：訪客所在格；訪客不在場才退回貿易點。
            IntVec3 dropCell = (envoy != null && envoy.Spawned) ? envoy.Position : DropCellFinder.TradeDropSpot(map);
            PlaceReward(ThingDefOf.Silver, b.silver, dropCell);
            if (b.extraThings != null)
            {
                foreach (ThingDefCountClass tc in b.extraThings)
                {
                    if (tc == null || tc.thingDef == null || tc.count <= 0)
                    {
                        continue;
                    }
                    PlaceReward(tc.thingDef, tc.count, dropCell);
                }
            }

            if (!b.message.NullOrEmpty())
            {
                Messages.Message(b.message.Translate(), MessageTypeDefOf.PositiveEvent, false);
            }

            // 交貨成功 → 遣離訪客（移除對話＋走出地圖）
            ForgeEnvoyUtil.SendEnvoyAway(envoy);
        }

        private void PlaceReward(ThingDef def, int stack, IntVec3 cell)
        {
            if (def == null || stack <= 0)
            {
                return;
            }
            Thing t = ThingMaker.MakeThing(def);
            t.stackCount = stack;
            GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
        }
    }
}
