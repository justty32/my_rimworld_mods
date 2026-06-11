using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.outposts
{
    /// <summary>哨站放置核心。public static 供 FinalizeInit / tick / 第三方共用。
    /// 途中放置範式：MakeWorldObject → Tile → SetFaction → Add。</summary>
    public static class OutpostPlacer
    {
        public static NpcOutpost TryPlaceFor(Settlement parent, OutpostProfileDef profile, OutpostTypeDef type = null)
        {
            if (parent == null || parent.Faction == null || profile == null || profile.types.NullOrEmpty())
            {
                return null;
            }
            if (type == null)
            {
                type = profile.types.RandomElementByWeight(e => e.weight).type;
            }
            if (type?.worldObjectDef == null)
            {
                return null;
            }
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(
                    parent.Tile, profile.radius.min, profile.radius.max, out PlanetTile tile,
                    t => TileFinder.IsValidTileForNewSettlement(t)))
            {
                return null;
            }
            NpcOutpost outpost = (NpcOutpost)WorldObjectMaker.MakeWorldObject(type.worldObjectDef);
            outpost.Tile = tile;
            outpost.SetFaction(parent.Faction);
            outpost.Setup(type, parent);
            outpost.Name = "pas_outposts_NameFormat".Translate(parent.Name);
            Find.WorldObjects.Add(outpost);
            return outpost;
        }
    }
}
