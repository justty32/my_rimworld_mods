# 03 — 聚落領主系統（F 承載 + G 點數影響）

> 對應調查：F（:107-119 承載與連動）、G（:123-130 點數成長/削弱）、K（:156-161 容器）。
> 依賴：`named-officers`（屬性 comp + record）；與 city-economy（`05`）、決策層（`06`）相鄰。

## 職責

每座 NPC 聚落（含 RimWar City / Empire 附庸 / NpcOutpost）掛一名**領主（太守）**
與 N 名**官員**；領主屬性影響該城點數成長/衰退、稅收、防守、叛亂傾向。

## 承載設計（F，:115-116）

- 數值走 `named-officers` 屬性 comp（掛 Settlement 子類，先例 `RimWarSettlementComp:9078`）。
- 具現走懶生成真 Pawn + `previouslyGeneratedInhabitants` 橋（玩家拜訪該城請回同一 pawn，`RebelSpawner.cs:24`）。
- 屬性影響四管道（F，:116）：產出/稅收（Empire `WorldSettlementFC.CreateTax`）、
  防守（`RimWarPoints`/npc-outposts `defenderPointsFactor`）、叛亂傾向（餵 `RebelRecord.ratePerDay`）、成長（見下 G）。

## 點數影響掛載點（G，核心 — 必走逐城 postfix）

- 成長公式：`IncrementSettlementGrowth`（`RW:17567`，公式 `RW:17622-17631`：
  `num4=(Rand(2,3)+biome)×num×tech×growthAttribute×settlementGrowthRate`，每城每輪最多 +100）。
- **關鍵限制（G，:126）**：`growthAttribute`（`RW:1173`）是**派系級共享**，動它污染全派系。
  領主倍率**必須逐城 postfix 補乘**——這是與將領（E 可用 `combatAttribute` 鉤子）的根本差異。
- **骨架**（G，:127-129）：postfix 逐城讀領主 comp `GovernanceFactor`（政務×忠誠，0.5~1.5）；
  `gov≥1` 補成長、`gov<1` 扣 `RimWarPoints`（庸主→淨衰退）。
  鏡像「`PointDamage>0` 跳過」（`RW:17616-17620`）、復用 Mod 1 `GrowthCapFor` 上限鏡像。
- **衰退地板**（G，:128）：`RimWarPoints` getter 地板 100（`RW:9267`），衰退停在 100；
  真正摧毀城需走 `ConvertSettlement` 易主（`RW:11168`），**非本注入點**。

## 歸屬（G，:129）

- postfix 在**領主系統 mod 自己**（與 Mod 1 哨站貢獻 postfix 正交；多 postfix 疊加同方法安全）。
- **勿塞進 Mod 1**。`GrowthCapFor` 抽共享 util。

## 存檔策略

- 領主/官員 record 與屬性走 `named-officers` 層（`02`）。
- 領主↔城綁定隨 record（`homeSettlement` ref，仿 `RebelRecord.cs:12/23`）。

## 風險（G，:130）

- 上限/地板鏡像漂移（隨 RimWar 版本）。
- PointDamage 語意誤用。
- **派系級 vs 聚落級混淆**（勿動 `RW:1171/1173/17625` 係數）。
- threading（`RW:17062` UpdateFactions 在背景）→ postfix 須 thread-safe。
