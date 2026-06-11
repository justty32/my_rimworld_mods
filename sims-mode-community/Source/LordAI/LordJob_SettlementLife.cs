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
