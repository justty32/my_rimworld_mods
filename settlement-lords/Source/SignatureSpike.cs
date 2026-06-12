using System;
using RimWar;
using RimWar.Planet;
using RimWorld;
using Verse;

namespace pas.officers.settlements
{
    /// <summary>T0 簽章 spike 殘留物：在「編譯期」釘住 RimWar 目標簽章。
    /// RimWar 改版斷簽章 → build 直接紅（先於實機）；執行期另有 HarmonyInit TryPatch 降級雙保險。
    /// 成員 internal（非 private）避 CS0414/IDE 破零警告；永不被呼叫、無執行期成本。</summary>
    internal static class SignatureSpike
    {
        /// <summary>RW:17567 — 成長 postfix 目標（public 實例方法、無參數）。</summary>
        internal static void PinIncrementSettlementGrowth(WorldComponent_PowerTracker tracker)
            => tracker.IncrementSettlementGrowth();

        /// <summary>RW:15146 — 派系 rwd 查詢（過濾 Player/Excluded 用）。</summary>
        internal static readonly Func<Faction, RimWarData> GetRimWarDataForFactionPin =
            WorldUtility.GetRimWarDataForFaction;

        /// <summary>RW:9216/9228/9080 — comp 成員：PointDamage get、RimWarPoints get/set、
        /// isCapitol、parent（上限鏡像與點數加扣所需）。</summary>
        internal static int PinSettlementComp(RimWarSettlementComp comp)
        {
            comp.RimWarPoints = comp.RimWarPoints;
            return comp.PointDamage + (comp.isCapitol ? 1 : 0) + (comp.parent != null ? 1 : 0);
        }

        /// <summary>RimWarData.behavior 公開欄位（行為過濾用）。</summary>
        internal static bool PinBehavior(RimWarData rwd)
            => rwd.behavior == RimWarBehavior.Player || rwd.behavior == RimWarBehavior.Excluded;
    }
}
