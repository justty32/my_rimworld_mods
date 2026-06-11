using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.sims
{
    public class WorldObjectCompProperties_VisitMap : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_VisitMap()
        {
            compClass = typeof(WorldObjectComp_VisitMap);
        }
    }

    /// <summary>由 PatchOperation 掛上原版 Settlement WorldObjectDef：給 float menu 注入「進入」，
    /// 並在拜訪進行中（圖在影子宿主名下）於聚落圖示轉發「重組遠行隊」gizmo。</summary>
    public class WorldObjectComp_VisitMap : WorldObjectComp
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            if (parent is MapParent mapParent)
            {
                foreach (FloatMenuOption option in CaravanArrivalAction_VisitMap.GetFloatMenuOptions(caravan, mapParent))
                {
                    yield return option;
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (parent is MapParent mapParent)
            {
                foreach (Gizmo gizmo in VisitMapParent.ReformGizmosFor(mapParent))
                {
                    yield return gizmo;
                }
            }
        }
    }
}
