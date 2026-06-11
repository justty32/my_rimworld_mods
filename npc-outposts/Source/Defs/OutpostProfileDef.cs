using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.outposts
{
    public class OutpostTypeEntry
    {
        public OutpostTypeDef type;
        public float weight = 1f;
    }

    public class OutpostProfileDef : Def
    {
        public List<FactionDef> factionDefs;
        public List<TechLevel> techLevels;
        public bool isDefault;
        public IntRange countPerSettlement = new IntRange(1, 3);
        public IntRange radius = new IntRange(2, 4);
        public float spawnMtbDays = 15f;
        public List<OutpostTypeEntry> types;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors()) yield return e;
            if (types.NullOrEmpty()) yield return "no types";
        }
    }

    /// <summary>掛在 FactionDef 上指定 profile（解析鏈最高優先）。</summary>
    public class OutpostProfileExtension : DefModExtension
    {
        public OutpostProfileDef profile;
    }

    /// <summary>掛在 FactionDef 上停用該派系的哨站。</summary>
    public class OutpostDisabledExtension : DefModExtension
    {
    }
}
