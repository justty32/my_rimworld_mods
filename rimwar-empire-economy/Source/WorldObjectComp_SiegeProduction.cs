using FactionColonies;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.empire.wartimeeconomy
{
    public class WorldObjectCompProperties_SiegeProduction : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SiegeProduction()
        {
            compClass = typeof(WorldObjectComp_SiegeProduction);
        }
    }

    /// <summary>
    /// 圍困減產（消費端 ResourceFC.CalculateProductionMult 的 IResourceProductionModifier 掃描，
    /// Empire 端零 Harmony）。掛在 WorldSettlementFC 上，Empire 重算各資源產量時逐 comp 乘上倍率。
    /// 當聚落正被 RimWar 部隊圍困時回傳 < 1 的倍率，否則回傳 1（無效果）。
    /// </summary>
    public class WorldObjectComp_SiegeProduction : WorldObjectComp, IResourceProductionModifier
    {
        private bool ActiveFor(out double mult, out WorldSettlementFC settlement)
        {
            mult = 1.0;
            settlement = parent as WorldSettlementFC;
            WartimeEconomySettings s = WartimeEconomyMod.Settings;
            if (settlement == null || !s.enableSiegeProductionCut) return false;
            if (!RimWarSignals.IsBesieged(settlement)) return false;
            mult = s.siegeProductionMultiplier;
            return true;
        }

        public double GetResourceAdditiveModifier(ResourceFC resource) => 0;

        public double GetResourceMultiplierModifier(ResourceFC resource)
        {
            return ActiveFor(out double mult, out _) ? mult : 1.0;
        }

        public string GetResourceAdditiveDesc(ResourceFC resource) => null;

        public string GetResourceMultiplierDesc(ResourceFC resource)
        {
            return ActiveFor(out double mult, out _)
                ? "pas_wte_SiegeProductionDesc".Translate(mult.ToString("0.##")).ToString()
                : null;
        }
    }
}
