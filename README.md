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

### 戰爭叢集（Empire × Rim War）
讓 Rim War 的世界大戰真正打到玩家帝國與哨站上：附庸淪陷、哨站易主、戰時經濟回饋。彼此有明確依賴鏈（`npc-outposts-rimwar` → `empire-warfare` / `empire-outposts-war`）。

| Mod | packageId | 一句話 | 相依 |
| --- | --- | --- | --- |
| [`npc-outposts-rimwar`](npc-outposts-rimwar/) | `pas.outposts.rimwar` | 把 npc-outposts 的衛星哨站接進 Rim War 大戰略：哨站貢獻母聚落點數、可被戰隊攻打／易主／摧毀，戰局勝敗回饋增生速率。 | Harmony + Rim War + NPC Outposts |
| [`empire-warfare`](empire-warfare/) | `pas.empire.warfare` | 讓 Rim War 世界大戰真正打到玩家帝國：附庸聚落防守潰敗（或連敗）會正規淪陷、易主給攻方並註冊進 Rim War，可用帝國既有奪城作戰收復。 | Harmony + Empire Refactored + Rim War |
| [`empire-outposts-war`](empire-outposts-war/) | `pas.empire.outposts.war` | 帝國／Rim War／NPC 哨站三方膠水層：玩家附庸也能長衛星哨站（加稅收與防守）、哨站參與攻防戰、聚落易主時衛星哨站隨之易主。 | Harmony + Empire Refactored + Rim War + NPC Outposts + NPC Outposts: Rim War + Empire Warfare |
| [`rimwar-empire-economy`](rimwar-empire-economy/) | `pas.empire.wartimeeconomy` | 補上 Rim War 世界局勢回饋帝國經濟的缺口：附庸被戰隊圍攻時產出打折，帝國參戰期間全附庸加徵戰時稅。 | Harmony + Empire Refactored + Rim War |

### 三國叢集（Named Officers 生態）
以 `named-officers`（純 API 基礎層）為地基，替 Rim War 的抽象聚落／戰隊長出具名職官，帶三國志風味。`faction-politics`（列於世界大戰略叢集）亦為本叢集的可選消費者之一。

| Mod | packageId | 一句話 | 相依 |
| --- | --- | --- | --- |
| [`named-officers`](named-officers/) | `pas.officers.community` | 具名職官基礎層：世界物件掛載的職官（軍閥／太守／將軍）record，七維屬性＋雙軌關係＋懶生成，純 API 不含玩法，供三國志家族 mod 消費。 | — |
| [`warband-generals`](warband-generals/) | `pas.officers.warband` | 三國志風味：NPC 戰隊有機率出具名將領（武力／統率），左右抽象戰局勝負與傷亡，戰勝將領延續、戰隊團滅則除名。 | Harmony + Rim War + Named Officers |
| [`settlement-lords`](settlement-lords/) | `pas.officers.settlements` | 三國志風味：NPC 聚落逐步任命具名太守（政務／忠誠），能吏加速聚落 Rim War 點數成長、庸吏使其萎縮；城破或易主則卸任。 | Harmony + Rim War + Named Officers |
| [`city-economy`](city-economy/) | `pas.sanguo.cityeconomy` | 三國志風味：NPC 聚落擁有真實財富（銀／糧／貨）與防禦力，戰隊圍城先耗防禦、破城劫掠實際財富，交易回饋聚落經濟；裝 Settlement Lords 可加速成長。 | Harmony + Rim War（選配 Named Officers／Settlement Lords） |

### 開局世界工作流
把「給定 modlist → 產出配套開局世界」拆成引擎＋範本：`faction-relation-seeder` 是資料驅動的關係播種引擎，`opening-world-demo` 是串起佈景＋開局外交的範本。

| Mod | packageId | 一句話 | 相依 |
| --- | --- | --- | --- |
| [`faction-relation-seeder`](faction-relation-seeder/) | `pas.relations.community` | 資料驅動的開局派系關係播種器：XML 宣告善意矩陣，新遊戲開局套用一次即不再介入，定下開局政治幾何後放手給世界演化。 | — |
| [`opening-world-demo`](opening-world-demo/) | `pas.openingworld.demo` | 開局世界管線範本：Worldbuilder 佈景預設「兩大陣營」＋配套 Faction Relation Seeder 關係表，示範「佈景＋開局外交」如何串成一條可實機的管線。 | Faction Relation Seeder（選配 Worldbuilder） |
| [`ratkin-faction-preset`](ratkin-faction-preset/) | `justty32.RatkinAcornGuild` | 新增鼠族派系「橡實大遠征商會」：認真出包的搬運商會，隨附 Worldbuilder 世界預設「橡實之路」；沒裝 Worldbuilder 也能作為普通派系 mod 獨立生效。 | NewRatkinPlus（＋HAR＋Harmony）（選配 Worldbuilder） |
| [`faction-gear-seeder`](faction-gear-seeder/) | `pas.gear.community` | 管線的**裝備層**，與 relation-seeder 平行：XML 宣告 `FactionGearSeedDef`（逐 PawnKind 的強制服裝／武器／品質），套用到該派系生成的每個 pawn（襲擊者／商隊／居民／守衛）。單一 `PawnGenerator.GeneratePawn` postfix；defName 軟解析，缺件跳過不報錯。 | Harmony（loadAfter Worldbuilder） |
| [`gear-seed-demo`](gear-seed-demo/) | `pas.gear.demo` | faction-gear-seeder 的示範內容：PirateWaster 派系的單一 `FactionGearSeedDef`，由 yc's Faction Editor preset 經 `tools/transcribe_yc_preset.py` 轉寫。純資料零 C#。 | Faction Gear Seeder |

| [`worldpreset-anime2`](worldpreset-anime2/) | `justty32.worldpreset.anime2` | 二次元定製世界的 Worldbuilder 世界預設（對應部署側 profile `2-anime`）：30 個派系佈局＋27 份 `CustomIdeos/*.rid` 自訂信仰。純資料零 C#。⚠ **從未 in-game 驗證**（worldpreset 的 ideo headless 測不到）；另有 4 個信仰口味小偏待處理。 | Worldbuilder |

> 開局世界管線共三層：**佈景**（Worldbuilder preset）／**關係**（faction-relation-seeder）／**裝備**（faction-gear-seeder），各層都有對應的 demo 內容 mod。
>
> `worldpreset-anime2` 是 2026-08-27 從部署側收回的——它原本只存在於 `~/notes/projects/modding/rimworld/worldpreset-2-anime/`。**本目錄是原始碼權威版本**，部署側那份是副本，改完要重新複製過去才生效（同 `local-*-fix/` 系列的處理方式）。沿革見該目錄的 `SOURCE-HISTORY.md`。

### 任務線
| Mod | packageId | 一句話 | 相依 |
| --- | --- | --- | --- |
| [`ratkin-questlines`](ratkin-questlines/) | `justty32.ratkinquestlines` | 「有鼠族的世界」沉浸式任務包：以 CQF 製作的鼠族相遇任務系列，玩家以聚居點管理者身分接待商販／難民／騎士／掮客／學徒。已實作鐵匠屋 T1-T3 共 8 條 forge 委託。**目前最活躍的專案**（`PROJECT.md` 52k）。 | Harmony + Custom Quest Framework + NewRatkinPlus |

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
| [`ariandel-example-character`](ariandel-example-character/) | `pas.ariandel.examplecharacter` | Ariandel Library「特殊角色框架（SCMF）」純 XML 範例：具名獨特角色「燼歌者・薇絲珀」經虛境突入儀式入場、自動登錄特殊角色管理器、不會真死。 | Ariandel Library（含 Harmony） |
| [`dms-example-mech`](dms-example-mech/) | `pas.dms.examplemech` | DMS／FFF 生態純 XML 內容範例：超輕偵察機兵「紅隼」＋機兵武器／光學感測插件／（選配）龍騎兵部件，示範四條新增內容路徑。 | Biotech + Fortified Feature Framework + DMS Core（選配 Mobile Dragoon） |
| [`dryads-example-pack`](dryads-example-pack/) | `pas.dryads.examplepack` | 純 XML 示範新增 Gauranlen 樹精階型：樹脂樹精（定期產化學燃料）、救援樹精（可訓練拖回倒地隊友），生態位刻意與原版／VIE 不重疊。 | Ideology（軟：VIE Dryads 僅排序） |
| [`pocket-dimension-example`](pocket-dimension-example/) | `pas.pocketdimension.example` | 可建造「異空間門」範例：站在原版 pocket map 機制上生成小型持久異空間地圖，小人物資自由進出，拆門自動疏散回收。 | — |

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
