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

    /// <summary>由 PatchOperation 掛上原版 Settlement WorldObjectDef，給 float menu 注入「進入」。</summary>
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
    }
}
