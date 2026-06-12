using System;
using System.Reflection;
using HarmonyLib;
using RimWar.Planet;
using RimWorld.Planet;
using Verse;

namespace pas.officers.settlements
{
    /// <summary>接線總管（仿 Mod 1／P1）：patch 逐一 TryPatch fail-soft——RimWar 簽章不符
    /// 就 WarnOnce＋該功能降級，不連坐其餘功能。指派/退場走 WorldComponent 心跳，免 patch。</summary>
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            Harmony harmony = new Harmony("pas.officers.settlements");

            // 治理：逐城補成長/扣點（獨立 postfix，與 Mod 1 哨站貢獻疊加安全）
            TryPatch(harmony, typeof(WorldComponent_PowerTracker), "IncrementSettlementGrowth",
                postfix: AccessTools.Method(typeof(Patch_IncrementSettlementGrowth),
                    nameof(Patch_IncrementSettlementGrowth.Postfix)));

            // 顯示：inspect 附領主行（名字＋政務/忠誠＋治理係數）
            TryPatch(harmony, typeof(Settlement), "GetInspectString",
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
                    LordsUtility.WarnOnce("missing:" + methodName,
                        $"找不到 {targetType.Name}.{methodName}（RimWar 版本不符？），對應功能降級停用。");
                    return;
                }
                harmony.Patch(target,
                    prefix == null ? null : new HarmonyMethod(prefix),
                    postfix == null ? null : new HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                LordsUtility.WarnOnce("patchFail:" + methodName,
                    $"patch {targetType.Name}.{methodName} 失敗（簽章不符？），對應功能降級停用：{e}");
            }
        }
    }
}
