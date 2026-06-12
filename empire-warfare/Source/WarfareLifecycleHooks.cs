using System;
using FactionColonies;

namespace pas.empire.warfare
{
    /// <summary>
    /// Empire 生命週期回呼（B-1 契約層，零 Harmony 對 Empire）。
    /// OnBattleResolved 是自動與手動防守戰的唯一匯流點（SettlementMilitary.EndBattle），
    /// job 固定為 DefendFriendlySettlement——攻方作戰（capture/raid/enslave）走別的 job，不會誤觸。
    /// </summary>
    public class WarfareLifecycleHooks : LifecycleParticipantBase
    {
        public override void OnBattleResolved(WorldSettlementFC settlement, MilitaryJobDef job, bool victory, BattleResult result)
        {
            try
            {
                if (settlement == null || job == null || job != MilitaryJobDefOf.DefendFriendlySettlement) return;
                WorldComponent_WarfareTracker tracker = WorldComponent_WarfareTracker.Current;
                if (tracker == null) return;

                if (victory) tracker.NotifyDefenseWon(settlement);
                else tracker.NotifyDefenseLost(settlement, result);
            }
            catch (Exception e)
            {
                LogUtil.Warning("[EmpireWarfare] OnBattleResolved hook failed: " + e);
            }
        }

        public override void OnSettlementCreated(WorldSettlementFC settlement)
        {
            try
            {
                if (settlement == null) return;
                WorldComponent_WarfareTracker.Current?.NotifySettlementCreated(settlement);
            }
            catch (Exception e)
            {
                LogUtil.Warning("[EmpireWarfare] OnSettlementCreated hook failed: " + e);
            }
        }

        public override void OnSettlementRemoved(WorldSettlementFC settlement)
        {
            try
            {
                if (settlement == null) return;
                WorldComponent_WarfareTracker.Current?.NotifySettlementRemoved(settlement);
            }
            catch (Exception e)
            {
                LogUtil.Warning("[EmpireWarfare] OnSettlementRemoved hook failed: " + e);
            }
        }
    }

    /// <summary>
    /// 接點預留（構想 Mod 2 選做項）：未來依戰況事件對附庸防守方加成。
    /// 目前刻意不改動戰力——只佔住 BattleModifierRegistry 的掛載位。
    /// </summary>
    public class VassalDefenseBattleModifier : IBattleModifier
    {
        public void ModifyForce(MilitaryForce force, bool isAttacker)
        {
            // 預留：附庸防守加成鉤（例如近期戰績、馳援距離）。暫無內容。
        }
    }
}
