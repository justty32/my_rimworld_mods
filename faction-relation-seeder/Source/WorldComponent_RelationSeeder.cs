using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.relations
{
    /// <summary>開局一次性播種派系關係矩陣（讀所有 RelationSeedDef）。零 Harmony。
    /// WorldComponent 由引擎自動實例化，無需註冊 Def。
    ///
    /// 觸發時機（option C）：FinalizeInit **只標記待播**，實際 Apply() 延到
    /// WorldComponentTick——因為 FinalizeInit 跑在世界生成期，此時 Faction.OfPlayer 尚未就緒，
    /// 提早改善意會讓 vanilla 每次撲空記「Could not find player faction.」。等玩家派系
    /// （有 Ideology 時再等 PrimaryIdeo）就緒後才播一次。
    ///
    /// 生命週期：只在「新世界」（FinalizeInit fromLoad=false）標記待播；舊檔載入不打擾既有關係。
    /// `seeded`/`pendingSeed` 皆 Scribe 持久化 → 已播存檔不重播、冪等。</summary>
    public class WorldComponent_RelationSeeder : WorldComponent
    {
        private bool seeded;
        /// <summary>已標記待播、但尚未實際 Apply（等玩家派系就緒）。持久化以防「標記後、播種前」被存檔。</summary>
        private bool pendingSeed;

        public WorldComponent_RelationSeeder(World world) : base(world)
        {
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            // 只「標記待播」，不在此碰 goodwill / 玩家派系（世界生成期玩家派系未就緒）。
            // 新遊戲（fromLoad=false）且尚未播過才標記；舊檔載入一律不打擾既有關係。
            if (!fromLoad && !seeded)
            {
                pendingSeed = true;
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (!pendingSeed || seeded)
            {
                return;
            }
            // 等玩家派系就緒；有 Ideology 時再等玩家 ideo 就緒（SameIdeo 善意情勢需要它）。
            if (Current.Game == null || Faction.OfPlayer == null)
            {
                return;
            }
            if (ModsConfig.IdeologyActive && Faction.OfPlayer.ideos?.PrimaryIdeo == null)
            {
                return;
            }
            // 冪等：先落旗標，再 Apply——即使 Apply 出狀況也不會下一 tick 重播/重疊。
            pendingSeed = false;
            seeded = true;
            Apply();
        }

        /// <summary>套用所有 RelationSeedDef。逐條例外隔離，一條壞不拖垮其餘。
        /// 也是 dev「重新播種」入口（見 RelationSeederDebug）。</summary>
        public void Apply()
        {
            int applied = 0;
            int skipped = 0;
            foreach (RelationSeedDef seed in DefDatabase<RelationSeedDef>.AllDefsListForReading)
            {
                foreach (RelationSeedEntry entry in seed.relations)
                {
                    try
                    {
                        applied += ApplyEntry(entry, ref skipped);
                    }
                    catch (System.Exception e)
                    {
                        Log.Warning("[relation-seeder] " + seed.defName + " 條目 "
                            + entry.a + "↔" + entry.b + " 例外，跳過：" + e);
                    }
                }
            }
            // 重算善意情勢，讓關係即時生效（鏡像 faction-politics 分裂路徑）——但這只是便利收尾，非必要。
            // 世界生成期玩家派系的 ideo 尚未指派，RecalculateAll 會走到 vanilla
            // GoodwillSituationWorker_SameIdeo.GetNaturalGoodwillOffset 對 null PrimaryIdeo 解參考而 NRE。
            // 故加「玩家 ideo 已就緒」守衛 + try/catch；失敗就交給遊戲之後自然重算。關係值本身已在上面套好。
            if (applied > 0
                && Current.Game != null
                && Faction.OfPlayer?.ideos?.PrimaryIdeo != null)
            {
                try
                {
                    Find.GoodwillSituationManager.RecalculateAll(canSendHostilityChangedLetter: false);
                }
                catch (System.Exception e)
                {
                    Log.Warning("[relation-seeder] RecalculateAll 延後失敗，改由遊戲自然重算：" + e.Message);
                }
            }
            Log.Message("[relation-seeder] 播種完成：套用 " + applied + " 對，略過 " + skipped + " 對（缺席派系）。");
        }

        /// <summary>對「所有符合 a 的派系 × 所有符合 b 的派系」設目標善意。回傳實際套用對數。</summary>
        private static int ApplyEntry(RelationSeedEntry entry, ref int skipped)
        {
            List<Faction> aList = FactionsOfDef(entry.a);
            List<Faction> bList = FactionsOfDef(entry.b);
            if (aList.Count == 0 || bList.Count == 0)
            {
                skipped++;   // 目標派系未在此世界 → 軟略過（soft-optional）
                return 0;
            }
            int goodwill = Mathf.Clamp(entry.goodwill, -100, 100);
            int count = 0;
            foreach (Faction fa in aList)
            {
                foreach (Faction fb in bList)
                {
                    if (fa == fb || !fa.HasGoodwill || !fb.HasGoodwill)
                    {
                        continue;   // 同派系、或永久敵/隱藏派系（無善意軸）→ 略過
                    }
                    FactionRelation rel = fa.RelationWith(fb, allowNull: true);
                    int current = rel != null ? rel.baseGoodwill : 0;
                    int delta = goodwill - current;
                    if (delta != 0)
                    {
                        // vanilla 對稱套用雙向並重算 kind；靜音，不發訊息/敵對信。
                        fa.TryAffectGoodwillWith(fb, delta,
                            canSendMessage: false, canSendHostilityLetter: false);
                    }
                    count++;
                }
            }
            return count;
        }

        private static List<Faction> FactionsOfDef(string defName)
        {
            List<Faction> result = new List<Faction>();
            if (defName.NullOrEmpty())
            {
                return result;
            }
            foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
            {
                if (f.def != null && f.def.defName == defName)
                {
                    result.Add(f);
                }
            }
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref seeded, "seeded", defaultValue: false);
            Scribe_Values.Look(ref pendingSeed, "pendingSeed", defaultValue: false);
        }
    }
}
