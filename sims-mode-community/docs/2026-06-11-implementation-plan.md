# Sims Mode Community P1（活的聚落）Implementation Plan（索引）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. 各 task 內容在 `plan/task-*.md`（checkbox 追蹤在各檔內）。

**Goal:** 玩家商隊訪問非敵對派系聚落時，聚落 pawn 按「派系 profile → 角色 → 作息表」過生活（白天工作/傍晚社交/夜間睡床），被襲擊切防禦；體系四層 Def 化供其他 mod patch。(Living settlements: when visiting a non-hostile faction settlement, pawns live by faction-profile-driven roles & schedules instead of standing guard.)

**Architecture:** XML PatchOperation 往 `Base_Faction` MapGeneratorDef 附加 `GenStep_SettlementLife`（order 9999，跑在守軍生成後）→ 掃描設施（`FacilityTagDef` matcher 管線）→ 解析派系 `LifeProfileDef` → `RoleAssignmentWorker` 分角色 → 把 pawn 從 `LordJob_DefendBase` 移到 `LordJob_SettlementLife`（生活 toil 按時辰查表發 duty；Trigger 切原版 `LordToil_DefendBase`/`LordToil_AssaultColony`）。零 Harmony。

**Tech Stack:** C# net48（SDK-style csproj + `Krafs.Rimworld.Ref` 1.6 參考組件，本機無遊戲安裝也可編譯）＋ XML Defs/PatchOperation。namespace `pas.sims`，defName 前綴 `pas_sims_`，packageId `pas.sims.community`。

> **權威源**：`C:\code\mine\pas\projects\rimworld`（1.6 反編譯）。所有 API 已坐實，關鍵座標：
> - `RimWorld.Planet\Settlement.cs:80-94`（聚落用 `MapGeneratorDefOf.Base_Faction`）、`:48-58`（`Visitable`＝非玩家+非敵對+非太空）
> - `Verse\GenStepDef.cs:1-21`（`order`/`genStep` 欄位）、`Verse\MapGenerator.cs:309-311`（按 `order, index` 排序執行）
> - `RimWorld.BaseGen\SymbolResolver_Settlement.cs:43-64`（守軍掛 `LordJob_DefendBase`）
> - `RimWorld\LordJob_DefendBase.cs:33-77`（StateGraph/Trigger 範本 + ExposeData 範本）
> - `Verse.AI.Group\LordToil.cs:51-57`（`Init`/`UpdateAllDuties`/`LordToilTick` 每 tick）、`Verse.AI.Group\Lord.cs:593-614`（tick 鏈）
> - `Verse.AI\PawnDuty.cs:60`（`PawnDuty(DutyDef, LocalTargetInfo focus, float radius=-1)`）
> - `Verse.AI.Group\LordMaker.cs:8-33`（`MakeNewLord(faction, lordJob, map, startingPawns)`）
> - `RimWorld.Planet\CaravanFormingUtility.cs:118-131`（換 lord 官方範式：`Notify_PawnLost(ForcedToJoinOtherLord)` → `MakeNewLord`）
> - `RimWorld\RestUtility.cs:162-196`（`IsValidBedFor`；:187 派系檢查＝床派系==pawn 派系即通過 → **NPC 在自己聚落能睡床**）
> - `RimWorld\JobDriver_LayDown.cs`（`JobDefOf.LayDown` + 床 target）、`Verse.AI\Toils_General.cs:19-37`（`Wait(ticks, face)`）
> - `RimWorld\ThinkNode_Duty.cs:1-31`（duty.thinkNode 查表入口）、`Verse.AI\DutyDef.cs`（`thinkNode`/`socialModeMax`）
> - `RimWorld\GenLocalDate.cs:16-19`（`HourOfDay(Map)` 回 0-23）
> - `Verse\Scribe_Collections.cs:361-497`（Dictionary keysAndValues + LookMode.Reference/Def）
> - 「SatisfyBasicNeeds」是 XML-only ThinkTreeDef（C# 無；RimCities 1.6 Duties.xml 實際引用中，證明 Core 存在）
>
> **測試現實**：無 pytest/unittest 可跑 RimWorld 邏輯。每 task 的驗證＝`dotnet build`（編譯即型別檢查）＋ `python tests\healthcheck.py`（Task 9 建立）＋ 最終實機 E2E（Task 11 手動清單）。
>
> **commit 規則**：只 `git add` 本 mod 明確路徑（`sims-mode-community/...`），勿 `-A`。訊息附 `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`。
>
> **在地化決策**：v1 無玩家 UI 字串（唯一可見字串是 JobDef reportString），Languages/ 延後到 P2。

---

## 檔案結構

```
sims-mode-community/
├── About/
│   └── About.xml                          # packageId pas.sims.community, 1.6, 無相依
├── 1.6/Assemblies/                        # 建置輸出（SimsModeCommunity.dll）
├── Defs/
│   ├── FacilityTagDefs/Facilities.xml     # Bed / GatherSpot / Workbench / FarmPlot
│   ├── JobDefs/Jobs.xml                   # pas_sims_FakeWork
│   ├── DutyDefs/Duties.xml                # FakeWork / Social / Sleep / Guard / HomeLife（純 XML think node）
│   ├── LifeRoleDefs/Roles.xml             # Guard / Worker / Farmer / Resident（含作息表）
│   └── LifeProfileDefs/Profiles.xml       # default + tribal（派系維度）
├── Patches/
│   └── MapGenerator_SettlementLife.xml    # 往 Base_Faction genSteps 加 li
├── Source/
│   ├── SimsModeCommunity.csproj
│   ├── SimsDefOf.cs                       # [DefOf] JobDef
│   ├── Defs/
│   │   ├── FacilityTagDef.cs              # + FacilityTagExtension (DefModExtension)
│   │   ├── FacilityMatcher.cs             # 抽象 + ThingClass/DefNames/Crop/Table 四內建
│   │   ├── LifeRoleDef.cs                 # + ScheduleEntry
│   │   └── LifeProfileDef.cs              # + LifeRoleEntry
│   ├── Facility/
│   │   └── MapComponent_FacilityRegistry.cs
│   ├── Assign/
│   │   ├── ProfileResolver.cs             # + LifeProfileExtension (DefModExtension)
│   │   └── RoleAssignmentWorker.cs
│   ├── LordAI/
│   │   ├── GenStep_SettlementLife.cs
│   │   ├── LordJob_SettlementLife.cs
│   │   └── LordToil_SettlementLife.cs
│   └── Jobs/
│       ├── JobGiver_FakeWork.cs
│       ├── JobDriver_FakeWork.cs
│       └── JobGiver_SleepAtDutyFocus.cs
├── tests/
│   └── healthcheck.py                     # 靜態健檢（仿 colony-archival-outpost）
├── docs/
│   ├── 2026-06-11-design.md               # spec（權威）
│   ├── 2026-06-11-implementation-plan.md  # 本索引
│   ├── plan/task-*.md                     # 各 task 詳細步驟（見下表）
│   └── examples/extension-sample.xml      # 第三方擴充示範（不載入，文件用）
├── PROJECT.md
└── session_log.md
```

## Task 清單（依序執行；checkbox 在各檔內）

| Task | 檔案 | 內容 | 產出驗證 |
|---|---|---|---|
| 0 | `plan/task-00-api-verification.md` | grep 反編譯源碼坐實殘餘 API（think node 類名/欄位、Lord 雜項、IsValidBedFor 簽名） | 偏差記入 session_log |
| 1 | `plan/task-01-skeleton.md` | About.xml + csproj（Krafs）+ 空建置 | dotnet build 綠 |
| 2a | `plan/task-02a-facility-defs.md` | FacilityMatcher 管線 + FacilityTagDef/Extension | （與 2b 合併建置） |
| 2b | `plan/task-02b-role-profile-defs.md` | LifeRoleDef/ScheduleEntry + LifeProfileDef/LifeRoleEntry + SimsDefOf + 兩個暫時殼 | dotnet build 綠 |
| 3 | `plan/task-03-facility-registry.md` | MapComponent_FacilityRegistry 實作 + Facilities.xml | dotnet build 綠 |
| 4 | `plan/task-04-job-atoms.md` | 假工作 JobDriver/JobGiver + 指定床睡覺 JobGiver + Jobs.xml | dotnet build 綠 |
| 5 | `plan/task-05-duties.md` | 五個 DutyDef（純 XML think node） | （XML，Task 9 健檢驗） |
| 6a | `plan/task-06a-assignment-workers.md` | ProfileResolver 解析鏈 + RoleAssignmentWorker 實作 | dotnet build 綠 |
| 6b | `plan/task-06b-roles-profiles-xml.md` | Roles.xml（四角色作息）+ Profiles.xml（default+tribal） | dotnet build 綠 |
| 7 | `plan/task-07-lord.md` | LordJob_SettlementLife（狀態機+存檔）+ LordToil（查表發 duty） | dotnet build 綠 |
| 8 | `plan/task-08-genstep.md` | GenStep_SettlementLife + GenStepDef + Base_Faction patch | dotnet build 綠 |
| 9 | `plan/task-09-healthcheck.md` | healthcheck.py（XML/交叉引用/patch 鏈/DefOf 防呆） | healthcheck OK |
| 10 | `plan/task-10-extension-sample.md` | 第三方擴充示範 XML + PROJECT.md 收尾 | — |
| 11 | `plan/task-11-e2e.md` | 實機 E2E 手動清單（載入/作息/翻臉/攻打/存讀檔/離場） | 記入 session_log |

## 自我審查結果

- **Spec 覆蓋**：spec §2「做」全對應——非敵對判定/換 Lord（Task 8）、作息+角色（Task 5-7）、派系維度（Task 6）、防禦切換（Task 7）、可擴充性示範（Task 10）、存讀檔（Task 11 Step 7）。spec §7 完成定義逐條對應 Task 11 步驟。
- **「進場時辰正確」**：`MakeNewLord` → `GotoToil(StartingToil)` → `LordToil_SettlementLife.UpdateAllDuties()` 立即按當前 `GenLocalDate.HourOfDay` 發 duty（`LordMaker.cs:24`），無需額外處理。
- **型別一致性**：`RoleAssignmentWorker.Assign` 簽名在 Task 2b（殼）與 6a（實作）一致；`MapComponent_FacilityRegistry.Get/RebuildAll` 在 Task 3 定義、6a/7/8 使用一致；`ScheduleEntry` 欄位名 XML 與 C# 一致。
- **已知開放風險**（不阻塞，Task 11 驗證）：(a) NPC 睡床的 `BedOwnerWillShare`/指派細節——派系檢查已坐實會過，但多 pawn 搶同床可能被 owner-share 條件擋，靠 `PickFocus` 取模錯開緩解；(b) `JobGiver_AIFightEnemies` 等 XML 類名以 Task 0 結果為準；(c) `Trigger_PawnHarmed` 對「玩家誤傷單一 NPC」即全聚落切防禦——v1 接受（原版 DefendBase 同款敏感度）。
