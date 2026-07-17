# Opening World: Two Blocs（開局世界管線・示範）

> RimWorld 1.6。packageId `pas.openingworld.demo`。**純資料，無 C#。**
> 這是「開局世界工作流」的第一步落地：把 **Worldbuilder preset（佈景）** 與
> **Faction Relation Seeder（開局關係）** 串成一條可實機的管線。下一步規劃：接 yc faction editor。

## 這條管線是什麼

| 層 | 由誰負責 | 本 mod 提供 |
|---|---|---|
| 佈景（派系陣容、名稱/顏色/描述、生成參數） | **Worldbuilder** preset | `Worldbuilder/OpeningWorldTwoBlocs/Preset.xml` |
| 開局敵友（關係矩陣） | **Faction Relation Seeder** 引擎（`pas.relations.community`） | `1.6/Defs/Relations/OpeningRelations.xml`（一個 `RelationSeedDef`） |

**為什麼需要兩者**：worldbuilder preset **不支援關係矩陣**（只有 permanentEnemy 布林，
見 `analysis/rimworld_mods/worldbuilder/details/02_boundaries_extension_points.md`）。
worldbuilder 源碼自己建議的補法就是「`World.FinalizeInit` 掛鉤遍歷 factions 設 goodwill」——
我們的 seeder 正是這個（用 WorldComponent.FinalizeInit，零 Harmony）。兩者天生互補。

## 示範內容（中性，純原版派系）

- **藍盟**（定居者）：`OutlanderCivil` + `OutlanderRough`，藍色，內部結盟（+90）。
- **赤盟**（部族）：`TribeCivil` + `TribeRough`，紅色，內部結盟（+85）。
- **兩盟交戰**：四對交叉全敵對（-100）。
- **海盜**：永久敵，關係表不列（列了也會被 seeder 軟略過）。

→ 開新局即得「兩大陣營對峙」的開局政治幾何，一眼可辨（顏色）＋外交面板可查（敵友）。

## 檔案

| 檔案 | 內容 |
|---|---|
| `About/About.xml` | packageId `pas.openingworld.demo`；硬相依 `pas.relations.community`（RelationSeedDef 型別）；loadAfter worldbuilder |
| `loadFolders.xml` | `/`（載 Worldbuilder/、About/）+ `1.6`（載 Defs/） |
| `Worldbuilder/OpeningWorldTwoBlocs/Preset.xml` | 佈景 preset（saveFactions + saveFactionCustomizations + saveGenerationParameters；不開 saveTerrain/saveBases/saveIdeologies） |
| `1.6/Defs/Relations/OpeningRelations.xml` | `RelationSeedDef OWD_OpeningRelations`（6 對關係） |
| `tests/healthcheck.py` | 管線一致性靜態檢：preset schema 白名單、派系存在性、seed 引用皆在 preset 陣容內、saveFactionCustomizations 開關 |

## 相依關係

- **硬**：`pas.relations.community`（faction-relation-seeder 引擎；沒它 `RelationSeedDef` 型別無法解析→報錯）。
- **軟**：`ferny.Worldbuilder`（沒它，preset 惰性不出現在清單，但 `RelationSeedDef` 仍照常在新局套用）。

## 狀態

- ✅ healthcheck PASS（管線資料一致：5 派系、6 對關係、開關正確、defName 全對原版 Core）。
- ⏳ **未實機**。E2E 步驟：
  1. 啟用 Worldbuilder + Faction Relation Seeder + 本 mod → 新遊戲。
  2. 建世界頁選 preset「兩盟對峙（Demo）」→ 生成。
  3. 看 log 有 `[relation-seeder] 播種完成：套用 6 對…`。
  4. 外交/派系面板核對：藍盟兩支互盟、赤盟兩支互盟、藍↔赤全敵對；派系顏色藍/紅、名稱已改。
  5. 存檔再讀 → 關係不重刷；不裝 worldbuilder 單測 → preset 消失但關係仍套用。

## 換成你的主題

把 `Preset.xml` 的 `savedFactionDefs`/override 換成你的 FactionDef，`OpeningRelations.xml` 的
`a`/`b`/`goodwill` 換成你的敵友表（兩邊派系 defName 要一致；healthcheck 會擋不一致）。
