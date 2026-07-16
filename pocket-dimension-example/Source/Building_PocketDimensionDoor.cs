using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PocketDimensionExample
{
    /// <summary>
    /// 異空間門：MapPortal 的最小子類。
    /// 原版 MapPortal 已提供：lazy 生成 pocket map（GetOtherMap →
    /// PocketMapUtility.GeneratePocketMap，RimWorld/MapPortal.cs:247-254/324-334）、
    /// 進入 gizmo（Dialog_EnterPortal）、右鍵進入、裝載搬運（leftToLoad + HaulToPortal）、
    /// 存檔（pocketMap/exit 的 Scribe_References）。
    ///
    /// 本類唯一補充：原版 MapPortal 被摧毀時不會回收 pocket map（會留下
    /// 一張再也進不去的地圖、困死裡面的 pawn）。這裡在 Destroy 時：
    /// 1. 把異空間內所有 pawn 與地面物品 SkipTo 傳送回門口（借 Anomaly 迷宮
    ///    收圖同款 API，RimWorld/LabyrinthMapComponent.cs:70/82）；
    /// 2. 玩家正看著異空間時把鏡頭跳回宿主地圖；
    /// 3. PocketMapUtility.DestroyPocketMap 正式回收。
    /// </summary>
    public class Building_PocketDimensionDoor : MapPortal
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map pocket = PocketMap;
            Map hostMap = Map;
            IntVec3 doorPos = Position;
            base.Destroy(mode);
            if (pocket == null || hostMap == null)
            {
                return;
            }

            // 疏散 pawn（含動物、俘虜；AllPawns 涵蓋未 spawn 的載員）。
            List<Pawn> pawns = new List<Pawn>(pocket.mapPawns.AllPawns);
            foreach (Pawn pawn in pawns)
            {
                IntVec3 dest = CellFinder.StandableCellNear(doorPos, hostMap, 8f);
                if (!dest.IsValid)
                {
                    dest = DropCellFinder.TradeDropSpot(hostMap);
                }
                if (SkipUtility.SkipTo(pawn, dest, hostMap) is Pawn skipped &&
                    PawnUtility.ShouldSendNotificationAbout(skipped))
                {
                    Messages.Message("MessagePawnReappeared".Translate(skipped.Named("PAWN")),
                        skipped, MessageTypeDefOf.NeutralEvent, historical: false);
                }
            }

            // 疏散地面物品（容器內的東西會跟著容器一起走）。
            List<Thing> things = new List<Thing>();
            foreach (Thing thing in pocket.listerThings.AllThings)
            {
                if (thing.def.category == ThingCategory.Item && thing.Spawned)
                {
                    things.Add(thing);
                }
            }
            foreach (Thing thing in things)
            {
                IntVec3 dest = CellFinder.StandableCellNear(doorPos, hostMap, 8f);
                if (dest.IsValid)
                {
                    SkipUtility.SkipTo(thing, dest, hostMap);
                }
            }

            if (Find.CurrentMap == pocket)
            {
                CameraJumper.TryJump(new GlobalTargetInfo(doorPos, hostMap));
            }
            PocketMapUtility.DestroyPocketMap(pocket);
            Messages.Message("PDE_MessageDimensionCollapsed".Translate(),
                new TargetInfo(doorPos, hostMap), MessageTypeDefOf.NeutralEvent);
        }
    }
}
