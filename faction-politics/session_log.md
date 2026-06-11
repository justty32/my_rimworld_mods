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

### Rim War bridge 校準完成（晚間）

Rim War v1.6 實體就在本機 workshop（`2222935097`，Torann.RimWar）→ 免等使用者供檔，ikdasm 直接核對簽名完成綁定：

- **掛載點只需一個**：`PoliticsBridges.FactionSplit` → `WorldUtility.Get_WCPT()` + `WorldComponent_PowerTracker.AddRimWarFaction(Faction)`（public instance）。後者內建 `CheckForRimWarFaction` 防重複、`GenerateFactionBehavior`、`AssignFactionSettlements`（把已易主聚落納入新派系 RimWarData）。
- **母派系側免處理**：IL 核實 `RimWarData.WorldSettlements` 為自癒式 getter——到期 `Clear()` + 重掃 `Find.WorldObjects` 按派系過濾。in-place SetFaction 於下個更新週期自動被接收；`RimWarSettlementComp` 隨 WorldObject 留存、戰力點不歸零。
- **確證不呼叫 `ConvertSettlement`**：IL 實體為 `Destroy()` → `SettlementUtility.AddNewHome` 摧毀重建（與先前 pas 分析一致），與 in-place 路線衝突。
- `SettlementDefected` hook 對 Rim War 維持無訂閱（自癒已覆蓋）；簽名不符版本退化為 no-op + 一次性警告。建置綠、healthcheck OK。

### Visit Settlements 相容校準 + forced-keep 退化 bug 修復（晚間）

使用者指定「拜訪見本人」的參考 mod：**Visit Settlements**（workshop `3535955435`，`NinaGoblin.VisitSettlements`，alt44s fork，1.6）。ikdasm 其 DLL + 遊戲本體 `Assembly-CSharp.dll`（實體 IL，非 ref）交叉核對：

- **見本人機制成立**：VS 拜訪 gizmo → `GetOrGenerateMapUtility.GetOrGenerateMap(tile, null, null)` = 原版聚落生成路徑。1.6 實體 IL 證實 `PawnGenerator.GeneratePawn` 對「請求 tile 上的聚落且 `previouslyGeneratedInhabitants` 非空」會優先 redress 名單內 world pawn 入圖（`GetValidCandidatesToRedress` ∩ 名單 → `RedressPawn` + `WorldPawns.RemovePawn`）。注意 redress 需 kind/faction 匹配（反叛者＝basicMemberKind ✓）且按權重隨機——非逐一保證，但名單通常僅反叛者一人。
- **地圖生命週期自洽**：VS 快取拜訪地圖（`WorldComponent_SettlementData.settlementMaps`），殖民者在場時 prefix 擋 `MapDeiniter.Deinit`；全員離場才移除快取放行原版 Deinit。tracker「rebel.Spawned → 分裂凍結」與此自洽，無死鎖。VS 另 patch `SettlementDefeatUtility.CheckDefeated`，拜訪中擊殺不會誤判聚落被攻滅 → 擊殺重生測試安全。
- **修復 forced-keep 退化（真 bug，原版層級）**：redress 的 `WorldPawns.RemovePawn` 會連帶把 pawn 移出 `pawnsForcefullyKeptAsWorldPawns`；地圖 Deinit 以 `Decide` 模式回世界 → **拜訪一次後反叛者失去 KeepForever**，之後可能被 WorldPawnGC 悄悄回收（進度歸零、換人）。修復：`Heal()` 對「在世界名單但不在 forced-keep」者補 `Find.WorldPawns.ForcefullyKeptPawns.Add(rebel)`（public getter 回傳活集合；`PassToWorld(KeepForever)` 內部即此 Add，冪等零副作用）。建置綠、healthcheck OK。

### Empire Refactored 軟相容：PColony 停用 patch（晚間）

使用者指定 Empire Refactored（workshop `3701480464`，`Matathias.Empire` 1.3.74，含 PDB）。ikdasm 核對發現 **P1 地雷**：

- `PColony` FactionDef（玩家附庸帝國專屬派系）繼承 `OutlanderFactionBase` → humanlike、非 hidden；Empire 執行期 `FactionManager.Add` 建立、**不設 hidden/temporary** → 通過本案 `Eligible()`。
- `WorldSettlementFC : Settlement`（IL 核實）→ 會被 `CountSettlements`/`OwnedNonSatellite` 計數 → tracker 會給玩家帝國養反叛者、達標後分裂其聚落給新敵對派系。
- **修補（零-Harmony、純 XML）**：`Compat/Empire/Patches/PColonyDisabled.xml` 以 `PatchOperationAddModExtension` 對 PColony 掛 `PoliticsDisabledExtension`（解析鏈第一關 Disabled→null，整派系停用）；`loadFolders` 加 `IfModActive="Matathias.Empire"`。healthcheck OK。
- 反向確認：Empire 未 patch `FactionManager.Add`（IL 中 "Add" 字串皆為 debug 顯示），我們分裂生成的新 NPC 派系對它是普通派系，無干擾。
- P2/P3 紅利：實裝版含 **Empire.pdb**，`docs/p2-p3-references.md` §5 已補記（四維狀態/FCEventDef 設計可直接對照真實符號）。

### Rim War 反方向審計 + 母派系即時重掃（晚間）

使用者再點名 Rim War（workshop `2222935097`）。bridge 我方→Rim War 方向已於稍早校準；本輪補**反方向**審計（Rim War 事件打到我們的 record）：

- Rim War 攻城易主走 `ConvertSettlement`（全 DLL 唯一呼叫點）＝摧毀重建 → 若摧毀反叛者駐地，`Heal()` 的 `homeSettlement.Destroyed → PickHome` 自癒 ✓；反叛者本體是 forced-keep world pawn 不受累 ✓。
- Rim War 滅派系（`RemoveRWDFaction`）→ `faction.defeated` → tracker 移除 record ✓。自癒鏈完備，反方向零修改。
- **bridge 補強**：`RimWarData.rwdNextUpdateTick` 為 **public 欄位**（IL :3706）→ 分裂後反射設 0，母派系下次存取 `WorldSettlements` 即重掃，消除滯後窗口（Rim War 自家 `ConvertSettlement` 對 `rwdFrom` 同此手法）。選配綁定：缺欄位僅退化為等週期自檢，不擋主綁定。建置綠、healthcheck OK。

### E2E 首輪（使用者 310-mod 全包）：兩缺陷回報 + 心跳隔離修復（晚間）

使用者實測回報：(1) 只有一個派系發蠢動信；(2) sims-mode 拜訪聚落生成**空地圖**（無建築無 pawn）。Player.log 取證：

- log 在首張地圖生成時觸頂截斷（`Reached max messages limit`），之後全瞎——含後續心跳例外與拜訪地圖生成記錄。
- 第三方干擾證據：`GenStep_ScatterLumpsMineable` NRE（礦物 mod def 缺漏）；`QuestEditor_Library.Patch_ExtraGenStepDefs`（`hailuan.customquestframework`）by-ref prefix 改寫 `GenerateContentsIntoMap` 的 genstep 清單；`Found no good central spot…numStand=343` + 大量 region 失敗。
- **(1) 主嫌（已修）**：`EnsureRebels` foreach 無逐派系隔離——第一派系成功發信後，第二派系 pawn 生成丟例外（大包基因/種族 mod 常見）即中斷整個迴圈，且 log 截斷掩蓋。修復：`EnsureRebels` 逐派系、心跳逐 record try-catch，同 defName 一次性具名警告（`WarnOnce`）。壞派系不再拖垮其餘。
- **(2) 暫判第三方**：原版 IL 證實 genstep 錯誤逐步 catch 不中斷、`Settlement.MapGeneratorDef`＝`Base_Faction` 路徑正常；空地圖更像 quest framework 改清單或礦物 NRE 連鎖。待最小 modlist 復測分流。
- 新增 dev 工具 `PoliticsDebugActions.DumpRebellionState`（Debug actions → pas.politics）：逐派系列出 tracked/skip 原因（player/hidden/defeated/temporary/non-humanlike/disabled/no-profile/聚落數/basicMemberKind）＋record 細節（progress/home/spawned/world/forcedKeep/inhabitantsList）。tracker 227 行（超 200 行慣例，接受）。
- 測試加速參數僅存 staging（progressPerDay 24、respawnDelayDays 0.5），repo 保持正式值；**每次 rsync 部署後須重套**。

### E2E 二輪：dump 定案「一封信」根因 = basicMemberKind 原版盲點（已修）

使用者跑 dump（Quick Test 世界）：除鼠族 mod 派系（Rakinia，被正常追蹤且 world/forcedKeep/inhabitantsList 全 True ✓）外，**所有原版 NPC 派系 `skip: basicMemberKind null`**——grep 原版 Data 證實 1.6 只有玩家派系 def 填 `basicMemberKind`（Colonist/Tribesperson），NPC 派系全靠 `pawnGroupMakers`。Task 0 #7 只驗了欄位存在、沒驗 NPC def 有填值。

- **修復**：`RebelSpawner.GeneratePawn` 改 `Faction.RandomPawnKind()`（實體 IL 核對：彙整 pawnGroupMakers 全部 Humanlike 選項隨機，後備 basicMemberKind）；dump 工具判定同步更新。
- 同輪修 **npc-outposts 真 bug**：`NpcOutpost` 靜態貼圖欄位於世界生成背景執行緒觸發 cctor 載圖而炸（Quick Test 紅字）→ 補 `[StaticConstructorOnStartup]`（npc-outposts commit `cffedc8`）。
- 使用者證實先前「空地圖」為誤判：敵對派系本就只有進攻選項；中立以上拜訪入圖正常（建築/pawn/作息/守衛皆對，sims-mode 生活 genstep 工作正常）。
- 既有測試檔免重開：修復部署後下次心跳 EnsureRebels 會給其餘合格派系補發（一波蠢動信）。

### E2E 三輪：RandomPawnKind 修復驗證 ✓ + 見本人 ✓ + sims-mode 拜訪摧毀 bug（已修）

- dump 二跑：**全部合格派系 tracked**（原版 Outlander/Tribe/Empire/海盜/多個 mod 派系），隱藏派系正確跳過。鼠國 record 顯示 `spawned=True world=False forcedKeep=False`＝反叛者正被 redress 在拜訪圖上（forced-keep 暫時清除為預期，離場後 Heal 補回）。
- **見本人 ✓**：使用者拜訪駐地找到同名反叛者。P1 無互動選項為設計（殺=鎮壓；勸誘/支持是 P2 loyalty 範疇）——已向使用者說明。
- **sims-mode 真 bug（已修 `aaf2d73`）**：拜訪生成出空圖（無該派系 pawn）時，原版 `Settlement.TickInterval → CheckDefeated` 把拜訪判成攻陷→聚落摧毀（使用者盟友聚落實測陣亡）。VS 用 Harmony prefix 擋；sims-mode 零-Harmony 改用**入場防護**：生成後圖上無該派系人形 → `DeinitAndRemoveMap` 回收 + message 撤銷進場（`pas_sims_VisitAborted`，雙語）。
- 空圖根因仍待定位：高度懷疑該盟友是 TradersGuild（虛空企業，駐地「宇宙碼頭」＝Odyssey 軌道層特殊聚落，生成器非 Base_Faction）。防護已覆蓋此類；若證實，可考慮 P1.1 對特殊層聚落直接不給拜訪選項。

### E2E 四輪：鎮壓循環 ✓ + 分裂全套 ✓

- **鎮壓循環 ✓**：使用者殺 Rakinia 反叛者 → dump 數學自證（全派系 progress=30、Rakinia=15 ＝ 歸零+0.5 天冷卻+重生後 0.625 天累積）。繼任者咖啡豆三不變量全綠。已向使用者澄清兩個設計行為：重生靜默無信；新反叛者僅於地圖「重新生成」時 redress 登場（已生成地圖不會憑空出現）。
- **分裂全套 ✓**：使用者確認成功（分裂信/新派系/倒戈/敵對）。
- E2E 主線（清單 #1/#3/#4/#5）完成於 310-mod 全包環境。

### 待辦

- Task 10 實機 E2E（`docs/plan/task-10-e2e.md`）：開局/舊檔補發 → 拜訪見本人（裝 Visit Settlements `3535955435` + Harmony）→ 離場再訪（驗 forced-keep 修復：反叛者同一人、進度不歸零）→ 擊殺歸零重生 → 達標分裂（letter/新派系/聚落+哨站易主/母敵對）→ 存讀檔 → 上限觸頂 → 無 RimWar/無 outposts 環境 log 乾淨。本機 RimWorld 1.6.4850（Proton）+ Rim War + Visit Settlements workshop 齊備；faction-politics 與 npc-outposts 已部署至 `~/rimworld_mods/` 並 symlink 進遊戲 Mods。
- E2E 加驗 Rim War bridge：啟動 log 應見「Rim War bridge 已綁定」；分裂後新派系應出現於 Rim War 派系資料。
- E2E 加驗 Empire 相容：啟用 Empire Refactored 建附庸聚落 → dev mode 確認 PColony 無 RebelRecord（tracker 不追蹤）、分裂候選不含 PColony 聚落。
