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

### 補充：P2/P3 設計參照落檔（`docs/p2-p3-references.md`）

掃 `pas/analysis/rimworld_mods`：**#3 的 `FactionGeneratorParms` 原始呼叫無命中**（mod 分析皆高階剖析）→ #3 確定留本機。但 P2/P3 參照豐收，已落 `docs/p2-p3-references.md`：
- **faction-territories** Vassalage：玩家 gizmo+對話框（`Settlement_GetGizmos_Vassalise:7742`）、藩屬點數貨幣（`VassalagePointsComponent:8496`）、聚落割讓 `ExecuteCedeToFactionAtTile:10878`（與本案 SetFaction 同類）。注意它攔毀城信件用 Harmony，本案零-Harmony 須改 gizmo/alert。
- **empire-refactored** 四維狀態 `unrest/loyalty/happiness/prosperity`（`WorldSettlementFC.cs:78-94`）+ `FCEventDef` → P2 可把單一 progress 升級為 loyalty 主軸 + def 化事件。
- **架構背書**：empire-refactored 9 個 `IfModActive` compat DLL 與本案 npc-outposts bridge 同款技法；warband-warfare League 同盟系統為 P3 參照。

### 本機補完：Task 0 殘餘 4 項全驗 + 雙建置綠（晚間）

dotnet 10 + Krafs.Rimworld.Ref 1.6.4850 ref 組件直驗（monop），不需反編譯源：

- **#3 完整確認**：3 參建構子實簽 `FactionGeneratorParms(FactionDef, IdeoGenerationParms, Nullable<bool> hidden)` —— 第三參數確為 `hidden`（nullable，`true` 隱式轉換 OK）；`NewGeneratedFaction(PlanetLayer, FactionGeneratorParms)` 多載存在。原碼免改。
- **#6 修正**：分析素材（persistent_world_pawns.md）記的 `RemoveAndDiscardPawn` 是舊名，1.6 實名 **`RemoveAndDiscardPawnViaGC(Pawn)`**（public）。FactionSplitter 已改。素材的「核對 2026-06-01」標記對此項不可靠，後續引用該檔需留意版本漂移。
- 另修 FactionSplitter 缺 `using UnityEngine;`（Mathf）。
- **`dotnet build` 雙綠**：`Source/` → `1.6/Assemblies/FactionPolitics.dll`、`SourceBridgeOutposts/` → `Compat/NpcOutposts/Assemblies/FactionPoliticsOutpostsBridge.dll`。0 警告 0 錯誤。編譯通過即驗畢 #1/#4/#7 簽名。`tests/healthcheck.py` 重跑 OK。DLL 依 repo 慣例入 git，`.gitkeep` 佔位移除。

### 待辦
- Task 10 實機 E2E（`docs/plan/task-10-e2e.md`）：開局/舊檔補發 → 拜訪見本人 → 擊殺歸零重生 → 達標分裂（letter/新派系/聚落+哨站易主/母敵對）→ 存讀檔 → 上限觸頂 → 無 RimWar/無 outposts 環境 log 乾淨。
- Rim War bridge 校準：經 `pas/analysis/rimworld_mods/rim-war` 核對，`WorldUtility.ConvertSettlement(:15289)` 為「摧毀重建」式易主，**與本案 in-place SetFaction 衝突**，校準應走 `RimWarSettlementComp.RimWarPoints` 調和（public setter）而非呼叫 ConvertSettlement。骨架 dump 已加此註記；實際綁定仍待使用者供 Rim War DLL 做 E2E。
