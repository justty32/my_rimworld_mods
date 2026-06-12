using System;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.outposts.rimwar
{
    /// <summary>功能 4 訊號源：IncidentUtility.ResolveBattle_Settlement（RW:11086，聚落戰唯一勝負點）。
    /// prefix 快照守方 parent＋派系；postfix 以「parent 被毀或派系變了」判攻方勝
    /// （涵蓋 captured/夷平的 Destroy 分支與本 mod 的哨站易主），否則守方勝。</summary>
    public static class Patch_ResolveBattleSettlement
    {
        public class BattleSnapshot
        {
            public WorldObject defenderParent;
            public Faction defenderFaction;
        }

        public static void Prefix(RimWarSettlementComp defender, out BattleSnapshot __state)
        {
            __state = null;
            try
            {
                if (defender?.parent != null)
                {
                    __state = new BattleSnapshot
                    {
                        defenderParent = defender.parent,
                        defenderFaction = defender.parent.Faction,
                    };
                }
            }
            catch (Exception e)
            {
                OutpostRimWarUtility.WarnOnce("battlePrefix", "聚落戰快照 prefix 異常：" + e);
            }
        }

        public static void Postfix(WarObject attacker, BattleSnapshot __state)
        {
            try
            {
                if (__state?.defenderParent == null || attacker == null)
                {
                    return;
                }
                Faction attackerFaction = attacker.Faction;
                Faction defenderFaction = __state.defenderFaction;
                if (attackerFaction == null || defenderFaction == null || attackerFaction == defenderFaction)
                {
                    return;
                }
                bool attackerWon = __state.defenderParent.Destroyed
                    || __state.defenderParent.Faction != defenderFaction;
                Find.World?.GetComponent<WorldComponent_OutpostWarMomentum>()?.RecordBattle(
                    attackerWon ? attackerFaction : defenderFaction,
                    attackerWon ? defenderFaction : attackerFaction);
            }
            catch (Exception e)
            {
                OutpostRimWarUtility.WarnOnce("battlePostfix", "聚落戰戰績 postfix 異常，本筆不入帳：" + e);
            }
        }
    }
}
