using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace pas.sanguo.cityeconomy
{
    /// <summary>P2 settlement-lords 的 soft-optional 反射橋（唯一接點）：
    /// 領主在 → 財富成長吃 LordsUtility.GovernanceFactor；P2/P0 不在 → 中性 1.0。
    /// 鐵律：不 ref SettlementLords.dll／NamedOfficers.dll（csproj healthcheck guard），
    /// OfficerRecord 全程以 object 傳遞。任一解析/呼叫失敗 → 永久降級＋WarnOnce。</summary>
    public static class LordGovernanceBridge
    {
        private static bool resolved;
        private static bool available;
        private static MethodInfo getComponent;     // WorldComponent_SettlementLords.Get()
        private static MethodInfo lordOf;           // 實例 LordOf(Settlement) → OfficerRecord
        private static MethodInfo governanceFactor; // LordsUtility.GovernanceFactor(OfficerRecord)

        private static void Resolve()
        {
            resolved = true;
            System.Type compType =
                AccessTools.TypeByName("pas.officers.settlements.WorldComponent_SettlementLords");
            System.Type utilType = AccessTools.TypeByName("pas.officers.settlements.LordsUtility");
            if (compType == null || utilType == null)
            {
                return;   // P2 未安裝：靜默中性（正常情境，不警告）
            }
            getComponent = AccessTools.Method(compType, "Get");
            lordOf = AccessTools.Method(compType, "LordOf");
            governanceFactor = AccessTools.Method(utilType, "GovernanceFactor");
            if (getComponent == null || lordOf == null || governanceFactor == null
                || governanceFactor.ReturnType != typeof(float))
            {
                EconomyUtility.WarnOnce("lordBridge",
                    "settlement-lords 在場但介面簽章不符（版本不符？），治理係數降級為 1.0。");
                return;
            }
            available = true;
        }

        /// <summary>該城治理係數；無 P2/無主/任何異常 → 1.0。clamp 0.25~2 防極端。</summary>
        public static float GovernanceFactorFor(Settlement settlement)
        {
            if (!resolved)
            {
                Resolve();
            }
            if (!available || settlement == null)
            {
                return 1f;
            }
            try
            {
                object component = getComponent.Invoke(null, null);
                if (component == null)
                {
                    return 1f;
                }
                object record = lordOf.Invoke(component, new object[] { settlement });
                float factor = (float)governanceFactor.Invoke(null, new object[] { record });
                return Mathf.Clamp(factor, 0.25f, 2f);
            }
            catch (System.Exception e)
            {
                available = false;   // 永久降級，不重試不洗版
                EconomyUtility.WarnOnce("lordBridgeInvoke",
                    "呼叫 settlement-lords 治理係數失敗，永久降級為 1.0：" + e);
                return 1f;
            }
        }
    }
}
