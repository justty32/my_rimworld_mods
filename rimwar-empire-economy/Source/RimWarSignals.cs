using System;
using FactionColonies;
using RimWar;
using RimWar.Planet;
using RimWorld;
using Verse;

namespace pas.empire.wartimeeconomy
{
    /// <summary>
    /// 純讀 RimWar 公開狀態的訊號層。所有對 RimWar 的存取集中於此並 fail-soft，
    /// 讓消費端（comp / tax participant）保持乾淨、且 RimWar 改版時只崩在這一層。
    /// 不寫入 RimWar 任何狀態。
    /// </summary>
    public static class RimWarSignals
    {
        /// <summary>
        /// 該 Empire 聚落是否正被 RimWar 部隊圍困。
        /// 讀 settlement 自身的 RimWarSettlementComp.AttackingUnits（Empire 的 Patch-RW 已把此 comp
        /// 經 XML 掛上 WorldSettlementDefBase，故所有 WorldSettlementFC 皆可 GetComponent 取得）。
        /// </summary>
        public static bool IsBesieged(WorldSettlementFC settlement)
        {
            if (settlement == null) return false;
            try
            {
                RimWarSettlementComp comp = settlement.GetComponent<RimWarSettlementComp>();
                // UnderAttack 等價於 AttackingUnits.Count > 0（RW:9153），是最乾淨的圍困訊號。
                return comp != null && comp.UnderAttack;
            }
            catch (Exception e)
            {
                LogUtil.Warning("[WartimeEconomy] IsBesieged read failed (RimWar drift?): " + e);
                return false;
            }
        }

        /// <summary>
        /// 該聚落所屬派系（玩家附庸＝PColony）在 RimWar 世界中是否處於戰爭狀態。
        /// 讀 RimWarData.IsAtWar（即 WarFactions.Count > 0，RW:1506）。
        /// </summary>
        public static bool FactionAtWar(WorldSettlementFC settlement)
        {
            if (settlement == null) return false;
            Faction faction = settlement.Faction;
            if (faction == null) return false;
            try
            {
                RimWarData rwd = WorldUtility.GetRimWarDataForFaction(faction);
                return rwd != null && rwd.IsAtWar;
            }
            catch (Exception e)
            {
                LogUtil.Warning("[WartimeEconomy] FactionAtWar read failed (RimWar drift?): " + e);
                return false;
            }
        }
    }
}
