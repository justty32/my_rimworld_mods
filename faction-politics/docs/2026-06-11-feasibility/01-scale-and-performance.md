# 01 規模與效能結論

## 決策：原版量級（~15-40 派系），零效能工程

來源報告 §2 的核心發現（本調查抽查屬實）：

- `Faction.relations` 每派系各存一份對所有他派系的關係，雙向寫入（Faction.cs:398-407）→ 總儲存 **O(N²)**；`RelationWith` 線性搜尋 → 單查 O(N)。
- 每個 humanlike 派系誕生即造 1 個首領 world pawn，`PassToWorld(KeepForever)` 永不被 GC（WorldPawnGC.cs:192-194 對 `IsFactionLeader` 直接回 critical）。
- 派系滅亡原版只標 `defeated`，不真刪；`FactionManager.Remove` 是 private 且只收 `temporary` 派系（FactionManager.cs:107-112，已驗證）。

**在 N≈15-40 的量級，這三者全部無感**：關係筆數 ~40²=1600（原版開局本來就有數百筆）、首領 pawn 數十個（原版常態）、defeated 派系殘留是原版既有語意。因此：

1. **全部用原版 Faction**，不做活躍/休眠分層、不做名字層分離。
2. **不真刪派系**——沿用原版 defeated 語意，完全避開報告 §3.2 的自管刪除雷區（`RemoveAllRelations` O(N²)、首領 GC 解鎖、quest/site/caravan cross-ref 炸點）。
3. **不用 `temporary` 派系**——它無 goodwill、無首領、不參與外交（Faction.cs:166、202-212），與「完整政治勢力」需求衝突（報告 §3.2 的權衡，我們選完整外交）。

## 防膨脹機制（取代「真刪」）

分裂會讓派系數隨遊戲時長單調遞增。控制手段：

- **動態派系總上限**（`PoliticsSettingsDef.maxDynamicFactions`，預設 5）：由本 mod 分裂誕生的派系（含已 defeated 者）計數達上限後，反叛進展凍結在閾值下，不再觸發分裂。
- 上限可由第三方 XML patch 調整；上限觸頂是穩態而非錯誤（letter/log 不刷）。
- 最壞情況：原版 ~15 派系 + 5 動態派系 = 20，關係筆數 ~400，毫無壓力。

## 本 mod 自身的 tick 成本

- 單一 `WorldComponent`，每 2500 tick 跑一輪：遍歷 `Find.FactionManager.AllFactionsListForReading`（~40 項）+ 每追蹤派系一筆 record 的進展加法。成本與 sims-mode/npc-outposts 的 WorldComponent 同級，可忽略。
- 每追蹤派系新增 1 個反叛者 world pawn（KeepForever）：~30 個 pawn，與原版首領同量級。mothball 機制（WorldPawns.cs:435，每 15000 tick）會把不在地圖上的 pawn 轉低頻 tick。

## 存檔影響

- 反叛 record（faction ref + pawn ref + settlement ref + float）× ~30：可忽略。
- 反叛者 pawn deep-save × ~30：與原版首領同量級，可忽略。
- 動態派系：每分裂 +1 Faction（含 relations ~40 筆 + 1 leader pawn），上限 5 → 可忽略。
