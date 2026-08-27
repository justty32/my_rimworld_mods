# session log

## 2026-06-12 — P0 實作 session（T0–T9）

- T0 API spike：8/8 簽章在 decompile 源（`~/repo/projects/rimworld/`，Krafs ref 1.6.4850）
  全數驗證通過，行號記入 `_ideas/sanguo-plan/plans/p0-named-officers/01-task-skeleton.md`。
  名字策略定案**方案 B**（首次具現後快取 pawn 名）。無一落空，未回改任務設計。
- T1 骨架：About/csproj/.gitignore，空建置 0/0。
- T2 Def 層：OfficerRoleDef / OfficersSettingsDef（含 ConfigErrors）/ OfficersDefOf + Roles.xml / Settings.xml。
- T3 OfficerRecord：七維全 scribe、opinions dict（key=record id）、dead 標記、PostLoadInit 自癒。
- T4 registry：扁平 Deep list + nextId + 執行期 index（不 scribe）；心跳節流 2500-tick、
  逐 record 例外隔離；自癒五分支抽到 `OfficerHealer.cs`（守 <200 行鐵律——計畫單檔會超）。
- T5 OfficerSpawner：懶生成 Materialize（KeepForever + inhabitants 橋 + SyncName 方案 B）。
- T6 關係雙軌：Relations.xml（SwornBrother +60 / BloodFeud -60，reflexive）+
  OpinionEvolver（同宿主回歸 bias，不觸發具現）+ RelationsUtility_Officers（G4 按需具現）。
- T7 OfficersApi：查詢/生命週期/屬性/關係 + 三個 static event（訂閱者逐一 try-catch 隔離）+
  無狀態 WorldObjectComp_OfficersView（G3）。
- T8 dev actions（10 條，對應 P0 驗收）+ tests/healthcheck.py（8 項，含零 Harmony/零相依 grep 防呆）。
  healthcheck 首跑抓到兩個真問題（缺 keyed key、註解含禁字）→ 修畢。
- T9 Languages 三語（en/zh-CN/zh-TW，user 鐵律加 zh-CN）+ PROJECT.md（API 契約表）+
  本檔；**實機 E2E 留 checklist 未簽收**（見 PROJECT.md）。

最終：`dotnet build` 0 警告 0 錯誤；`python3 tests/healthcheck.py` → healthcheck OK；
`1.6/Assemblies/` 僅 NamedOfficers.dll。未 commit、未部署。
