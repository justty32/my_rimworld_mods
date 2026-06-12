using System.Collections.Generic;
using pas.outposts;
using RimWorld.Planet;
using Verse;

namespace pas.empire.outposts.war
{
    /// <summary>共用小工具：去重警告、母聚落數哨站、最近聚落、PColony 判定。</summary>
    public static class OutpostWarUtility
    {
        private static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>同 key 只 Warning 一次（防衛式降級不洗版）。</summary>
        public static void WarnOnce(string key, string message)
        {
            if (warned.Add(key))
            {
                Log.Warning("[EmpireOutpostsWar] " + message);
            }
        }

        public static void Message(string message)
        {
            Log.Message("[EmpireOutpostsWar] " + message);
        }

        /// <summary>數某母聚落當前存活、同派系的衛星哨站。</summary>
        public static int CountSatellites(Settlement parent)
        {
            if (parent == null)
            {
                return 0;
            }
            int n = 0;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is NpcOutpost outpost && !outpost.Destroyed
                    && outpost.ParentSettlement == parent && outpost.Faction == parent.Faction)
                {
                    n++;
                }
            }
            return n;
        }

        /// <summary>spawner 私有 caps 字典搬鍵（舊母→新母）。反射 fail-soft：失敗只警告。</summary>
        public static void MigrateSpawnerCap(Settlement oldHome, Settlement newHome)
        {
            if (oldHome == null)
            {
                return;
            }
            WorldComponent_OutpostSpawner spawner = Find.World?.GetComponent<WorldComponent_OutpostSpawner>();
            if (spawner == null)
            {
                return;
            }
            Dictionary<Settlement, int> caps =
                HarmonyLib.AccessTools.Field(typeof(WorldComponent_OutpostSpawner), "caps")?.GetValue(spawner)
                    as Dictionary<Settlement, int>;
            if (caps == null)
            {
                WarnOnce("capsField", "讀不到 WorldComponent_OutpostSpawner.caps，cap 搬鍵降級停用。");
                return;
            }
            if (caps.TryGetValue(oldHome, out int cap))
            {
                caps.Remove(oldHome);
                if (newHome != null && !caps.ContainsKey(newHome))
                {
                    caps[newHome] = cap;
                }
            }
        }
    }
}
