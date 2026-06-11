# Task 4: 行為原子（假工作 + 指定床睡覺）

> 屬於 `../2026-06-11-implementation-plan.md`。

**Files:**
- Create: `sims-mode-community/Source/Jobs/JobDriver_FakeWork.cs`
- Create: `sims-mode-community/Source/Jobs/JobGiver_FakeWork.cs`
- Create: `sims-mode-community/Source/Jobs/JobGiver_SleepAtDutyFocus.cs`
- Create: `sims-mode-community/Defs/JobDefs/Jobs.xml`

- [ ] **Step 1: JobDriver_FakeWork.cs（走到目標前、面向它、停留——不產出）**

```csharp
using System.Collections.Generic;
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
```

- [ ] **Step 2: JobGiver_FakeWork.cs**

```csharp
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
```

- [ ] **Step 3: JobGiver_SleepAtDutyFocus.cs（睡 duty focus 指定的床；不行就讓位給 SatisfyBasicNeeds 睡地上）**

```csharp
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
```

- [ ] **Step 4: Jobs.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <JobDef>
    <defName>pas_sims_FakeWork</defName>
    <driverClass>pas.sims.JobDriver_FakeWork</driverClass>
    <reportString>working.</reportString>
    <suspendable>false</suspendable>
  </JobDef>

</Defs>
```

- [ ] **Step 5: 建置 + commit**

Run: `dotnet build sims-mode-community/Source/SimsModeCommunity.csproj -c Release` → Build succeeded。

```
git add sims-mode-community/Source sims-mode-community/Defs sims-mode-community/1.6
git commit -m "feat: 假工作 JobDriver/JobGiver + 指定床睡覺 JobGiver"
```
