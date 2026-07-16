using RimWorld;
using Verse;

namespace PocketDimensionExample
{
    /// <summary>
    /// 異空間房間生成：整圖鋪地形、外圈圍實體岩牆、中央生成出口並設定玩家起始視角。
    /// 出口 thingClass 為原版 PocketMapExit —— 在 SpawnSetup 期間讀取
    /// PocketMapUtility.currentlyGeneratingPortal（RimWorld/PocketMapExit.cs:27-34）
    /// 自動與入口 MapPortal 互相綁定，本類不需要任何綁定代碼。
    /// 所有欄位由 GenStepDef XML 餵值（PDE_MapGenerator.xml）。
    /// </summary>
    public class GenStep_PocketRoom : GenStep
    {
        public ThingDef exitDef;
        public TerrainDef floorTerrain;
        public ThingDef wallRockDef;
        public int borderThickness = 4;

        public override int SeedPart => 841257912;

        public override void Generate(Map map, GenStepParams parms)
        {
            TerrainDef floor = floorTerrain ?? TerrainDefOf.MetalTile;
            ThingDef rock = wallRockDef ?? ThingDefOf.Granite;
            CellRect interior = CellRect.WholeMap(map).ContractedBy(borderThickness);

            foreach (IntVec3 cell in map.AllCells)
            {
                map.terrainGrid.SetTerrain(cell, floor);
                if (!interior.Contains(cell))
                {
                    GenSpawn.Spawn(rock, cell, map);
                }
            }

            // 出口放正中央；玩家首次切到這張圖時鏡頭落在出口上。
            IntVec3 center = map.Center;
            GenSpawn.Spawn(ThingMaker.MakeThing(exitDef), center, map);
            MapGenerator.PlayerStartSpot = center;
        }
    }
}
