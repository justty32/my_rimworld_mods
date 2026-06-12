using System;
using RimWar;
using RimWar.Planet;
using RimWorld.Planet;
using UnityEngine;

namespace pas.officers.settlements
{
    /// <summary>Postfix WorldComponent_PowerTracker.IncrementSettlementGrowth（RW:17567）。
    /// 獨立 postfix，與 Mod 1 哨站貢獻 postfix 疊加同方法、互不知情、各自鏡像守則：
    /// PointDamage&gt;0（療傷分支，RW:17616-17620）跳過、成長上限（RW:17597-17612）clamp。
    /// 治理係數 ≥1 補成長、&lt;1 且衰退開關開 → 扣 RimWarPoints（setter Max(0,)、
    /// getter 地板 100 自然托底——摧毀城走 ConvertSettlement，非本注入點）。
    /// 鐵律：不讀寫任何派系級係數，只動單城點數。
    /// threading（RW:17062）：threadingEnabled 時本 postfix 在 tasker 背景執行緒跑 →
    /// 不用 Rand、不發信件、綁定走 snapshot、整體 try/catch。</summary>
    public static class Patch_IncrementSettlementGrowth
    {
        public static void Postfix()
        {
            try
            {
                SettlementLordsSettings settings = SettlementLordsMod.Settings;
                if (settings == null || settings.govAmplitude <= 0f)
                {
                    return;
                }
                WorldComponent_SettlementLords comp = WorldComponent_SettlementLords.Get();
                if (comp == null)
                {
                    return;
                }
                LordBinding[] bindings = comp.BindingsSnapshot();
                for (int i = 0; i < bindings.Length; i++)
                {
                    Settlement host = bindings[i].host;
                    if (host == null || host.Destroyed)
                    {
                        continue;   // heal 心跳收尾
                    }
                    OfficerRecord record = OfficersApi.GetById(bindings[i].recordId);
                    if (record == null || record.dead)
                    {
                        continue;
                    }
                    RimWarSettlementComp rwsc = host.GetComponent<RimWarSettlementComp>();
                    if (rwsc == null || rwsc.PointDamage > 0)
                    {
                        continue;   // 鏡像療傷分支：當輪不成長（本 mod 也不加不扣）
                    }
                    RimWarData rwd = WorldUtility.GetRimWarDataForFaction(host.Faction);
                    if (rwd == null || rwd.behavior == RimWarBehavior.Player
                        || rwd.behavior == RimWarBehavior.Excluded)
                    {
                        continue;
                    }
                    float gov = LordsUtility.GovernanceFactor(record);
                    int delta = Mathf.RoundToInt((gov - 1f) * LordsUtility.GovPointsScale);
                    if (delta > 0)
                    {
                        int cap = LordsUtility.GrowthCapFor(rwsc, rwd);
                        if (rwsc.RimWarPoints < cap)
                        {
                            rwsc.RimWarPoints = Mathf.Min(rwsc.RimWarPoints + delta, cap);
                        }
                    }
                    else if (delta < 0 && settings.decayEnabled)
                    {
                        rwsc.RimWarPoints = Mathf.Max(0, rwsc.RimWarPoints + delta);
                    }
                }
            }
            catch (Exception e)
            {
                LordsUtility.WarnOnce("growthPostfix", "治理成長 postfix 異常，本輪跳過：" + e);
            }
        }
    }
}
