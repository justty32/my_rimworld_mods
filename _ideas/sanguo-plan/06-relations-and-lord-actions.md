# 06 — 關係雙軌（I）+ 領主決策層 `ILordAction`（J/K）

> 對應調查：I（:142-147 關係）、J（:149-154 outpost 選型）、K（:160 治理動作層）。
> 依賴：named-officers（關係住此層）、settlement-lords、city-economy、npc-outposts hook。

## A. 關係雙軌（I）— 住 named-officers 層，此處定玩法接點

> 雙軌設計細節見 `02`（A 軌 DirectPawnRelation / B 軌 opinion dict）。本段定**玩法消費**。

- 餵叛亂：官員集體厭領主 → 拉高 `RebelRecord.ratePerDay`（`RebellionTracker.cs:51`）→ 更快 `TrySplit`（I:146）。
- 餵戰力：兩將不和 → `ResolveCombat_Units RW:11271` 打折（E×I，:146）。
- 餵治理：經 `GovernanceFactor` 影響 `IncrementSettlementGrowth RW:17567`（G×I，:146）。
- A 軌結拜/世仇做初始 bias、B 軌動態漲跌（由 named-officers 2500-tick 心跳演化）。

## B. 領主決策層 `ILordAction`（J + K 共用骨架）

> K 明確（:160）：**領主治理動作層 = 與 J 共用 `ILordAction` 決策骨架**。
> 同一 per-tick/per-lord 迴圈讀領主 comp，分對內/對外動作集。

### 對內動作（寫 city-economy comp，K:160）

- 蓋倉庫 / 修防禦（提 defenseLevel）/ 徵糧（食物↔忠誠權衡）。
- `GovernanceFactor`（政務×忠誠）調制動作成敗（K:160）。

### 對外動作（建哪種 outpost，J）

- 走 D 的 `CreateSettlement` 接點 / npc-outposts 增生（K:160）。
- **選型唯一處**：`OutpostPlacer.TryPlaceFor`（`OutpostPlacer.cs:11`），
  第 17-20 行 `profile.types.RandomElementByWeight` 純隨機；`type=null` 參數可繞過（J，:151）。
- **npc-outposts 加第三個 static hook `TypeSelector`**（仿既有 `GrowthRateMultiplier`/`ParentEligibilityOverride`，
  `WorldComponent_OutpostSpawner.cs:17/22`）：`type ??= TypeSelector?.Invoke(parent,profile)`；
  spawner（line 61/89）與 D 兩路徑同時受益、本體 hook=null 零變化（J，:152）。
- **權重函數註冊在 settlement-lords**：讀母聚落領主 comp 能力值 + RimWar `behavior` +
  Mod 1 `WorldComponent_OutpostWarMomentum` score（`GetGrowthMultiplierFor`/`Score`，已驗證存在）
  → 重加權 `profile.types`。**MVP=純權重函數，非 FSM**（J，:153）。

## 串接（J，:153-154 / K:160-161）

- 對內動作 → city-economy（`05`）。
- 對外動作 → D 的 `CreateSettlement`（`08` 進行中片段）+ npc-outposts TypeSelector。
- 成本/成長 → H 財富（`05`）/ G 成長（`03`）。

## 歸屬

- `TypeSelector` hook 在 **npc-outposts 本體**（J，:154）。
- 權重函數 + `ILordAction` 決策迴圈在 **settlement-lords mod**（J/K）。

## 風險

- **硬前置**（J 明寫，:154）：領主屬性 comp（named-officers）須先建，否則無能力值可讀。
- 決策迴圈 per-lord per-tick 開銷 → 節流（沿用 2500-tick 心跳，勿每 tick）。
- `ILordAction` 過度抽象 → MVP 只實作 2-3 個對內 + 1 個對外動作。
