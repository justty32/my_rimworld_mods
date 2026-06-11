# VOE 哨站增強（VOE Outpost Enhancement）

## 目標
為 Vanilla Outposts Expanded（VOE）的所有哨站加上升級系統，讓哨站從「固定產出」變成可長期投資的據點。透過 Harmony patch 注入哨站 gizmo 與產出／砲擊邏輯。

## 範圍
- **通用升級**：所有哨站可花白銀提升產出（上限約 ×2.2）。
- **砲兵哨站**：花鋼材／元件提升每輪彈量、降低冷卻、延長射程；可循環切換彈種（HE／燃燒／EMP／泡沫滅火）。
- 升級狀態存於 `WorldComponent_OutpostUpgrades`，由 `UpgradeService` 統一讀寫與扣費。
- 三語系（en / zh-Hant / zh-Hans）Keyed。

## 技術棧
C#（net48）；**硬相依 Harmony ＋ Vanilla Outposts Expanded**（`loadAfter vanillaexpanded.outposts`）。

## 對應 RimWorld 版本
1.6（編譯產物 `1.6/Assemblies/VOEOutpostEnhancement.dll`）。

## 關鍵文件
- `Source/WorldComponent_OutpostUpgrades.cs`：升級狀態存讀檔。
- `Source/UpgradeService.cs`：扣費與升級套用（注意：對 Outpost 須用 `outpost.Things` 而非 `GetDirectlyHeldThings()`，後者恆為 null）。
- `Source/Patch_Outpost_GetGizmos.cs`：注入升級／砲擊 gizmo。
- `Source/Patch_Outpost_ProducedThings.cs`、`Patch_Outpost_Range.cs`、`Patch_Artillery.cs`、`Patch_Strike.cs`：產出與砲擊邏輯。
- `Source/Dialog_ArtilleryFire.cs`：砲擊目標選擇視窗。
- `session_log.md`：執行記錄（含 UpgradeService null-ref 修復）。
