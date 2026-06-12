using System;
using System.Collections.Generic;
using pas.outposts;
using RimWar;
using RimWar.Planet;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.outposts.rimwar
{
    /// <summary>功能 1：Postfix WorldComponent_PowerTracker.IncrementSettlementGrowth（RW:17567）。
    /// 哨站按數量/類型給母聚落加 RimWarPoints。原方法的上限閘（RW:17597）關不到 postfix，
    /// 故鏡像同款上限自行 clamp；同樣鏡像 PointDamage>0（療傷中）與 Player/Excluded 的跳過。</summary>
    public static class Patch_IncrementSettlementGrowth
    {
        public static void Postfix()
        {
            try
            {
                float perOutpost = OutpostsRimWarMod.Settings?.pointsPerOutpost ?? 0f;
                if (perOutpost <= 0f)
                {
                    return;
                }
                Dictionary<Settlement, float> gains = null;
                List<Settlement> settlements = Find.WorldObjects.Settlements;
                for (int i = 0; i < settlements.Count; i++)
                {
                    if (!(settlements[i] is NpcOutpost outpost) || outpost.Destroyed)
                    {
                        continue;
                    }
                    Settlement parent = outpost.ParentSettlement;
                    if (parent == null || parent.Destroyed || parent.Faction == null
                        || outpost.Faction != parent.Faction)
                    {
                        continue;
                    }
                    gains = gains ?? new Dictionary<Settlement, float>();
                    gains.TryGetValue(parent, out float sum);
                    gains[parent] = sum + perOutpost * OutpostRimWarUtility.PointsFactor(outpost.TypeDef);
                }
                if (gains == null)
                {
                    return;
                }
                foreach (KeyValuePair<Settlement, float> kv in gains)
                {
                    RimWarSettlementComp comp = kv.Key.GetComponent<RimWarSettlementComp>();
                    if (comp == null || comp.PointDamage > 0)
                    {
                        continue;
                    }
                    RimWarData rwd = WorldUtility.GetRimWarDataForFaction(kv.Key.Faction);
                    if (rwd == null || rwd.behavior == RimWarBehavior.Player || rwd.behavior == RimWarBehavior.Excluded)
                    {
                        continue;
                    }
                    int cap = OutpostRimWarUtility.GrowthCapFor(comp, rwd);
                    if (comp.RimWarPoints < cap)
                    {
                        comp.RimWarPoints = Mathf.Min(comp.RimWarPoints + Mathf.RoundToInt(kv.Value), cap);
                    }
                }
            }
            catch (Exception e)
            {
                OutpostRimWarUtility.WarnOnce("growthPostfix", "哨站貢獻成長 postfix 異常，本輪跳過：" + e);
            }
        }
    }
}
