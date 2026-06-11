# 身體強化 Hediff（Body Fortification Hediff）

## 目標
提供一個特殊 hediff「身體強化」，依 severity 等級讓 pawn 所有身體部位的耐久度倍增，達成「硬到難以被打殘」的效果。透過 Harmony patch 在傷害結算階段套用倍率。

## 範圍
- 單一 `HediffDef`：`BFH_BodyFortification`，三個 severity 階段——輕度（×2，0.0–0.33）／中度（×5，0.34–0.66）／極限（×10，0.67–1.0）。
- `HediffComp_BodyFortification`（comp props 帶 `multiplier`）紀錄當前倍率。
- `DamageWorker_Patch`：Harmony patch 注入傷害結算，依 hediff 倍率折減進入部位的傷害。
- 三語系（en / zh-Hant / zh-Hans）Keyed。
- 套用方式：開發者模式 → Add hediff。

## 技術棧
C#（net48）＋ XML HediffDef；**硬相依 Harmony**（`loadAfter brrainz.harmony`）。namespace `BodyFortificationHediff`；Harmony id `justty32.BodyFortificationHediff`；defName 前綴 `BFH_`。

## 對應 RimWorld 版本
1.6。

## 關鍵文件
- `1.6/Defs/HediffDefs/BodyFortification.xml`：hediff 定義與三階段。
- `Source/DamageWorker_Patch.cs`：傷害折減 patch。
- `Source/HediffComp_BodyFortification.cs`：倍率 comp。
- `Source/BodyFortificationMod.cs`：Harmony 啟動。
