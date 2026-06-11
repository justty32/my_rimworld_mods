using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.sims
{
    /// <summary>派系 → 生活檔案解析鏈。小顆 public static 方法，供 Harmony patch 個別環節。</summary>
    public static class ProfileResolver
    {
        public static LifeProfileDef Resolve(Faction faction)
        {
            if (faction?.def == null)
            {
                return null;
            }
            return ByExtension(faction)
                ?? ByFactionDef(faction)
                ?? ByTechLevel(faction)
                ?? Default();
        }

        public static LifeProfileDef ByExtension(Faction faction)
        {
            return faction.def.GetModExtension<LifeProfileExtension>()?.profile;
        }

        public static LifeProfileDef ByFactionDef(Faction faction)
        {
            List<LifeProfileDef> all = DefDatabase<LifeProfileDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].factionDefs != null && all[i].factionDefs.Contains(faction.def))
                {
                    return all[i];
                }
            }
            return null;
        }

        public static LifeProfileDef ByTechLevel(Faction faction)
        {
            List<LifeProfileDef> all = DefDatabase<LifeProfileDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].techLevels != null && all[i].techLevels.Contains(faction.def.techLevel))
                {
                    return all[i];
                }
            }
            return null;
        }

        public static LifeProfileDef Default()
        {
            List<LifeProfileDef> all = DefDatabase<LifeProfileDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].isDefault)
                {
                    return all[i];
                }
            }
            return null;
        }
    }

    /// <summary>派系 mod 作者在自家 FactionDef 上掛這個 extension 直接指定 profile（解析鏈第一優先）。</summary>
    public class LifeProfileExtension : DefModExtension
    {
        public LifeProfileDef profile;
    }
}
