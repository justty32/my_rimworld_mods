# 07 — faction-politics 擴充：叛亂改寫成領主帶城叛變（F × 叛亂）

> 對應調查：F（:109-119 rebellion 連動）、I（:146 關係餵叛亂）。
> **擴充既有 mod，不開全新 mod**（F 明確，:118）。地基已在 faction-politics。

## 職責

把現有「藏匿反叛者→分裂新派系」改寫成**三國志式「城池領主對母派系不滿→公開帶城叛變獨立」**。
80% 現成（F，:117）。

## 既有地基（已驗證屬實，F:109-117）

- `RebelSpawner.cs:13-36`：`GeneratePawn`+`WorldPawns.PassToWorld(KeepForever)`、
  `homeSettlement.previouslyGeneratedInhabitants.Add`（玩家拜訪 redress 同一 pawn）。
- `RebelRecord.cs`（IExposable：faction/rebel(ref)/homeSettlement(ref)/progress/ratePerDay/respawnAtTick）。
- `WorldComponent_RebellionTracker.cs:29-52`（2500-tick 心跳 + 自癒 `EnsureRebels` + `TrySplit`）。
- `FactionSplitter.Split`（`FactionSplitter.cs:14`：`NewGeneratedFaction`→`newFaction.leader=record.rebel`→
  `Transfer` 倒戈聚落含哨站；goodwill 轉敵）。
- 倒戈通知 `PoliticsBridges.SettlementDefected`（哨站跟隨/RimWar 同步已掛，F:117）。

## 改寫設計（F，:117-119）

1. **從「藏匿反叛者」→「公開城池領主」**：反叛者就是 `03` 的領主（共用 named-officers record）；
   叛變即「太守自立」，敘事上由暗轉明。
2. **`ratePerDay` 改由領主屬性驅動**（F:117 / I:146）：
   `ratePerDay = f(領主忠誠↓, 魅力↑, 官員集體厭領主)` ← 讀 named-officers 屬性 + 關係 B 軌。
   現況 `ratePerDay` 自 `profile.progressPerDay` 擲定（`RebelSpawner.cs:29`）→ 改為動態函數。
3. **泛化 record**：`RebelRecord` 從「單一反叛者」→「領主 + N 官員 record list」（F:119），
   實際做法是消費 named-officers 的 `NamedOfficerRecord`（`02`），叛變追蹤欄位（progress 等）疊在領主 record 上。
4. `TrySplit`（`RebellionTracker.cs:152-172`）/ `FactionSplitter.Split` 邏輯**幾乎不動**——
   反叛者升 leader 本就是三國志「割據自立」。

## 與其他支柱連動

- 領主低忠誠 → 高 `ratePerDay` → 帶城（含衛星哨站，`Transfer` 既有）叛變 → 勢力消長。
- 新派系立即參與 RimWar/哨站攻防（war-cluster 開放問題#5，:71，需驗證 RimWarData 註冊時序）。

## 存檔策略

- 沿用既有：record Deep、pawn `Scribe_References`（`RebelRecord.cs:19-27`）。
- 領主屬性走 named-officers 層；叛變 progress 仍在 politics 心跳推進。

## 風險（F，:119）

- world pawn 數量控管（懶生成，已有範式）。
- comp 掛載時序（沿用軟橋）。
- 泛化 record 須向後相容舊存檔（單反叛者→領主+官員 list 的遷移）。
- 與 settlement-lords（`03`）共用領主 record 的**所有權邊界**須劃清（誰生、誰存、誰演化）。
