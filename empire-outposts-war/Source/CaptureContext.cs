using System;
using FactionColonies;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.empire.outposts.war
{
    /// <summary>玩家 Capture 期間的暫存上下文（單筆，OnResolved 為同步、單線程）。
    /// 由 Capture prefix 在開戰前填入：目標 tile＋目標 NPC 派系＋目標聚落引用。
    /// 供功能 2（IBattleModifier 數目標哨站加敵防）與功能 3（OnSettlementCreated 認領哨站）讀取。</summary>
    public static class CaptureContext
    {
        public static bool Active;
        public static PlanetTile TargetTile = PlanetTile.Invalid;
        public static Faction TargetFaction;
        public static Settlement TargetSettlement;

        public static void Set(PlanetTile tile, Settlement target)
        {
            Active = true;
            TargetTile = tile;
            TargetSettlement = target;
            TargetFaction = target?.Faction;
        }

        public static void Clear()
        {
            Active = false;
            TargetTile = PlanetTile.Invalid;
            TargetFaction = null;
            TargetSettlement = null;
        }
    }

    /// <summary>對 Empire 唯一的 Harmony 接點：MilitaryJobHandler_Capture.OnResolved 前後設/清 CaptureContext。
    /// 簽章探測 fail-soft：方法不存在 → 玩家側削防＋玩家側哨站認領降級停用，附庸側功能完全不受影響。</summary>
    public static class Patch_CaptureContext
    {
        public static void Prefix(WorldObjectComp_SettlementMilitary milComp)
        {
            try
            {
                Settlement target = Find.WorldObjects.SettlementAt(milComp.militaryLocation);
                CaptureContext.Set(milComp.militaryLocation, target);
            }
            catch (Exception e)
            {
                CaptureContext.Clear();
                OutpostWarUtility.WarnOnce("captureCtx", "Capture 上下文設定異常，本次削防/認領降級：" + e);
            }
        }

        public static void Finalizer()
        {
            // OnSettlementCreated 在 OnResolved 內同步觸發、已讀過 context；此處（無論成敗）收尾清除。
            CaptureContext.Clear();
        }

        /// <summary>由 Init 呼叫：探測並掛 patch。回傳是否成功（玩家側子功能是否啟用）。</summary>
        public static bool TryPatch(Harmony harmony)
        {
            System.Reflection.MethodInfo target = AccessTools.Method(
                typeof(MilitaryJobHandler_Capture), "OnResolved");
            if (target == null)
            {
                OutpostWarUtility.WarnOnce("captureMethod",
                    "找不到 MilitaryJobHandler_Capture.OnResolved；玩家側 Capture 削防/哨站認領降級停用。");
                return false;
            }
            harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(Patch_CaptureContext), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(Patch_CaptureContext), nameof(Finalizer)));
            return true;
        }
    }
}
