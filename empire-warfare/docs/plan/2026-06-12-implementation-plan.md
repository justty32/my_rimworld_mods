# empire-warfare 實作計畫（2026-06-12）

> 構想出處：`_ideas/2026-06-12-empire-rimwar-war-cluster.md` Mod 2（含「2026-06-12 讀源碼修正」段——範圍縮小為**改寫敗戰後果＋目標性驗證**）。
> 上游分析：`pas/analysis/rimworld_mods/empire-refactored/`（bridge walkthrough / extension_points / tutorial）、`pas/analysis/rimworld_mods/rim-war/details/target_selection_and_arrival.md`。

## 0. 範圍界定（聚焦）

Empire Refactored 的官方 Patch-RW 橋接已提供：附庸可見性、點數漏斗、warband 攻擊導流進 Empire 防守流程（1 天預告＋自動/手動防守）、奪城防衛（`Patch_ConvertSettlement` prefix 折點數不易主）。本 mod 只做：

1. **附庸可淪陷（核心）**：Empire 防守判定失敗 → 附庸正規退場、原 tile 建攻方 NPC 聚落、註冊進 RimWarData。
2. **節奏調校**：淪陷開關／判定門檻／保護期 ModSettings；vassalHeat 閘暴露（每日衰減旋鈕）。
3. **奪回閉環（輕量）**：既有 `MilitaryJobHandler_Capture` 奪回 + 「收復」信件。
4. **（選做）**`IBattleModifier` 接點 stub（附庸防守加成預留，不做內容）。

不做：新 UI、馳援系統（Patch-RW 已有）、哨站互動（Mod 1/3 範疇）。

## 1. 攔截點決策（核心問題）

### 1.1 防守失敗的權威路徑（讀 Empire 核心源碼確認）

```
RimWar warband 抵達附庸
└─ RimWar IncidentUtility.ResolveWarObjectAttackOnSettlement   (RW:10340)
   └─ Empire Patch-RW prefix → MilitaryUtilFC.AttackPlayerSettlement (Core Military/MilitaryUtilFC.cs:12)
      └─ FCEvent settlementBeingAttacked（1 天倒數）
         └─ FCEventMaker.ProcessEvents → MilitaryComp.StartDefence (Comps/SettlementMilitary.cs:741)
            ├─ 自動結算：SimulateBattleFc.FightBattle → EndBattle(won, remaining, battleResult)
            └─ 手動地圖戰：…… → EndAttack → EndBattle(won, remaining, null)
               └─ EndBattle (SettlementMilitary.cs:1183)
                  ├─ 敗：LoseBattle()（折繁榮/忠誠/拆建築/降級——現狀的「不易主」後果）
                  └─ LifecycleRegistry.InvokeOnBattleResolved(WorldSettlement,
                         MilitaryJobDefOf.DefendFriendlySettlement, won, battleResult)   ← ★攔截點
```

### 1.2 選定：`ILifecycleParticipant.OnBattleResolved`（B-1 契約層，零 Harmony 對 Empire）

候選比較：

| 候選 | 評估 |
|---|---|
| (a) postfix `EndBattle`／`LoseBattle` | 可行但是 B-2（Harmony 打 Empire 核心），版本脆弱 |
| (b) 接管 `Patch_ConvertSettlement` | **錯誤路徑**——Patch-RW 的 `Patch_ResolveWarObjectAttack` prefix 已把附庸攻擊整條導流進 Empire 事件流，RimWar 的 `ResolveCombat_Settlement → ResolveBattle_Settlement → ConvertSettlement` 鏈對附庸**根本不會跑**（且 RW:11151 Vassal 永不佔領寫死在 ResolveBattle_Settlement，更上游就被導流了）。`Patch_ConvertSettlement` 只是 belt-and-suspenders，保留原樣即可 |
| (c) **`LifecycleRegistry` 的 `OnBattleResolved` 回呼** | Empire 官方擴充面（extension_points B-1 首選）；`EndBattle` 是自動與手動戰的唯一匯流點，且帶 `MilitaryJobDefOf.DefendFriendlySettlement` 標記與 `BattleResult`；單參與者 try/catch 不炸鏈 |

→ 選 (c)。與 `Patch_ConvertSettlement` 的共存方式＝**不碰它**：我們的易主發生在 Empire 事件流內部結算之後，RimWar 的 ConvertSettlement 對附庸依然被官方 prefix 短路（兜底防複製聚落），兩者無交集、無 Harmony 順序問題。

過濾條件：`job == MilitaryJobDefOf.DefendFriendlySettlement && !victory`（`EndBattle` 固定傳此 job；攻方作戰 `ProcessMilitaryEvent` 傳 capture/raid/enslave，不會誤觸）。

### 1.3 唯一的 Harmony patch：RimWar 攻擊標記（postfix）

要區分「RimWar 行軍部隊的攻擊」與「Empire 原生襲擊事件」（兩者都匯入同一個 EndBattle），在
`IncidentUtility.ResolveWarObjectAttackOnSettlement` 加 **postfix**：defender.parent 是 `WorldSettlementFC` 時記錄（tile→攻方派系/tick）標記。

- 與 Empire 官方 prefix 共存：Harmony 語意下 prefix return false 只跳過**原方法**，postfix 照常執行——不互踩。
- 簽章探測 fail-soft：`AccessTools.Method` 找不到目標 → 警告＋降級（喪失「只認 RimWar 攻擊」過濾，淪陷判定照常但不過濾來源）。

## 2. 淪陷流程設計

### 2.1 判定（OnBattleResolved，敗戰時）

1. 總開關 `enableVassalFall`（預設開）。
2. 來源過濾 `onlyRimWarAttacks`（預設開）：tile 需有未過期（5 天內）的 RimWar 攻擊標記。
3. 失敗計數：每敗 `failStreak[tile]++`，防守成功歸零。
4. 淪陷條件（任一）：
   - **徹底潰敗**（預設啟用）：自動結算 `BattleResult.winner == Attacker` 且攻方殘餘比例 `attackerRemaining/attackerInitial ≥ crushingAttackerRemainRatio`（預設 0.5）；手動地圖戰敗（result==null、防守者全滅）視同徹底潰敗。
   - **連續失敗**：`failStreak ≥ consecutiveFailuresForFall`（預設 0＝停用）。
5. 保護期：`now - 聚落建立tick < protectionDays`（預設 15 天；建立 tick 由 `OnSettlementCreated` 記錄，未知者回退 mod 啟用 tick——涵蓋「開局/舊檔加 mod/新附庸」三種保護）。受保護時發中性訊息、不淪陷。
6. 攻方派系驗證：非 null、非玩家、非 PColony（`FactionCache.IsPlayerColonyFaction`）、未 defeated。

判定成立 → 排入 `pendingFalls`，**延後到 WorldComponent 下一 tick 執行**（避免在 `LifecycleRegistry` 迭代中移除聚落——回呼後它還會對 settlement 呼 `InvalidateStatCache`，且 `EndBattle`/`EndAttack` 後續仍觸碰 comp 狀態）。pendingFalls 可序列化（存檔點剛好落在中間也不丟）。

### 2.2 執行（WorldComponent tick）

1. 重新驗證：聚落仍在 `FactionCache.FactionComp.settlements`、無開啟中的地圖、攻方派系仍有效。
2. 取退場前點數：`GetComponent<RimWarSettlementComp>().RimWarPoints`（走 Empire 的 `Patch_RimWarPoints` getter，即軍事+經濟實力）。
3. **正規退場**：`ColonyUtil.RemovePlayerSettlement(settlement)`——Empire 唯一的聚落移除 API（聚落視窗「放棄」按鈕同款），內含：
   - `SettlementTypeExtension.PreDestruction` → `settlement.PrepareDestroy()`（設 destroyFlag）
   - `LifecycleRegistry.InvokeOnSettlementRemoved`（通知所有參與者）
   - `faction.settlements.Remove` ＋ 孤兒 Bill 清理 ＋ `DirtyFactionProfitCache`/`DirtyAveragesCache`/roadBuilder 旗標
   - `Find.WorldObjects.Remove(...)` ＋ `MilitaryComp.ReturnMilitary` ＋ 相關 FCEvent 全清（稅務/軍事/UI 快取不殘留）
4. **建攻方 NPC 聚落**：`RimWar.Planet.SettlementUtility.AddNewHome(tile, attackerFaction, null)`（RimWar 自家包裝，RimCities 相容；不呼叫 `ConvertSettlement`——避開官方 prefix 與 destroyFlag 守衛，也不冒 `RemoveRWDFaction` 清掉 PColony 的險）。
5. **註冊進 RimWarData**：`WorldUtility.CreateRimWarSettlementWithPoints(rwdTo, newHome, points×0.6 clamp[500,100000], false, 0)` ＋ `rwdTo.rwdNextUpdateTick = now`（強制 RimWar 下次更新重建聚落清單）。
6. 紅字信件（NegativeEvent）：哪個附庸丟給了誰，LookTargets 指向新聚落。
7. 記錄淪陷 tile → 供收復偵測。

### 2.3 收復閉環

`OnSettlementCreated`（`MilitaryJobHandler_Capture` 勝利後走 `ColonyUtil.CreatePlayerColonySettlement` 必觸發）→ tile 在淪陷記錄中 → 發「收復失土」PositiveEvent 信件、清記錄。不做新 UI。

## 3. 目標性與節奏（vassalHeat 閘）

源碼事實（target_selection_and_arrival.md §4.2）：warband/launched/scout 選附庸為目標需 `PlayerHeat ≥ 目標comp.vassalHeat`（RW:18027 等），出手後 `vassalHeat += 2×heat` 遞增、**RimWar 不會衰減它** → 同一附庸被連打頻率天然遞減（防洗版），但長期會越來越少被打。

暴露手段：ModSetting `vassalHeatDecayPerDay`（預設 0＝RimWar 原生節奏）；>0 時 WorldComponent 每日對每個附庸的 `RimWarSettlementComp.vassalHeat`（public 欄位）扣減該值（地板 0）→ 附庸更常被選為目標。fail-soft：try/catch 包覆，欄位異動只記警告。

## 4. ModSettings 清單

| 欄位 | 型別/範圍 | 預設 | 意義 |
|---|---|---|---|
| `enableVassalFall` | bool | true | 附庸可淪陷總開關 |
| `onlyRimWarAttacks` | bool | true | 僅 RimWar 行軍部隊的攻擊會觸發淪陷 |
| `crushingAttackerRemainRatio` | float 0.1–1.0 | 0.5 | 徹底潰敗判定：攻方殘餘戰力比例門檻 |
| `consecutiveFailuresForFall` | int 0–10 | 0（停用） | 連續防守失敗 N 次亦淪陷 |
| `protectionDays` | int 0–60 | 15 | 開局/新附庸保護期（天） |
| `vassalHeatDecayPerDay` | int 0–50 | 0 | 附庸熱度每日衰減（>0 更常被打） |

## 5. 工程結構

```
empire-warfare/
├── About/About.xml                  packageId pas.empire.warfare；modDependencies Harmony+Empire+RimWar；loadAfter 同
├── 1.6/Assemblies/EmpireWarfare.dll （建置產物，隨倉庫提交）
├── Source/
│   ├── EmpireWarfare.csproj         net48、Krafs.Rimworld.Ref 1.6.*、HintPath 引 Empire/Empire.RW/RimWar/0Harmony 全 Private=False
│   ├── WarfareInit.cs               [StaticConstructorOnStartup]：簽章探測+手動 Patch、Registry 註冊、EmpireCacheUtil.RegisterCacheInvalidator 重註冊防護
│   ├── WarfareMod.cs                Mod + ModSettings + 設定 UI
│   ├── WarfareLifecycleHooks.cs     LifecycleParticipantBase：OnBattleResolved / OnSettlementCreated / OnSettlementRemoved
│   ├── Patch_RecordWarObjectAttack.cs  postfix 攻擊標記
│   ├── VassalFallUtility.cs         淪陷執行（退場+建城+註冊+信件）
│   ├── WorldComponent_WarfareTracker.cs 狀態（標記/失敗計數/建立tick/淪陷記錄/pendingFalls）+ 每日 heat 衰減
│   └── VassalDefenseBattleModifier.cs  IBattleModifier stub（接點預留）
├── Languages/{English,ChineseTraditional}/Keyed/EmpireWarfare.xml
├── docs/plan/（本文件）、PROJECT.md、session_log.md
└── tests/healthcheck.py             離線靜態健檢
```

防衛式守則（照 Empire tutorial 陷阱表）：
- Registry 只在 StaticConstructorOnStartup 註冊會被 `Game.ClearCaches` 清空 → 配 `EmpireCacheUtil.RegisterCacheInvalidator` 重註冊。
- 所有 Reference `Private=False`。
- Harmony 手動 patch + 簽章探測，找不到目標降級不炸。
- 回呼/Patch 內全 try/catch；PColony 判斷用 `FactionCache.IsPlayerColonyFaction`。

## 6. 驗收

- `dotnet build -c Release` 0 error 0 warning。
- `tests/healthcheck.py` 通過（XML well-formed、packageId/相依宣告、翻譯 key 對齊、C# 引用 key 存在、csproj 不變式、DLL 存在）。
- E2E 清單寫進 PROJECT.md（warband 實測步驟含 dev 工具路徑）。
