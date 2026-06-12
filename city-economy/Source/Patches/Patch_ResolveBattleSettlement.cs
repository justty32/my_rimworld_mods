using RimWar.Planet;
using RimWorld;
using UnityEngine;

namespace pas.sanguo.cityeconomy
{
    /// <summary>Prefix+Postfix IncidentUtility.ResolveBattle_Settlement（RW:11108）。
    /// sack 指紋（T0 定案，四結局中唯一倖存分支 RW:11197-11212）：parent 活著＋派系未變
    /// ＋PointDamage==RimWarPoints-1＋AttackingUnits 已清空＋攻方 EffectivePoints&gt;0。
    /// 命中 → 按 sackLossRatio 搬走真資源、defensePoints 折半（城防殘破）。
    /// 不雙算：RimWar 原 sack 點數搬移與信件全保留——點數歸 RimWar、實財歸本 mod。</summary>
    public static class Patch_ResolveBattleSettlement
    {
        public static void Prefix(RimWarSettlementComp defender, ref Faction __state)
        {
            __state = defender?.parent?.Faction;   // 佔領（ConvertSettlement）偵測快照
        }

        public static void Postfix(RimWarSettlementComp defender, WarObject attacker, Faction __state)
        {
            try
            {
                float ratio = CityEconomyMod.Settings?.sackLossRatio ?? 0f;
                if (ratio <= 0f)
                {
                    return;
                }
                if (defender?.parent == null || defender.parent.Destroyed
                    || defender.parent.Faction != __state)
                {
                    return;   // 焚毀/互滅/佔領結局：非 sack
                }
                if (attacker == null || attacker.EffectivePoints <= 0
                    || defender.AttackingUnits.Count != 0
                    || defender.PointDamage != defender.RimWarPoints - 1)
                {
                    return;   // 守軍勝（只 Remove 攻方）或戰鬥未了：非 sack
                }
                SettlementWealthComp comp = defender.parent.GetComponent<SettlementWealthComp>();
                if (comp == null || !comp.initialized)
                {
                    return;
                }
                ratio = Mathf.Clamp01(ratio);
                comp.silver -= Mathf.FloorToInt(comp.silver * ratio);
                comp.food -= Mathf.FloorToInt(comp.food * ratio);
                comp.goods -= Mathf.FloorToInt(comp.goods * ratio);
                comp.defensePoints /= 2;
            }
            catch (System.Exception e)
            {
                EconomyUtility.WarnOnce("sackPostfix", "劫掠搬資源 postfix 異常，本場保留原財富：" + e);
            }
        }
    }
}
