using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace pas.sims
{
    public class JobDriver_FakeWork : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;    // 刻意不預約：NPC 觀賞用行為，允許共用設施
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil work = Toils_General.Wait(600, TargetIndex.A);
            work.socialMode = RandomSocialMode.Quiet;
            yield return work;
        }
    }
}
