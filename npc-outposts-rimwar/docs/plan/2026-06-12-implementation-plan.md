# npc-outposts-rimwar 實作計畫（2026-06-12）

> 構想來源：`_ideas/2026-06-12-empire-rimwar-war-cluster.md`（Mod 1）。
> 技術定案：`pas/analysis/rimworld_mods/rim-war/details/target_selection_and_arrival.md`、
> `pas/analysis/rimworld_mods/_mod_ideas/world_map_grand_strategy/03_rimwar_warband_territories_integration.md` §3。
> 行號引用＝`pas/projects/rimworld_mods/rim-war/decompiled/RimWar.decompiled.cs`（關鍵簽章已逐一核對）。

## 範圍：四項功能

### 1. 哨站貢獻母聚落 RimWarPoints
- **接點**：Harmony Postfix `RimWar.Planet.WorldComponent_PowerTracker.IncrementSettlementGrowth`（`RW:17567`，`public void`、無參數，已核對）。
- **行為**：每成長週期掃 `Find.WorldObjects.Settlements`，把 `NpcOutpost`（`ParentSettlement != null`、與母聚落同派系、雙方未毀）按母聚落分組；
  對每個母聚落的 `RimWarSettlementComp` 加 `每哨站點數 × 類型係數`。
- **守規**：postfix 加點不受原方法 clamp 閘控（`RW:17626` 只夾成長量；上限閘 `RW:17597` 在 if 裡）→ 自行尊重上限：
  基礎 50000、`City_Citadel` +5000、`isCapitol` +5000（Vassal +1000），鏡像原公式。另鏡像原方法的跳過條件：
  `PointDamage > 0` 不加（原版該狀態在療傷）；RWD 為 `Player`/`Excluded` 不加。
- **類型係數**：`RimWarOutpostExtension : DefModExtension { pointsFactor }`，由本 mod 的 Patches XML 掛到 npc-outposts 的
  `OutpostTypeDef` 上（缺省 1.0），免改 npc-outposts 本體即可按類型調平衡。
- **設定**：`pointsPerOutpost`（0–20，預設 4，0＝停用）。

### 2. 哨站可被 warband 攻打
- **接法（深掘文件 §3.2 推薦，零 C#）**：XML Patch 把 `RimWar.Planet.WorldObjectCompProperties_RimWarSettlement`
  注入 `pas_outposts_Outpost` WorldObjectDef（仿 RimWar 自帶 `v1.6/Patches/RimWarCompsx.xml` 的 test+add 防衛寫法；
  本體 def 現無 `<comps>`，先補空節點再加 li，避免 RimWar 原版 pattern 的重複注入問題）。
- **效果**：哨站通過 `NearbyHostileSettlements` 的 `comp != null && RimWarPoints > 0` 過濾（`RW:9411`）→
  被 warband / launched warband 自然鎖定；抵達後 `ResolveWarObjectAttackOnSettlement → ResolveCombat_Settlement →
  ResolveBattle_Settlement` 全鏈可用。**不動 `IsValidSettlement`**（哨站不入派系資產、不出兵、不吊命派系存亡）。
- **初始點數**：哨站不在 IsValidSettlement 白名單 → 不會被 `CreateRimWarSettlement` 初始化 → 本 mod 的 WorldComponent
  每 2500t 掃描未初始化哨站，寫入 `初始點數 × 類型係數 × Rand(0.8,1.2)`（已初始化 ID 持久化防重複）。
  注意 comp getter 會把 backing 夾到 ≥100（`RW:9228` getter clamp 100–100000），故下限自然成立。
- **設定**：`initialOutpostPoints`（100–2000，預設 400；哨站≠聚落，刻意偏低——NPC 聚落初始多在 1000+）。

### 3. 哨站被佔的後果
- **接點**：Harmony Prefix `RimWar.Planet.WorldUtility.ConvertSettlement(Settlement, RimWarData, RimWarData, int, int=0)`
  （`RW:15289`，`public static`，已核對；全 RimWar 僅 `ResolveBattle_Settlement` captured 分支呼叫 `RW:11168`）。
- **行為**：`worldSettlement is NpcOutpost` 才攔（其餘 return true 放行）：
  - **易主（預設）**：`SetFaction(攻方)` → `Setup(TypeDef, 攻方最近非哨站聚落)` 重掛母聚落（找不到＝null，spawner 計數自然忽略）→
    comp 點數重設為克制值（夾在 100 ～ 2×初始）、清 `AttackingUnits` → 發信。
  - **摧毀**：`outpost.Destroy()` → 發信。
  - 兩者皆 `return false` skip 原版（原版會 Destroy 後 `AddNewHome` 重建成 vanilla Settlement，`RW:15296-15300`——哨站會被「升格」，不要）。
  - 原版的 `RemoveRWDFaction` 派系存亡判定不需搬：哨站不在 `WorldSettlements`，丟哨站不影響存亡。
- **連帶（postfix 同方法）**：普通聚落被 RimWar 易主（原版＝毀舊建新）時，其衛星哨站跟著易主——
  `SetFaction(攻方)`＋`Setup(TypeDef, 同 tile 新聚落)`；spawner 的 `caps` 字典經 AccessTools 反射搬鍵（舊→新，fail-soft：
  失敗只 Warning，毀掉的舊鍵由 npc-outposts 既有存檔清理治本兜底，不會 null-key 紅字回歸）。
- **設定**：`captureToConqueror`（bool，預設 true＝易主；false＝摧毀）。

### 4. 戰局動態增減
- **訊號**：Harmony Prefix+Postfix `RimWar.Planet.IncidentUtility.ResolveBattle_Settlement(RimWarSettlementComp, WarObject, float)`
  （`RW:11086`；比 `Archive_RWLetter` 結構化——不必 parse 信件文字）。prefix 快照守方 parent＋派系；
  postfix 以「parent 被毀或派系變了」判攻方勝（涵蓋 captured/夷平/sacked-vassal 各 Destroy 分支與本 mod 的哨站易主），否則守方勝。
- **狀態**：`WorldComponent_OutpostWarMomentum`（ExposeData）：`List<WarScoreEntry{Faction(ref), tick, delta}>`，
  30 天滑窗線性衰減，score 夾 ±5。
- **增生掛鉤**：npc-outposts 本體加**唯一接點** `WorldComponent_OutpostSpawner.GrowthRateMultiplier`
  （`public static Func<Faction,float>`，null 或回傳 ≤0 視為 1 → 零行為變化；呼叫包 try/catch）。
  本 mod 啟動時註冊：勝者 mtb ÷ 倍率（最高 `momentumMaxMultiplier`，預設 1.5）、敗者 × 倒數。
- **萎縮**：本 mod WorldComponent 每天檢查：score ≤ −4 的派系 20% 機率隨機荒廢一座哨站（跳過有玩家地圖者）＋發信。
- **設定**：`warMomentumEnabled`（預設 true）、`momentumMaxMultiplier`（1–3，預設 1.5）、`shrinkEnabled`（預設 true）。

## 防衛式不變式
- 所有 patch 本體包 try/catch；異常 `Log.Warning` 一次（key 去重）後降級，絕不連環紅字。
- Harmony 接線用 `AccessTools.Method` 找不到／簽章不符 → Warning 一次＋該功能跳過（fail-soft），其餘功能照常。
- `ConvertSettlement` prefix 內部出錯 → return true 放行原版（語意退回 RimWar 原行為，不卡死結算）。

## 工程
- packageId `pas.outposts.rimwar`；modDependencies＝Harmony＋Rim War＋npc-outposts；loadAfter 同三者。
- csproj 仿 faction-politics（Krafs.Rimworld.Ref 1.6.*、net48、`..\1.6\Assemblies\`）＋ `$(HOME)`/Condition 風格 HintPath：
  - RimWar.dll：`$(HOME)/.local/share/Steam/steamapps/workshop/content/294100/2222935097/v1.6/Assemblies/RimWar.dll`（已 ls 核實，注意是 `v1.6` 不是 `1.6`）
  - 0Harmony.dll：`.../2009463077/Current/Assemblies/0Harmony.dll`（已核實）
  - NpcOutposts.dll：`..\..\npc-outposts\1.6\Assemblies\NpcOutposts.dll`
- npc-outposts 本體改動＝上述單一 hook，改後重編須 0 error 0 warning。
- 交付：About/、Patches/、Source/、Languages/（EN＋ZH-TW Keyed）、PROJECT.md、session_log.md、tests/healthcheck.py。

## 任務序
1. 計畫（本文件）✅
2. 鷹架：About、csproj、Mod+Settings、Languages
3. Patches XML（comp 注入＋extension 掛載）
4. Patch_IncrementSettlementGrowth（功能 1）
5. WorldComponent_OutpostWarMomentum（comp 點數初始化＝功能 2 收尾；戰績/萎縮＝功能 4）
6. Patch_ConvertSettlement（功能 3）＋ Patch_ResolveBattleSettlement（功能 4 訊號）
7. npc-outposts hook ＋ 雙邊重編 0/0
8. healthcheck ＋ 文件（PROJECT.md、session_log.md）

## 已知限制（設計時點）
- 哨站抽象戰平衡未實機校準（初始 400 點 vs warband 成本公式 `RW:15871`＝目標×1.1–1.8，哨站會是便宜目標——符合「前哨先被拔」設計）。
- 哨站不會主動出兵、不算派系資產（刻意；要反向能力時再評估 IsValidSettlement postfix 的連帶清單）。
- 戰績只收聚落戰（`ResolveBattle_Settlement`）；野戰（`ResolveBattle_Units`）暫不入帳，避免噪音。
- E2E（實機開檔驗證）由主線統一執行。
