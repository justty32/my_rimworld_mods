# ariandel-example-character — 燼歌者・薇絲珀

Ariandel Library「特殊角色框架（SCMF）」的**純 XML 範例 mod**：一名具名、唯一、不會真死的原版人類英雄。

## 目標

- 示範用 Ariandel Library 創作特殊角色的最小完整集合（零 C#、零種族 mod 相依）。
- 範式來源＝官方教學範例 `Ariandel.UserGuideSCMF`（Workshop 3668177055），本 mod 把它從「米莉拉種族宿主」移植到原版人類，並補上「自訂主動技能」示範。
- 對應分析報告：`~/repo/pas/analysis/rimworld_mods/ariandel-library/tutorial/01_create_unique_character.md`。

## 角色設定

| 項目 | 值 |
|---|---|
| 姓名 | 薇絲珀・燼落（Vesper Cinderfall），暱稱薇絲珀 |
| 種族 | 原版人類（`ParentName="VillagerBase"`） |
| 固定身分 | 女性、34 歲、背景「灰燼孤兒／燼歌者」、特質「燼魂」＋堅韌 |
| 被動 | hediff「餘燼之心」：治癒 ×2、耐寒 -20°C（生成時強制掛上） |
| 主動技 | 「燼焰戰吼」：自我增益 60 秒（移速/閃避/近戰命中），冷卻 1 天，需帶餘燼之心 |
| 唯一性 | `SpecialPawnExtension.uniqueID=PAS_AshSinger` + `ShroudOutcomeDef.isUnique` |
| 不死 | `AL_Kill_Manager_Extension`：死亡改送回虛境，復活冷卻 45 天 |
| 入場 | 虛境突入儀式（建造 AL 內建 `AL_RitualSpot`，儀式結果池高權重抽中） |

## 相依

- 硬相依：`Ariandel.AriandelLibrary`（Workshop 3665997350；其自身硬相依 `brrainz.harmony`）。
- 不需 HAR、不需任何 DLC（Anomaly 相關 extension 用 `MayRequire` gate）。

## 檔案結構

```
About/About.xml                         相依宣告
1.6/Defs/
  AriandelLibrary.SpecialPawnTabDef/    SCM 自訂分頁
  BackstoryDef/                         固定背景故事 ×2（原版 BackstoryDef）
  TraitDef/                             專屬特質（掛 AL_NoSkillDecay/AL_LockSkill）
  HediffDef/                            被動「餘燼之心」＋戰吼增益
  AbilityDef/                           主動技（abilityClass=AL_Ability + 原版 GiveHediff comp）
  PawnKindDef/                          角色本體（必填三件套＋保護開關群）
  AriandelLibrary.ShroudOutcomeDef/     入場：儀式結果池
1.6/Languages/                          English Keyed + 繁中 Keyed/DefInjected
Textures/PAS_AEC/Icon/AshSinger.png     SCM 頭像（佔位圖，規格見下）
tests/healthcheck.py                    離線靜態健檢
```

## 貼圖規格（佔位待替換）

| 路徑 | 現況 | 規格 |
|---|---|---|
| `Textures/PAS_AEC/Icon/AshSinger.png` | 程式生成的火焰圖佔位 | SCM 面板頭像，**256x256 或 128x128 px** RGBA png（官方範例註解，PawnKinds_Milira_Sample.xml:210-211） |
| （技能圖示） | 借用核心 `UI/Commands/Attack` | 正式版換 64x64 png，放 `Textures/PAS_AEC/Ability/AshenWarcry.png`，`iconPath` 填 `PAS_AEC/Ability/AshenWarcry`（**不含 .png**） |
| （鎖定頭像） | 用 AL 內建 `AriandelLibrary/Icon/NotRecruited`（`iconPathLocked` 預設值） | 如需自訂，同頭像規格 |

## 完成定義

- [x] 全部 XML well-formed（`tests/healthcheck.py`）
- [x] AL class 名逐一對照反編譯源存在；vanilla 欄位對照 1.6 核心 Data 確認
- [ ] 實機：啟用 Harmony + Ariandel Library + 本 mod，開發者模式 spawn `PAS_AEC_AshSinger`，驗證固定姓名/特質/被動/技能
- [ ] 實機：建 `AL_RitualSpot` 行虛境突入儀式 → 薇絲珀入隊並出現在 SCM「PAS Heroes」分頁
- [ ] 實機：擊殺薇絲珀 → 不真死、進 45 天復活冷卻

## 已知留待實機驗證的點

- `weight=99999` 是否穩壓過 AL 內建結果池（官方範例同手法，應無虞）。
- `AL_Ability` 對「無消耗、純自我增益」技能的 CanCast 行為（若異常，退回 vanilla `abilityClass` 預設值即可，效果 comp 不變）。
- 繁中 DefInjected 的 backstory key 形態（`title/titleShort/description`）是否與 1.6 抽取器一致。
