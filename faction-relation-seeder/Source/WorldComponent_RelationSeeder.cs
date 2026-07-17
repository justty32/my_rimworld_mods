using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.relations
{
    /// <summary>開局一次性播種派系關係矩陣（讀所有 RelationSeedDef）。
    /// 零 Harmony：靠 WorldComponent.FinalizeInit 原生鉤子。只在「新世界」播一次
    /// （fromLoad=false），之後任其被 rimwar 等演化層改動、不每次載檔重刷；
    /// 舊檔中途裝也不打擾既有關係。WorldComponent 由引擎自動實例化，無需註冊 Def。</summary>
    public class WorldComponent_RelationSeeder : WorldComponent
    {
        private bool seeded;

        public WorldComponent_RelationSeeder(World world) : base(world)
        {
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            // 只在全新遊戲播種；已播過或載入舊檔一律跳過。
            if (seeded || fromLoad)
            {
                return;
            }
            Apply();
            seeded = true;
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
        }
    }
}
