# Task 7: 生活 Lord（LordJob + LordToil）

> 屬於 `../2026-06-11-implementation-plan.md`。

**Files:**
- Create: `sims-mode-community/Source/LordAI/LordJob_SettlementLife.cs`
- Create: `sims-mode-community/Source/LordAI/LordToil_SettlementLife.cs`

- [ ] **Step 1: LordJob_SettlementLife.cs**

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace pas.sims
{
    public class LordJob_SettlementLife : LordJob
    {
        private Faction faction;
        private IntVec3 baseCenter;
        private Dictionary<Pawn, LifeRoleDef> roleAssignments = new Dictionary<Pawn, LifeRoleDef>();
        private List<Pawn> tmpPawns;
        private List<LifeRoleDef> tmpRoles;

        public IntVec3 BaseCenter => baseCenter;

        public LordJob_SettlementLife()
        {
        }

        public LordJob_SettlementLife(Faction faction, IntVec3 baseCenter, Dictionary<Pawn, LifeRoleDef> roleAssignments)
        {
            this.faction = faction;
            this.baseCenter = baseCenter;
            this.roleAssignments = roleAssignments;
        }

        public LifeRoleDef RoleFor(Pawn pawn)
        {
            return roleAssignments.TryGetValue(pawn, out LifeRoleDef role) ? role : null;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            LordToil_SettlementLife life = new LordToil_SettlementLife();
            graph.StartingToil = life;
            LordToil_DefendBase defend = new LordToil_DefendBase(baseCenter);
            graph.AddToil(defend);
            LordToil_AssaultColony assault = new LordToil_AssaultColony(attackDownedIfStarving: true)
            {
                useAvoidGrid = true
            };
            graph.AddToil(assault);

            Transition toDefend = new Transition(life, defend);
            toDefend.AddTrigger(new Trigger_PawnHarmed());
            toDefend.AddTrigger(new Trigger_BecamePlayerEnemy());
            toDefend.AddPostAction(new TransitionAction_WakeAll());
            graph.AddTransition(toDefend);

            Transition toAssault = new Transition(defend, assault);
            toAssault.AddTrigger(new Trigger_FractionPawnsLost(0.2f));
            graph.AddTransition(toAssault);

            Transition toLife = new Transition(defend, life);
            toLife.AddSource(assault);
            toLife.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            graph.AddTransition(toLife);

            return graph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref baseCenter, "baseCenter");
            Scribe_Collections.Look(ref roleAssignments, "roleAssignments", LookMode.Reference, LookMode.Def, ref tmpPawns, ref tmpRoles);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && roleAssignments == null)
            {
                roleAssignments = new Dictionary<Pawn, LifeRoleDef>();
            }
        }
    }
}
```

- [ ] **Step 2: LordToil_SettlementLife.cs**

```csharp
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace pas.sims
{
    public class LordToil_SettlementLife : LordToil
    {
        private int lastUpdateHour = -1;

        private LordJob_SettlementLife LifeJob => (LordJob_SettlementLife)lord.LordJob;

        public override void UpdateAllDuties()
        {
            Map map = lord.Map;
            int hour = GenLocalDate.HourOfDay(map);
            lastUpdateHour = hour;
            MapComponent_FacilityRegistry registry = map.GetComponent<MapComponent_FacilityRegistry>();
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                LifeRoleDef role = LifeJob.RoleFor(pawn);
                ScheduleEntry entry = role?.EntryAt(hour);
                if (entry == null || entry.duty == null)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.DefendBase, LifeJob.BaseCenter);
                    continue;
                }
                pawn.mindState.duty = new PawnDuty(entry.duty, PickFocus(entry, i, registry));
            }
        }

        /// <summary>同設施多 pawn 時按 index 取模錯開（兩人不會擠同一張床/工作台）。public virtual 供覆寫。</summary>
        public virtual LocalTargetInfo PickFocus(ScheduleEntry entry, int pawnIndex, MapComponent_FacilityRegistry registry)
        {
            if (entry.focusFacility != null)
            {
                List<Thing> list = registry.Get(entry.focusFacility);
                if (list.Count > 0)
                {
                    return list[pawnIndex % list.Count];
                }
            }
            return LifeJob.BaseCenter;
        }

        public override void LordToilTick()
        {
            if (lord.ticksInToil % 250 == 0 && GenLocalDate.HourOfDay(lord.Map) != lastUpdateHour)
            {
                UpdateAllDuties();
            }
        }
    }
}
```

（若 Task 0 發現 `lord.Map` 不存在，`LordToil` 基類有 `Map` property 就直接用 `Map`。）

- [ ] **Step 3: 建置 + commit**

Run: `dotnet build sims-mode-community/Source/SimsModeCommunity.csproj -c Release` → Build succeeded。

```
git add sims-mode-community/Source sims-mode-community/1.6
git commit -m "feat: 生活 Lord 狀態機（作息查表發 duty + 原版防禦/反擊 toil 切換）"
```
