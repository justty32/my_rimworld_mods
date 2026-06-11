using System;
using System.Reflection;
using System.Text;
using Verse;

namespace pas.politics
{
    /// <summary>Rim War 軟相容骨架。以型別存在性偵測（不猜 packageId）；P1 不綁定呼叫，
    /// 僅 dump ConvertSettlement 簽名供校準（使用者提供 Rim War 檔案後完成綁定）。</summary>
    [StaticConstructorOnStartup]
    public static class RimWarBridge
    {
        public static readonly bool RimWarPresent;

        static RimWarBridge()
        {
            Type worldUtility = GenTypes.GetTypeInAnyAssembly("RimWar.Planet.WorldUtility");
            if (worldUtility == null)
            {
                return;   // Rim War 未安裝：零成本
            }
            RimWarPresent = true;
            DumpSignatures(worldUtility);
        }

        private static void DumpSignatures(Type worldUtility)
        {
            StringBuilder sb = new StringBuilder("[faction-politics] Rim War 偵測到。ConvertSettlement 候選簽名（供 bridge 校準）：");
            MethodInfo[] methods = worldUtility.GetMethods(BindingFlags.Public | BindingFlags.Static);
            int found = 0;
            foreach (MethodInfo method in methods)
            {
                if (method.Name == "ConvertSettlement")
                {
                    sb.AppendLine().Append("  ").Append(method);
                    found++;
                }
            }
            if (found == 0)
            {
                sb.AppendLine().Append("  （無——Rim War 版本差異，bridge 維持 no-op）");
            }
            sb.AppendLine().Append("[faction-politics] 易主同步未綁定（待校準）；原版 SetFaction 已先行，Rim War 戰力資料可能滯後至其週期自檢。");
            Log.Message(sb.ToString());
        }
    }
}
