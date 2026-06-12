using System;
using System.Reflection;
using FactionColonies;
using HarmonyLib;
using Verse;

namespace pas.empire.warfare
{
    /// <summary>
    /// 入口。對 Empire 走契約層（LifecycleRegistry / BattleModifierRegistry），
    /// 對 RimWar 只掛一個攻擊標記 postfix（簽章探測 fail-soft）。
    /// Registry 會被 Game.ClearCaches 清空，故配 EmpireCacheUtil.RegisterCacheInvalidator 重註冊。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class WarfareInit
    {
        public const string HarmonyId = "pas.empire.warfare";

        private static readonly WarfareLifecycleHooks lifecycleHooks = new WarfareLifecycleHooks();
        private static readonly VassalDefenseBattleModifier battleModifier = new VassalDefenseBattleModifier();

        /// <summary>RimWar 攻擊標記 patch 是否掛上；false 時「僅 RimWar 攻擊」過濾降級停用。</summary>
        public static bool AttackMarkerPatchActive { get; private set; }

        static WarfareInit()
        {
            try
            {
                PatchRimWarAttackMarker();
                RegisterAll();
                EmpireCacheUtil.RegisterCacheInvalidator(HarmonyId, RegisterAll);
                LogUtil.MessageForce("[EmpireWarfare] loaded. attackMarker="
                    + (AttackMarkerPatchActive ? "on" : "off (degraded: source filter disabled)"));
            }
            catch (Exception e)
            {
                Log.Error("[EmpireWarfare] init failed; vassal fall disabled for this session: " + e);
            }
        }

        private static void PatchRimWarAttackMarker()
        {
            // 簽章探測：RimWar 改版移除/改名時降級不炸
            MethodInfo target = AccessTools.Method(
                typeof(RimWar.Planet.IncidentUtility), "ResolveWarObjectAttackOnSettlement");
            if (target == null)
            {
                LogUtil.Warning("[EmpireWarfare] IncidentUtility.ResolveWarObjectAttackOnSettlement not found; "
                    + "RimWar attack marker disabled (falls will not filter by attack source).");
                return;
            }

            new Harmony(HarmonyId).Patch(target, postfix: new HarmonyMethod(
                typeof(Patch_RecordWarObjectAttack), nameof(Patch_RecordWarObjectAttack.Postfix)));
            AttackMarkerPatchActive = true;
        }

        private static void RegisterAll()
        {
            LifecycleRegistry.Register(lifecycleHooks);
            BattleModifierRegistry.Register(battleModifier);
        }
    }
}
