using RimWorld;
using Verse;
using Verse.AI;

namespace pas.sims
{
    public class JobGiver_SleepAtDutyFocus : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState.duty;
            if (duty == null || pawn.needs?.rest == null)
            {
                return null;
            }
            if (pawn.needs.rest.CurLevel > 0.98f)
            {
                return null;    // 睡飽了 → 落到後續節點（遊蕩）
            }
            if (duty.focus.Thing is Building_Bed bed && !bed.DestroyedOrNull() && bed.Spawned
                && RestUtility.IsValidBedFor(bed, pawn, pawn, checkSocialProperness: false))
            {
                return JobMaker.MakeJob(JobDefOf.LayDown, bed);
            }
            return null;        // 無有效床 → SatisfyBasicNeeds 子樹會處理（含睡地上）
        }
    }
}
