using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.politics
{
    /// <summary>派系如何反叛。解析鏈見 RebellionProfileResolver。</summary>
    public class RebellionProfileDef : Def
    {
        public List<FactionDef> factionDefs;
        public List<TechLevel> techLevels;
        public bool isDefault;
        /// <summary>每日反叛進展（每個反叛者生成時擲定一次，存進 record）。</summary>
        public FloatRange progressPerDay = new FloatRange(0.2f, 0.6f);
        public float threshold = 100f;
        /// <summary>倒戈聚落比例（母派系保底留 1 個）。</summary>
        public FloatRange defectFraction = new FloatRange(0.3f, 0.5f);
        /// <summary>反叛者死後重生冷卻（天）。</summary>
        public float respawnDelayDays = 20f;
        /// <summary>派系至少幾個聚落才養反叛者（<2 會讓分裂無聚落可分）。</summary>
        public int minSettlements = 2;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (threshold <= 0f)
            {
                yield return "threshold must be > 0";
            }
            if (minSettlements < 2)
            {
                yield return "minSettlements must be >= 2";
            }
            if (defectFraction.min <= 0f || defectFraction.max >= 1f)
            {
                yield return "defectFraction must be within (0,1)";
            }
        }
    }

    /// <summary>全域設定，恰好 1 個實例（健檢把關）。</summary>
    public class PoliticsSettingsDef : Def
    {
        public int maxDynamicFactions = 5;
        public int checkIntervalTicks = 2500;
    }

    /// <summary>FactionDef 直綁 profile（解析鏈最高優先）。</summary>
    public class PoliticsProfileExtension : DefModExtension
    {
        public RebellionProfileDef profile;
    }

    /// <summary>停用某派系的反叛系統。</summary>
    public class PoliticsDisabledExtension : DefModExtension
    {
    }
}
