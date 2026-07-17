# Opening World: Two Blocs (Demo) — 使用說明

一個可實機的「開局世界」示範，把兩件事串成一條管線：

- **Worldbuilder preset（佈景）** — 決定世界上有哪些派系、長什麼樣、怎麼生成。
- **Faction Relation Seeder（開局關係）** — 決定這些派系開局是敵是友。

單靠 Worldbuilder 做不出「派系關係矩陣」（它只有一個 permanentEnemy 開關），所以由 Seeder 補上。
這個 mod 就是兩者的**配套內容**：一張 preset + 一張關係表，用純原版派系示範。

---

## 一、需要哪些 mod（相依）

| mod | 角色 | 必要性 |
|---|---|---|
| **Harmony**（`brrainz.harmony`） | 前置 | 必要（Worldbuilder 需要） |
| **Vanilla Expanded Framework**（`OskarPotocki.VanillaFactionsExpanded.Core`） | 前置 | 必要（Worldbuilder 需要） |
| **Worldbuilder**（`ferny.Worldbuilder`） | 佈景引擎 | **建議**（沒它 preset 不出現，但關係仍會套用） |
| **Faction Relation Seeder**（`pas.relations.community`） | 關係引擎 | **必要**（本 mod 硬相依它；缺了會報錯） |
| **本 mod**（`pas.openingworld.demo`） | 內容（preset + 關係表） | — |

## 二、載入順序（由上到下）

```
Harmony
Vanilla Expanded Framework
Worldbuilder
Faction Relation Seeder        (pas.relations.community)
Opening World: Two Blocs (Demo)  (pas.openingworld.demo)   ← 必須在以上兩個引擎之後
```

## 三、怎麼玩（啟用步驟）

1. 依上表啟用全部 mod，順序如上。
2. 開始新遊戲 → 進到**建立世界**頁。
3. 在 Worldbuilder 的世界預設清單裡選 **「兩盟對峙（Demo）」**（preset 名 `OpeningWorldTwoBlocs`）。
4. 生成世界、照常開局。

## 四、開局會看到什麼

這張 demo 把原版派系分成兩個對立陣營：

| 陣營 | 派系 | 顏色 | 陣營內 |
|---|---|---|---|
| **藍盟**（定居者） | 藍盟・商邦（OutlanderCivil）、藍盟・邊民（OutlanderRough） | 藍 | 互相**結盟**（+90） |
| **赤盟**（部族） | 赤盟・聚落（TribeCivil）、赤盟・獵團（TribeRough） | 紅 | 互相**結盟**（+85） |

- **藍盟 ↔ 赤盟**：四對交叉全**敵對**（-100）。
- **海盜**：維持永久敵（關係表刻意不列它）。

→ 一打開派系/外交面板，就是「兩大陣營對峙」的開局政治幾何；派系顏色藍/紅、名稱已改，一眼可辨。

---

## 五、給測試者的 E2E 驗證清單

> 這個 mod **尚未實機驗證**（首個 pre-E2E 版本）。請照下列逐項核對。

1. **載入無紅字**：三個 mod（Worldbuilder / Faction Relation Seeder / 本 mod）啟用後進主選單，開發者主控台無 error。
   - 特別確認沒有 `Could not find type named pas.relations.RelationSeedDef`（那代表引擎沒載到/順序錯）。
2. **preset 出現**：新遊戲 → 建立世界頁 → 世界預設清單有「兩盟對峙（Demo）」。
3. **播種 log**：選該 preset 生成世界、進入遊戲後，log 應出現：
   `[relation-seeder] 播種完成：套用 6 對，略過 0 對（缺席派系）。`
   （若某派系未生成，略過數可能 >0，屬正常軟略過。）
4. **關係正確**（外交/派系面板，或 dev mode）：
   - 藍盟・商邦 ↔ 藍盟・邊民 = 結盟。
   - 赤盟・聚落 ↔ 赤盟・獵團 = 結盟。
   - 藍盟任一 ↔ 赤盟任一 = 敵對（4 對）。
5. **主題化生效**：上述四派系顏色為藍/紅、名稱為「藍盟/赤盟…」、藍盟・商邦與赤盟・聚落有自訂描述。
6. **只播一次**：存檔 → 讀檔 → 關係不被重置（若你手動改過某對關係，讀檔後應維持你改的值）。
7. **軟相容**（選測）：只關掉 Worldbuilder（留 Seeder + 本 mod）開新局 → preset 消失，但 log 仍顯示播種、關係仍套用。

### 疑難排解
- **報 `Could not find type pas.relations.RelationSeedDef`**：Faction Relation Seeder 沒啟用，或載入順序排在本 mod 之後。
- **preset 沒出現**：Worldbuilder 沒啟用，或本 mod 的 `Worldbuilder/OpeningWorldTwoBlocs/` 沒被載到。
- **顏色/名稱沒變**：preset 的 `saveFactionCustomizations` 需為 True（本 mod 已設）；確認選到的是這個 preset。
- **關係沒變**：確認是**新遊戲**（舊存檔不會被播種）；看 log 有無播種訊息。

---

## 六、換成你自己的世界

1. 編輯 `Worldbuilder/OpeningWorldTwoBlocs/Preset.xml`：把 `savedFactionDefs` / 各 override 換成你的 FactionDef。
2. 編輯 `1.6/Defs/Relations/OpeningRelations.xml`：把 `a`/`b`/`goodwill` 換成你的敵友表。
3. 兩邊的派系 defName 要一致（專案內 `tests/healthcheck.py` 會擋不一致）。
