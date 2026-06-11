using System.Linq;
using RimWorld;
using Verse;

namespace pas.outposts
{
    /// <summary>派系 → OutpostProfileDef。鏈：Disabled → null；Extension > factionDefs > techLevels > isDefault。</summary>
    public static class OutpostProfileResolver
    {
        public static OutpostProfileDef Resolve(Faction faction)
        {
            if (faction == null || faction.def.HasModExtension<OutpostDisabledExtension>())
            {
                return null;
            }
            return ByExtension(faction) ?? ByFactionDef(faction) ?? ByTechLevel(faction) ?? Default();
        }

        public static OutpostProfileDef ByExtension(Faction faction)
        {
            return faction.def.GetModExtension<OutpostProfileExtension>()?.profile;
        }

        public static OutpostProfileDef ByFactionDef(Faction faction)
        {
            return DefDatabase<OutpostProfileDef>.AllDefsListForReading
                .FirstOrDefault(p => p.factionDefs != null && p.factionDefs.Contains(faction.def));
        }

        public static OutpostProfileDef ByTechLevel(Faction faction)
        {
            return DefDatabase<OutpostProfileDef>.AllDefsListForReading
                .FirstOrDefault(p => p.techLevels != null && p.techLevels.Contains(faction.def.techLevel));
        }

        public static OutpostProfileDef Default()
        {
            return DefDatabase<OutpostProfileDef>.AllDefsListForReading.FirstOrDefault(p => p.isDefault);
        }
    }
}
