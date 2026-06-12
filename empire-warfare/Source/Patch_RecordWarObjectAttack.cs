using System;
using FactionColonies;
using RimWar.Planet;

namespace pas.empire.warfare
{
    /// <summary>
    /// RimWar 行軍部隊抵達聚落的唯一入口 postfix。
    /// 與 Empire 官方 Patch-RW 的 prefix（導流進 AttackPlayerSettlement 並 return false）共存：
    /// prefix skip 只略過原方法本體，postfix 照常執行——藉此記下「此附庸正被 RimWar 部隊攻擊」，
    /// 供一天後 OnBattleResolved 區分攻擊來源（RimWar vs Empire 原生襲擊事件）。
    /// </summary>
    public static class Patch_RecordWarObjectAttack
    {
        public static void Postfix(WarObject attacker, RimWarSettlementComp defender)
        {
            try
            {
                if (!(defender?.parent is WorldSettlementFC settlement)) return;
                if (attacker?.Faction == null) return;
                WorldComponent_WarfareTracker.Current?.RecordRimWarAttack(settlement.Tile, attacker.Faction);
            }
            catch (Exception e)
            {
                LogUtil.Warning("[EmpireWarfare] attack marker postfix failed: " + e.Message);
            }
        }
    }
}
