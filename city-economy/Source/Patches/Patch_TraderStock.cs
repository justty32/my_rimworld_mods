using HarmonyLib;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.sanguo.cityeconomy
{
    /// <summary>Postfix Settlement_TraderTracker.RegenerateStock（vanilla :265）。
    /// M 段定案：在 ThingSetMaker 外層注入（避開 RimWar 唯一相關 patch RW:6089）。
    /// 生成後按財富因子（clamp 0.25~2，播種值＝1.0 中性）縮放白銀/糧食/貨物 stack 數量。
    /// 一期只調數量、不增刪 Thing、不動價格/TraderKind（出界）。
    /// private 欄位 stock 走 FieldRefAccess（失敗 → 貨架縮放整組降級，不連坐回寫）。</summary>
    public static class Patch_TraderStock
    {
        private static AccessTools.FieldRef<Settlement_TraderTracker, ThingOwner<Thing>> stockRef;
        private static bool stockRefFailed;

        public static void Postfix(Settlement_TraderTracker __instance)
        {
            try
            {
                if (!(CityEconomyMod.Settings?.traderEconomyEnabled ?? false))
                {
                    return;
                }
                SettlementWealthComp comp =
                    __instance?.settlement?.GetComponent<SettlementWealthComp>();
                if (comp == null || !comp.initialized)
                {
                    return;   // 玩家城/非 RimWar 城自然跳過
                }
                int points = __instance.settlement.GetComponent<RimWarSettlementComp>()
                    ?.RimWarPoints ?? 0;
                if (points <= 0)
                {
                    return;
                }
                ThingOwner<Thing> stock = StockOf(__instance);
                if (stock == null)
                {
                    return;
                }
                float silverFactor = EconomyUtility.StockFactor(comp.silver, points);
                float foodFactor = EconomyUtility.StockFactor(comp.food, points * 0.5f);
                float goodsFactor = EconomyUtility.StockFactor(comp.goods, points * 0.5f);
                System.Collections.Generic.List<Thing> things = stock.InnerListForReading;
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing is Pawn)
                    {
                        continue;   // 奴隸/動物不縮放
                    }
                    float factor = thing.def == ThingDefOf.Silver ? silverFactor
                        : thing.def.IsNutritionGivingIngestible ? foodFactor : goodsFactor;
                    thing.stackCount = Mathf.Max(1, Mathf.RoundToInt(thing.stackCount * factor));
                }
            }
            catch (System.Exception e)
            {
                EconomyUtility.WarnOnce("traderStock", "貨架縮放 postfix 異常，本次保留原版 stock：" + e);
            }
        }

        private static ThingOwner<Thing> StockOf(Settlement_TraderTracker tracker)
        {
            if (stockRefFailed)
            {
                return null;
            }
            if (stockRef == null)
            {
                try
                {
                    stockRef = AccessTools.FieldRefAccess<Settlement_TraderTracker,
                        ThingOwner<Thing>>("stock");
                }
                catch (System.Exception e)
                {
                    stockRefFailed = true;
                    EconomyUtility.WarnOnce("stockField",
                        "找不到 Settlement_TraderTracker.stock 欄位（遊戲版本不符？），貨架縮放降級停用：" + e);
                    return null;
                }
            }
            return stockRef(tracker);
        }
    }
}
