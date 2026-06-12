using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ColonyArchivalOutpost
{
    // E1：共用建站 helper。ArchivalService.Archive 的「直建哨站」段與 VOE 建站選單的「建立」
    // 共用此處，避免兩處邏輯漂移。一律用穩定的 pas_archival_Outpost def，跳過 CostToMake。
    public static class OutpostFactory
    {
        // 建 outpost（掛指定陣營、餵 snapshot 複本、設名稱/圖標），但不搬 pawn、不加入世界。
        // 呼叫端負責 Find.WorldObjects.Add 與 AddPawn（搬 pawn 順序因情境而異）。
        public static Outpost_Sampled Create(PlanetTile tile, Faction faction,
            ProductivitySnapshot snapshot, string name = null, string iconPath = null)
        {
            var outpost = (Outpost_Sampled)WorldObjectMaker.MakeWorldObject(
                DefDatabase<WorldObjectDef>.GetNamed("pas_archival_Outpost"));
            outpost.Tile = tile;
            outpost.SetFaction(faction);
            outpost.SetSnapshot(snapshot ?? new ProductivitySnapshot());
            if (!name.NullOrEmpty()) outpost.Name = name;
            if (!iconPath.NullOrEmpty()) outpost.chosenIconPath = iconPath;
            return outpost;
        }

        // 完整建站：建 outpost、加入世界、把 pawns 逐一進駐。回傳建好的 outpost。
        public static Outpost_Sampled CreateWithPawns(PlanetTile tile, Faction faction,
            ProductivitySnapshot snapshot, string name, string iconPath, IEnumerable<Pawn> pawns)
        {
            var outpost = Create(tile, faction, snapshot, name, iconPath);
            Find.WorldObjects.Add(outpost);
            if (pawns != null)
                foreach (var pawn in new List<Pawn>(pawns))
                    outpost.AddPawn(pawn);
            return outpost;
        }
    }
}
