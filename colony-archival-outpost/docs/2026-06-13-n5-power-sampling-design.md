# N5 電力採樣 — 設計 spec（權威）

> 日期：2026-06-13
> 狀態：設計定案，待寫實作計畫。
> 來源研究：`docs/2026-06-12-n5-power-sampling-external-mod-research.md`（VOE Power Grid 的 CompPowerPlant-outlet 範式）。
> 取代：`docs/ideas-features-flow.md` N5 節的舊構想（電量緩衝/brownout/換 silver 皆**不採用**）。

## 1. 概念一句話

採樣端玩家放一個**「供電節點」**建築量測該電網淨功率（每小時一次、跨採樣窗平均得 `avgNetPowerW`，有號）；封存後，玩家在主基地蓋一個**「電力輸出端 outlet」**建築連到該哨站，由 outlet 把哨站的 `avgNetPowerW`**有號**灌進主基地電網——正值＝對主基地發電、負值＝從主基地抽電（封存基地的赤字由主基地供）。**主基地電網本身就是緩衝，不另做緩衝/節流/換幣。**

## 2. 採樣端：供電節點 `CAO_PowerSamplingNode`

- **外觀/放置**：無貼圖方框（純色 graphic 或借貼圖改色）；`isEdifice=false` 使其**可與牆體/其他建築疊放、任意位置**；建造成本低、研究前提 `Electricity`。
- **限制**：**一張地圖只允許一個**（PlaceWorker 擋第二個；免去多電網去重）。
- **電氣**：`CompProperties_Power { transmitsPower = true }`，像導線一樣併入它所在的電網；`node.PowerComp.PowerNet` 取得該網。
- **量測**：採樣期間**每小時（2500 tick）** 瞬時讀一次該電網淨功率（瓦）：
  - `netWatts = PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick`（＝Σ 該網 powerComps 的 `PowerOutput`，發電正、耗電負）。
  - ⚠ 此 API 換算需對照 1.6 反編譯 RimWorld 確認（見 §8）。
  - 累加到 tracker 的 `powerAccumW`、`powerSampleCount++`。
- **太陽能**：不特別處理。每小時取樣 × 跨多天平均，自動把日夜變化抹平（等於免費吃到平均值）。
- **無節點時**：不產生電力採樣資料（`avgNetPowerW` 維持 0、套用開關 no-op）。

## 3. 產出端：電力輸出端 `CAO_PowerOutlet`

- **外觀/放置**：同樣無貼圖方框、`isEdifice=false` 可疊牆任意放；研究前提 `Electricity`；蓋在主基地地圖。
- **電氣**：自製 `CompArchivalPowerOutlet : CompPowerTrader`，`transmitsPower = true`，併入主基地電網。
- **連線**：gizmo「連接封存哨站」→ 浮動選單列出所有 `applyPowerSampling` 的 `Outpost_Sampled`，選一個設 `connectedOutpost`。哨站側存 `connectedOutlet` 反向引用。一哨站對一 outlet（連新 outlet 則舊的自動斷）。
- **輸出**：每 tick `PowerOutput = connectedOutpost?.PowerWatts ?? 0`（有號）：
  - 正 → 對主基地電網發電；
  - 負 → 從主基地電網抽電（你要的「包括消耗」）。
  - 香草 `PowerNet` 自然結算 = 真實跨地圖供/耗電。**無距離損耗**（YAGNI，未來可加）。
- **未連線/哨站已毀**：`PowerOutput = 0`，清除失效引用。
- **inspect**：顯示連線哨站名 + 當前輸出 W。

## 4. 資料模型

### `ProductivitySnapshot`（+2 欄）
- `float avgNetPowerW`（有號平均淨功率，封存當下算好、隨哨站存檔）。
- `bool applyPowerSampling`。
- 更新 `Clone()`、`ExposeData()`（`Scribe_Values`）、`IsEmpty`（納入 `applyPowerSampling`）。

### `Outpost_Sampled`（+1 欄、+1 唯讀）
- `CompArchivalPowerOutlet connectedOutlet`（反向引用，UI/去重用；`Scribe_References`）。
- `float PowerWatts => snapshot.applyPowerSampling ? snapshot.avgNetPowerW : 0f`。
- `Produce()` **不動電力**（電力完全由 outlet 即時處理）。

### `ColonyArchivalTracker`（+2 欄 + tick）
- `double powerAccumW`、`int powerSampleCount`。
- `MapComponentTick()`：`if (isSampling && TicksGame % 2500 == 0)` → 找本地圖的供電節點（第一個），有則讀 netWatts 累加。
- `BeginSampling()`/`Reset()` 歸零兩欄；`ExposeData()` 保存（中途存檔不丟）。

### `ArchivalService`
- `ComputeSnapshot`：`if (powerSampleCount > 0) snapshot.avgNetPowerW = (float)(powerAccumW / powerSampleCount)`。
- `Archive(...)`：加 `bool applyPower` 參數；`if (applyPower && powerSampleCount > 0) snapshot.applyPowerSampling = true`。

## 5. UI / 語系

- `Dialog_ArchivalConfirm`：若 `powerSampleCount > 0` → 顯示「平均淨功率 ±X W」一行 + 一個明確選擇：**「計入電網消耗與產出」↔「無視電力」**（即 `applyPowerSampling` 開關）。
  - **計入** = 用採樣到的有號淨值（產出−消耗，可能為負，封存後由 outlet 對主基地發電/抽電）。
  - **無視** = 完全不做電力（`applyPowerSampling=false`，封存後該哨站無電力行為，outlet 連上也輸出 0）。
  - 串進 `Archive(applyPower:)`。預設值：建議**計入**（既然放了節點並採到了資料）。
- `Dialog_SamplingStatus`：採樣中即時顯示目前供電節點電網的瞬時淨功率（無節點則提示「未放置供電節點」）。
- `CAO_PowerOutlet` inspect string：連線哨站 + 當前輸出。
- `Languages/`（zh-Hant + en）新增 `CAO.Power.*` 鍵（節點/outlet label+desc、預覽行、連線 gizmo、狀態字串）。

## 6. 存讀檔

- snapshot 兩新欄走 `Scribe_Values`。
- tracker 累加器走 `Scribe_Values`。
- outlet↔outpost 雙向走 `Scribe_References`（跨 Thing↔WorldObject，比照 VOE Power Grid，PostLoadInit 後重綁）。
- outlet 是地圖建築，隨地圖存檔；哨站是 WorldObject，隨世界存檔——引用跨存檔域，需 `Scribe_References` 正確標註。

## 7. 邊界情況

| 情況 | 行為 |
|---|---|
| 無供電節點 | 不採電力；確認窗無電力行、開關無效 |
| 節點未接任何電網 | 讀 0 |
| 多個節點（玩家繞過 PlaceWorker） | 只採第一個 |
| outpost 無電力資料但連了 outlet | outlet 輸出 0 |
| outlet 連的哨站被毀/解封 | 清引用、輸出 0 |
| 採樣中存讀檔 | 累加器保存，續採不歸零 |

## 8. 不確定 / 待實作驗證

- `PowerNet` 即時淨功率的**確切 1.6 API**（`CurrentEnergyGainRate()` 回 Wd/tick，需 `/ WattsToWattDaysPerTick` 換瓦）——實作前對照 `~/repo/pas/projects/rimworld/` 反編譯確認。
- `CompPowerTrader.PowerOutput` 設負值是否被香草正確當「消耗」併入電網結算——比照 VOE Power Grid 的 `CompPowerGridOutlet` 確認虛方法名與行為。
- `isEdifice=false` 是否足以允許疊牆放置（或需額外 PlaceWorker 放寬）。

## 9. 範圍外（YAGNI）

電量緩衝/儲能、brownout 節流、換 silver、距離損耗、太陽能日夜加權、多節點/多電網去重、outlet 對哨站產出的二次效果。

## 10. 動到的檔

- 新：`Defs/ThingDefs/CAO_Power.xml`（兩個 ThingDef）、`Source/CompArchivalPowerOutlet.cs`、（必要時）`Source/PlaceWorker_SingleSamplingNode.cs`。
- 改：`Source/ProductivitySnapshot.cs`、`Source/ColonyArchivalTracker.cs`、`Source/ArchivalService.cs`、`Source/Outpost_Sampled.cs`、`Source/Dialog_ArchivalConfirm.cs`、`Source/Dialog_SamplingStatus.cs`、`Languages/**`、`tests/healthcheck.py`。
- 研究前提沿用香草 `Electricity`。
