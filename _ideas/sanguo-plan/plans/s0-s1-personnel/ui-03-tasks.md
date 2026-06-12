# C2/C3 UI 任務切分與驗證

歸屬：S1 personnel mod（ui-01 定案）。檔案全落 `personnel/Source/UI/` 與 `Source/Patches/`。

## C2 — MVP 只讀清單（1d，依賴 B1 骨架＋A1 欄位；可與 B2~B5 並行開發、
## 但實機驗收需 B2 撒種供料）

### C2-1 gizmo＋空視窗殼（0.25d）

**Create:** `Source/Patches/Patch_SettlementGizmo.cs`、`Source/UI/Window_SettlementTalents.cs`

- postfix `Settlement.GetGizmos`（抄 `colony-archival-outpost/Source/Settlement_GetGizmos_Patch.cs`）；
  顯示守衛：非玩家城、`GetOfficers+GetIdleAt` 非空。
- Window 殼：InitialSize/標題行/底欄[關閉]，內容區先畫佔位字串。
- HarmonyInit 登錄走既有 TryPatch fail-soft。

**驗證**：build 過；實機選 NPC 城（先 dev 撒一名在野）→ gizmo 出現 → 開窗/關窗不炸；
無料之城無 gizmo；玩家城無 gizmo。

### C2-2 資料拉取＋列表繪製（0.5d）

**Create:** `Source/UI/TalentRowDrawer.cs`（靜態繪行 util，仿 `SnapshotPreviewDrawer` 拆檔慣例）

- 開窗拉 serving/idle、EnsureNameCached、組 Row 快取（ui-02 規格）；
- 兩 section＋表頭＋scroll view＋行繪（欄寬/著色照 ui-02 表）；
- 排序：在任 rank 降序、在野屬性和降序；
- `[重新整理]` 重拉。

**驗證**：dev 城上湊「太守(P2)＋2 在野＋1 待命」→ 開窗：分區正確、欄位對齊、
著色正確、>15 筆可捲動；名字全有（無 "officer" fallback 漏網）；
開窗前後 `Dump registry`——**無新 pawn 具現**（爆量鐵律）；30+ 在野城開窗無卡頓。

### C2-3 tooltip＋keyed＋潤飾（0.25d）

- 行 tooltip：七維全示＋履歷 ≤3 筆＋同清單關係 ≤3 筆（ui-02 規格，開窗預組字串）；
- 三語 keyed 全鋪；Text.Font/Anchor 進出對稱（UI 髒狀態鐵律）；
- healthcheck 補 keyed 交叉引用掃描。

**驗證**：hover 各行 tooltip 正確；對 Offset 過 opinion 的兩官，關係行顯示數值與符號；
結拜對（debug 建立）顯示⚔/♥標記；切英文語言無缺 key 紅字。

## C3 — 互動鈕後補（0.5d，依賴 C2＋B5 RecruitService）

### C3-1 徵辟入口（0.35d）

- 底欄 `[徵辟…]`＋在野行尾 `[辟]` 鈕（ui-02 位置定案）；
- 守衛鏈：玩家任一地圖有可用通訊台（`Building_CommsConsole` 可用性檢查，
  抄 vanilla `CanUseCommsNow`）、該派系非敵對、冷卻已過、銀夠——任一不過 → 灰＋原因；
- 點擊 → `RecruitService.TryRecruit(record, faction, bestNegotiator)`（B5 共用核心；
  negotiator 取通訊台地圖最高社交殖民者——RecruitService 簽章在 B5 預留 map/negotiator 解析）；
- 成功 → 行從清單移除（重拉）＋視窗頂 flash 訊息。

**驗證**：通訊台在/不在、銀夠/不夠、冷卻中三態鈕灰邏輯正確；徵辟成功入場、
清單即時更新；與 B5 通訊台路徑共用冷卻（一邊徵辟另一邊立刻灰）。

### C3-2 關係查看（0.15d）

- MVP+：點行展開（行高×2）列完整 opinions（對同清單全部 record）；
- 跳 `Dialog_InfoCard`（需具現）**否決**：為看資料生 world pawn 違反爆量鐵律——
  記 backlog（待「拜訪中自動開卡」這種已具現場景）。

**驗證**：展開/收合不破版；無關係者顯示「無」；捲動位置不跳。

## 不做清單（明示，防範圍蔓延）

- 可點表頭排序、搜尋框、跨城總覽（MainTabWindow 全人才表）→ backlog（S2 評估）。
- Empire `ISettlementWindowOverview` 分頁 → S3 附庸任命時的薄橋 mod。
- 玩家殖民地人才視窗 → 永不（殖民者不進 record 系統）。
