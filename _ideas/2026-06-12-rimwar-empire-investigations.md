# Rim War × Empire × 三國志化 調查彙整（2026-06-12）

> 一批唯讀源碼調查的結論彙整，供後續挑選實作。各項由背景 subagent 完成、本檔回填。
> 源碼基準：Rim War 反編譯 `~/repo/pas/projects/rimworld_mods/rim-war/decompiled/RimWar.decompiled.cs`（簡記 `RW:`）、
> VOE 反編譯 `~/repo/pas/projects/rimworld_mods/vanilla-outposts-expanded/decompiled-framework/Outposts.decompiled.cs`（簡記 `VOE:`）、
> Empire workshop 源碼、自家 mod `~/repo/my_rimworld_mods/`。

## 進度

| # | 主題 | 狀態 |
|---|---|---|
| A | warband 是否襲擊 VOE outpost | ✅ 完成 |
| B | 如何讓 warband 襲擊 VOE/封存哨站 | ✅ 完成 |
| C | RimWar × Empire 更多聯動機會 | ✅ 完成 |
| D | warband 建聚落變體改建哨站＋降頻 | ⏳ 進行中 |
| E | warband 三國志式屬性＋將領佔位符 | ⏳ 進行中 |
| F | 聚落掛領主/官員佔位符 × faction-politics | ⏳ 進行中 |

---

## A. Rim War warband **不會**襲擊 VOE outpost（預設）

**結論：不會**（乾淨，無需 patch 即不會誤觸）。

- VOE `Outpost : MapParent`（`VOE:731`），**不是** `Settlement`/`Caravan`/`WarObject`。
- warband 目標查詢 `GetRimWorldSettlementsInRange`（`RW:16154-16168`）對每個世界物件先做硬轉型 `(Settlement)(obj is Settlement ? obj : null)`，`MapParent` 子類直接丟棄。
- comp 過濾閘 `RimWarSettlementComp != null && RimWarPoints > 0`（`RW:9411`）位於 `is Settlement` **之後** → 不可達。
- Scout 廣掃唯一掃非聚落物件，但用 `is Caravan / is WarObject / is Settlement` 三分支派發（`RW:18505-18522`），Outpost 三者皆非 → fall-through。
- 全 Rim War 反編譯**零個** `is MapParent` 泛化處理點。
- 你之前擔心的 `IsValidSettlement` defName 白名單（`RW:16685`）其實**不在目標路徑上**，真正關卡是更早的 `is Settlement` 型別過濾。

---

## B. 如何讓 warband 襲擊 VOE/封存哨站 → 推薦**影子 Settlement 橋接**

**Settlement vs MapParent 硬牆**：Mod 1（`npc-outposts-rimwar`）只靠「XML 注入 `WorldObjectCompProperties_RimWarSettlement` + prefix `ConvertSettlement`」就讓 npc-outposts 被鎖定，是因為 `NpcOutpost : Settlement`（通得過型別過濾）。VOE `Outpost_Sampled : Outpost : MapParent` **不是 Settlement**，同手法搬過去會「注入成功但毫無效果」。

**路徑評比**：
- **A. patch 型別過濾**：transpile/postfix `GetRimWorldSettlementsInRange`（`RW:16154`）。死結＝其回傳型別是 `List<Settlement>`，硬塞 MapParent 會 InvalidCast。中工程、脆弱。
- **B. 影子 Settlement 橋接（★推薦，最低風險）**：在每座 Outpost 同/鄰 tile 掛一個隱形真 `Settlement` 當「戰鬥替身」→ 自動通過所有型別過濾、戰鬥/易主全鏈原生可用；prefix `ConvertSettlement`（仿 Mod 1 `Patch_ConvertSettlement.cs`）偵測影子→改對綁定的 Outpost 做摧毀/易主。**完全複用 Mod 1 已驗證手法，不碰 Rim War 型別系統。**
- **C. Outpost 改繼承 Settlement**：否決（VOE 全框架建於 MapParent 假設）。
- **D. 主動派兵補充**：`WorldUtility.CreateWarband/CreateWarObject`（`RW:15467/15348`，`public static`）可主動指定目標 tile 派兵，作為 B 的加味。

**核心戰鬥鏈幾乎不碰 Settlement 型別、全對 comp 操作**（這是 B 可行的關鍵）：`ResolveWarObjectAttackOnSettlement`（`RW:10340`，守方靠 `defender` comp）→ `ResolveCombat_Settlement`（`RW:11018`）→ `ResolveBattle_Settlement`（`RW:11086`）。**唯一真雷在 captured 分支**（`RW:11167`）：`WorldUtility.ConvertSettlement(SettlementAt(tile),…)`，對 MapParent 的 `SettlementAt(tile)` 回 null → `ConvertSettlement`（`RW:15289`）守衛擋下 → 靜默 no-op，哨站永遠打不掉（傷害累積卻無結局）。影子方案天然解掉此雷（影子是真 Settlement，SettlementAt 找得到）。

**實作骨架（路徑 B）**：①隱形 `WorldObjectDef`（Settlement 子類或 `RimWarSettlement`）＋XML 注入 comp（複製 `NpcOutpostRimWarComps.xml`），隱藏圖示；②WorldComponent 維護 `Dictionary<Outpost, Settlement shadow>`，Outpost spawn 時在**鄰 tile**（比同 tile 安全，避 SettlementAt 衝突）建影子、`SetFaction`、設 `RimWarPoints`（public setter）；Outpost destroy 時移除影子；③prefix `ConvertSettlement` 偵測影子→改對 Outpost `Destroy()`（`VOE:1138`）/`SetFaction()`（`VOE:221`），`return false` skip 原版；④可選 postfix `IncrementSettlementGrowth` 加點。

---

## C. RimWar × Empire 新聯動機會（8 項，按價值/工程量比排序）

> 已排除三 mod 叢集與內建 Patch-RW 已做掉的（可見性/點數置換/攻擊導流/奪城防衛/Vassal鎖/淪陷易主/哨站入戰局/防守 IBattleModifier）。
> **通用陷阱**：Empire 的 Registry 不序列化、`Game.ClearCaches` 每次讀檔會 ClearAll（`CachePatches.cs:36-50`）→ 必須 `EmpireCacheUtil.RegisterCacheInvalidator`（`CachePatches.cs:21`）重註冊（官方 Patch-RW 自己都漏做）。

### 第一梯隊
1. **戰時加稅／被圍困減產（RimWar 局勢→Empire 經濟）★最高比值起手**。Empire 端**全免 Harmony**：減產 `IResourceProductionModifier`（消費 `ResourceFC.cs:338/363/582/645`）、戰時稅 `ITaxTickParticipant.PostSettlementCreateTax`（`WorldSettlementFC.cs:1917`，可 `ref` 改白銀）。局勢訊號純讀：`RimWarSettlementComp.AttackingUnits/nextCombatTick`（`RW:9088`）判圍困、`RimWarData.IsAtWar/WarFactions`（`RW:1462/1506`）判戰爭。補上目前缺的「RimWar→Empire 反向因果」。工程小、零 patch、風險低。
2. **附庸繁榮回饋 RimWar 點數**。`ITaxTickParticipant.PostTaxResolution`（`FactionFC.cs:1696`，免 Harmony）讀收益→`RimWarSettlementComp.RimWarPoints` public setter 加點（尊重上限 50000，`RW:17597`）。注意別與 `Patch_RimWarPoints` getter 雙算（加到別的派系/做盟友紅利）。
3. **RimWar 全事件流→Empire FCEvent（信件匯流排對接）**。RimWar 端單一漏斗 postfix `RW_LetterMaker.Archive_RWLetter`（`RW:7851`，六類事件 def `RW:1772-1782`）；Empire 端免 Harmony `FactionFC.AddEvent`（`FactionFC.cs:1736`）+ XML `FCEventDef`。需節流去重避免事件洪水。

### 第二梯隊
4. **帝國威望/軍力受 RimWar 勝負回饋**。`IThreatScalingContributor`（消費 `ThreatScalingUtil.cs:83-84`，免 Harmony，目前無人用）；訊號讀 `RimWarData.TotalFactionPoints` 趨勢（`RW:1510`）或 postfix `ResolveBattle_Settlement`（`RW:11086`）。需自存戰績滑動視窗。
5. **帝國成為 RimWar 外交一方（宣戰/和談互通）**。RimWar 現成 `public static`：`RimWarFactionUtility.DeclareWarOn/DeclareAllianceWith/EndAllianceWith`（`RW:468/526/598`，免 patch）。Empire 端缺外交入口需自建（gizmo/通訊台）。風險中高：`DeclareWarOn` 直寫 `baseGoodwill=-100`（`RW:489`），與 Empire relations/playerVS 可能衝突。「玩家帝國＝世界一方勢力」願景核心。
6. **Empire squad 介入 NPC vs NPC 戰局（傭兵/代理戰）**。`IAutoDefender`（`FCInterfaces.cs:280`，消費 `MilitaryUtilFC.cs:62/150/206`，免 Harmony）；攻方複用 `SimulateBattleFc.FightBattle`（`SimulateBattleFC.cs:10`，public）+ `ConvertSettlement` 轉給盟友。工程大（合約 UI、squad 路徑）。

### 第三梯隊
7. **世界勢力範圍→附庸可擴張範圍/被攻擊頻率**。`IRaidWeightProvider.GetSettlementRaidWeight`（`FactionFC.cs:709/718`）＋`ISettlementFoundingValidator.CanFoundSettlement`（`CreateColonyWindowFC.cs:130/406`），皆免 Harmony、目前無人用；讀 `WorldComponent_PowerTracker.AllRimWarSettlements`（public）。
8. **用未利用的 `ILifecycleParticipant` 事件對接**（11 事件 Patch-RW 一個沒用）：`OnSettlementUpgraded`/`OnResearchCompleted`（`ResearchPatches.cs:24`）/`OnSquadDeployed`（`SettlementMilitary.cs:1207/1563`）等→RimWar public setter 調點。注意 `OnBattleResolved` 已被 `empire-warfare` 用。一個 `LifecycleParticipantBase` 子類覆寫數方法，零 Harmony。

**Empire 端全免 Harmony 的最乾淨機會**：#1、#2、#4、#7、#8（只需 RimWar 側讀 public 狀態，多數連 RimWar 都不用 patch）。**RimWar 側唯一需 patch ＝#3**（薄 postfix）。#5/#6 用 public static API 外呼免 patch。

---

## D. warband 建聚落變體改建哨站＋降頻

⏳ 進行中（首次派發跑空、已重派）。完成後回填。

---

## E. warband 三國志式屬性＋將領佔位符

⏳ 進行中。完成後回填。

---

## F. 聚落掛領主/官員佔位符 × faction-politics（colony rebellion）

⏳ 進行中。背景：使用者的「colony rebellion」＝本機 `faction-politics` mod（有 `RebellionProfileDef`、派系分裂、named NPC bridge）。完成後回填。

---

## 三國志化願景（跨 D/E/F 的整合圖像，待調查回填後細化）

使用者要的整體感覺：**Rim War 大地圖戰爭 + 具名人物（將領率軍、太守治城）+ 叛亂/勢力消長**，把抽象點數戰爭變成有人物的三國志式大戰略。
- **E（warband 將領）** 與 **F（聚落領主/官員）** 應共用一套「具名 pawn 佔位符 + 屬性」基礎設施（待兩調查確認可共用）。
- **F** 與 faction-politics 的 rebellion 連動 ＝「領主對母派系不滿→帶城叛變」。
- **D** 降低 RimWar 建聚落頻率、改以哨站擴張，讓世界版圖節奏更可控。
- **B** 讓 warband 能打哨站，閉合「哨站也是可攻防的戰爭節點」。
