using RimWar.Planet;
using UnityEngine;

namespace pas.sanguo.cityeconomy
{
    /// <summary>Prefix+Postfix IncidentUtility.ResolveCombat_Settlement（RW:11018）。
    /// K 鐵律：防禦勿疊進 RimWarPoints 存量——守城時降 PointDamage（set 無 clamp、可為負）
    /// 臨時抬高 EffectivePoints（RW:9277），受原方法 tier clamp（200/500/2000/4000）自動封頂，
    /// 戰後 postfix 還原。還原 clamp ≤ RimWarPoints-1：①sack 倖存分支已設
    /// PointDamage=RimWarPoints-1，疊回會讓 EffectivePoints&lt;0 → CompTick 守門戰鬥停擺；
    /// ②城被毀/易主（ResolveBattle 佔領/焚毀分支）→ 不還原，comp 隨城亡。
    /// 呼叫源＝RimWarSettlementComp.CompTick（主執行緒）；不用 Rand、不發信件。</summary>
    public static class Patch_ResolveCombatSettlement
    {
        public static void Prefix(RimWarSettlementComp defender, ref int __state)
        {
            __state = 0;
            try
            {
                SettlementWealthComp comp = defender?.parent?.GetComponent<SettlementWealthComp>();
                int bonus = comp?.DefenseBonus ?? 0;
                if (bonus <= 0)
                {
                    return;
                }
                defender.PointDamage -= bonus;
                __state = bonus;
            }
            catch (System.Exception e)
            {
                __state = 0;
                EconomyUtility.WarnOnce("defensePrefix", "守城折算 prefix 異常，本輪不加成：" + e);
            }
        }

        public static void Postfix(RimWarSettlementComp defender, int __state)
        {
            if (__state <= 0)
            {
                return;
            }
            try
            {
                if (defender?.parent == null || defender.parent.Destroyed)
                {
                    return;   // 佔領/焚毀結局：comp 隨城亡，不還原
                }
                defender.PointDamage = Mathf.Min(defender.PointDamage + __state,
                    defender.RimWarPoints - 1);
            }
            catch (System.Exception e)
            {
                EconomyUtility.WarnOnce("defensePostfix", "守城折算 postfix 異常：" + e);
            }
        }
    }
}
