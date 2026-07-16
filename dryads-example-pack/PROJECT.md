# Dryads Example Pack: Resinmaker and Rescuer

以「定義新樹精」為題的最小示範 mod：兩種新 Gauranlen 樹精階型，純 XML、零 Harmony、零框架依賴（僅需 Ideology DLC）。配套分析報告在 `~/repo/pas/analysis/rimworld_mods/vanilla-ideology-expanded-dryads/tutorial/01_define_new_dryad.md`。

## 目標與範圍

- 示範新增一種樹精的**最小 def 三件套**：`ThingDef`（race，掛能力）＋ `PawnKindDef`（掛貼圖）＋ `GauranlenTreeModeDef`（模式選單條目）。
- 驗證「進樹的模式選單**不需要任何 patch**」：`Dialog_ChangeDryadCaste` 直接枚舉 `DefDatabase<GauranlenTreeModeDef>.AllDefs`（反編譯源 `RimWorld/Dialog_ChangeDryadCaste.cs:51`）。
- 兩種樹精刻意選**與原版六種及 VIE Dryads 四種都不重疊的生態位**：

| 樹精 | 生態位 | 能力機制（皆純 XML） |
|---|---|---|
| 樹脂樹精 `DEP_Dryad_Resinmaker` | 化工燃料生產（無人覆蓋） | 原版 `CompProperties_Spawner` → 35 化學燃料/2 日；Flammability 1.5 當代價 |
| 救援樹精 `DEP_Dryad_Rescuer` | 戰場救援（Carrier 只會拉貨） | `trainability=Advanced` + `trainableTags=Basic,Help` → Rescue 訓練項；連樹自動練滿（`CompTreeConnection.ResetDryad`） |

## 技術棧

- RimWorld 1.6 + Ideology DLC（`modDependencies`）；`loadAfter` VIE Dryads（僅為選單欄位排序穩定，非硬依賴）。
- 純 XML（Defs + Languages/ChineseTraditional DefInjected + Textures），無 Source/、無 Assemblies/。

## 關鍵檔案

- `Defs/ThingDefs_Races/Races_Animal_DryadsExample.xml` — 兩組 race + kind（含逐段註解與原版出處行號）
- `Defs/GauranlenTreeModDefs/GauranlenTreeModeDefs_Example.xml` — 兩個模式條目；`requiredMemes`/`previousStage` 以註解示範
- `Languages/ChineseTraditional/DefInjected/` — 繁中翻譯
- `Textures/Things/Pawn/Animal/DEP_Dryad_*/` — 佔位貼圖（規格見同層 README.md），可由 `tests/gen_placeholder_textures.py` 重生
- `tests/healthcheck.py` — 離線健檢：XML well-formed + def 交叉引用

## 完成定義 / 驗證狀態

- [x] 所有 XML well-formed（xmllint）
- [x] healthcheck 交叉引用通過（modeDef.pawnKindDef → PawnKindDef → race → ThingDef；texPath → 實體 PNG）
- [ ] **實機驗證（未做，本次任務明確不啟動遊戲）**：
  1. 模式選單出現兩個新條目、圖示位置不與原版/VIE 重疊（drawPosition x=0.4166 欄）
  2. 換模式後基本樹精進繭 → 變形為新樹精、貼圖三向顯示正常
  3. 樹脂樹精每 2 日產 35 化學燃料（訊息通知）
  4. 救援樹精連樹後 Rescue 自動練滿，倒地小人會被拖回床（留意 body 0.7 ≥ Rescue minBodySize 0.65）
  5. 繁中 DefInjected 生效
  6. 佔位貼圖僅供載入驗證，正式貼圖需重繪（256×256 RGBA，_north/_south/_east + Dessicated_*_east）

## 部署提醒（倉庫慣例）

實體不放遊戲目錄；要實測時放 `~/rimworld_mods/dryads-example-pack` 並在遊戲 `install/Mods` 建 symlink（見 MEMORY：RimWorld mod 部署）。
