# session_log — empire-warfare

## 2026-06-12 初版實作（從零到建置/健檢通過）

### 前置研讀
- 構想定案 `_ideas/2026-06-12-empire-rimwar-war-cluster.md`（Mod 2，含讀源碼修正：範圍縮小為改寫敗戰後果＋目標性驗證）。
- Empire 分析三件套（bridge_module_walkthrough / extension_points / tutorial 01）＋ RimWar `target_selection_and_arrival.md`。
- 精讀 Empire 源碼：`Patch-RW` 七檔（重點 `Patch_ResolveWarObjectAttack`、`Patch_ConvertSettlement`）、`MilitaryUtilFC.AttackPlayerSettlement`、`FCEventMaker.ProcessEvents`（settlementBeingAttacked 分支）、`SettlementMilitary.StartDefence/EndBattle/LoseBattle/EndAttack/ProcessMilitaryEvent`、`ColonyUtil.RemovePlayerSettlement/CreatePlayerColonySettlement`、`MilitaryJobHandler_Capture`、`LifecycleRegistry`/`LifecycleParticipantBase`/`FCInterfaces`、`CachePatches`（EmpireCacheUtil）、`FactionCache`、`MilitaryForce`、`SimulateBattleFC`/`BattleResult`。
- 對照 RimWar 反編譯：`ResolveWarObjectAttackOnSettlement`（:10340）、`ConvertSettlement`（:15289）、`SettlementUtility.AddNewHome`（:9044）、`CreateRimWarSettlementWithPoints`（:15221）、`RimWarData.rwdNextUpdateTick`（:1155）、`RimWarSettlementComp.vassalHeat`（:9106，public、scribed）、`GetRimWarDataForFaction`。

### 關鍵決策
1. **攔截點＝`LifecycleRegistry.OnBattleResolved`（B-1 契約層）**而非 Harmony 打 Empire 或接管 `Patch_ConvertSettlement`：
   - Patch-RW 的 `Patch_ResolveWarObjectAttack` prefix 已把附庸攻擊整條導流進 Empire 事件流（1 天預告→`StartDefence`→`EndBattle`），RimWar 的 `ResolveBattle_Settlement→ConvertSettlement` 鏈對附庸不會執行——「接管 ConvertSettlement」是死路。
   - `EndBattle` 是自動結算與手動地圖戰的唯一匯流點，固定以 `MilitaryJobDefOf.DefendFriendlySettlement` 呼叫 `InvokeOnBattleResolved`（SettlementMilitary.cs:1207）；攻方作戰走 `ProcessMilitaryEvent`（:1594）帶 capture/raid/enslave job，不會誤觸。
   - 與官方 `Patch_ConvertSettlement` 共存方式＝完全不碰（保留其兜底語意），無 Harmony 順序問題。
2. **攻擊來源辨識**：postfix `IncidentUtility.ResolveWarObjectAttackOnSettlement` 記標記（prefix skip 不影響 postfix 執行），預設「僅 RimWar 攻擊會淪陷」。
3. **退場走 Empire 正規 API**：`ColonyUtil.RemovePlayerSettlement`（聚落視窗「放棄」同款；含 PreDestruction→PrepareDestroy→`InvokeOnSettlementRemoved`→settlements/Bill/FCEvent/快取/軍事清理）。
4. **建城避開 ConvertSettlement**：`RimWar.Planet.SettlementUtility.AddNewHome`（RimCities 相容）＋`CreateRimWarSettlementWithPoints`＋`rwdNextUpdateTick=now`。
5. **延後執行**：判定在回呼、執行在 `WorldComponent` 下一 tick（pendingFalls 可序列化）——避免在 LifecycleRegistry 迭代中移除聚落（迭代後還會對 settlement `InvalidateStatCache`）。
6. **Registry 防 ClearCaches**：`EmpireCacheUtil.RegisterCacheInvalidator` 重註冊（官方 Patch-RW 自己都沒做，照 tutorial §7.3 樣板補上）。
7. **PlanetTile**：pendingFalls 用 PlanetTile 保留 layer（Orbital 附庸）；其餘字典 int key。

### 踩坑記錄
- `SettlementUtility` 在 RimWar.Planet 與 RimWorld.Planet（1.6 新增）撞名 → 全限定。
- `RimWarData` 在 `RimWar` 命名空間（不是 `RimWar.Planet`）→ 全限定。
- healthcheck「不得呼叫 ConvertSettlement」檢查誤抓註解 → 改 regex 比對呼叫形 `ConvertSettlement\s*\(`。

### 產出與驗證
- `dotnet build -c Release`：**Build succeeded. 0 Warning(s) 0 Error(s)**（輸出 `1.6/Assemblies/EmpireWarfare.dll`）。
- `python3 tests/healthcheck.py`：**healthcheck OK**。
- 文件：`docs/plan/2026-06-12-implementation-plan.md`、`PROJECT.md`（含 E2E 待驗清單）、Languages EN/ZH-TW 全 key 對齊。
- 未 git commit（主線統一提交）；未動 staging 部署。

### 待辦（下一階段）
- 實機 E2E（PROJECT.md 清單 9 項，重點：warband 實際選附庸為目標、淪陷後 RimWar 派系視窗點數、Capture 收復、存讀檔重註冊）。
- 叢集整合：Mod 3（empire-outposts-war）將掛 `VassalDefenseBattleModifier` 接點做哨站防守加成。
