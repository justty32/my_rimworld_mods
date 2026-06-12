using System;
using FactionColonies;
using HarmonyLib;
using pas.outposts;
using RimWorld.Planet;
using Verse;

namespace pas.empire.outposts.war
{
    /// <summary>入口。三項功能皆優先走契約層（Empire Registry ＋ npc-outposts 的 static hook），
    /// 對 Empire 唯一的 Harmony 接點是 Capture 上下文 patch（簽章探測 fail-soft）。
    /// Empire Registry 會被 Game.ClearCaches 清空 → 配 EmpireCacheUtil.RegisterCacheInvalidator 重註冊。</summary>
    [StaticConstructorOnStartup]
    public static class OutpostsWarInit
    {
        public const string HarmonyId = "pas.empire.outposts.war";

        private static readonly OutpostTransferHooks transferHooks = new OutpostTransferHooks();
        private static readonly OutpostBattleModifier battleModifier = new OutpostBattleModifier();
        private static readonly OutpostTaxParticipant taxParticipant = new OutpostTaxParticipant();

        /// <summary>玩家側 Capture 削防/哨站認領是否啟用（Capture patch 掛上才為 true）。</summary>
        public static bool CapturePatchActive { get; private set; }

        static OutpostsWarInit()
        {
            try
            {
                // 功能 1（增生面）：把附庸納入 npc-outposts 的合格母體（opt-in，受設定開關控制）。
                WorldComponent_OutpostSpawner.ParentEligibilityOverride = VassalEligibility;

                // 對 Empire 唯一 Harmony：Capture 上下文。
                CapturePatchActive = Patch_CaptureContext.TryPatch(new Harmony(HarmonyId));

                RegisterAll();
                EmpireCacheUtil.RegisterCacheInvalidator(HarmonyId, RegisterAll);

                OutpostWarUtility.Message("loaded. capturePatch="
                    + (CapturePatchActive ? "on" : "off (degraded: player-side strip/claim disabled)"));
            }
            catch (Exception e)
            {
                Log.Error("[EmpireOutpostsWar] init failed; cluster glue disabled this session: " + e);
            }
        }

        /// <summary>合格母體覆寫：附庸（PColony）依設定開關納入/不表態；其餘聚落一律不表態（沿用本體預設）。</summary>
        private static bool? VassalEligibility(Settlement settlement)
        {
            if (settlement?.Faction != null && FactionCache.IsPlayerColonyFaction(settlement.Faction))
            {
                return OutpostsWarMod.Settings != null && OutpostsWarMod.Settings.vassalOutpostsEnabled;
            }
            return null;
        }

        private static void RegisterAll()
        {
            LifecycleRegistry.Register(transferHooks);
            BattleModifierRegistry.Register(battleModifier);
            TaxTickRegistry.Register(taxParticipant);
        }
    }
}
