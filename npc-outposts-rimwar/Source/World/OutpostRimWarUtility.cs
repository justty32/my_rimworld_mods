using System.Collections.Generic;
using pas.outposts;
using RimWar;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.outposts.rimwar
{
    /// <summary>共用小工具：類型係數、上限鏡像、最近聚落、去重警告。</summary>
    public static class OutpostRimWarUtility
    {
        private static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>同 key 只 Warning 一次（防衛式降級不洗版）。</summary>
        public static void WarnOnce(string key, string message)
        {
            if (warned.Add(key))
            {
                Log.Warning("[NpcOutposts-RimWar] " + message);
            }
        }

        public static float PointsFactor(OutpostTypeDef typeDef)
        {
            return typeDef?.GetModExtension<RimWarOutpostExtension>()?.pointsFactor ?? 1f;
        }

        public static int InitialPointsFor(OutpostTypeDef typeDef)
        {
            return Mathf.Max(100, Mathf.RoundToInt(OutpostsRimWarMod.Settings.initialOutpostPoints * PointsFactor(typeDef)));
        }

        /// <summary>聚落成長上限，鏡像 IncrementSettlementGrowth（RW:17597-17612）：
        /// 基礎 50000、City_Citadel +5000、首都 +5000（Vassal +1000）。</summary>
        public static int GrowthCapFor(RimWarSettlementComp comp, RimWarData rwd)
        {
            int cap = 50000;
            if (comp.parent?.def?.defName == "City_Citadel")
            {
                cap += 5000;
            }
            if (comp.isCapitol)
            {
                cap += (rwd != null && rwd.behavior == RimWarBehavior.Vassal) ? 1000 : 5000;
            }
            return cap;
        }

        /// <summary>派系最近的非哨站聚落（哨站易主後重掛母聚落用）。找不到回 null。</summary>
        public static Settlement ClosestSettlementOf(Faction faction, PlanetTile tile)
        {
            Settlement best = null;
            float bestDist = float.MaxValue;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement s = settlements[i];
                if (s is NpcOutpost || s.Faction != faction || s.Destroyed)
                {
                    continue;
                }
                float dist = Find.WorldGrid.ApproxDistanceInTiles(tile, s.Tile);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = s;
                }
            }
            return best;
        }
    }
}
