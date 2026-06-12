# T1 — Mod 骨架（編譯綠燈、零行為）

## 產出檔

```
warband-generals/
  About/About.xml                                  packageId pas.officers.warband；
                                                   modDependencies+loadAfter：brrainz.harmony /
                                                   Torann.RimWar / pas.officers.community
  Defs/OfficerRoleDefs/Roles.xml                   pas.officers.OfficerRoleDef
                                                   defName=pas_warband_General, displayPriority=10
  Languages/English/Keyed/WarbandGenerals.xml      6 keys（下表）
  Languages/ChineseSimplified/Keyed/WarbandGenerals.xml
  Languages/ChineseTraditional/Keyed/WarbandGenerals.xml
  Source/WarbandGenerals.csproj                    net48、Krafs 1.6.*、RimWar/0Harmony/
                                                   NamedOfficers 三 Reference（Private=false、
                                                   路徑屬性可 /p: 覆寫，仿 Mod 1）
  Source/SignatureSpike.cs                         T0 編譯期釘
  Source/WarbandGeneralsMod.cs                     ModSettings：generalChance=0.5、bonusMax=0.3
                                                   ＋兩 slider 設定頁（仿 Mod 1 風格）
  Source/GeneralsUtility.cs                        WarnOnce(HashSet 去重)；GeneralRole 懶解析
                                                   （DefDatabase GetNamedSilentFail）；
                                                   CombatBonus/InActiveBattle 先佔位（T2/T3 填）
  Source/HarmonyInit.cs                            [StaticConstructorOnStartup] + TryPatch
                                                   fail-soft 框架（仿 Mod 1），先不掛任何 patch
  tests/healthcheck.py                             雛形：XML wellformed＋About 三依賴＋
                                                   三語 key 集合一致＋dep DLL 存在
```

## Keyed keys（三語集合必一致）

| key | en |
|---|---|
| `pas_warband_ModName` | Warband Generals (Community) |
| `pas_warband_GeneralChance` | General spawn chance: {0}% |
| `pas_warband_GeneralChanceTip` | Chance that a newly created NPC warband gets a named general. |
| `pas_warband_BonusMax` | Max general combat swing: ±{0}% |
| `pas_warband_BonusMaxTip` | Attribute 100 → +X% damage ratio, attribute 0 → −X%. Applied per battle round, never to faction stats. |
| `pas_warband_InspectGeneral` | General: {0} (Might {1}, Command {2}) |

zh-TW：將領生成率/將領戰力擺幅上限/將領：{0}（武力 {1}、統率 {2}）等；zh-CN 同義簡體。

## 細節決議

- Role def 的 label 留英文小寫 `general`（P0 同例 `officer`；DefInjected 三語非本期）。
- About `<name>` Warband Generals (Community)；author justty32；supportedVersions 1.6。
- csproj OutputPath `..\1.6\Assemblies\`；AssemblyName `WarbandGenerals`；
  RootNamespace `pas.officers.warband`。
- healthcheck 雛形即含「鐵律 guard」：Source/ 不得出現 `combatAttribute`/`growthAttribute` 字串。

## 驗證

```bash
dotnet build Source/WarbandGenerals.csproj -c Release   # 0 警告 0 錯誤
python3 tests/healthcheck.py                            # healthcheck OK
```
產物 `1.6/Assemblies/WarbandGenerals.dll` 存在；遊戲載入（含三 dep）應零行為、零紅字。
