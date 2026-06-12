# C1 UI 載體決策：聚落人才一覽往哪掛

需求：玩家選中**任一聚落**能看到該地特殊人才——在野＋在任職官清單
（名字/官職/七維/狀態），含後續互動入口（徵辟/查看關係）。

## 三方案比較

### 方案 A ★推薦：聚落 gizmo 開自家 `Window`

選中 Settlement → gizmo「人才」→ `Window_SettlementTalents`（自家 Window 子類）。

- **掛載**：Harmony postfix `Settlement.GetGizmos()`——自家**雙先例逐行可抄**：
  `colony-archival-outpost/Source/Settlement_GetGizmos_Patch.cs:13`
  （postfix yield 原 gizmo＋條件追加 Command_Action）、
  `voe-outpost-enhancement/Source/Patch_Outpost_GetGizmos.cs`（同型）。
- **視窗慣例**：仿 `colony-archival-outpost/Source/Dialog_ArchivalConfirm.cs`
  （InitialSize/DoWindowContents/Widgets.BeginScrollView 列表/底部按鈕）＋
  vanilla `Dialog_InfoCard` 的 TabRecord 分頁式（暫不需分頁，留擴充）。
- **覆蓋面**：patch 基類 → RimWar 城、NpcOutpost、Empire `WorldSettlementFC` 等
  Settlement 子類全吃（子類 override GetGizmos 通常呼 base，仍觸發 postfix——
  colony-archival 已實戰驗證 vanilla 面；Empire 面列入 E2E 抽驗）。
- **相依**：零新增（S1 本就有 Harmony）。
- **缺點**：gizmo 列擁擠（緩解：無任何 record 的聚落不顯示 gizmo）；
  視窗自繪工作量（自家先例攤平）。

### 方案 B：Empire `ISettlementWindowOverview` 分頁

Empire 聚落視窗加「人才」tab（`empire-refactored` 分析：`FCInterfaces.cs:23`，
`PreOpenWindow/OnTabSwitch/DrawOverviewTab(Rect)/PostCloseWindow/OverviewTabName`，
由 `SettlementWindowFC` 掃 comp `comp is ISettlementWindowOverview` 發現，
comp 經 XML 注入 `WorldSettlementDefBase`）。

- **致命錯位**：`WorldSettlementFC`＝**玩家附庸殖民地**視窗——而人才系統的主舞台是
  **NPC 派系聚落**（RimWar 城），根本不開這個窗。覆蓋面錯誤。
- 硬相依 Empire＋comp 注入＋Registry/ClearCaches 陷阱（調查 C 通用陷阱）工程稅。
- **處置**：否決為主載體；記 backlog——S3 做 Empire 附庸任命時，以獨立薄橋 mod
  給附庸城加同款 tab（介面免 Harmony，屆時順手）。

### 方案 C：inspect pane 擴充（P0 既有 `WorldObjectComp_OfficersView`）

P0 已出貨單行 inspect comp（`CompInspectStringExtra` 列名+職）。

- **資訊密度天花板**：inspect pane 數行純文字，放不下七維表格/排序/tooltip/按鈕；
  無互動可言。
- **處置**：否決為主載體；但**保留並小擴**為「一眼摘要」：S1 把 view comp props 注入
  Settlement def（P1/P2 只注入了各自宿主），inspect 行加在野數——
  與方案 A 互補（inspect 掃一眼、開窗看細節）。

## 決策

| 維度 | A gizmo+Window | B Empire tab | C inspect |
|---|---|---|---|
| 覆蓋 NPC 聚落 | ✅ 全部 | ❌ 玩家附庸城 | ✅ 全部 |
| 資訊密度/互動 | ✅ 任意 | ✅ 任意 | ❌ 數行唯讀 |
| 新相依 | 無 | Empire hard | 無 |
| 自家先例 | ✅×2 | △（分析檔） | ✅（P0 現貨） |
| 工程量 | 中 | 中＋稅 | 近零 |

**定案：A 為主載體（C 保留作摘要層；B 記 S3 backlog）。**

## 歸屬（00 決策 6 複述＋理由）

放 **S1 personnel mod**（`Source/UI/`＋`Source/Patches/Patch_SettlementGizmo.cs`）：

- UI 呈現的核心資料（在野人才）由 S1 生產，拆薄 mod＝多一個發佈/版本配對負擔；
- S1 已具 Harmony 框架與 P0 ref，gizmo postfix 零增量成本；
- 互動鈕（徵辟）直接複用 S1 `RecruitService`（B5），跨 mod 反而要再開 API。

反方（獨立薄 UI mod 可單獨用）不成立：沒有 S1 撒種，視窗常年只有 P2 太守一行，
價值不足以支撐獨立 mod。

分期：**C2 MVP 只讀清單 → C3 互動鈕後補**（見 ui-03）。
