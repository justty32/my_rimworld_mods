using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace pas.politics
{
    /// <summary>Rim War 軟相容（反射綁定，零硬引用）。簽名經 Rim War v1.6 實體 DLL（workshop
    /// 2222935097）ikdasm 核對 2026-06-11：
    /// - WorldUtility.Get_WCPT() : static → WorldComponent_PowerTracker。
    /// - WorldComponent_PowerTracker.AddRimWarFaction(Faction) : public instance void；
    ///   內含 CheckForRimWarFaction 防重複 + GenerateFactionBehavior + AssignFactionSettlements
    ///   （後者把已易主聚落納入新派系 RimWarData）→ 分裂後唯一需要的掛載點。
    /// - 不呼叫 WorldUtility.ConvertSettlement：其實體為 Destroy()→AddNewHome 摧毀重建，
    ///   與本案 in-place SetFaction（保留聚落/comp/哨站）衝突。
    /// - 母派系側免處理：RimWarData.WorldSettlements 為自癒式 getter（到期 Clear+重掃
    ///   Find.WorldObjects 按派系過濾），倒戈聚落於下個更新週期自動脫離母派系清單；
    ///   RimWarSettlementComp 隨 WorldObject 留存，戰力點不歸零。</summary>
    [StaticConstructorOnStartup]
    public static class RimWarBridge
    {
        public static readonly bool RimWarPresent;

        private static readonly MethodInfo getPowerTracker;     // WorldUtility.Get_WCPT()
        private static readonly MethodInfo addRimWarFaction;    // PowerTracker.AddRimWarFaction(Faction)
        private static bool warned;

        static RimWarBridge()
        {
            Type worldUtility = GenTypes.GetTypeInAnyAssembly("RimWar.Planet.WorldUtility");
            if (worldUtility == null)
            {
                return;   // Rim War 未安裝：零成本
            }
            Type powerTracker = GenTypes.GetTypeInAnyAssembly("RimWar.Planet.WorldComponent_PowerTracker");
            getPowerTracker = worldUtility.GetMethod("Get_WCPT", BindingFlags.Public | BindingFlags.Static);
            addRimWarFaction = powerTracker?.GetMethod("AddRimWarFaction", new[] { typeof(Faction) });
            if (getPowerTracker == null || addRimWarFaction == null)
            {
                Log.Warning("[faction-politics] Rim War 偵測到但簽名不符（版本差異），bridge 維持 no-op；"
                    + "分裂新派系將依 Rim War 週期自檢納管。");
                return;
            }
            RimWarPresent = true;
            PoliticsBridges.FactionSplit += OnFactionSplit;
            Log.Message("[faction-politics] Rim War bridge 已綁定（分裂→AddRimWarFaction）。");
        }

        /// <summary>分裂完成（聚落已易主、hidden 已揭示）後把新派系註冊進 Rim War。</summary>
        private static void OnFactionSplit(Faction mother, Faction newFaction)
        {
            try
            {
                object tracker = getPowerTracker.Invoke(null, null);
                if (tracker != null)
                {
                    addRimWarFaction.Invoke(tracker, new object[] { newFaction });
                }
            }
            catch (Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning("[faction-politics] Rim War 同步失敗（僅首次記錄）：" + e);
                }
            }
        }
    }
}
