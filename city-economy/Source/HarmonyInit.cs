using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace pas.sanguo.cityeconomy
{
    /// <summary>接線總管（仿 P1/P2）：patch 逐一 TryPatch fail-soft——目標簽章不符
    /// 就 WarnOnce＋該功能降級，不連坐其餘功能。成長走 comp 自身 CompTick，免 patch。</summary>
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            Harmony harmony = new Harmony("pas.sanguo.cityeconomy");

            // 守城：降 PointDamage 臨時抬 EffectivePoints（戰後還原，tier clamp 封頂）
            TryPatch(harmony, typeof(RimWar.Planet.IncidentUtility), "ResolveCombat_Settlement",
                prefix: AccessTools.Method(typeof(Patch_ResolveCombatSettlement),
                    nameof(Patch_ResolveCombatSettlement.Prefix)),
                postfix: AccessTools.Method(typeof(Patch_ResolveCombatSettlement),
                    nameof(Patch_ResolveCombatSettlement.Postfix)));

            // 劫掠：sack 分支同步搬真資源（RimWar 點數搬移保留，不雙算）
            TryPatch(harmony, typeof(RimWar.Planet.IncidentUtility), "ResolveBattle_Settlement",
                prefix: AccessTools.Method(typeof(Patch_ResolveBattleSettlement),
                    nameof(Patch_ResolveBattleSettlement.Prefix)),
                postfix: AccessTools.Method(typeof(Patch_ResolveBattleSettlement),
                    nameof(Patch_ResolveBattleSettlement.Postfix)));

            // 貨架：stock 生成後按財富縮放數量（protected method，AccessTools 字串找）
            TryPatch(harmony, typeof(RimWorld.Planet.Settlement_TraderTracker), "RegenerateStock",
                postfix: AccessTools.Method(typeof(Patch_TraderStock),
                    nameof(Patch_TraderStock.Postfix)));

            // 交易回寫：玩家賣 → 城財富↑；玩家買 → 城財富↓
            TryPatch(harmony, typeof(RimWorld.Planet.Settlement_TraderTracker), "GiveSoldThingToTrader",
                postfix: AccessTools.Method(typeof(Patch_TraderGiveSold),
                    nameof(Patch_TraderGiveSold.TraderPostfix)));
            TryPatch(harmony, typeof(RimWorld.Planet.Settlement_TraderTracker), "GiveSoldThingToPlayer",
                postfix: AccessTools.Method(typeof(Patch_TraderGiveSold),
                    nameof(Patch_TraderGiveSold.PlayerPostfix)));

            // 顯示：inspect 附財富/防禦行（獨立 postfix，與 RimWar/P2 疊加安全）
            TryPatch(harmony, typeof(RimWorld.Planet.Settlement), "GetInspectString",
                postfix: AccessTools.Method(typeof(Patch_SettlementInspectString),
                    nameof(Patch_SettlementInspectString.Postfix)));
        }

        private static void TryPatch(Harmony harmony, Type targetType, string methodName,
            MethodInfo prefix = null, MethodInfo postfix = null)
        {
            try
            {
                MethodInfo target = AccessTools.Method(targetType, methodName);
                if (target == null)
                {
                    EconomyUtility.WarnOnce("missing:" + methodName,
                        $"找不到 {targetType.Name}.{methodName}（目標 mod/遊戲版本不符？），對應功能降級停用。");
                    return;
                }
                harmony.Patch(target,
                    prefix == null ? null : new HarmonyMethod(prefix),
                    postfix == null ? null : new HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                EconomyUtility.WarnOnce("patchFail:" + methodName,
                    $"patch {targetType.Name}.{methodName} 失敗（簽章不符？），對應功能降級停用：{e}");
            }
        }
    }
}
