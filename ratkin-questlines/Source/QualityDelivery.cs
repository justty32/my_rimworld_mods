using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // 品質門檻交貨（quality-gated delivery）。
    // ------------------------------------------------------------------
    // 為什麼要自製：CQF 的 requiredThings（CQFThingDefCount）只認 ThingDef＋stuff＋數量，**不認 quality**。
    // 武器商採購令要「特定品質以上的軍械」，所以自製一對：
    //   ● DialogCondition_ThingQuality —— 閂交貨選項（hideWhenDisabled）：地圖上要有 >=count 件、品質 >= minQuality 的指定物。
    //   ● CQFAction_ConsumeQualityThings —— 交貨時「依品質」扣料（優先扣達標的，不會誤扣爛貨）。
    // 兩者共用 Util 掃玩家地圖 listerThings。取代原本的 requiredThings，達成真品質門檻＋品質正確扣料。
    public static class QualityDeliveryUtil
    {
        public static Map MapFrom(Dictionary<string, TargetInfo> targets)
        {
            if (targets != null)
            {
                foreach (KeyValuePair<string, TargetInfo> kv in targets)
                {
                    Map m = kv.Value.Map;
                    if (m != null)
                    {
                        return m;
                    }
                }
            }
            return Find.CurrentMap;
        }

        public static List<Thing> Matches(Map map, ThingDef def, QualityCategory min)
        {
            List<Thing> result = new List<Thing>();
            if (map == null || def == null)
            {
                return result;
            }
            List<Thing> pool = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < pool.Count; i++)
            {
                Thing t = pool[i];
                if (t == null || t.MapHeld == null)
                {
                    continue;
                }
                CompQuality cq = t.TryGetComp<CompQuality>();
                if (cq != null && (int)cq.Quality >= (int)min)
                {
                    result.Add(t);
                }
            }
            return result;
        }

        public static int CountMatching(Map map, ThingDef def, QualityCategory min)
        {
            int n = 0;
            List<Thing> ms = Matches(map, def, min);
            for (int i = 0; i < ms.Count; i++)
            {
                n += ms[i].stackCount;
            }
            return n;
        }
    }

    // 閂交貨選項：地圖上要有足量、達品質的指定物才顯示（hideWhenDisabled）。
    public class DialogCondition_ThingQuality : DialogCondition
    {
        public ThingDef thing;
        public int count = 1;
        public QualityCategory minQuality = QualityCategory.Good;

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            Map map = QualityDeliveryUtil.MapFrom(targets);
            int have = QualityDeliveryUtil.CountMatching(map, thing, minQuality);
            if (have >= count)
            {
                reason = null;
                return true;
            }
            reason = Translator.Translate(failReason);
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref thing, "thing");
            Scribe_Values.Look(ref count, "count", 1);
            Scribe_Values.Look(ref minQuality, "minQuality", QualityCategory.Good);
        }
    }

    // 交貨時依品質扣料：從目標地圖移除 count 件、品質 >= minQuality 的指定物。
    public class CQFAction_ConsumeQualityThings : CQFAction_Target
    {
        public ThingDef thing;
        public int count = 1;
        public QualityCategory minQuality = QualityCategory.Good;

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Map map = QualityDeliveryUtil.MapFrom(targets);
            if (map == null)
            {
                return;
            }
            int need = count;
            List<Thing> ms = QualityDeliveryUtil.Matches(map, thing, minQuality);
            for (int i = 0; i < ms.Count && need > 0; i++)
            {
                Thing t = ms[i];
                int take = Math.Min(need, t.stackCount);
                t.SplitOff(take).Destroy(DestroyMode.Vanish);
                need -= take;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref thing, "thing");
            Scribe_Values.Look(ref count, "count", 1);
            Scribe_Values.Look(ref minQuality, "minQuality", QualityCategory.Good);
        }
    }
}
