# Session Log

## 2026-06-11

### Task 0: API 殘餘驗證 —— 7/8 經 pas 分析素材核對，#3 部分待建置

無反編譯源，改以 `pas/analysis/rimworld`（帶「核對 2026-06-01」標記）+ `pas/derived/rimworld-mod-guide`（專題頁附程式碼）交叉驗證：

- **#1 已驗** `int delta = GoodwillToMakeHostile(faction)` → 回傳 **int**；`rimworld-mod-guide/html/31-factions.html:333,336` 並述「算負增量→餵 TryAffectGoodwillWith」正是 FactionSplitter 寫法。
- **#2 已驗** `TryAffectGoodwillWith(Faction, int, canSendMessage:bool, canSendHostilityLetter:bool)`：`details/optimized_outpost_core.md:102-104`「核對 2026-06-01」第二參數 **int**。
- **#4 已驗** `PawnGenerationRequest(kind, faction, context, ...)` 前三位置參數＝kind/faction/context：`11-pawn-generation.html:179`。guide 強烈建議具名引數防跨版本錯位 → RebelSpawner 已改具名。
- **#5 已驗** `WorldPawns.PassToWorld(pawn, KeepForever)`：`persistent_world_pawns.md:20`。
- **#6 已驗** `WorldPawns.RemoveAndDiscardPawn(Pawn)` public：`persistent_world_pawns.md:132,153`。→ FactionSplitter 已加自動首領防御式棄置。
- **#7 已驗** `FactionDef.basicMemberKind : PawnKindDef`：`31-factions.html:144`。
- **#8 已驗** `LetterStack.ReceiveLetter(label, text, def[, lookTarget])`：`details/` 多範例（Settlement/Pawn 皆可）。

**#3 部分**：guide 只示範 1 參 `new FactionGeneratorParms(def)` + 1 參 `NewGeneratedFaction(parms)`（`31-factions.html:401`）；本案用 3 參 `(def, IdeoGenerationParms, hidden:true)` + 2 參 `NewGeneratedFaction(PlanetLayer, parms)`，源自 feasibility 的 vanilla 用例 `BackCompatibility.cs:424` / `FactionGenerator.cs:130`，guide 未交叉證實 → 3 參 `hidden` 多載待建置最終確認（不符則改 1 參 + 物件初始器設 hidden 欄位）。

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
- Rim War bridge 校準：經 `pas/analysis/rimworld_mods/rim-war` 核對，`WorldUtility.ConvertSettlement(:15289)` 為「摧毀重建」式易主，**與本案 in-place SetFaction 衝突**，校準應走 `RimWarSettlementComp.RimWarPoints` 調和（public setter）而非呼叫 ConvertSettlement。骨架 dump 已加此註記；實際綁定仍待使用者供 Rim War DLL 做 E2E。
