using RimWorld;
using Verse;

namespace pas.outposts
{
    public class OutpostTypeDef : Def
    {
        public WorldObjectDef worldObjectDef;
        public IntVec3 mapSize = new IntVec3(150, 1, 150);
        public float defenderPointsFactor = 0.4f;
        public MapGeneratorDef mapGeneratorDef;   // null = 沿用 Settlement.MapGeneratorDef（Base_Faction）
    }
}
