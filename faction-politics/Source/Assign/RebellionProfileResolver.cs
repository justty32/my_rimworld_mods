using RimWorld;
using Verse;

namespace pas.politics
{
    /// <summary>解析鏈：Disabled→null；Extension ?? FactionDef ?? TechLevel ?? Default。</summary>
    public static class RebellionProfileResolver
    {
        public static RebellionProfileDef Resolve(Faction faction)
        {
            if (faction?.def == null || Disabled(faction))
            {
                return null;
            }
            return ByExtension(faction) ?? ByFactionDef(faction) ?? ByTechLevel(faction) ?? Default();
        }

        public static bool Disabled(Faction faction)
        {
            return faction.def.HasModExtension<PoliticsDisabledExtension>();
        }

        public static RebellionProfileDef ByExtension(Faction faction)
        {
            return faction.def.GetModExtension<PoliticsProfileExtension>()?.profile;
        }

        public static RebellionProfileDef ByFactionDef(Faction faction)
        {
            foreach (RebellionProfileDef def in DefDatabase<RebellionProfileDef>.AllDefsListForReading)
            {
                if (def.factionDefs != null && def.factionDefs.Contains(faction.def))
                {
                    return def;
                }
            }
            return null;
        }

        public static RebellionProfileDef ByTechLevel(Faction faction)
        {
            foreach (RebellionProfileDef def in DefDatabase<RebellionProfileDef>.AllDefsListForReading)
            {
                if (def.techLevels != null && def.techLevels.Contains(faction.def.techLevel))
                {
                    return def;
                }
            }
            return null;
        }

        public static RebellionProfileDef Default()
        {
            foreach (RebellionProfileDef def in DefDatabase<RebellionProfileDef>.AllDefsListForReading)
            {
                if (def.isDefault)
                {
                    return def;
                }
            }
            return null;
        }
    }
}
