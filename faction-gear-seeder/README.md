# Faction Gear Seeder — 使用與實機驗證

派系裝備播種器：宣告「某派系 → 各兵種強制裝備/武器/品質」，該派系生成的 pawn 自動穿上。
「開局世界管線」第三層（佈景 Worldbuilder → 關係 relation-seeder → **裝備 本 mod**）。

## 安裝（載入順序）

1. **Harmony**（`brrainz.harmony`）
2. **Faction Gear Seeder (Community)**（`pas.gear.community`，本引擎）
3. 你的裝備內容 mod（硬相依本引擎，提供 `FactionGearSeedDef`）——
   示範見 **Gear Seed Demo (PirateWaster)**（`pas.gear.demo`）。

## 作者流程（yc → 管線）

1. 遊戲內用 **yc's Faction Editor** 把某派系某兵種的裝備/武器/品質調好 → **Save Preset**。
2. `python3 tools/transcribe_yc_preset.py --config "<Mod_..._FactionGearCustomizerMod.xml>" --preset <名> --out MyGear.xml`
3. 把 `MyGear.xml` 放進你的內容 mod `1.6/Defs/`，About 硬相依 `pas.gear.community`。**發佈物不需玩家裝 yc。**

## 實機 E2E 回歸步驟（PirateWaster 示範）

前置：啟用 Harmony + Faction Gear Seeder + Gear Seed Demo，重啟遊戲。

1. **啟動期**：開發者主控台應見 `[gear-seeder] Harmony 補丁就緒（PawnGenerator.GeneratePawn postfix）。`，**無紅字**。
2. **生成 PirateWaster pawn**：載入任一有 PirateWaster 的存檔（或開新局），dev mode →
   生成一場 PirateWaster 突襲（或 dev「Try raid」選 PirateWaster）。
3. **檢查裝備**：突襲 pawn 應穿戴/持有謄回的 loadout（而非全隨機）。特別驗：
   - `Mercenary_GunnerTox`：裝備品質為 **Poor**、背包裡有 **Weapon_GrenadeTox**（tox 手雷，
     不是當主武器）、主武器為池中一把槍（Autopistol 權重最高）。
   - 各兵種是一套**不互相衝突**的連貫穿搭（不會同時穿三件外套）。
4. **既有 pawn 手動重套**（可選）：dev actions → `pas.gear` → **Re-apply gear (spawned pawns)** →
   對地圖上 PirateWaster pawn 立即重套，訊息報「重套裝備於 N 隻」。
5. **無回歸**：非 PirateWaster 的派系 pawn 生成不受影響、無紅字/NRE。

通過 = 首個 E2E-verified cut。失敗請貼主控台紅字。
