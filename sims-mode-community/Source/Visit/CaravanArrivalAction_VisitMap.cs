using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.sims
{
    /// <summary>拜訪＝實際生成地圖進場（原版拜訪只停格不生圖）。size Invalid = 世界初始尺寸。
    /// 最小版：待參考 mod 校準 Arrived 細節（letter/善後），介面凍結供 npc-outposts 共用。</summary>
    public class CaravanArrivalAction_VisitMap : CaravanArrivalAction
    {
        private MapParent mapParent;
        private IntVec3 mapSize = IntVec3.Invalid;

        public override string Label => "pas_sims_EnterSettlement".Translate(mapParent?.Label ?? "");

        public override string ReportString => "CaravanVisiting".Translate(mapParent?.Label ?? "");

        public CaravanArrivalAction_VisitMap()
        {
        }

        public CaravanArrivalAction_VisitMap(MapParent mapParent, IntVec3? mapSize = null)
        {
            this.mapParent = mapParent;
            this.mapSize = mapSize ?? IntVec3.Invalid;
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
            if (!report)
            {
                return report;
            }
            if (mapParent != null && mapParent.Tile != destinationTile)
            {
                return false;
            }
            return CanVisit(caravan, mapParent);
        }

        public override void Arrived(Caravan caravan)
        {
            bool newMap = !mapParent.HasMap;
            IntVec3 size = mapSize.IsValid ? mapSize : Find.World.info.initialMapSize;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(mapParent.Tile, size, null);
            if (map == null)
            {
                return;
            }
            if (mapParent is Settlement settlement && settlement.Faction != null && !settlement.Faction.IsPlayer)
            {
                // 退化生成防護（E2E 實測）：圖上無該派系人形（第三方 genstep 干擾/特殊層）時撤銷進場。
                if (!AnyFactionHumanlike(map, settlement.Faction))
                {
                    if (newMap)
                    {
                        Current.Game.DeinitAndRemoveMap(map, notifyPlayer: false);
                    }
                    Messages.Message("pas_sims_VisitAborted".Translate(mapParent.Label),
                        caravan, MessageTypeDefOf.NegativeEvent, historical: false);
                    return;
                }
                // 反攻陷（E2E 實測根因，IL 核對）：IsDefeated 只認「對玩家構成主動威脅」的人形，
                // 友方/中立住民永不構成威脅 → 聚落掛著拜訪圖必被 CheckDefeated 判攻陷。
                // 生成（含 redress）仍在聚落名下完成後，把圖交給影子宿主，Settlement.Map 回 null → 早退。
                if (!settlement.Faction.HostileTo(Faction.OfPlayer) && map.Parent == settlement)
                {
                    VisitMapParent shadow = (VisitMapParent)WorldObjectMaker.MakeWorldObject(
                        DefDatabase<WorldObjectDef>.GetNamed("pas_sims_VisitMapParent"));
                    shadow.Tile = settlement.Tile;
                    shadow.SetFaction(settlement.Faction);
                    shadow.VisitedSettlement = settlement;
                    // 先把圖掛到影子宿主再加入世界：否則 Add 後、賦 parent 前的窗口內 shadow 是
                    // HasMap==false 的已註冊世界物件，若被 tick 到會被 TickInterval 自清。
                    map.info.parent = shadow;
                    Find.WorldObjects.Add(shadow);
                    // Settlement.PostMapGenerate 對任何非家園圖一律啟動「敵人即將到達」倒數——拜訪語境下取消。
                    settlement.GetComponent<TimedDetectionRaids>()?.ResetCountdown();
                }
            }
            if (newMap)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
            }
            if (caravan.IsPlayerControlled)
            {
                Find.LetterStack.ReceiveLetter(
                    "LetterLabelCaravanEnteredMap".Translate(mapParent),
                    "LetterCaravanEnteredMap".Translate(caravan.Label, mapParent).CapitalizeFirst(),
                    LetterDefOf.NeutralEvent, caravan.PawnsListForReading);
            }
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: false);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_Values.Look(ref mapSize, "mapSize", IntVec3.Invalid);
        }

        private static bool AnyFactionHumanlike(Map map, Faction faction)
        {
            List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].RaceProps.Humanlike)
                {
                    return true;
                }
            }
            return false;
        }

        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, MapParent mapParent)
        {
            if (mapParent == null || !mapParent.Spawned)
            {
                return false;
            }
            if (mapParent is Settlement settlement)
            {
                return settlement.Visitable;
            }
            return mapParent.Faction != null && mapParent.Faction != Faction.OfPlayer
                && !mapParent.Faction.HostileTo(Faction.OfPlayer);
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, MapParent mapParent, IntVec3? mapSize = null)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(
                () => CanVisit(caravan, mapParent),
                () => new CaravanArrivalAction_VisitMap(mapParent, mapSize),
                "pas_sims_EnterSettlement".Translate(mapParent.Label),
                caravan, mapParent.Tile, mapParent);
        }
    }
}
