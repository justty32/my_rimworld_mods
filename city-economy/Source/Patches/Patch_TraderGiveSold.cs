using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.sanguo.cityeconomy
{
    /// <summary>Postfix Settlement_TraderTracker.GiveSoldThingToTrader（:164）／
    /// GiveSoldThingToPlayer（:187，皆 public virtual、__instance.settlement 直達）。
    /// 交易回寫（M 段）：玩家賣給城 → 銀/食/貨↑；玩家從城買走 → 銀/食/貨↓。
    /// 買賣對偶自洽：買貨付的銀經 GiveSoldThingToTrader（Silver）回流城庫。
    /// 只用 toGive.def＋countToGive（原方法已 SplitOff，不碰 Thing 實體）。</summary>
    public static class Patch_TraderGiveSold
    {
        /// <summary>玩家賣給城（含玩家買貨時付的白銀）。</summary>
        public static void TraderPostfix(Settlement_TraderTracker __instance,
            Thing toGive, int countToGive)
        {
            Writeback(__instance, toGive, countToGive, 1);
        }

        /// <summary>玩家從城買走（含城付給玩家的白銀）。</summary>
        public static void PlayerPostfix(Settlement_TraderTracker __instance,
            Thing toGive, int countToGive)
        {
            Writeback(__instance, toGive, countToGive, -1);
        }

        private static void Writeback(Settlement_TraderTracker tracker, Thing toGive, int count,
            int sign)
        {
            try
            {
                if (!(CityEconomyMod.Settings?.traderEconomyEnabled ?? false)
                    || toGive?.def == null || count <= 0)
                {
                    return;
                }
                SettlementWealthComp comp = tracker?.settlement?.GetComponent<SettlementWealthComp>();
                if (comp == null || !comp.initialized)
                {
                    return;
                }
                if (toGive.def == ThingDefOf.Silver)
                {
                    comp.OffsetWealth(WealthKind.Silver, sign * count);
                }
                else if (toGive.def.IsNutritionGivingIngestible)
                {
                    comp.OffsetWealth(WealthKind.Food, sign * EconomyUtility.TradeValue(toGive.def, count));
                }
                else
                {
                    comp.OffsetWealth(WealthKind.Goods, sign * EconomyUtility.TradeValue(toGive.def, count));
                }
            }
            catch (System.Exception e)
            {
                EconomyUtility.WarnOnce("tradeWriteback", "交易回寫 postfix 異常，本筆略過：" + e);
            }
        }
    }
}
