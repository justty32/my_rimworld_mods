# Body HP ×10（Debug）

## 目標
Debug／測試用 mod：提供單一 hediff，把攜帶者所有身體部位的最大 HP 乘上一個倍率（預設 ×10，設定面板可調）。用來快速造出「打不死」的測試對象。

## 範圍
- 單一 `HediffDef`：`BodyHPX10`。
- `BodyHPX10Patch`：Harmony patch 在部位最大 HP 計算處套用倍率。
- `BodyHPX10_Settings` Mod 設定：倍率滑桿（1–100，預設 10），存於 `ModSettings`。
- 三語系（en / zh-Hant / zh-Hans）Keyed。
- 套用方式：開發者模式 → Add hediff。

## 技術棧
C#（net48）＋ XML HediffDef；**硬相依 Harmony**。namespace `BodyHPX10`；Harmony id `justty32.BodyHPX10`。

## 對應 RimWorld 版本
1.6。

## 關鍵文件
- `1.6/Defs/HediffDefs/BodyHPX10.xml`：hediff 定義。
- `Source/BodyHPX10Patch.cs`：最大 HP 倍增 patch ＋ Mod 設定面板。

## 備註
與 `body-fortification-hediff` 為姊妹案：此案直接放大最大 HP（debug 取向、倍率可調），前者依 severity 折減傷害（玩法取向、分階段）。
