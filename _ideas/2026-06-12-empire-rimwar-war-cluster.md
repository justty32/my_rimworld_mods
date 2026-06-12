# Empire × Rim War × npc-outposts 戰爭叢集（三 mod 構想）

> 2026-06-12 brainstorm 記錄（已釐清需求與拆解，**尚未進實作計畫**）。
> 上游分析：`pas/analysis/rimworld_mods/empire-refactored/`（extension_points＋bridge walkthrough＋tutorial）、
> `pas/analysis/rimworld_mods/_mod_ideas/world_map_grand_strategy/03_rimwar_warband_territories_integration.md`（戰力公式定案）。

## 願景一句話

讓世界戰爭真正打到玩家帝國頭上，且 NPC 派系也有「帝國式」的衛星經濟參戰——Empire Refactored（玩家附庸帝國）、Rim War（NPC 大戰略）、npc-outposts（NPC 衛星哨站）三者互通成一個會互相吞併的活世界。

## 需求決策（brainstorm 問答定案）

| 問題 | 決定 |
|---|---|
| 擴展方向 | 新玩法 ＋ 與自家 mod 叢集整合 ＋ 與 Rim War 整合 |
| 核心玩法 | **帝國參戰（外戰）** ＋ **NPC 帝國鏡像**（不做附庸忠誠內政線） |
| 附庸被攻打的結算 | **混合**：預設抽象結算（Empire `SimulateBattleFc` 戰力 vs RimWar 點數），來襲有預告期，可派 squad 馳援加成，也可選實體戰鬥親自打 |
| 附庸淪陷後果 | **易主進 Rim War 版圖**：變成攻擊方派系的正規 NPC 聚落，之後可用 Empire 既有 Capture 奪回（征服迴圈閉環） |
| NPC 鏡像規模 | **中量**：哨站入戰局 ＋ 易主跟隨 ＋ 戰局動態增減（勝者哨站增生快、敗者萎縮） |
| 打包形態 | **三個獨立 mod**（見下） |
| 三合一互動內容 | 玩家附庸也長哨站 ＋ 哨站參與防守/前哨戰 ＋ 哨站隨聚落易主（全勾） |

## 三 mod 拆解

### Mod 1：`npc-outposts-rimwar`（NPC 哨站入戰局）
相依：npc-outposts ＋ Rim War（＋Harmony）。建造順序第一（最小、技術已定案、驗證 RimWar 接線手法）。

- 哨站貢獻母聚落 RimWarPoints：Harmony Postfix `WorldComponent_PowerTracker.IncrementSettlementGrowth`（03 報告 §3.3 定案；公式無 outpost 維度、無 extension 注入點，postfix 是唯一不 fork 的路）。
- 行軍隊伍可掠奪/抹除哨站削弱敵方。
- 戰局動態增減：依派系近期戰績調 npc-outposts 的增生速率/上限。
- NPC 互奪聚落時哨站跟著易主（複用 faction-politics 倒戈搬運邏輯）。

### Mod 2：`empire-warfare`（帝國參戰）
相依：Empire Refactored ＋ Rim War（＋Harmony）。建造順序第二。

- Rim War 行軍隊伍會把玩家附庸（`WorldSettlementFC`）當攻擊目標。
- 防守混合結算（見上表）；馳援走 Empire squad 派遣。
- 淪陷＝附庸轉為攻擊方 NPC 聚落、註冊進 RimWarData。
- 攻方既有路徑不動：Empire `MilitaryJobHandler_Capture` 已是完整征服機制（抽象結算、勝利轉附庸、按科技給升級），Patch-RW 的 `RWStrengthBattleModifier` 已讓防守戰力吃 RimWar 點數。

### Mod 3：`empire-outposts-war`（三合一互動）
相依：全部（Empire＋RimWar＋npc-outposts＋Mod 1＋Mod 2）。建造順序第三，純膠水。

- 玩家附庸聚落也長衛星哨站（複用 npc-outposts 增生），哨站加成附庸產出與防守戰力。
- 前哨戰：附庸被攻時周邊哨站先被打（緩衝層、存活加防守戰力）；玩家主攻 NPC 聚落前也可先拔其哨站削防。
- 征服戰利品：Capture 奪城時其衛星哨站隨之易主（雙向——附庸淪陷時哨站也敷出去）。

## 關鍵技術座標（設計時已核對）

- **Empire 已有征服**：`MilitaryJobHandler_Capture`（`1.6/Source/Core/FactionColonies/Military/MilitaryJobHandler_Capture.cs`）——派 squad → `SimulateBattleFc.FightBattle` → 勝利 `target.Destroy()` ＋ `ColonyUtil.CreatePlayerColonySettlement`。
- **Empire Patch-RW 現況**＝防禦性橋：`Patch_IsValidSettlement`/`Patch_ConvertSettlement`/`Patch_EmpireColonyCheck` 讓 RimWar **無視/不捕獲**帝國聚落；`RWStrengthBattleModifier : IBattleModifier` 讓戰鬥結算吃 RimWar 點數。**Mod 2 最大風險＝要繞過/再 patch 這層「無視」**，讓 warband 能選附庸當目標但不觸發 RimWar 自己的捕獲邏輯（仍由我方接管淪陷流程）。
- **Empire 擴充面**：優先走 B-1（`FCInterfaces` 20+ 介面 ＋ 15 個 Registry，免 Harmony；`IBattleModifier`/`ILifecycleParticipant`/`IRaidTarget`/`IThreatScalingContributor` 都直接相關），逼不得已才 B-2 compat DLL。註冊慣例 `[StaticConstructorOnStartup]` ＋ `XxxRegistry.Register`。
- **RimWar 戰力**：`RimWarSettlementComp.RimWarPoints` 為 public 可直接讀寫；成長唯一入口 `IncrementSettlementGrowth`（`RW:17567`，核心公式 `RW:17622-17626`）；上限基礎 50000，postfix 加點不受原方法 Clamp 閘控、須自行尊重上限。
- **npc-outposts 是自家碼**：`NpcOutpost : Settlement`（有 `ParentSettlement` 引用＋`OutpostTypeDef`）、增生在 `WorldComponent_OutpostSpawner`（2500 tick 週期、per-settlement cap 字典）。**不需反射**，橋接 mod 可直接引用；也可考慮在 npc-outposts 本體加少量擴充接點（事件/虛方法）讓 Mod 1/3 掛載更乾淨。
- 載入順序：橋接 mod loadAfter Empire/RimWar/npc-outposts；gated 模組照 Empire/Ariandel 的 `LoadFolders.xml IfModActive` 慣例。

## 開放問題（進實作計畫前要回答）

1. Mod 1 哨站→RimWarPoints 的換算公式與平衡（按哨站類型/數量？上限佔比？）。
2. 「戰局動態增減」的戰績訊號從哪讀：RimWar 的勝負事件？聚落數變化？`TotalFactionPoints` 趨勢？
3. Mod 2 預告期的實作載體：複用 Empire 的 `FCEvent` 計時事件，還是 RimWar 的 WarObject 行軍時間天然就是預告期（偵測 warband 目標＝附庸時發信）？
4. 附庸實體防守戰的地圖路徑：Empire 的 manual battle（標記脆弱）vs 原版 `GetOrGenerateMap`＋我方 LordJob（sims-mode 已有相關經驗）。
5. faction-politics 在場時的交互：分裂出的新派系是否立即參與哨站/附庸攻防（應該自然成立，但要驗證 RimWarData 註冊時序）。
6. Mod 3 玩家附庸哨站的 UI/管理面：用 Empire 聚落視窗加分頁（`ISettlementWindowOverview`）還是 gizmo。
