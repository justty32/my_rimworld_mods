# N5 電力採樣 — 外部 mod 研究（設計輸入）

> 日期：2026-06-12
> 用途：為未動工的 **N5「電力採樣」** 蒐集兩個 VOE 擴展 mod 的可借鏡點，並釐清電力建模現實。
> 相關：`docs/ideas-features-flow.md`（N5 原始意圖）、`Source/Outpost_Sampled.cs`、`Source/ProductivitySnapshot.cs`、`Source/ColonyArchivalTracker.cs`、`Source/ArchivalService.cs`。
> 方法：A 段（Additional Outposts）僅 workshop DLL，依 Def/語言鍵/DLL 字串掃描推斷；B 段（Power Grid）clone GitHub 原始碼實讀。

---

## TL;DR（給未來實作 N5 的人）

- **N5 ①（採樣 + 換算/緩衝）現在就能做，不卡任何參考 mod。** 兩個外部 mod 都「不採樣 PowerNet」，①必須自製：採樣期累加 `PowerNet` 淨功率求平均 → 換算成 silver 代理 → 塞進現有 `ProductivitySnapshot.dailyRates[Silver]` → **完全複用現有 `ResultOptions`（正）＋ `Produce()` TakeItems（負）管線，零新管線**。
- **N5 ②（正電量跨地圖送回主基地）的藍本＝VOE Power Grid。** 它的「跨地圖」其實是錯覺：靠在**收電地圖上 spawn 一個 `CompPowerPlant` 假發電機**把瓦數灌進本地 PowerNet。這是香草能力，**借鏡重寫即可，不需硬相依此 mod，也不需要第二個「分層 mod」**。
- **Additional Outposts 對電力零貢獻**，但其 **Bank「緩衝累積→延時結算」idiom** 可參考 N5 的電量緩衝形狀。
- **電力建模鐵則**：RimWorld 電力是 PowerNet 每 tick 瓦特流、**無電力物品 ThingDef**，不能當 Thing 投遞；進入投遞管線前必須換算成 Thing（silver）。兩個 mod 都印證這點——電力都不走 VOE 的 ResultOptions 物品管線。

---

# A 段：Vanilla Outposts Expanded: Additional Outposts（`MrHydralisk.VOEAdditionalOutposts`）

> Workshop `294100/2873841790`。結論：**零電力相關內容**，僅 Bank idiom 有參考價值。

## A.1 內容清單（它加的哨站）
全部 `ParentName="OutpostBase"`，以 VEF `Outposts.OutpostExtension(_Choose)` 宣告產出（證據 `Defs/WorldObjectDefs/Outposts.xml`）：
- Construction Site（產 Silver）、Field Hospital（醫療品/賣藥換銀，多結果浮選）、Restaurant（餐點/賣餐）、Circus（Silver）、Embassy（提派系好感，自訂 `Outpost_Embassy`）、Border Post（抓走私）、Mercenary Camp（風險任務換銀）、Prison（囚犯換銀）、Ranch（動物產品）、Church（募捐+傳教，需 Ideology）、Educational Center（教育，需 Biotech）、Fishing（捕魚，需 Odyssey）。
- **Bank**（`1.4/Patches/Outposts.xml`）：`TicksPerProduction=-1`（脫離固定週期）→ `OutpistExtension_Choose_Deposit` 的 `InterestPerSeason`/`DurationInSeasons` **按季結息→投遞利息**。**全 mod 唯一「緩衝累積→延時結算」模型。**

## A.2 電力搜尋結果 — 零命中
- 全 XML grep（power/electric/energy/battery/generator/fuel/conduit/watt/Wh/電/發電）：唯二命中在 `About/Manifest.xml` 的 `<suggests>`，是作者推銷自家其他 mod 的字串（含 `MrHydralisk.VOEPowerGrid`），**非本 mod 內容**。
- DLL 字串掃描：唯一近似 `ConversionPower` = vanilla 意識形態轉化 Stat（Church 用），**與電力無關**。
- **無 PowerNet / CompPowerTrader / 發電 / 電池 / 電量物品的任何 Def 或 code。**

## A.3 對 N5 可借鏡點
- **直接可重用：無。**
- **Bank 的「緩衝累積→延時結算」idiom**：`TicksPerProduction=-1` + 自訂 extension 累積本金、按季結息再投遞——正是 N5「電量緩衝→換算銀」需要的形狀。
- **ChooseFloat 多結果選單 idiom**（Hospital/Restaurant）：若 N5 想讓玩家選「電力盈餘換成什麼（銀/元件/僅抵消耗）」，可參考。
- **建模啟發**：本 mod 證實「VOE 哨站從不投遞抽象流量，只投遞 Thing」（Embassy/Church 走自訂 class 特殊路徑、不經 ResultOptions）→ **N5 電力必須在進投遞管線前換算成 Thing。**

---

# B 段：VOE Power Grid（`MrHydralisk.VOEPowerGrid`）★N5 真正的參考★

> GitHub `https://github.com/MrHydralisk/VOE-Power-Grid`（已 clone 實讀 C# 原始碼）。docs `ideas-features-flow.md:91` 點名的電力輸送哨站擴展。**本機未安裝**。

## B.1 結論
對 **N5②** 是現成的跨地圖輸電解法藍本；對 **N5①** 幾乎無採樣程式碼可借（它根本不採樣 PowerNet）。

## B.2 完整機制（end-to-end）

### 它加什麼
- **多個發電哨站 WorldObjectDef**（`Defs/WorldObjectDefs/Outposts.xml`）：Solar/WindTurbine/Watermill/Tidal/Geothermal/Nuclear/Chemfuel/WoodFired/PowerCell…，皆 `ParentName="OutpostBase"`。
- **2 個 ThingDef 建築**（`Defs/ThingDefs_Buildings/Buildings_Misc.xml`）：
  - `Outpost_PowerGrid_DeliverySpot`（"power grid outlet"）★跨地圖輸電核心★：`<comps><li Class="CompProperties_Power"><compClass>VOEPowerGrid.CompPowerGridOutlet</compClass><transmitsPower>true</transmitsPower>`。玩家**在主基地地圖實際建造**，研究前提 `Electricity`。
  - `PowerTransmissionTower`：純抽象「世界地圖傳輸距離」道具，不 spawn，只增加哨站 `PowerNetworkRange`。

### 世界/哨站層「power」建模
**沒有 Wh、沒有虛擬電池、沒有世界電網。**（`Outpost_PowerGrid.cs`）
- 哨站持 `List<ThingDefCountClass> ActiveBuildingsCounter`（玩家在 WITab +/- 啟用幾棟發電機）。
- `UpdateProducedPower()`（`Outpost_PowerGrid.cs:116-123`）算 `cashedProducedPower`：
  `Sum( -thingDef.GetCompProperties<CompProperties_Power>().PowerConsumption × count ) × terrainPowerMultiplier × PowerMultiplier`
  取**建築 def 額定 `PowerConsumption`**（發電機負值取負變正產出）。子類覆寫：Solar `Mathf.Lerp(NightPower, FullSunPower, CurSkyGlow)`（`Outpost_PowerGrid_Solar.cs:629`）、Wind `× WindSpeed`（`_WindTurbine.cs:666`）、Refuelable `× fuelPowerMultiplier`（`_Refuelable.cs:528`）。
- **此瓦數是額定/模擬值，非任何真實 PowerNet 測量**。且 Solar/Wind 取的天氣是**收電端地圖**的（`Outlet?.Map.skyManager`），不在乎被封存基地真實電況。
- 容量 `maxStructureAmount` = `BaseBuildingCapacity + 建築技能總和 × BuildingCapacityPerSkill × terrainCapacityMultiplier`。

### 電「進」哨站
不採樣、不接導線。玩家用哨站庫存物資（`containedItems`）當建材，花 `WorkToBuild` ticks 蓋抽象發電建築（gizmo "Construct" → `ConstructStart`/`ConstructFinish`）。Refuelable 每 60000 tick `ConsumeFuel()`（`:537`）扣燃料、不足降倍率。

### 電「出」——跨地圖輸電確切機制 ★重點★
1. 玩家在目標地圖（主基地）放 `Outpost_PowerGrid_DeliverySpot`，掛 `CompPowerGridOutlet : CompPowerPlant`（`CompPowerGridOutlet.cs:11`）。
2. outlet gizmo "Connect to Power Grid" 列世界地圖所有 `Outpost_PowerGrid`，玩家連線：記 `outpostPowerGrid`、依 tile 距離算 `powerLossDueToRange`、`opd.SetNewOutlet(this.parent)`。有距離限制（`PowerNetworkRange` < 距離則不可連，靠 PowerTransmissionTower 延伸）。
3. **注入靠繼承 `CompPowerPlant`**：
   ```csharp
   protected override float DesiredPowerOutput => outpostPowerGrid?.ProducedPower ?? 0f;   // :15
   public override void UpdateDesiredPowerOutput()
       { PowerOutput = DesiredPowerOutput * (1f - powerLossDueToRange); }                   // :88
   ```
   因為它**是收電地圖上真實 spawn、`transmitsPower=true` 的 `CompPowerPlant`**，香草 `PowerNet` 每 tick 自然把它的 `PowerOutput` 當發電收進那張地圖電網。**「跨地圖」是錯覺**：瓦數來自世界哨站快取 `ProducedPower`，但灌電 100% 發生在收電地圖本地 PowerNet，由本地假發電機完成。**沒有 map→world→map 的 PowerNet 橋接——香草 CompPowerPlant 就是橋。**

### 其他關鍵
- `OutpostExtension_PowerGrid : DefModExtension`：帶 `ConstructionOptions`、地形/生態/河流→倍率字典、`fuelFilter`。
- `WITab_Outpost_PowerGrid`：+/- 啟用建築面板。
- **唯一 Harmony patch**（`HarmonyPatches.cs`）：prefix `Outpost.TakeItems`，讓蓋建築可跨多 stack 湊料——**與電力無關，純庫存工具**。
- **無 GameComponent/WorldComponent、無虛擬電池/儲能、無 ResultOptions 覆寫**（`Produce()` 是空的，`Outpost_PowerGrid.cs:184`——電力不走 VOE 物品產出投遞管線）。

## B.3 可重用點分類

| 項目 | 證據 | 分類 |
|---|---|---|
| `DeliverySpot` ThingDef + `CompPowerGridOutlet : CompPowerPlant` 假發電機注入本地 PowerNet 範式 | `Buildings_Misc.xml`；`CompPowerGridOutlet.cs:15,88` | **借鏡重寫**（做簡化 CompPowerPlant 子類）|
| `DesiredPowerOutput`/`UpdateDesiredPowerOutput` 覆寫把外部抽象瓦數餵進 CompPowerPlant | `CompPowerGridOutlet.cs:15,84-89` | **直接重用 pattern**（API 是香草 CompPowerPlant）|
| 距離損耗公式 | `CompPowerGridOutlet.cs` Connect 分支 | **借鏡**（可選）|
| 「電力＝Σ建築額定 × 倍率」快取模型 | `Outpost_PowerGrid.cs:116` | **不適用**（我們要採樣真實淨功率，模型相反）|
| 哨站↔outlet 雙向引用狀態機 + `Scribe_References` 存檔 | `Outpost_PowerGrid.cs:102,441`；`CompPowerGridOutlet.cs` | **直接重用 pattern**（②需要）|
| 電力不走 ResultOptions、走獨立 CompPowerPlant 通道 | `Outpost_PowerGrid.cs:184` 空 Produce | **關鍵借鏡**：證實電力不該塞 ResultOptions |

## B.4 對 N5① 的啟發（採樣+換算/緩衝）
**借不到程式碼**（它不讀 PowerNet）。①自製：
- **採樣端**：`ColonyArchivalTracker` 每 N tick（仿其 250-tick 節流）累加 `map.powerNetManager.AllNetsListForReading` 各 net 的即時淨功率（產−耗）求採樣期平均。
- **換算/緩衝**（electricity 無 ThingDef）：
  - **選項1（最低成本，推薦先做）**：換算成 silver 代理塞進 `ProductivitySnapshot.dailyRates[Silver]`（正→產、負→耗），**完全複用現有 ResultOptions（正）+ Produce() TakeItems（負）管線**（`Outpost_Sampled.cs:86,114`），零新管線、零跨地圖問題。
  - 選項2（真實電量緩衝）：`ProductivitySnapshot` 加 `float avgNetWattsPerDay` + `float powerBuffer`，`Produce()` 每週期累加扣抵。純自製。
- 反例提醒：它的 Solar/Wind 採樣的是**收電地圖**天氣，對「採樣封存前本基地電況」無參考價值。

## B.5 對 N5② 的啟發（跨地圖輸電）
- **它怎麼做到**：見 B.2——收電地圖本地 `CompPowerPlant` 假發電機，香草 PowerNet 收電。**它自己就是 docs 想找的「分層 mod A→B」機制，不需要第二個 mod。**
- **建議自己重寫（不相依）**，工作量中等（~1 Comp + 1 ThingDef + 哨站側 gizmo/存檔，~200-300 行，全香草 API）：
  1. ThingDef「採樣電力 outlet」+ `CompProperties_Power(transmitsPower=true)` 掛我們的 `CompPowerPlant` 子類，`DesiredPowerOutput => 連線的 Outpost_Sampled.AvgSurplusWatts`。
  2. `Outpost_Sampled` 加 `ThingWithComps outlet` 雙向引用 + connect/disconnect gizmo + `Scribe_References`（照抄其狀態機）。
- **不建議軟相依 interop**：它的 Connect gizmo 只 `OfType<Outpost_PowerGrid>()`，不認我們的型別，零成本掛接不可行（需反射/patch）。**借鏡重寫更乾淨。**
- **撤銷 docs 假設**：「需第二個分層 mod」可撤——一個 CompPowerPlant 子類就夠。

## B.6 不確定/限制
- clone 成功，C# 全讀；`Source/` 跨 1.3–1.6 共用單一份。
- `CompPowerPlant.PowerOutput`/`DesiredPowerOutput`/`UpdateDesiredPowerOutput` 香草簽名依其 override 推定，**整合前應對照 1.6 反編譯 RimWorld 確認虛方法名**。
- `CurrentEnergyGainRate` 等 PowerNet 採樣 API 是我為①提的建議（非來自此 mod），**實作前需查證 1.6 `PowerNet`/`PowerNetManager` 正確即時淨功率取法**。

---

# 整體 N5 建議路徑

1. **先做 ①、只做 ①-選項1（silver 代理）**：採樣 PowerNet 平均淨瓦 → 換 silver → 走現有 `dailyRates` 管線。繞開跨地圖難題，「現在就能做、不卡參考 mod」。兩個外部 mod 在①皆不扮演角色（VOE Power Grid 不採樣、Additional Outposts 無電力）。
2. **② 列後續可選增強，自己重寫**：仿 `CompPowerGridOutlet` + `Outpost_PowerGrid` outlet 雙向狀態機。VOE Power Grid＝唯一藍本，借鏡重寫、不硬相依。
3. 緩衝形狀若要做，參考 Additional Outposts 的 Bank「累積→延時結算」idiom。

**一句話**：①＝採樣淨瓦→換 silver→現有管線（無外援、立刻可做）；②＝照抄 VOE Power Grid 的 CompPowerPlant-outlet 範式自製（它是藍本，非相依）。
