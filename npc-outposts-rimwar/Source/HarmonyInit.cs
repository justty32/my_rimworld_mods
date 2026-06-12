using System;
using System.Reflection;
using HarmonyLib;
using pas.outposts;
using RimWar.Planet;
using Verse;

namespace pas.outposts.rimwar
{
    /// <summary>接線總管：Harmony patch（逐一 fail-soft——RimWar 簽章不符就警告一次＋該功能降級，
    /// 不影響其餘功能）＋ npc-outposts 增生倍率 hook 註冊。</summary>
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            Harmony harmony = new Harmony("pas.outposts.rimwar");

            // 功能 1：哨站貢獻母聚落成長
            TryPatch(harmony, typeof(WorldComponent_PowerTracker), "IncrementSettlementGrowth",
                postfix: AccessTools.Method(typeof(Patch_IncrementSettlementGrowth), nameof(Patch_IncrementSettlementGrowth.Postfix)));

            // 功能 3：哨站被佔易主/摧毀 ＋ 衛星跟隨聚落易主
            TryPatch(harmony, typeof(WorldUtility), "ConvertSettlement",
                prefix: AccessTools.Method(typeof(Patch_ConvertSettlement), nameof(Patch_ConvertSettlement.Prefix)),
                postfix: AccessTools.Method(typeof(Patch_ConvertSettlement), nameof(Patch_ConvertSettlement.Postfix)));

            // 功能 4：聚落戰勝負入帳
            TryPatch(harmony, typeof(IncidentUtility), "ResolveBattle_Settlement",
                prefix: AccessTools.Method(typeof(Patch_ResolveBattleSettlement), nameof(Patch_ResolveBattleSettlement.Prefix)),
                postfix: AccessTools.Method(typeof(Patch_ResolveBattleSettlement), nameof(Patch_ResolveBattleSettlement.Postfix)));

            // 功能 5：按機率把 settler 落地改建哨站變體（淨降 RimWar 建聚落頻率）
            TryPatch(harmony, typeof(WorldUtility), "CreateSettlement",
                prefix: AccessTools.Method(typeof(Patch_CreateSettlement), nameof(Patch_CreateSettlement.Prefix)));

            // 功能 4：增生倍率 hook（npc-outposts 唯一擴充接點；未註冊時零行為變化）
            WorldComponent_OutpostSpawner.GrowthRateMultiplier = WorldComponent_OutpostWarMomentum.GetGrowthMultiplierFor;
        }

        private static void TryPatch(Harmony harmony, Type targetType, string methodName,
            MethodInfo prefix = null, MethodInfo postfix = null)
        {
            try
            {
                MethodInfo target = AccessTools.Method(targetType, methodName);
                if (target == null)
                {
                    OutpostRimWarUtility.WarnOnce("missing:" + methodName,
                        $"找不到 {targetType.Name}.{methodName}（RimWar 版本不符？），對應功能降級停用。");
                    return;
                }
                harmony.Patch(target,
                    prefix == null ? null : new HarmonyMethod(prefix),
                    postfix == null ? null : new HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                OutpostRimWarUtility.WarnOnce("patchFail:" + methodName,
                    $"patch {targetType.Name}.{methodName} 失敗（簽章不符？），對應功能降級停用：{e}");
            }
        }
    }
}
