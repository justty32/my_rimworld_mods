# Task 6: RimWarBridge 反射骨架（偵測 + 簽名記錄 + 防呆 no-op）

**Files:**
- Create: `faction-politics/Source/Compat/RimWarBridge.cs`

背景（可行性 `04`）：Rim War DLL/反編譯源不在本機 → 無法編譯期引用，`ConvertSettlement`（rim-war 反編譯 :15289，public static）確切參數表無從查證。本 task 交付**誠實骨架**：型別偵測 + 簽名 dump 到 log（E2E 裝 Rim War 跑一次即得校準素材）+ 防呆 no-op。校準（實際綁定 + 掛 `PoliticsBridges.SettlementDefected`）等使用者提供 Rim War 檔案後一次小 commit 完成。

- [ ] **Step 1: Source/Compat/RimWarBridge.cs**

```csharp
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
```

- [ ] **Step 2: 建置驗證**

Run: `dotnet build C:\code\mine\my_rimworld_mods\faction-politics\Source\FactionPolitics.csproj`
Expected: 0 Warning(s) 0 Error(s)（`GenTypes.GetTypeInAnyAssembly` 存在性 task-0 #8 已驗）

- [ ] **Step 3: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add faction-politics/Source faction-politics/1.6
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: faction-politics Rim War 反射偵測骨架（簽名 dump 待校準）

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
