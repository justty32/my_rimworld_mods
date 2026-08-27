# faction-politics 可行性調查（索引）

> 來源 idea：`analysis/rimworld_mods/_mod_ideas/world_map_grand_strategy/01_faction_scale_and_lifecycle.md`（idea 7+8：派系規模/生命週期 + 具名 NPC 政治）。
> 本調查對該報告做獨立源碼覆核（權威源 `C:\code\mine\pas\projects\rimworld`，2026-06-11），並依使用者四項範圍決策收斂出 P1 可行結論。

## 結論速覽

| 問題 | 結論 |
|---|---|
| 原版量級（~15-40 派系）下做動態政治 | **可行，零效能工程、零 Harmony**（詳 `01`） |
| 遊戲途中造新派系（分裂用） | **可行**：`NewGeneratedFaction(hidden:true)` 跳過自動聚落，事後揭示（詳 `02`） |
| 聚落倒戈易主 | **可行**：`WorldObject.SetFaction`，原版自家也這麼做（詳 `02`） |
| 母派系↔新派系立即敵對 | **可行**：揭示後 `TryAffectGoodwillWith` + `GoodwillToMakeHostile`（詳 `02`） |
| 具名反叛者 NPC 長期保存 + 拜訪時找得到 | **可行**：world pawn KeepForever + `previouslyGeneratedInhabitants` redress 橋，**整條鏈已靜態驗通**（詳 `03`） |
| Rim War 軟相容 | **可做骨架**：反射呼叫，但 Rim War DLL/反編譯源不在本機，簽名待校準（詳 `04`） |
| npc-outposts 軟相容（哨站跟隨倒戈） | **可行**：loadFolders `IfModActive` 條件載入 bridge assembly，機制已驗證（詳 `04`） |

## 使用者範圍決策（2026-06-11）

1. **規模＝原版量級 ~15-40**（不做活躍/休眠分層、不做名字層分離）。
2. **Rim War 軟相容 P1 就做**（因本機無 DLL，落地為反射骨架＋待校準）。
3. **P1 垂直切片＝反叛者→分裂一條龍**（同盟/合併/通用生命週期引擎留後）。
4. **玩家干預 P1 只做「找得到」**（安撫/煽動互動留 P2）。

## 對來源報告的修正（5 處）

1. **`CreateFactionAndAddToManager` 是 `public static void`**（FactionGenerator.cs:106/111），不回傳 Faction——分裂編排要拿引用，須自呼 `NewGeneratedFaction` + `FactionManager.Add`。
2. **`NewGeneratedFactionWithRelations` 避不開自動造聚落**（報告 §3.1 建議有誤）：它內部就是呼叫 `NewGeneratedFaction`（FactionGenerator.cs:210），自動聚落的唯一跳過條件是 `faction.Hidden`（:175）。正確路徑＝`parms.hidden = true` 生成。
3. **PawnGenerator.cs:236 是死碼**（報告 §5.4 引用的「新生成居民記入聚落清單」）：條件是 `request.Inhabitant && !request.Tile.Valid`，tile 無效時 `WorldObjectAt(tile)` 必然 null——**原版 1.6 從不自動填 `previouslyGeneratedInhabitants`**（疑原版 bug，條件應為正向）。我們自己 Add 的橋不受影響，反而成為唯一供給者（無他源污染）。
4. **redress 過濾是 race + faction，不是 PawnKindDef 全等**（PawnGenerator.cs:371-375）——反叛者用任何 humanlike kind 生成都能被居民請回，kind 選擇自由度比報告假設大。
5. **hidden 期間 `HasGoodwill = false`**（Faction.cs:202-212：`!Hidden && !temporary`）——分裂編排的關係設定必須在揭示**之後**走 goodwill 路徑（或揭示前走 `SetRelationDirect`，此時防呆不觸發）。順序敏感，詳 `02`。

## 本調查新增驗證（報告未涵蓋）

- redress 供給鏈全通：`SymbolResolver_Settlement.cs:59`（`inhabitants = true`）→ `PawnGroupKindWorker_Normal.cs:67-71`（request 帶 tile + inhabitants）→ `PawnGenerator.cs:210-220`（從 `previouslyGeneratedInhabitants` 優先 redress）。
- `Faction.leader` 是 public 欄位（Faction.cs:26）、`Faction.hidden` 是 `public bool?`（:54）→ 都可直接賦值。
- `AllFactionsVisible` 是即時 LINQ filter（FactionManager.cs:43）→ 事後揭示 hidden 派系**不需任何 recache**。
- `SetRelation(FactionRelation)` 雙向寫入但反向那筆**不複製 baseGoodwill**（Faction.cs:443-449）→ 不採用它做敵對設定（詳 `02`）。
- loadFolders 的 `IfModActive` 屬性為原版機制（Verse\ModLoadFolders.cs:53）→ 條件載入 bridge assembly 成立。
- 本機環境：`pas/projects` **無** `rimworld_mods`（Rim War 反編譯源在使用者另一台機器）、Steam workshop 目錄不存在 → Rim War bridge 無法編譯期引用，**只能反射**（詳 `04`）。

## 檔案地圖

| 檔 | 內容 |
|---|---|
| `01-scale-and-performance.md` | 原版量級下的效能結論、不真刪派系決策、防膨脹上限 |
| `02-faction-creation-and-split.md` | 途中造派系 + 分裂編排逐步 API（含順序敏感點） |
| `03-named-npc-and-location-bridge.md` | 反叛者生成/保存/定位橋/自癒維護（含 :236 死碼發現） |
| `04-bridges-rimwar-outposts.md` | Rim War 反射 bridge + npc-outposts 條件 assembly bridge |
| `05-risks-and-verification.md` | 風險分級 + 實機待驗證清單 |
