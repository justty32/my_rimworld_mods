# npc-outposts-rimwar（NPC 哨站入戰局）

戰爭叢集（`_ideas/2026-06-12-empire-rimwar-war-cluster.md`）的 **Mod 1**：把 npc-outposts 的衛星哨站接進
Rim War 的大戰略模擬——哨站養母聚落、被 warband 攻打、被佔易主、戰局勝敗回饋增生速率。

## 目標

讓 NPC 派系的衛星哨站不再是純風景：它們是聚落的經濟血管（貢獻 RimWarPoints）、是戰爭的前哨
（warband 合法目標、便宜先拔）、也是戰局的溫度計（勝者哨站增生、敗者萎縮）。

## 範圍（四項功能）

| # | 功能 | 接點 | 預設行為 |
|---|---|---|---|
| 1 | 哨站貢獻母聚落 RimWarPoints | Harmony Postfix `WorldComponent_PowerTracker.IncrementSettlementGrowth`（RW:17567） | 每成長週期每哨站 +4 ×類型係數；自行尊重上限 50000（鏡像 RW:17597 階梯）；母聚落療傷中（PointDamage>0）不加 |
| 2 | 哨站可被 warband 攻打 | XML Patch 注入 `RimWar.Planet.WorldObjectCompProperties_RimWarSettlement` 到 `pas_outposts_Outpost`；本 mod WorldComponent 補初始點數（預設 400×係數×Rand0.8-1.2） | 通過 `NearbyHostileSettlements` 過濾（RW:9411）→ 抽象戰全鏈可用。**不動 IsValidSettlement**（哨站不入派系資產、不出兵） |
| 3 | 哨站被佔的後果 | Harmony Prefix `WorldUtility.ConvertSettlement`（RW:15289） | 易主給攻方（SetFaction＋重掛攻方最近聚落＋點數重設克制值）或直接摧毀（ModSettings 二選一，預設易主）；skip 原版「毀掉重建 vanilla Settlement」。postfix 順帶：普通聚落易主時其衛星哨站跟隨、spawner cap 字典搬鍵 |
| 4 | 戰局動態增減 | Harmony Prefix+Postfix `IncidentUtility.ResolveBattle_Settlement`（RW:11086）＋ npc-outposts `GrowthRateMultiplier` hook | 30 天滑窗戰績（WorldComponent，ExposeData）；勝者增生最高 ×1.5、敗者取倒數；連敗（score≤−4）每天 20% 荒廢一座哨站 |

非目標：哨站主動出兵、計入派系資產/存亡（刻意不 postfix `IsValidSettlement`）；玩家哨站；Empire 互動（Mod 2/3）。

## 技術棧

- RimWorld 1.6 / net48 / Krafs.Rimworld.Ref 1.6.*；Harmony 2（workshop 2009463077）。
- 硬相依：Rim War（`Torann.RimWar`，workshop 2222935097，**直接引用 RimWar.dll**，路徑注意是 `v1.6/`）、
  npc-outposts（`pas.outposts.community`，自家碼直接引用 NpcOutposts.dll）。
- 防衛式：所有 patch 本體 try/catch＋去重 Warning；`AccessTools.Method` 找不到/簽章不符 → 該功能降級停用，其餘照常；
  `ConvertSettlement` prefix 異常 → 放行原版（不卡死結算）。
- npc-outposts 本體改動＝**唯一接點** `WorldComponent_OutpostSpawner.GrowthRateMultiplier`
  （`public static Func<Faction,float>`，null/異常/非正值一律視為 1 → 零行為變化）。

## 完成定義

- [x] 四項功能照深掘文件接法落地（接點行號已對 decompiled 源核章）
- [x] `dotnet build -c Release` 0 error 0 warning（本 mod ＋ 改動後的 npc-outposts）
- [x] `tests/healthcheck.py` 通過（XML/def 引用/類名/Keyed 雙語對齊/hook 對齊/DLL 存在）
- [x] Languages：ChineseTraditional ＋ English Keyed（設定＋信件）
- [ ] E2E 實機驗證（主線統一執行，見 docs/plan 計畫「已知限制」）

## 關鍵文件

- `docs/plan/2026-06-12-implementation-plan.md`：實作計畫（接點/公式/防衛不變式）
- `session_log.md`：執行記錄
- 上游分析：`pas/analysis/rimworld_mods/rim-war/details/target_selection_and_arrival.md`（目標選擇/抵達鏈/§3 推薦方案）、
  `.../_mod_ideas/world_map_grand_strategy/03_rimwar_warband_territories_integration.md` §3（成長公式 postfix 接法）
- 權威反編譯：`pas/projects/rimworld_mods/rim-war/decompiled/RimWar.decompiled.cs`
