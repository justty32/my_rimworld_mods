using System.Collections.Generic;
using RimWar;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.sanguo.cityeconomy
{
    /// <summary>城池經濟/防禦單一 comp（H+K 定型）：typed int 主幹（Scribe_Values，
    /// 仿 RimWarSettlementComp.PostExposeData RW:9502）＋ Def-dict 旁路。
    /// 成長走自 comp CompTick＋絕對時刻 nextTick 節流（仿 RW:9585，與 RimWar 解耦）。
    /// 防禦為獨立維度，絕不疊進 RimWarPoints 存量（K 鐵律）。</summary>
    public class SettlementWealthComp : WorldObjectComp
    {
        public int silver;
        public int food;
        public int goods;
        public int defenseLevel;
        public int defensePoints;
        public bool initialized;
        private int nextTick;
        private Dictionary<SettlementAttributeDef, float> extraAttributes;

        /// <summary>守城折算用加成（T3 prefix 與 inspect 共用）：
        /// 有效工事（≤ level×1000）× defenseAmplitude；未播種/停用 → 0。</summary>
        public int DefenseBonus
        {
            get
            {
                float amplitude = CityEconomyMod.Settings?.defenseAmplitude ?? 0f;
                if (!initialized || amplitude <= 0f)
                {
                    return 0;
                }
                int effective = Mathf.Clamp(defensePoints, 0,
                    defenseLevel * EconomyUtility.DefensePointsPerLevel);
                return Mathf.Max(0, Mathf.RoundToInt(effective * amplitude));
            }
        }

        public float GetExtra(SettlementAttributeDef def, float fallback = 0f)
        {
            if (def == null)
            {
                return fallback;
            }
            if (extraAttributes != null && extraAttributes.TryGetValue(def, out float value))
            {
                return value;
            }
            return fallback;
        }

        public void SetExtra(SettlementAttributeDef def, float value)
        {
            if (def == null)
            {
                return;
            }
            (extraAttributes = extraAttributes ?? new Dictionary<SettlementAttributeDef, float>())[def] = value;
        }

        /// <summary>交易回寫（T4）：delta>0 補進（cap 托頂但不削既有超額）、delta<0 扣到 0。</summary>
        public void OffsetWealth(WealthKind kind, int delta)
        {
            if (delta == 0 || !initialized)
            {
                return;
            }
            int points = parent?.GetComponent<RimWarSettlementComp>()?.RimWarPoints ?? 0;
            switch (kind)
            {
                case WealthKind.Silver:
                    silver = Offset(silver, delta, points * EconomyUtility.SilverCapMultiplier);
                    break;
                case WealthKind.Food:
                    food = Offset(food, delta, points * EconomyUtility.FoodGoodsCapMultiplier);
                    break;
                default:
                    goods = Offset(goods, delta, points * EconomyUtility.FoodGoodsCapMultiplier);
                    break;
            }
        }

        private static int Offset(int current, int delta, int cap)
            => delta > 0 ? Mathf.Min(current + delta, Mathf.Max(current, cap)) : Mathf.Max(0, current + delta);

        public override void CompTick()
        {
            int ticks = Find.TickManager.TicksGame;
            if (nextTick == 0)
            {
                // 首輪 offset：1800 基底錯開 P0(0)/P2(600)/P1(1200)，再按 parent.ID 散開
                nextTick = ticks + EconomyUtility.HeartbeatBaseOffset + parent.ID % 700;
                return;
            }
            if (ticks < nextTick)
            {
                return;
            }
            nextTick = ticks + EconomyUtility.CycleTicks;
            try
            {
                TickEconomy();
            }
            catch (System.Exception e)
            {
                EconomyUtility.WarnOnce("economyTick", "經濟成長輪例外，本輪跳過：" + e);
            }
        }

        private void TickEconomy()
        {
            RimWarSettlementComp rwsc = parent?.GetComponent<RimWarSettlementComp>();
            if (rwsc == null || parent.Faction == null)
            {
                return;   // 非 RimWar 追蹤城（或無主）：不播種、不成長、不顯示
            }
            RimWarData rwd = WorldUtility.GetRimWarDataForFaction(parent.Faction);
            if (rwd == null || rwd.behavior == RimWarBehavior.Player
                || rwd.behavior == RimWarBehavior.Excluded)
            {
                return;
            }
            int points = rwsc.RimWarPoints;
            if (!initialized)
            {
                silver = points;
                food = points / 2;
                goods = points / 2;
                defenseLevel = 1;
                defensePoints = 500;
                initialized = true;
                return;   // 首輪只播種
            }
            if (rwsc.PointDamage > 0)
            {
                return;   // 鏡像 RimWar 療傷分支（RW:17616）：圍城/受創不長財富
            }
            float rate = CityEconomyMod.Settings?.growthRate ?? 0f;
            if (rate <= 0f)
            {
                return;
            }
            float gov = LordGovernanceBridge.GovernanceFactorFor(parent as Settlement);
            int unit = Mathf.Max(1, points / 100);
            silver = Grow(silver, unit * gov * rate, points * EconomyUtility.SilverCapMultiplier);
            food = Grow(food, 0.6f * unit * gov * rate, points * EconomyUtility.FoodGoodsCapMultiplier);
            goods = Grow(goods, 0.6f * unit * gov * rate, points * EconomyUtility.FoodGoodsCapMultiplier);
            GrowDefense(unit, rate);
        }

        private static int Grow(int current, float delta, int cap)
            => current >= cap ? current : Mathf.Min(current + Mathf.Max(1, Mathf.RoundToInt(delta)), cap);

        /// <summary>防禦不吃治理係數（治理只管經濟）；工事到頂且存銀充裕 → 升級城防。
        /// P5 改由領主決策驅動，本期為城池自治簡化版。</summary>
        private void GrowDefense(int unit, float rate)
        {
            int cap = defenseLevel * EconomyUtility.DefensePointsPerLevel;
            if (defensePoints < cap)
            {
                defensePoints = Mathf.Min(cap,
                    defensePoints + Mathf.Max(1, Mathf.RoundToInt(unit / 3f * rate)));
                return;
            }
            if (defenseLevel >= EconomyUtility.MaxDefenseLevel)
            {
                return;
            }
            int cost = (defenseLevel + 1) * EconomyUtility.DefenseUpgradeCostPerLevel;
            if (silver >= cost * 2)
            {
                silver -= cost;
                defenseLevel++;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref silver, "pas_cityecon_silver", 0);
            Scribe_Values.Look(ref food, "pas_cityecon_food", 0);
            Scribe_Values.Look(ref goods, "pas_cityecon_goods", 0);
            Scribe_Values.Look(ref defenseLevel, "pas_cityecon_defenseLevel", 0);
            Scribe_Values.Look(ref defensePoints, "pas_cityecon_defensePoints", 0);
            Scribe_Values.Look(ref initialized, "pas_cityecon_initialized", false);
            Scribe_Values.Look(ref nextTick, "pas_cityecon_nextTick", 0);
            Scribe_Collections.Look(ref extraAttributes, "pas_cityecon_extraAttributes",
                LookMode.Def, LookMode.Value);
        }
    }
}
