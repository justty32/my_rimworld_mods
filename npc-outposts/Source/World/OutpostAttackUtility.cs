using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.outposts
{
    /// <summary>攻打哨站＝小圖版 SettlementUtility.Attack（SettlementUtility.cs:29-59 範式，size 改用 OutpostTypeDef）。</summary>
    public static class OutpostAttackUtility
    {
        public static void Attack(Caravan caravan, NpcOutpost outpost)
        {
            if (!outpost.HasMap)
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    AttackNow(caravan, outpost);
                }, "GeneratingMapForNewEncounter", doAsynchronously: false, null);
            }
            else
            {
                AttackNow(caravan, outpost);
            }
        }

        private static void AttackNow(Caravan caravan, NpcOutpost outpost)
        {
            bool newMap = !outpost.HasMap;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(outpost.Tile, outpost.OutpostMapSize, null);
            TaggedString letterLabel = "LetterLabelCaravanEnteredEnemyBase".Translate();
            TaggedString letterText = "LetterCaravanEnteredEnemyBase".Translate(caravan.Label, outpost.Label.ApplyTag(TagType.Settlement, outpost.Faction.GetUniqueLoadID())).CapitalizeFirst();
            SettlementUtility.AffectRelationsOnAttacked(outpost, ref letterText);
            if (newMap)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(map.mapPawns.AllPawns, ref letterLabel, ref letterText, "LetterRelatedPawnsSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), informEvenIfSeenBefore: true);
            }
            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NeutralEvent, caravan.PawnsListForReading, outpost.Faction);
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
            Find.GoodwillSituationManager.RecalculateAll(canSendHostilityChangedLetter: true);
        }

        public static FloatMenuAcceptanceReport CanAttack(Caravan caravan, NpcOutpost outpost)
        {
            if (outpost == null || !outpost.Spawned || !outpost.Attackable)
            {
                return false;
            }
            if (outpost.EnterCooldownBlocksEntering())
            {
                return FloatMenuAcceptanceReport.WithFailMessage("MessageEnterCooldownBlocksEntering".Translate(outpost.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
            }
            return true;
        }

        /// <summary>中立/友好派系帶確認對話框，照原版 CaravanArrivalAction_AttackSettlement.cs:64-70。</summary>
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, NpcOutpost outpost)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(
                () => CanAttack(caravan, outpost),
                () => new CaravanArrivalAction_AttackOutpost(outpost),
                "AttackSettlement".Translate(outpost.Label),
                caravan, outpost.Tile, outpost,
                outpost.Faction.AllyOrNeutralTo(Faction.OfPlayer)
                    ? ((Action<Action>)delegate(Action action)
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "ConfirmAttackFriendlyFaction".Translate(outpost.LabelCap, outpost.Faction.Name),
                            delegate
                            {
                                action();
                            }));
                    })
                    : null);
        }
    }

    public class CaravanArrivalAction_AttackOutpost : CaravanArrivalAction
    {
        private NpcOutpost outpost;

        public override string Label => "AttackSettlement".Translate(outpost.Label);

        public override string ReportString => "CaravanAttacking".Translate(outpost.Label);

        public CaravanArrivalAction_AttackOutpost()
        {
        }

        public CaravanArrivalAction_AttackOutpost(NpcOutpost outpost)
        {
            this.outpost = outpost;
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
            if (!report)
            {
                return report;
            }
            if (outpost != null && outpost.Tile != destinationTile)
            {
                return false;
            }
            return OutpostAttackUtility.CanAttack(caravan, outpost);
        }

        public override void Arrived(Caravan caravan)
        {
            OutpostAttackUtility.Attack(caravan, outpost);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref outpost, "outpost");
        }
    }
}
