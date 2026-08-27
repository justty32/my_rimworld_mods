# ancot-vfx-example

Ancot Library（`Ancot.AncotLibrary`，Workshop 2988801276）特效系統的最小可載入純 XML 展示 mod。

## 目標

用零 C#、零自製貼圖示範三條 Ancot VFX 觸發路線：

| # | 展示物 | 路線 | Def 檔 |
|---|---|---|---|
| 1 | `AVE_TestGun` 測試手槍 | 投射物拖尾＋命中火花：`AncotLibrary.Projectile_Custom` + `Projectile_Custom_Extension`（trailFleck/impactEffecter） | `1.6/Defs/Weapons_Ranged.xml` |
| 2 | `AVE_TestSword` 測試劍 | 近戰命中刀光：自訂 ManeuverDef + `AncotLibrary.Verb_MeleeAttackDamage_Effecter` + `Effecter_Extension`，自訂 EffecterDef 染色複用 Ancot 內建刀光 fleck | `1.6/Defs/Weapons_Melee.xml` |
| 3 | `AVE_SmokeAura` 能力 | 常駐煙霧光環：`CompProperties_AbilityGiveHediffOnSelf` → hediff 帶 `HediffCompProperties_EffecterMaintain` | `1.6/Defs/Ability_Aura.xml` |

## 相依 / 載入序

`brrainz.harmony` → `Ancot.AncotLibrary` → 本 mod（About.xml 已列 modDependencies + loadAfter）。不需要 Milira/Kiiro。

## 貼圖佔位說明

全部借用原版或 Ancot 內建貼圖，自製時規格：
- 武器：512x512 RGBA PNG，朝右上 45 度（texPath 於各 Def 註解處替換）
- fleck：白/灰階發光形＋透明背景（斬擊類 512x512、點光 128x128），白圖才能被 `<color>`/trailColor 染色
- 能力圖示：64~128 平方 RGBA PNG

## 驗證（未實機，留待遊戲內確認）

1. 啟用 Harmony + Ancot Library + 本 mod，開 dev quicktest。
2. Dev mode → Spawn thing → `AVE_TestGun` / `AVE_TestSword` 給殖民者，攻擊目標：槍應有青色十字拖尾＋命中火花；劍命中應有橘色刀光弧。
3. Dev mode → 選 pawn → Give ability → `AVE_SmokeAura`，施放後 pawn 冒煙 10 秒。
4. 已知風險：`trailFreauency` 拼字必須照源碼；`Effecter_Extension` 欄位是 `effcterDef`。

## 分析依據

`~/repo/analysis/rimworld_mods/ancot-library/`（`architecture/00_overview.md`、`details/vfx_system.md`），反編譯源 `~/repo/projects/rimworld_mods/ancot-library/`。
