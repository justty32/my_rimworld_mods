# session_log — npc-outposts-rimwar

## 2026-06-12 初版實作（計畫 → 四功能 → 建置/健檢全綠）

### 前置核章
- 讀構想（`_ideas/2026-06-12-empire-rimwar-war-cluster.md` Mod 1）、RimWar 深掘
  （`target_selection_and_arrival.md`）、成長公式定案（`03_rimwar_warband_territories_integration.md` §3）。
- 對 `RimWar.decompiled.cs` 核對關鍵簽章：
  - `WorldComponent_PowerTracker.IncrementSettlementGrowth()`（:17567，public 無參）✓
  - `WorldUtility.ConvertSettlement(Settlement, RimWarData, RimWarData, int, int=0)`（:15289，public static）✓
  - `IncidentUtility.ResolveBattle_Settlement(RimWarSettlementComp, WarObject, float)`（:11086）✓
  - `RimWarSettlementComp.RimWarPoints/PointDamage` public 可讀寫（:9216/:9228）✓
  - **新發現**：`RimWarPoints` getter 會把 backing 夾到 ≥100（:9270 clamp 100–100000）→
    哨站掛 comp 後天然 ≥100 點、必過 `NearbyHostileSettlements` 的 points>0 過濾；初始化只是調平衡。
  - **新發現**：`ConvertSettlement` 全 RimWar 僅一個呼叫點（ResolveBattle_Settlement captured 分支 :11168）→
    prefix 攔截影響面precisely可控。
  - RimWar 自帶 `v1.6/Patches/RimWarCompsx.xml` 的 test+add pattern 在「def 原無 comps」時會重複注入
    （先 add comps+li，第二段 test 又過再 add li）→ 本 mod 改用「補空 comps 節點＋無條件 add li」兩步，不踩坑。
- 路徑核實：RimWar DLL 在 `2222935097/v1.6/Assemblies/RimWar.dll`（**v1.6 不是 1.6**）；
  0Harmony 在 `2009463077/Current/Assemblies/`。

### 實作
- `docs/plan/2026-06-12-implementation-plan.md` 先行。
- 鷹架：About（packageId `pas.outposts.rimwar`、硬相依 Harmony/RimWar/npc-outposts、loadAfter 同）、
  csproj（仿 faction-politics：Krafs 1.6.*、net48、`..\1.6\Assemblies\`；$(HOME)+Condition HintPath）。
- 功能 1：`Patch_IncrementSettlementGrowth`（postfix 分組加點；鏡像上限階梯與 PointDamage/Player/Excluded 跳過；
  類型係數走 `RimWarOutpostExtension`（DefModExtension，Patches XML 掛上 OutpostTypeDef，免改本體））。
- 功能 2：`Patches/NpcOutpostRimWarComps.xml` 注入 RimWar 聚落 comp；
  `WorldComponent_OutpostWarMomentum.InitializeOutpostPoints` 每 2500t 補初始點數（持久化已初始化 ID 防重複）。
- 功能 3：`Patch_ConvertSettlement` prefix（哨站易主/摧毀二選一、清 AttackingUnits、發信、skip 原版）＋
  postfix（衛星跟隨聚落易主；spawner `caps` 反射搬鍵 fail-soft——舊鍵兜底靠 npc-outposts 存檔前清理，無 null-key 回歸）。
- 功能 4：`Patch_ResolveBattleSettlement`（prefix 快照 parent+派系、postfix 判勝負入帳）＋
  `WorldComponent_OutpostWarMomentum`（30 天滑窗、score ±5、倍率對稱、連敗萎縮＋發信、ExposeData 含存檔前清理）。
- npc-outposts 本體：加唯一接點 `GrowthRateMultiplier`（static Func hook，null/異常/非正值＝1，呼叫包 try/catch）。
- Languages：ZH-TW＋EN Keyed（設定 10 鍵＋信件 6 鍵）；healthcheck 檢查雙語集合一致。

### 驗證
- `dotnet build`（npc-outposts）：Build succeeded. 0 Warning(s) 0 Error(s)
- `dotnet build`（npc-outposts-rimwar）：Build succeeded. 0 Warning(s) 0 Error(s)
- `tests/healthcheck.py`（本 mod）：healthcheck OK
- `tests/healthcheck.py`（npc-outposts，hook 改動後回歸）：healthcheck OK

### 待辦（主線）
- E2E：開新檔（RimWar＋Sims＋Outposts＋本 mod）驗證：哨站被 warband 鎖定/攻打、易主信件、
  母聚落點數成長曲線、連敗萎縮；存讀檔無紅字。
- git commit 由主線統一。staging 部署不在本 session 範圍。
