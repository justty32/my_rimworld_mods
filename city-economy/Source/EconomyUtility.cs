using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace pas.sanguo.cityeconomy
{
    /// <summary>共用小工具：去重警告、成長/貨架公式常數（仿 P1/P2 慣例）。</summary>
    public static class EconomyUtility
    {
        /// <summary>成長週期（tick）；節流走 comp 內絕對時刻 nextTick（仿 RW:9585）。</summary>
        public const int CycleTicks = 2500;

        /// <summary>首輪 offset 基底：錯開 P0(0)/P2(600)/P1(1200)；再加 parent.ID 雜湊散開。</summary>
        public const int HeartbeatBaseOffset = 1800;

        public const int MaxDefenseLevel = 5;

        /// <summary>每級城防可承載的工事點數（守城折算只吃 ≤ level×本值 的部分）。</summary>
        public const int DefensePointsPerLevel = 1000;

        /// <summary>升一級城防的白銀成本＝(level+1)×本值；需存銀 ≥ 2×成本才動工。</summary>
        public const int DefenseUpgradeCostPerLevel = 2000;

        /// <summary>silver 存量上限＝RimWarPoints×本值。</summary>
        public const int SilverCapMultiplier = 10;

        /// <summary>food/goods 存量上限＝RimWarPoints×本值。</summary>
        public const int FoodGoodsCapMultiplier = 5;

        private static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>同 key 只 Warning 一次（防衛式降級不洗版，仿 Mod 1／P1／P2）。</summary>
        public static void WarnOnce(string key, string message)
        {
            if (warned.Add(key))
            {
                Log.Warning("[CityEconomy] " + message);
            }
        }

        /// <summary>貨架縮放因子：財富/基準，clamp 0.25~2（播種值 → 1.0 中性）。</summary>
        public static float StockFactor(int wealth, float baseline)
        {
            if (baseline < 1f)
            {
                baseline = 1f;
            }
            return Mathf.Clamp(wealth / baseline, 0.25f, 2f);
        }

        /// <summary>非白銀品項的交易財富值（白銀走 count 直記，呼叫端自判）。</summary>
        public static int TradeValue(ThingDef def, int count)
        {
            if (def == null || count <= 0)
            {
                return 0;
            }
            return Mathf.Max(0, Mathf.RoundToInt(def.BaseMarketValue * count));
        }
    }
}
