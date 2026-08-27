# session_log — faction-gear-seeder

- 2026-07-18 新建：開局世界管線的**第三層＝裝備層**，補 yc's Faction Editor 那塊
  「能在遊戲內把派系裝備調好、卻無法隨管線發佈」的空缺（緣起見 PROJECT.md／
  analysis/rimworld_mods/yc-faction-editor/analysis.md）。
  - 使用者兩個裁示：(1) 由使用者進遊戲用 yc 填一個真 preset 當樣本；(2) 追求「完整裝備保真」，
    **破例引 Harmony**（vanilla 純資料表達不了確定性強制 loadout）。
  - 重新反編譯 DLL（ilspycmd）鎖定 yc 的 Scribe schema：`FactionGearData`（factionDefName/kindGearData/
    groupMakers/label…）、`KindGearData`（簡單池 GearItem{thingDefName} ＋進階 SpecRequirementEdit
    {thing/material/quality/color} ＋forceOnlySelected/forceNaked/itemQuality…）、`PawnGroupMakerData`
    （→vanilla PawnGroupMaker 1:1）。config XML 用的就是這些欄位名。
  - 設計：`FactionGearSeedDef : Def`（factionDef + List<GearKindEntry{kindDef/forceOnlySelected/
    forceNaked/quality/apparel/weapons}>，GearItemEntry{thingDef/stuff/quality/color}）
    + `GearSeedApplier`（單一 `PawnGenerator.GeneratePawn` postfix；ThreadStatic 防遞迴；
    ProgramState.Playing 守衛；faction 取 `request.Faction ?? __result.Faction`——皆鏡像 yc 成熟做法）。
  - 套用只用 vanilla API：`ThingMaker.MakeThing(def,stuff)`→`CompQuality.SetQuality`／`CompColorable.SetColor`
    →`pawn.apparel.Wear`／`pawn.equipment.AddEquipment`；forceOnlySelected/forceNaked 先脫既有。
    逐件例外隔離、缺席軟略過（機械族/異種穿不了→跳過）。
  - `tools/transcribe_yc_preset.py`：yc preset（`--config ... --preset aa`）／存檔（`--save`）／即時
    （`--live`）→ FactionGearSeedDef XML。合併簡單池＋進階逐件、保留 stuff/quality/color、
    無裝備兵種略過、空來源給友善提示。
  - dev「對已生成 pawn 重套裝備」DebugAction（肉眼驗證免重生 raid）。
  - ✅ dotnet build（net48／Krafs 1.6.* + Lib.Harmony 2.3.3 compile-only，零警告）→
    1.6/Assemblies/FactionGearSeeder.dll（確認只此一 DLL，未洩漏 Harmony/RimWorld）。
  - ✅ tests/healthcheck.py OK：含**反向 Harmony 不變式**（斷言確實用 Harmony 且補丁面僅
    PawnGenerator.GeneratePawn）＋轉換器合成樣本煙霧測試。
  - ⏳ **實機 E2E 未跑**。封鎖點：yc preset `aa` 目前為空（config／所有存檔的 factionGearData 皆
    `<... />`），需使用者先在遊戲內填一個真實裝備編輯 → Save Preset，才能謄真資料 → 建示範 Def
    → 開新局看派系穿對裝備。轉換器對真空 preset 已驗會給友善提示。

- 2026-07-18（續）使用者填了真 preset（實名 `asaf`，非 `aa`）：派系 `PirateWaster`、20 兵種。
  謄回跑通（1 派系/20 兵種）。**看真資料才修正一個語意 bug**：
  - yc 的 `apparel/weapons` 是**加權選池**（pick-from），不是「全部穿上」；`Pirate` 有 30 把武器池＝挑 1。
    `forceOnlySelected` 在 XML 不出現＝Scribe 省略了預設值 `true`（＝確實脫光只用池）。
    → 引擎改為：武器從非-alwaysTake 池加權挑 1 當主武器、alwaysTake（手雷等）進背包；
    衣物 alwaysTake 先穿、其餘依權重隨機序逐件試穿、與已穿 `ApparelUtility.CanWearTogether` 不衝突才穿
    （挑一套連貫穿搭）。Def 的 `GearItemEntry` 加 `weight`/`alwaysTake`；轉換器帶上兩者
    （簡單池→weight/非強制；specific*→alwaysTake＋stuff/quality/color）。真實例：`Mercenary_GunnerTox`
    謄出品質 Poor＋武器池帶分數權重＋`Weapon_GrenadeTox` alwaysTake（→背包）。重編 0 警告、healthcheck 綠。
  - 建示範內容 mod **gear-seed-demo**（`pas.gear.demo`，純資料，硬相依引擎），
    塞 `1.6/Defs/Gear/Gear_PirateWaster.xml`（謄好的真資料）。自帶 healthcheck 綠。
  - **打包＋部署**：package-mod.sh 打包 FactionGearSeeder-0.1.0 ＋ GearSeedDemo-0.1.0；
    symlink 進 `~/rimworld_mods` 及遊戲 `Mods/`（三層 symlink 鏈確認通、DLL/Def/相依都在）。
  - README 兼 E2E 回歸步驟已寫。**現況：等使用者實機跑 E2E**（啟用 Harmony＋引擎＋示範，
    生成 PirateWaster 突襲，驗 GunnerTox 品質 Poor＋背包 tox 手雷＋連貫穿搭、無紅字）。

- 2026-07-18（續2）**實機 E2E 綠**（部署側經 inbox 回執 `gear-seeder-e2e-result`）：
  第 1 關 headless PASS（48s 進主選單、Harmony 補丁就緒訊號、NRE 0、零真紅字）；
  第 3 關桌面實機 PASS（PirateWaster `Re-apply gear` 命中 N>0、全程零 `[gear-seeder]` 例外、不刷紅字、
  非本派系正確跳過；使用者目視 loadout 連貫非隨機）。首個 E2E-verified cut。
  可選加嚴：GunnerTox 三細節僅目視未逐項硬簽收（非阻塞）。
