using System;
using RimWar;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.officers.warband
{
    /// <summary>T0 簽章 spike 殘留物：以 method-group 轉換在「編譯期」釘住 RimWar 目標簽章。
    /// RimWar 改版斷簽章 → build 直接紅（先於實機）；執行期另有 HarmonyInit TryPatch 降級雙保險。
    /// 欄位 internal（非 private）避 CS0414 破零警告；永不被讀取、無執行期成本。</summary>
    internal static class SignatureSpike
    {
        /// <summary>RW:15467 — 生成鉤子目標。</summary>
        internal static readonly Func<int, RimWarData, Settlement, PlanetTile, WorldObject,
            WorldObjectDef, bool, bool, int, Warband> CreateWarbandPin = WorldUtility.CreateWarband;

        /// <summary>RW:11271 — 戰力注入目標。</summary>
        internal static readonly Action<WarObject, WarObject> ResolveCombatUnitsPin =
            IncidentUtility.ResolveCombat_Units;

        /// <summary>RW:15358 — 戰後傳承 context 目標。</summary>
        internal static readonly Action<WarObject, int, RimWarData, Settlement, PlanetTile,
            WorldObject, WorldObjectDef, PlanetTile, bool, bool, int> CreateWarObjectOfTypePin =
            WorldUtility.CreateWarObjectOfType;
    }
}
