using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace pas.sims
{
    public class LordJob_SettlementLife : LordJob
    {
        private const int DelayBeforeAssault = 25000;   // 原版 SymbolResolver_Settlement 給 LordJob_DefendBase 的值
        private const int CalmReturnTicks = 5000;       // 平靜約 2 小時 → 回歸生活

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

            // 觸發組照抄原版 LordJob_DefendBase 的 defend→assault：攻打聚落時行為貼齊原版
            Transition toAssault = new Transition(defend, assault);
            toAssault.AddTrigger(new Trigger_FractionPawnsLost(0.2f));
            toAssault.AddTrigger(new Trigger_PawnHarmed(0.4f));
            toAssault.AddTrigger(new Trigger_ChanceOnTickInterval(2500, 0.03f));
            toAssault.AddTrigger(new Trigger_TicksPassed(DelayBeforeAssault));
            toAssault.AddTrigger(new Trigger_UrgentlyHungry());
            toAssault.AddTrigger(new Trigger_ChanceOnPlayerHarmNPCBuilding(0.4f));
            toAssault.AddTrigger(new Trigger_OnClamor(ClamorDefOf.Ability));
            toAssault.AddPostAction(new TransitionAction_WakeAll());
            TaggedString message = faction.def.messageDefendersAttacking.Formatted(faction.def.pawnsPlural, faction.Name, Faction.OfPlayer.def.pawnsPlural).CapitalizeFirst();
            toAssault.AddPreAction(new TransitionAction_Message(message, MessageTypeDefOf.ThreatBig));
            graph.AddTransition(toAssault);

            Transition toLife = new Transition(defend, life);
            toLife.AddSource(assault);
            toLife.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            toLife.AddTrigger(new Trigger_CalmNonHostile(CalmReturnTicks));   // 非玩家敵對時的回歸退路（防卡死）
            graph.AddTransition(toLife);

            return graph;
        }

        /// <summary>pawn 死亡/離隊即清出字典，避免存檔殘留銷毀引用 → 讀檔 null key 紅字。</summary>
        public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
        {
            base.Notify_PawnLost(p, condition);
            roleAssignments.Remove(p);
        }

        public override void ExposeData()
        {
            base.ExposeData();
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
