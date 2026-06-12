# T8 — dev debug actions ＋ healthcheck（0.5d）

## T8a — `Source/Dev/OfficerDebugActions.cs`

仿 `PoliticsDebugActions.cs`（`[DebugAction]` 分類 `"pas.officers"`、
`allowedGameStates = AllowedGameStates.Playing`、輸出進 log）。P0 無玩法，
**dev actions 就是驗收工具**——每條對應一項 P0 驗收標準：

| action | 行為 | 驗收哪條 |
|---|---|---|
| `Create officer at selected` | 對 `Find.WorldSelector.SingleSelectedObject` 呼 `CreateOfficer`（Generic 角色） | 驗收 1 |
| `Materialize first officer` | 選中物件首官 `Materialize` → log pawn 名/ID | 驗收 3 前置 |
| `Dump officer registry` | 仿 `DumpRebellionState`：逐 record 印 id/名/角色/七維/宿主/`Spawned`/`world`/`forcedKeep`/`inhabitantsList`/opinions | 驗收 1-4 對帳總表 |
| `Roll attributes` | 首官七維重擲 → dump 前後對照 | 驗收 2 |
| `Add sworn brothers (first two)` | 選中物件前兩官 `AddPersistentRelation(SwornBrother)` | 驗收 4 |
| `Offset opinion -100` | 前兩官互打 -100 脈衝 | 驗收 4（回歸觀察） |
| `Kill officer pawn` | 首官 pawn `Kill(null)` → 觀察下兩輪心跳 G5 流程 | 鐵則自癒 |
| `Destroy host object test` | log 提示手動毀物件後 dump 看 `OfficerUnassigned` | 鐵則自癒 |
| `API null-safety probe` | 對 API 全方法餵 null，計通過數 | T7 驗證 3 |

Dump 格式照抄 `PoliticsDebugActions.Describe`（`:55-71`）的 key=value 風格，逐項可 grep。

## T8b — `tests/healthcheck.py`

複製 npc-outposts `tests/healthcheck.py` 骨架，改查核項（去 sims-mode 相依鏈、改前綴）：

1. 所有 XML well-formed（`Defs/**`、`Languages/**`、`About/About.xml`）。
2. About：`packageId == "pas.officers.community"`；**無** `modDependencies` 節點
   （零硬相依是 P0 鐵律，防手滑加依賴）。
3. csproj：無 `<Reference Include=` 第三方 DLL（同上，雙保險）；
   `RootNamespace == pas.officers`。
4. XML 引用的 `pas.officers.*` 類存在於 `Source/`（照抄 npc-outposts 第 5 段 regex 法）。
5. C# 引用的 `pas_officers_*` defName/key 都在 XML（照抄第 6 段；涵蓋 DefOf 與 Translate key）。
6. Def 完整性：恰好 1 個 `OfficersSettingsDef`；`checkIntervalTicks > 0`；
   `maxOfficersPerObject >= 1`；兩個 `PawnRelationDef`（SwornBrother/BloodFeud）存在
   且 `reflexive == true`。
7. `OfficerRecord.ExposeData` 含七維全部欄位名（regex 掃 `Scribe_Values.Look(ref <field>`
   七個都在——防「加欄位忘 scribe」這一類最常見存檔 bug）。
8. （守住 G3 決議）`Source/` 內 grep 不到 `Harmony`／`RimWar`／`FactionColonies` 字串。

```bash
python3 ~/repo/my_rimworld_mods/named-officers/tests/healthcheck.py
```

Expected：`healthcheck OK`。

## 驗證步驟

1. build + healthcheck 雙綠。
2. 實機 dev mode：每條 debug action 跑一遍、log 無紅字（完整 E2E 流程留 T9 串）。

## Commit

`feat: named-officers dev actions + healthcheck（P0 驗收工具）`
