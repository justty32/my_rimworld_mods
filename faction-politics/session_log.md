# Session Log

## 2026-06-11

### Task 0: API 殘餘驗證 —— 4/8 經 pas/analysis/rimworld 核對，4/8 留待本機

無反編譯源，改以 `pas/analysis/rimworld`（分析文件，多帶「核對 2026-06-01」日期標記）交叉驗證：

- **#2 已驗** `TryAffectGoodwillWith(Faction, int, canSendMessage:bool, canSendHostilityLetter:bool)`：`details/optimized_outpost_core.md:102-104`「核對 2026-06-01」明指第二參數是 **int**。FactionSplitter 餵 `GoodwillToMakeHostile(int)` + 兩具名 bool 的呼叫形正確。
- **#5 已驗** `WorldPawns.PassToWorld(pawn, KeepForever)`：`tutorial/persistent_world_pawns.md:20`。
- **#6 已驗** `WorldPawns.RemoveAndDiscardPawn(Pawn)` public 安全棄置：`persistent_world_pawns.md:132,153`。→ **解鎖 Task 4 註記**：FactionSplitter 已加殘留自動首領的防御式棄置（guard `Contains`）。
- **#8 已驗** `LetterStack.ReceiveLetter(label, text, def[, lookTarget])`：`details/` 多個範例（lookTarget 收 Pawn/WorldObject，Settlement 適用）。

仍待本機反編譯源/建置：**#1** `GoodwillToMakeHostile` 回傳型別（強烈推斷 int，因餵入 #2 的 int 參數）、**#3** `FactionGeneratorParms(FactionDef, IdeoGenerationParms, bool)` 建構子、**#4** `PawnGenerationRequest(PawnKindDef, Faction, PawnGenerationContext)` 最簡建構形、**#7** `FactionDef.basicMemberKind` 欄位。不符就地修。

### Task 1–9: 程式碼實作完成（建置延後）

照計畫逐 task 建檔，**未跑 `dotnet build`**（本環境無 dotnet/mono）。`tests/healthcheck.py` 在此實跑 → **healthcheck OK**。各 C# 檔行數（≤200 確認）：
RebelRecord 29 / OutpostsBridge 32 / PoliticsBridges 36 / RimWarBridge 46 / RebellionProfileDef 61 / Resolver 64 / RebelSpawner 82 / FactionSplitter 92 / WorldComponent_RebellionTracker 187。

與計畫的偏差：

1. **Task 0 經分析文件核對 4/8、套用 #6 首領棄置；建置與殘餘 4 項留本機**（環境無 dotnet、無反編譯源）。下次本機：補核 #1/#3/#4/#7 grep（不符就地修），再 `dotnet build Source/` 與 `SourceBridgeOutposts/` 雙綠。
2. **`Compat/NpcOutposts/Assemblies/` 補 `.gitkeep`**：bridge DLL（`FactionPoliticsOutpostsBridge.dll`）尚未建置；先建資料夾使 loadFolders `IfModActive` 條目與健檢第 3 檢成立。本機建 bridge csproj 後 DLL 落此。
3. WorldComponent 無需 Def 註冊（遊戲自動實例化所有 `WorldComponent` 子類），沿 npc-outposts 慣例。

### 待辦

- 本機補 Task 0 → 雙 `dotnet build` 綠 → 提交 DLL（repo 慣例：建置產物進 git）。
- Task 10 實機 E2E（`docs/plan/task-10-e2e.md`）：開局/舊檔補發 → 拜訪見本人 → 擊殺歸零重生 → 達標分裂（letter/新派系/聚落+哨站易主/母敵對）→ 存讀檔 → 上限觸頂 → 無 RimWar/無 outposts 環境 log 乾淨。
- Rim War bridge 校準：使用者供 Rim War DLL → 跑一次取 `ConvertSettlement` 簽名 dump → 完成綁定。
