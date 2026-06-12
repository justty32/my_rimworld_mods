# T1 — mod 骨架（建得起來、載得進去、什麼都不做）

## 產出檔案

```
settlement-lords/
├── About/About.xml                     packageId pas.officers.settlements；
│                                       modDependencies+loadAfter：brrainz.harmony /
│                                       Torann.RimWar / pas.officers.community（抄 P1 改名）
├── Defs/OfficerRoleDefs/Roles.xml      pas_settlement_Lord（pas.officers.OfficerRoleDef，
│                                       leaderLike=true、displayPriority=0）
├── Languages/{English,ChineseSimplified,ChineseTraditional}/Keyed/SettlementLords.xml
│                                       key：ModName / LordChance(+Tip) / GovAmplitude(+Tip) /
│                                       DecayEnabled(+Tip) / InspectLord——三邊集合一致
├── Source/SettlementLords.csproj       抄 P1：net48、Krafs.Rimworld.Ref 1.6.*、
│                                       RimWarDll/HarmonyDll/NamedOfficersDll 可 /p: 覆寫、
│                                       OutputPath ..\1.6\Assemblies\、Private=false
├── Source/SettlementLordsMod.cs        ModSettings 三項：lordChance(0.25) /
│                                       govAmplitude(0.5) / decayEnabled(true)；
│                                       slider×2＋CheckboxLabeled×1
├── Source/LordsUtility.cs              WarnOnce（[SettlementLords] 前綴）＋ LordRole 懶解析
│                                       （GetNamedSilentFail("pas_settlement_Lord")）＋
│                                       GovernanceFactor＋GrowthCapFor 鏡像（T3 填內容）
├── Source/SignatureSpike.cs            T0 編譯期釘
├── Source/HarmonyInit.cs               [StaticConstructorOnStartup]＋TryPatch fail-soft 框架
│                                       （抄 P1）；本任務先接 2 個 patch 的空殼或留待 T2/T3
└── tests/healthcheck.py                抄 P1 改：packageId/defName/前綴/接點清單
                                        （IncrementSettlementGrowth、GetInspectString）/
                                        鐵律 guard（combatAttribute、growthAttribute 字串
                                        不得出現於 Source/）
```

## 注意

- C# 源碼（含註解）**禁止出現** `growthAttribute`/`combatAttribute` 字串（healthcheck #8）。
- key 前綴 `pas_settlement_`；healthcheck #5 會掃 C# 引用的 key 必在 XML。
- GrowthCapFor 自帶（抄 Mod 1 `OutpostRimWarUtility.cs:38-50` 邏輯），**不 ref Mod 1**。

## 驗收

`dotnet build Source/SettlementLords.csproj -c Release` 0/0；`python3 tests/healthcheck.py` OK。
