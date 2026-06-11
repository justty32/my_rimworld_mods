using System.Collections.Generic;
using pas.outposts;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.politics.outposts
{
    /// <summary>npc-outposts 啟用時：衛星哨站不被當倒戈對象/駐地/聚落數，且跟隨母聚落易主。</summary>
    [StaticConstructorOnStartup]
    public static class OutpostsBridge
    {
        static OutpostsBridge()
        {
            PoliticsBridges.IsSatelliteResolver = (Settlement s) => s is NpcOutpost;
            PoliticsBridges.SettlementDefected += OnSettlementDefected;
        }

        private static void OnSettlementDefected(Settlement defector, Faction mother, Faction newFaction)
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (all[i] is NpcOutpost outpost && outpost.ParentSettlement == defector
                    && outpost.Faction == mother)
                {
                    outpost.SetFaction(newFaction);
                }
            }
        }
    }
}
