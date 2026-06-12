# T1 — 骨架（About/csproj/Patches XML/Languages/Settings/HarmonyInit 空殼＋healthcheck 雛形）

## 產出檔

```
city-economy/
├── About/About.xml
├── Patches/CityEconomyComps.xml
├── Languages/{English,ChineseSimplified,ChineseTraditional}/Keyed/CityEconomy.xml
├── Source/CityEconomy.csproj
├── Source/CityEconomyMod.cs          # ModSettings + 設定 UI
├── Source/EconomyUtility.cs          # WarnOnce + 常數 + 公式（先放 WarnOnce）
├── Source/SignatureSpike.cs          # T0 殘留物
├── Source/HarmonyInit.cs             # TryPatch 框架（patch 清單 T3/T4 補）
└── tests/healthcheck.py
```

## About.xml

- packageId `pas.sanguo.cityeconomy`；name `City Economy (Community)`；author justty32；
  supportedVersions 1.6。
- modDependencies：brrainz.harmony＋Torann.RimWar（**不含** officers/settlements——soft）。
- loadAfter：brrainz.harmony、Torann.RimWar、`pas.officers.community`、
  `pas.officers.settlements`（保證 P2 在場時先載，反射橋才解析得到）。

## Patches/CityEconomyComps.xml（仿 RimWar RimWarCompsx.xml）

- `PatchOperationAdd` → `*/WorldObjectDef[worldObjectClass = "Settlement"]/comps`
  加 `<li Class="pas.sanguo.cityeconomy.WorldObjectCompProperties_SettlementWealth"/>`。
- 兩組 `PatchOperationSequence`（success Always＋PatchOperationTest）分別對
  `Cities.City` 與 `FactionColonies.WorldSettlementFC`（鏡像 RimWar，相容 mod 城）。
- 不掛 Caravan/WarObject——只掛 RimWar 追蹤的聚落 def。

## csproj（抄 P2 改名）

net48；AssemblyName `CityEconomy`；RootNamespace `pas.sanguo.cityeconomy`；
OutputPath `..\1.6\Assemblies\`；Krafs.Rimworld.Ref 1.6.*；RimWar.dll／0Harmony.dll
（Steam 路徑、`/p:` 可覆寫）。**無 NamedOfficers/SettlementLords 參照**。

## ModSettings（CityEconomyMod.cs）

| 欄位 | 型別/預設 | 語意 |
|---|---|---|
| `growthRate` | float 1.0（0~3） | 財富成長率；0＝停成長（存量/劫掠/守城照常） |
| `sackLossRatio` | float 0.45（0~1） | sack 搬走比例；0＝停用劫掠搬資源 |
| `defenseAmplitude` | float 1.0（0~2） | 守城加成幅度；0＝停用守城折算 |
| `traderEconomyEnabled` | bool true | 貨架縮放＋交易回寫總開關 |

UI 仿 P2：Listing_Standard＋Slider（步進 0.05）＋Checkbox，三語 keyed。

## Keyed key 清單（三語同集合）

`pas_cityecon_ModName`、`pas_cityecon_GrowthRate(+Tip)`、`pas_cityecon_SackLossRatio(+Tip)`、
`pas_cityecon_DefenseAmplitude(+Tip)`、`pas_cityecon_TraderEconomy(+Tip)`、
`pas_cityecon_InspectLine`（財富/防禦一行：銀{0} 糧{1} 貨{2}｜城防 Lv{3}（{4}））。

## HarmonyInit

id `pas.sanguo.cityeconomy`；TryPatch helper 抄 P2（缺 method WarnOnce 降級不連坐）；
T1 先掛 GetInspectString（佔位可後補）或留空殼——**本期決議：留空殼**，patch 隨
T3/T4 各自任務加入，避免半成品 patch 上線。

## healthcheck 雛形（tests/healthcheck.py，抄 P2 改）

1. XML well-formed；2. About packageId/hard dep 兩件套（**檢查 officers 不在
   modDependencies**、但在 loadAfter）；3. Patches XML 引用的 comp props 類存在 Source；
4. C# 引用 `pas_cityecon_*` key 都在 XML；5. 三語 key 集合一致；
6. 鐵律 guard：禁字串 `combatAttribute`/`growthAttribute`/`ThingSetMakerDefOf`；
   csproj 禁 `NamedOfficers`/`SettlementLords` 參照（soft-optional 鐵律）；
7. RimWar.dll/0Harmony.dll 存在；8.（T3/T4 後）patch 接點字串齊全＋TryPatch 存在。

## 驗收

`dotnet build Source/CityEconomy.csproj -c Release` 0 警告 0 錯誤；healthcheck OK。
