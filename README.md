# my_rimworld_mods

justty32（pas / GuanYu Lu）的 RimWorld **1.6** mod 集合。每個子目錄是一個獨立 mod，自帶 `About/`、原始碼（`Source/`）、Def（XML）、語系與（多數附）`PROJECT.md`／`session_log.md`／`docs/`。

## Mod 一覽

### 世界大戰略叢集
源自 `world_map_grand_strategy` 構想軌的子 mod，讓 NPC 世界長出血肉。彼此可組合，多為零 Harmony、零硬相依（叢集內部相依除外）。

| Mod | packageId | 一句話 | 相依 |
| --- | --- | --- | --- |
| [`sims-mode-community`](sims-mode-community/) | `pas.sims.community` | 拜訪非敵對聚落時居民真的「過生活」——日間工作、傍晚聚會、夜間就寢；全資料驅動可被其他 mod patch。 | — |
| [`npc-outposts`](npc-outposts/) | `pas.outposts.community` | NPC 派系在聚落周圍長出衛星哨站，可拜訪／交易／攻打；世界隨時間增生。 | Sims Mode Community |
| [`faction-politics`](faction-politics/) | `pas.politics.community` | 派系內具名反叛者隨時間累積反叛，達閾值分裂出新敵對派系，聚落（含哨站）倒戈易主。 | （推薦 Sims/Outposts；軟橋 Rim War） |
| [`colony-archival-outpost`](colony-archival-outpost/) | `pas.colonyarchival.outpost` | 採樣一座殖民地的淨庫存增長，封存成持續產出的抽象哨站。 | VEF + VOE + Harmony |
| [`voe-outpost-enhancement`](voe-outpost-enhancement/) | `justty32.VOEOutpostEnhancement` | 為所有 VOE 哨站加升級系統：花銀提升產出、砲兵哨站可加彈量／減冷卻／延射程／切彈種。 | VOE + Harmony |

### 對話擴充
| Mod | packageId | 一句話 | 相依 |
| --- | --- | --- | --- |
| [`speakup-context-expansion`](speakup-context-expansion/) | `pas.speakup.contextexpansion` | 在 SpeakUp 之上新增三組殖民地層級情境變數（威脅／糧食危機／近期死亡）與對應中文台詞。 | SpeakUp + Harmony |

### 框架驗證
| Mod | packageId | 一句話 | 相依 |
| --- | --- | --- | --- |
| [`cqf-caravan-redemption`](cqf-caravan-redemption/) | `pas.cqf.caravanredemption` | 最小可行驗證：用 Custom Quest Framework 純 XML 路徑證明自訂 QuestScriptDef 可載入並觸發。 | CQF + Harmony |
| [`cqf-example-quests`](cqf-example-quests/) | `pas.cqf.examplequests` | CQF 教學範例集：任務鏈＋延時排程＋條件分支＋手寫 DialogTreeDef 對話樹（以物易物／對話接任務），全純 XML。 | CQF + Harmony |
| [`ancot-vfx-example`](ancot-vfx-example/) | `pas.ancot.vfxexample` | Ancot Library 特效系統純 XML 展示：拖尾+命中火花測試槍、刀光測試劍、煙霧光環能力。 | Ancot Library + Harmony |
| [`vpe-example-path`](vpe-example-path/) | `pas.vpe.examplepath` | 示範在 Vanilla Psycasts Expanded 新增一條完整靈能道路「寧神者」（5 能力 3 層樹），全純 XML 零 C#。 | Royalty + VEF + VPE + Harmony |

### 實驗 / 工具 Hediff
| Mod | packageId | 一句話 | 相依 |
| --- | --- | --- | --- |
| [`body-fortification-hediff`](body-fortification-hediff/) | `justty32.BodyFortificationHediff` | 特殊 hediff「身體強化」，依 severity（輕 ×2／中 ×5／極限 ×10）倍增所有部位耐久。 | Harmony |
| [`body-hp-x10`](body-hp-x10/) | `justty32.BodyHPX10` | Debug mod：單一 hediff 把攜帶者所有部位最大 HP ×10（倍率可在設定調整）。 | Harmony |

## 倉庫慣例

- **版本**：全部目標 RimWorld 1.6；編譯產物放各 mod 的 `1.6/Assemblies/*.dll`（隨倉庫提交以便直接安裝）。
- **建置產物**：`bin/`、`obj/`、`*.user` 等由根目錄 [`.gitignore`](.gitignore) 統一忽略。
- **子項目文件**：較完整的 mod 附 `PROJECT.md`（目標／範圍／技術棧／完成定義／關鍵文件）、`session_log.md`（執行記錄）與 `docs/`（設計 spec、實作計畫、健檢）。
- **靜態健檢**：部分 mod 在 `tests/healthcheck.py` 提供離線 Def／型別檢查（不啟動遊戲）。

## 作者

justty32（亦署名 pas / GuanYu Lu）。
