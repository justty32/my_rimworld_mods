using Verse;

namespace ColonyArchivalOutpost
{
    // 一張地圖只允許一個指定 def（含已建成、藍圖、施工框）。供電節點用，免去多電網去重。
    public class PlaceWorker_OnlyOnePerMap : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc,
            Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (!(checkingDef is ThingDef tDef)) return true;

            if (HasAny(map, tDef, thingToIgnore)
                || (tDef.blueprintDef != null && HasAny(map, tDef.blueprintDef, thingToIgnore))
                || (tDef.frameDef != null && HasAny(map, tDef.frameDef, thingToIgnore)))
            {
                return new AcceptanceReport("CAO.Power.OnlyOneNode".Translate());
            }
            return true;
        }

        private static bool HasAny(Map map, ThingDef def, Thing ignore)
        {
            foreach (var t in map.listerThings.ThingsOfDef(def))
                if (t != ignore) return true;
            return false;
        }
    }
}
