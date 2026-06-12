# 02 — 基礎層 `pas.named-officers`（具名職官）

> 對應調查：E（:103/118）、F（:115/118）、I（:147）。盤點需新建項①（:176）。
> **這是 E/F/G/I/J/K 的硬前置**（見 `01` 論證）。

## 職責

提供一套「具名 pawn + 屬性 + 關係 + 懶生成」的共用基礎設施，
讓**聚落領主**（`03`）與 **warband 將領**（`04`）共消費同款 record/comp，
不各自重造。其本身**不含玩法**，只供承載。

## 設計（混合承載，鏡像 Empire/faction-politics 模型）

- **屬性層 = WorldObjectComp**（F 推薦，:115）：存能力值/職位/忠誠。
  先例 `RimWarSettlementComp`（`RW:9078`，掛任何 `Settlement` 子類）、Empire `WorldSettlementFC`（unrest/loyalty/prosperity）。
- **具現層 = 真 Pawn 懶生成**（F，:115）：平時輕量 record，
  拜訪/攻打時才 `GeneratePawn` + 復用 faction-politics 的 `previouslyGeneratedInhabitants` 橋。
- **屬性集**（三國志式，typed int/float，`Scribe_Values`）：
  武力 / 統率 / 智力 / 政務 / 魅力 / 忠誠 / 士氣。
  將領主要讀「武力+統率」（E 戰力），領主主要讀「政務+忠誠+魅力」（G 治理、叛亂）。

## 掛載點 / 復用座標

- record/pawn 管線：泛化 `faction-politics` 的 `RebelRecord`（`RebelRecord.cs`）+
  `RebelSpawner`（`RebelSpawner.cs:13-36`：`GeneratePawn`→`PassToWorld(KeepForever)`→`previouslyGeneratedInhabitants.Add`）。
  **抽成 `NamedOfficerRecord` + `OfficerSpawner`**（從「單一反叛者」→「具名職官（領主/將領/官員）」）。
- 心跳：復用 `WorldComponent_RebellionTracker` 2500-tick 範式（`WorldComponent_RebellionTracker.cs:29-52`）演化屬性/關係。
- 屬性 comp：自訂 `CompProperties`，Harmony 注入目標 def 的 `comps`（E 對 `RW_Warband`、F 對 Settlement 子類）。

## 關係雙軌（住此層，I 全文 :144-147）

- **A 軌 持久關係**（結拜/世仇）= vanilla `DirectPawnRelation` + 自訂 `PawnRelationDef`。
  對 world pawn 完全可用、隨 pawn 存檔、零成本（`Pawn_RelationsTracker.cs:483/292`，無 Spawned 檢查）。
- **B 軌 連續好感度**（會漲跌）= 自訂 `Dictionary<otherPawnId,int>`（IExposable，`Scribe` value）。
  vanilla 動態 opinion 對 world pawn 凍結（`Pawn.cs:1659` Spawned 閘），故自存；由心跳演化，B 讀 A 當初始 bias。

## 存檔策略

- record 走 Deep、pawn ref 走 `Scribe_References`（仿 faction-politics，`RebelRecord.cs:19-27`）。
- 屬性 comp 走 `Scribe_Values`（仿 `RimWarSettlementComp.PostExposeData RW:9502`）。
- 關係 B 軌 dict 走 `Scribe_Collections`；A 軌隨 pawn 自動存。
- 懶生成的 pawn 用 `KeepForever`，數量控管靠 record 數量（非每城每軍即時生）。

## 風險

- **world pawn 數量爆量** → 嚴守懶生成：平時只有 record，具現按需。
- **comp 掛載時序**（讀檔 vs 注入）→ 沿用 faction-politics 軟橋 + `FinalizeInit(fromLoad)` 補鋪範式。
- **屬性集 over-design** → MVP 只實作將領用（武力/統率）+ 領主用（政務/忠誠/魅力），其餘預留欄位。
- 跨 mod 型別耦合 → comp/record 型別放此 mod，消費 mod hard-ref 本基礎層。

> **P0 已實作**（2026-06-12，`my_rimworld_mods/named-officers/`）：依
> `plans/p0-named-officers/`（00-overview G1–G6 決議）。要點：packageId=`pas.officers.community`（G1）；
> 七維全建 MVP 五維（G2）；屬性承載改「record 唯一真相＋無狀態 view comp，零 Harmony，注入由消費 mod 自做」（G3，
> 取代本檔「WorldObjectComp + Harmony 注入」）；A 軌對未具現職官按需具現（G4）；死亡=標記+事件+下心跳清理（G5）；
> maxOfficersPerObject=4、不自動鋪官（G6）。B 軌 dict key 用 record id 而非本檔的 otherPawnId（pawn 換體不斷鍵）。
