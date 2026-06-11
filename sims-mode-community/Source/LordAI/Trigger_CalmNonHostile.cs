using RimWorld;
using Verse.AI.Group;

namespace pas.sims
{
    /// <summary>來源 toil 持續 tickLimit ticks、近期無人受傷、且派系未與玩家敵對 → 觸發。
    /// 用途：防禦/反擊狀態的回歸生活退路——沒有它，被野獸等非玩家來源傷害一次就永久卡在防禦
    /// （Trigger_BecameNonHostileToPlayer 只在「曾敵對→解除」時觸發）。</summary>
    public class Trigger_CalmNonHostile : Trigger_TicksPassedAndNoRecentHarm
    {
        public Trigger_CalmNonHostile(int tickLimit) : base(tickLimit) { }

        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            return base.ActivateOn(lord, signal)
                && lord.faction != null
                && !lord.faction.HostileTo(Faction.OfPlayer);
        }
    }
}
