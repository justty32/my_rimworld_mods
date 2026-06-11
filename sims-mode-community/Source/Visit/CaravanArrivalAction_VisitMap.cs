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

        public override string Label => "pas_sims_EnterSettlement".Translate(mapParent.Label);

        public override string ReportString => "CaravanVisiting".Translate(mapParent.Label);

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
