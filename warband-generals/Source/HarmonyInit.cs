using System;
using System.Reflection;
using HarmonyLib;
using RimWar.Planet;
using Verse;

namespace pas.officers.warband
{
    /// <summary>接線總管（仿 Mod 1）：四個 patch 逐一 TryPatch fail-soft——RimWar 簽章不符
    /// 就 WarnOnce＋該功能降級，不連坐其餘功能。</summary>
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            Harmony harmony = new Harmony("pas.officers.warband");

            // 生成：新 warband 按機率掛將領；戰後重生消費 transfer context
            TryPatch(harmony, typeof(WorldUtility), "CreateWarband",
                postfix: AccessTools.Method(typeof(Patch_CreateWarband), nameof(Patch_CreateWarband.Postfix)));

            // 傳承：戰鬥結算以舊 warband 為樣板重生新物件 → 將領跟著搬家
            TryPatch(harmony, typeof(WorldUtility), "CreateWarObjectOfType",
                prefix: AccessTools.Method(typeof(Patch_CreateWarObjectOfType), nameof(Patch_CreateWarObjectOfType.Prefix)),
                postfix: AccessTools.Method(typeof(Patch_CreateWarObjectOfType), nameof(Patch_CreateWarObjectOfType.Postfix)));

            // 戰力：對單場 PointDamage delta 局部乘將領比值（勿動派系級 combat 係數）
            TryPatch(harmony, typeof(IncidentUtility), "ResolveCombat_Units",
                prefix: AccessTools.Method(typeof(Patch_ResolveCombatUnits), nameof(Patch_ResolveCombatUnits.Prefix)),
                postfix: AccessTools.Method(typeof(Patch_ResolveCombatUnits), nameof(Patch_ResolveCombatUnits.Postfix)));

            // 顯示：inspect 附將領行（名字＋武力/統率）
            TryPatch(harmony, typeof(WarObject), "GetInspectString",
                postfix: AccessTools.Method(typeof(Patch_WarObjectInspectString), nameof(Patch_WarObjectInspectString.Postfix)));
        }

        private static void TryPatch(Harmony harmony, Type targetType, string methodName,
            MethodInfo prefix = null, MethodInfo postfix = null)
        {
            try
            {
                MethodInfo target = AccessTools.Method(targetType, methodName);
                if (target == null)
                {
                    GeneralsUtility.WarnOnce("missing:" + methodName,
                        $"找不到 {targetType.Name}.{methodName}（RimWar 版本不符？），對應功能降級停用。");
                    return;
                }
                harmony.Patch(target,
                    prefix == null ? null : new HarmonyMethod(prefix),
                    postfix == null ? null : new HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                GeneralsUtility.WarnOnce("patchFail:" + methodName,
                    $"patch {targetType.Name}.{methodName} 失敗（簽章不符？），對應功能降級停用：{e}");
            }
        }
    }
}
