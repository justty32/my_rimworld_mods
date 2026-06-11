# Session Log

## 2026-06-11

### Task 0: API 殘餘驗證 —— 延後至 Windows 本機

本次於 Linux 雲端環境執行，**無反編譯源**（`pas/projects/rimworld` 不存在）→ Task 0 的 8 項 grep 簽名核對無法在此進行。實作照計畫已驗證的主鏈座標（feasibility + 計畫索引表）撰寫；殘餘 8 項簽名核對與「不符就地修」留待本機。重點待核：`PawnGenerationRequest` 最簡建構形（#4）、`TryAffectGoodwillWith`/`GoodwillToMakeHostile` 參數形（#1/#2）、`FactionGeneratorParms(FactionDef, IdeoGenerationParms, bool)` 建構子（#3）、`Faction.defeated`/`basicMemberKind` 可見性（#5/#7）。

### Task 1–9: 程式碼實作完成（建置延後）

照計畫逐 task 建檔，**未跑 `dotnet build`**（本環境無 dotnet/mono）。`tests/healthcheck.py` 在此實跑 → **healthcheck OK**。各 C# 檔行數（≤200 確認）：
RebelRecord 29 / OutpostsBridge 32 / PoliticsBridges 36 / RimWarBridge 46 / RebellionProfileDef 61 / Resolver 64 / RebelSpawner 82 / FactionSplitter 87 / WorldComponent_RebellionTracker 187。

與計畫的偏差：

1. **建置與 Task 0 簽名核對全部延後本機**（環境無 dotnet、無反編譯源）。下次本機：先補 Task 0 grep（任何不符就地修程式碼），再 `dotnet build Source/` 與 `SourceBridgeOutposts/` 雙綠。
2. **`Compat/NpcOutposts/Assemblies/` 補 `.gitkeep`**：bridge DLL（`FactionPoliticsOutpostsBridge.dll`）尚未建置；先建資料夾使 loadFolders `IfModActive` 條目與健檢第 3 檢成立。本機建 bridge csproj 後 DLL 落此。
3. WorldComponent 無需 Def 註冊（遊戲自動實例化所有 `WorldComponent` 子類），沿 npc-outposts 慣例。

### 待辦

- 本機補 Task 0 → 雙 `dotnet build` 綠 → 提交 DLL（repo 慣例：建置產物進 git）。
- Task 10 實機 E2E（`docs/plan/task-10-e2e.md`）：開局/舊檔補發 → 拜訪見本人 → 擊殺歸零重生 → 達標分裂（letter/新派系/聚落+哨站易主/母敵對）→ 存讀檔 → 上限觸頂 → 無 RimWar/無 outposts 環境 log 乾淨。
- Rim War bridge 校準：使用者供 Rim War DLL → 跑一次取 `ConvertSettlement` 簽名 dump → 完成綁定。
