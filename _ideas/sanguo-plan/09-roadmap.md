# 09 — 分期路線圖（地基 → 消費層 → 聯動）

> 每期可獨立編譯、驗證、可玩。對映 `01` 建造順序與 `08` 片段先後。
> 原則：先地基（無玩法但解鎖一切）→ 再消費層（逐支柱可玩）→ 再聯動（跨 mod 因果）。

## Phase 0 — 基礎層 `pas.named-officers`

- **內容**：從 faction-politics 抽取泛化 `NamedOfficerRecord`/`OfficerSpawner`；屬性 comp（`CompProperties`+注入）；關係雙軌骨架（A 軌 DirectPawnRelation、B 軌 opinion dict）；2500-tick 心跳。
- **驗證**：能對一個 NPC 生具名職官 record、屬性可存讀、拜訪請回同一 pawn、關係 dict 演化。
- **可玩性**：無玩法（純地基），但解鎖 P1–P5。
- 對應：`02`；調查 E/F/I。

## Phase 1 — warband 將領（E）

- **內容**：warband 屬性 comp（注入 `RW_Warband`）；postfix `ResolveCombat_Units RW:11271` 局部乘將領加成；inspect 顯示。
- **驗證**：名將 warband 戰鬥點數明顯高於庸將；存讀後將領屬性保留；inspect 顯示將領。
- **可玩性**：✅ 名將率軍更能打——支柱④落地。最小、最快驗證 named-officers 走通。
- 對應：`04`；調查 E。

## Phase 2 — 聚落領主 + 點數影響（F/G）

- **內容**：聚落掛領主/官員（懶生成）；屬性 comp；postfix `IncrementSettlementGrowth RW:17567` 逐城 `GovernanceFactor` 補乘/扣點。
- **驗證**：賢主城點數成長加速、庸主城淨衰退（停在地板 100）；不污染同派系他城（逐城驗證）。
- **可玩性**：✅ 太守治城、城池興衰——支柱①⑤落地。
- 對應：`03`；調查 F/G/K。

## Phase 3 — 城池經濟/防禦 comp（H/K）

- **內容**：`SettlementWealthComp`（silver/food/goods + defenseLevel/defensePoints，XML 注入）；自 comp tick 成長；sack 改搬真資源；守城降 PointDamage 折算；Empire `ITaxTickParticipant` 對接。
- **驗證**：城有真資源存量、戰敗被劫真資源、守城防禦影響 EffectivePoints；稅收進 Empire。
- **可玩性**：✅ 城池發展經濟、劫掠經濟——支柱②落地。
- **建議接** B 影子 Settlement（warband 打哨站）於本期後，與守城折算協調。
- 對應：`05`、`08-B`；調查 H/K、B。

## Phase 4 — faction-politics 擴充：領主帶城叛變（F×叛亂）

- **內容**：`ratePerDay` 改領主屬性/關係驅動；反叛者=領主（共用 record）；叛變即太守自立帶城（含哨站）獨立。
- **驗證**：低忠誠領主累積進度→分裂新派系、帶走城與衛星哨站、goodwill 轉敵、新派系參戰。
- **可玩性**：✅ 叛亂分裂、勢力消長——支柱⑥落地。
- 對應：`07`；調查 F/I。

## Phase 5 — 領主決策層 + 哨站擴張（J/K/D）

- **內容**：npc-outposts 加 `TypeSelector` hook；settlement-lords 註冊權重函數（讀領主 comp+behavior+momentum）；`ILordAction` 對內（蓋倉/修防/徵糧→寫 city-economy）+ 對外（建哪種 outpost→D 接點）。**前置 D 基礎版就緒**。
- **驗證**：賢主城傾向建特定 outpost 類型；對內動作改變 wealth/defense；D 把部分 settler 轉哨站（降建城頻率）。
- **可玩性**：✅ 哨站擴張、領主主動治理——支柱③+治理深化。
- 對應：`06`、`08-D`；調查 J/K/D。

## Phase 6 — RimWar × Empire 聯動（C 第一梯隊）

- **內容**：C#1 戰時加稅/被圍困減產（起手，零 patch）→ #2 附庸繁榮回饋點數 → #3 事件流匯流排（唯一 RimWar patch）。掛在 empire-warfare 上，勿改寫叢集。
- **驗證**：附庸所屬派系戰爭時稅率升/被圍困減產；附庸繁榮回饋母派系 RimWar 點數；RimWar 事件進 Empire 信件。
- **可玩性**：✅ RimWar 局勢↔Empire 經濟雙向因果——支柱⑦深化。
- **注意** Empire Registry `ClearCaches` 陷阱：必重註冊 invalidator。
- 對應：`08-A`；調查 C。

## 並行/獨立片段（不阻塞主鏈）

- **E1 封存哨站**（實作中）：與 P0–P2 並行；支柱⑧。
- **D**：須在 P5 前有基礎版（P5 對外動作呼它）；可在 P2/P3 期間先做。
- **B 影子 Settlement**：建議 P3 後；獨立於領主層。

## 里程碑驗證鐵則

每期 done = ①獨立編譯通過 ②不破壞前期 ③該支柱可在實機觀察到效果 ④存讀往返不丟資料。
