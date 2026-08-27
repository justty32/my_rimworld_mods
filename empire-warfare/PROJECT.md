# empire-warfare（帝國參戰）

RimWorld **1.6** mod。讓 Rim War 的世界戰爭真正打到玩家帝國頭上：Empire (Refactored) 附庸聚落防守失敗時可**真正淪陷**——正規退場、原 tile 變成攻方派系的 NPC 聚落並註冊進 Rim War 戰局；之後可用 Empire 既有「奪取聚落」作戰收復（征服迴圈閉環）。

- packageId：`pas.empire.warfare`
- 硬相依：Harmony（`brrainz.harmony`）＋ Empire Refactored（`Matathias.Empire`）＋ Rim War（`Torann.RimWar`）；loadAfter 同
- 構想出處：`../_ideas/2026-06-12-empire-rimwar-war-cluster.md`（Mod 2；範圍已縮小為「改寫敗戰後果＋目標性驗證」——防守/馳援/實體戰由 Empire 官方 Patch-RW 橋接提供）

## 目標 / 範圍

| 項目 | 內容 | 狀態 |
|---|---|---|
| 附庸可淪陷（核心） | 防守失敗（徹底潰敗或連續 N 敗）→ `ColonyUtil.RemovePlayerSettlement` 正規退場 → RimWar `SettlementUtility.AddNewHome` 建攻方聚落 → `CreateRimWarSettlementWithPoints` 註冊 → 紅字信件 | ✅ |
| ModSettings | 淪陷開關／來源過濾／潰敗門檻／連敗次數／保護期／vassalHeat 衰減 | ✅ |
| 目標性與節奏 | vassalHeat 閘行為確認＋每日衰減旋鈕暴露（見下） | ✅ |
| 奪回閉環（輕量） | 既有 `MilitaryJobHandler_Capture` 奪回；`OnSettlementCreated` 偵測淪陷 tile 重建 → 「收復失土」信件 | ✅（待 E2E） |
| 接點預留（選做） | `IBattleModifier` stub（`VassalDefenseBattleModifier`）佔位，未來做附庸防守加成 | ✅（空實作） |
| 不做 | 新 UI、馳援系統（Patch-RW 已有）、哨站互動（叢集 Mod 1/3） | — |

## 技術棧 / 架構

- **對 Empire 走契約層（零 Harmony）**：`LifecycleRegistry`（`OnBattleResolved`＝攔截點、`OnSettlementCreated/Removed`）＋ `BattleModifierRegistry`（stub）。`Game.ClearCaches` 會清空 Registry → 以 `EmpireCacheUtil.RegisterCacheInvalidator` 自動重註冊。
- **對 RimWar 僅一個 Harmony postfix**：`IncidentUtility.ResolveWarObjectAttackOnSettlement` 記錄攻擊標記（tile→攻方派系），與 Empire 官方 Patch-RW 的 prefix 共存（prefix skip 不影響 postfix 執行）。簽章探測 fail-soft：目標不存在則降級停用來源過濾、不炸。
- **淪陷判定**在 `OnBattleResolved`（敗戰），**執行**延後到 `WorldComponent_WarfareTracker` 下一 tick（與 Registry 迭代解耦；pendingFalls 可序列化）。
- 刻意**不呼叫** RimWar `WorldUtility.ConvertSettlement`：其 Destroy 會撞 Empire destroyFlag 守衛、被官方 `Patch_ConvertSettlement` prefix 短路，且可能 `RemoveRWDFaction` 清掉整個 PColony。
- 攔截點選型完整論證見 `docs/plan/2026-06-12-implementation-plan.md` §1。

### 檔案導覽

| 檔 | 職責 |
|---|---|
| `Source/WarfareInit.cs` | 入口：簽章探測＋手動 patch、Registry 註冊＋重註冊防護、載入錨點 log `[EmpireWarfare] loaded.` |
| `Source/WarfareMod.cs` | `Mod`＋`ModSettings`＋設定 UI |
| `Source/WarfareLifecycleHooks.cs` | Empire 生命週期回呼（防守敗戰／聚落建立移除）＋ `IBattleModifier` stub |
| `Source/Patch_RecordWarObjectAttack.cs` | RimWar 攻擊標記 postfix |
| `Source/WorldComponent_WarfareTracker.cs` | 狀態：標記／連敗計數／建立 tick（保護期）／pendingFalls／淪陷記錄；每日 vassalHeat 衰減 |
| `Source/VassalFallUtility.cs` | 淪陷執行（退場＋建城＋註冊＋信件） |

## ModSettings 一覽

| 設定 | 預設 | 說明 |
|---|---|---|
| 附庸可淪陷 | 開 | 總開關 |
| 僅 Rim War 部隊可奪城 | 開 | Empire 原生襲擊事件失敗仍只折損 |
| 徹底潰敗門檻 | 50% | 自動結算敗戰且攻方殘餘 ≥ 此比例 → 一次淪陷；手動地圖戰敗（守軍全滅）一律視同 |
| 連續失敗淪陷次數 | 0（停用） | 連敗 N 次亦淪陷；防守成功歸零 |
| 新附庸保護期 | 15 天 | 聚落建立或本 mod 加入存檔起算 |
| 附庸熱度每日衰減 | 0（原生） | >0 → 附庸更常被選為攻擊目標 |

## vassalHeat 閘（目標性驗證結論）

讀源碼確認（`rim-war/details/target_selection_and_arrival.md` §4）：warband／launched warband／scout 三條軍事路徑都會把附庸列為合法目標（`HostileTo`＋comp points>0），但需過 `PlayerHeat ≥ 目標comp.vassalHeat`（RW:18027 等）；每次出手後 `vassalHeat += 2×heat` 且 RimWar **不會衰減**它 → 附庸「能被打但不被洗版」，同一附庸被連打頻率天然遞減。本 mod 以 `vassalHeatDecayPerDay` 設定暴露此節奏（直接寫 public 欄位 `RimWarSettlementComp.vassalHeat`，try/catch 包覆）。

## 建置 / 健檢

```bash
cd Source && dotnet build -c Release   # 0 error 0 warning；輸出 1.6/Assemblies/EmpireWarfare.dll
python3 tests/healthcheck.py           # 離線靜態健檢
```

## 已知限制

1. **Empire 內部錯誤路徑視同潰敗**：`SetupAttack` 失敗等 bug 路徑會以 `EndBattle(false, 0, null)` 收場，與手動戰敗同形（result==null）——若該攻擊有 RimWar 標記且不在保護期，會觸發淪陷。機率極低，列為已知。
2. **攻擊標記窗口 5 天**：手動地圖戰拖過 5 天（極罕見）標記過期，該敗戰在「僅 RimWar 攻擊」模式下不會淪陷。
3. **Orbital 附庸**：淪陷後在原 PlanetTile 建原版 `Settlement`（pendingFalls 以 PlanetTile 保留 layer），NPC 聚落出現在軌道層的視覺/行為由 RimWar/原版自理，未深測。
4. 淪陷時 Empire 的 `RemovePlayerSettlement` 會另發一條「聚落移除」Message，與本 mod 紅字信件並存（保留 Empire 原語意）。
5. faction-politics 分裂出的新派系：理論上有 RimWarData 即可成為奪城方；註冊時序未實測。

## E2E 驗證清單（待實機）

部署：`ln -s ~/repo/my_rimworld_mods/empire-warfare <RimWorld>/Mods/empire-warfare`，排序在 Harmony/Empire/RimWar 之後。

1. **載入錨點**：log 出現 `[EmpireWarfare] loaded. attackMarker=on`；無紅字 HarmonyException。
2. **重註冊防護**：開新局→存檔→讀檔，再觸發一次戰鬥回呼仍有反應（防 ClearCaches 清空 Registry）。
3. **warband 真的會來打附庸**（目標性實測）：
   - 開新局（RimWar 開啟），建 1 個 Empire 附庸；確認附庸與某 NPC 派系敵對（必要時 dev 調 goodwill -100）。
   - Dev mode → RimWar 的 debug planet 工具對該敵對派系聚落執行 `AttemptWarbandActionAgainstTown`（forcePlayer 變體會挑玩家/附庸），或等待自然行動（注意 `preventActionsAgainstPlayerUntilTick` 開局保護與 vassalHeat 閘——把本 mod「附庸熱度每日衰減」拉高可加速）。
   - 驗證：warband 抵達附庸 → 出現 Empire 的 1 天「即將被攻擊」事件（證明 Patch-RW 導流）＋ 本 mod 攻擊標記（log）。
4. **淪陷路徑**：設定門檻調低（潰敗 10%／保護期 0），讓防守必敗（撤掉駐軍）→ 1 天後自動結算敗 → 下一 tick 附庸消失、原 tile 出現攻方聚落、紅字「附庸淪陷」信件；RimWar 派系資訊視窗可見新聚落點數；PColony 未被清掉、稅務/軍事分頁無殘留條目。
5. **手動戰敗**：battleMode 手動，進地圖戰故意輸 → 同上淪陷。
6. **保護期**：保護期 15 天內敗戰 → 出現「保護期」訊息、不淪陷。
7. **收復**：對淪陷聚落派 Capture 作戰勝利 → 聚落回歸＋「收復失土」信件、淪陷記錄清除。
8. **總開關**：關閉淪陷 → 敗戰回到 Empire 原版折損行為。
9. **存檔相容**：pendingFall 排入後立即存讀檔 → 淪陷仍在讀檔後 tick 執行；舊檔（無本 mod）載入無錯誤。

## 關鍵文件

- `docs/plan/2026-06-12-implementation-plan.md`：攔截點論證＋完整設計
- `session_log.md`：執行記錄
- 上游分析：`~/repo/analysis/rimworld_mods/empire-refactored/`、`~/repo/analysis/rimworld_mods/rim-war/details/target_selection_and_arrival.md`
