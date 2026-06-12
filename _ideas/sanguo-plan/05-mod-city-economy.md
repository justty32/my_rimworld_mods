# 05 — 城池經濟/防禦單一 comp（H + K）

> 對應調查：H（:132-140 金錢/資源維度）、K（:156-161 容器設計、防禦、合併論）。
> 依賴：可獨立於 named-officers 編譯；但治理寫入由 settlement-lords（`03`）/決策層（`06`）驅動。

## 職責

給 Rim War 據點補上**真實經濟維度**（silver/food/goods）與**防禦維度**（defenseLevel/defensePoints），
單一 comp 承載；領主可投資/徵糧/修防（決策層寫入），戰敗被真劫掠，Empire 稅收對接。

## 現況（H，:134）

- Rim War 經濟全建在抽象 `RimWarPoints`，**無真實 silver/wealth 欄位**；
  silver 只在玩家↔NPC 介面層（`GetPlayerSilver RW:124`、`TributeSilver RW:667`）。
- Trader 無 inventory、貨物即時生成；trade 結算純點數轉移（`RW:10448/11938`）。
- 掠奪已存在但搬的是點數：`ResolveBattle_Settlement` sack 分支（`RW:11197-11212`），"wealth" 只在信件文案。

## 容器設計（K 定型，:158 + H:136）

- **核心維度 = typed int 欄位**（`silver/food/goods/defenseLevel/defensePoints`，`Scribe_Values`，
  與 `RimWarSettlementComp.PostExposeData RW:9502` 同形、最安全）。
- 擴充性預留 `Dictionary<SettlementAttributeDef,float> extraAttributes` 旁路
  （本機 `OutpostProfileDef` 已驗證 Def 驅動屬性可行）。**純 string-dict 否決**（K，:158）。
- **新增獨立 `SettlementWealthComp`（XML 注入，勿擴 RimWarSettlementComp）**（H，:136）。
- 成長走自 comp `CompTick`+nextTick 節流（仿 `RW:9585`，與 Rim War 解耦，H:136）。

## 防禦維度（K，:159 — 獨立維度、勿疊進存量）

- 防禦**勿疊進經濟存量**（污染 sack）。守城時透過**降 `PointDamage` 臨時抬高 `EffectivePoints`**
  （`EffectivePoints=RimWarPoints-PointDamage RW:9277`）參與 `ResolveCombat_Settlement RW:11018`，
  受其 tier clamp 自動封頂。
- 玩家實打另走 `defenderPointsFactor` 範式（`OutpostTypeDef.cs:10`）。

## 互動接點（H，:137）

- 戰敗被劫：postfix `ResolveBattle_Settlement` sack 分支（`RW:11197`，換搬真資源）。
- 貿易：postfix trade 結算（`RW:10448/11938`）。
- Empire 稅收：`ITaxTickParticipant.PostSettlementCreateTax`（`ref silverAmount`，**免 Harmony**）。
- 領主貪腐：接 settlement-lords comp（F/G）。

## 歸屬與形態（H，:139）

- **獨立 mod**，「RimWar 側自有 comp + Empire 側 registry participant」形態（同 C#1 範式、同 Mod 3 tax participant 範式）。
- 顯示走獨立 `GetInspectString` postfix（仿 `RW:6570`），勿改 Rim War postfix。

## 存檔策略

- comp 全 `Scribe_Values` + extraAttributes 走 `Scribe_Collections`（DefRef key）。

## 風險

- **Empire Registry `ClearCaches` 陷阱**（C 通用陷阱，:59）：讀檔 ClearAll →
  必須 `EmpireCacheUtil.RegisterCacheInvalidator`（`CachePatches.cs:21`）重註冊（官方 Patch-RW 都漏做）。
- 設計決策：抽象計數器（推薦，合 Rim War 哲學）vs 真 Thing（H，:140）。
- sack 分支改寫須保留點數搬移或明確取代（避免雙算）。
