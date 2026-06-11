using RimWorld;
using Verse;
using Verse.AI;

namespace pas.sims
{
    public class JobGiver_FakeWork : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState.duty;
            if (duty == null || !duty.focus.HasThing)
            {
                return null;
            }
            Thing target = duty.focus.Thing;
            if (target.DestroyedOrNull() || !target.Spawned || target.Map != pawn.Map)
            {
                return null;
            }
            if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Some))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(SimsDefOf.pas_sims_FakeWork, target);
            job.expiryInterval = Rand.RangeInclusive(1200, 2400);
            return job;
        }
    }
}
