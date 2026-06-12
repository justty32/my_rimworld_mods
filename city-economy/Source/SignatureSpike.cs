using System;
using RimWar;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.sanguo.cityeconomy
{
    /// <summary>T0 簽章 spike 殘留物：在「編譯期」釘住 RimWar/vanilla 目標簽章。
    /// RimWar 改版斷簽章 → build 直接紅（先於實機）；執行期另有 HarmonyInit TryPatch 降級雙保險。
    /// 注意：Settlement_TraderTracker.RegenerateStock（protected）與 stock（private）
    /// 無法編譯期釘，只能 runtime AccessTools 找＋fail-soft；P2 介面走反射橋、刻意不釘。
    /// 成員 internal（非 private）避 CS0414/CS0169 破零警告；永不被呼叫、無執行期成本。</summary>
    internal static class SignatureSpike
    {
        /// <summary>RW:11018 — 守城 prefix/postfix 目標（public static、(comp, WarObject)）。</summary>
        internal static readonly Action<RimWarSettlementComp, WarObject> PinResolveCombatSettlement =
            IncidentUtility.ResolveCombat_Settlement;

        /// <summary>RW:11108 — 劫掠 prefix/postfix 目標（public static、(comp, WarObject, float)）。</summary>
        internal static readonly Action<RimWarSettlementComp, WarObject, float> PinResolveBattleSettlement =
            IncidentUtility.ResolveBattle_Settlement;

        /// <summary>RW:9216/9228/9277/9131 — comp 成員：RimWarPoints get/set、PointDamage get/set、
        /// EffectivePoints get、AttackingUnits、parent（守城折算與 sack 指紋所需）。</summary>
        internal static int PinSettlementComp(RimWarSettlementComp comp)
        {
            comp.RimWarPoints = comp.RimWarPoints;
            comp.PointDamage = comp.PointDamage;
            return comp.EffectivePoints + comp.AttackingUnits.Count + (comp.parent != null ? 1 : 0);
        }

        /// <summary>RW:14403 — WarObject.EffectivePoints get（sack 指紋：攻方存活）。</summary>
        internal static int PinWarObject(WarObject unit) => unit.EffectivePoints;

        /// <summary>RW:15146 — 派系 rwd 查詢（過濾 Player/Excluded 用）。</summary>
        internal static readonly Func<Faction, RimWarData> PinGetRimWarDataForFaction =
            WorldUtility.GetRimWarDataForFaction;

        /// <summary>RimWarData.behavior 公開欄位（行為過濾用）。</summary>
        internal static bool PinBehavior(RimWarData rwd)
            => rwd.behavior == RimWarBehavior.Player || rwd.behavior == RimWarBehavior.Excluded;

        /// <summary>vanilla :164/:187/:9 — 交易回寫 postfix 目標（public virtual）＋ settlement 欄位。</summary>
        internal static void PinTraderTracker(Settlement_TraderTracker tracker, Thing thing, Pawn negotiator)
        {
            tracker.GiveSoldThingToTrader(thing, 1, negotiator);
            tracker.GiveSoldThingToPlayer(thing, 1, negotiator);
            _ = tracker.settlement;
        }
    }
}
