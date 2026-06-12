# E1 自訂哨站類型 — 設計 spec

> 2026-06-12。brainstorm 定案（使用者核可、免複審、直接接 writing-plans）。
> 上游構想：`docs/ideas-expansions.md` E1 節。源碼核對：VOE `Outposts.decompiled.cs`（VEF/2023507013 出貨）、本 mod `Source/`。

## 願景一句話

把一座「從殖民地封存而來」的 `Outpost_Sampled` 升格為**玩家定義、可重複建造**的哨站類型；日後用商隊在 VOE 既有的建站選單裡**不花資源**直接蓋出同類型哨站（忠實復刻其產出/消耗 snapshot）。

## 需求決策（brainstorm 定案）

| 問題 | 決定 |
|---|---|
| 類型存什麼 | 名稱（label）＋ 世界圖標（iconPath）＋ `ProductivitySnapshot`**原樣複本**（含 `perPawnScaling`/`basePawnCount`）。忽略技能需求等其他屬性。 |
| 速率語意 | **原樣複製 snapshot**——是 per-pawn 就隨人數縮放、是絕對速率就不論人數套同值。註冊 UI 標明該類型屬哪種，玩家註冊前知情。 |
| 註冊入口 | 選中大地圖上一座封存來的 `Outpost_Sampled` → gizmo「註冊哨站類型」。 |
| 建造入口 | **VOE 既有建站選單**（`Dialog_CreateCamp`，商隊 gizmo 開）內，把已註冊類型列為可建項。 |
| 取消註冊入口 | 同一選單，每個自訂類型列**右邊**加「取消註冊」鈕。 |
| 花費 | **不花資源**（跳過 `OutpostExtension.CostToMake`）。 |
| 存哪 | per-save `GameComponent`（單存檔的類型清單）。 |
| 存檔安全 | 建出的哨站各自帶 snapshot 複本、掛**穩定** def `pas_archival_Outpost`；取消註冊永不影響已建好的哨站。 |

## 技術現實（設計時已核對）

- **VOE 建站選單迭代 `OutpostsMod.Outposts`**（`Outposts.decompiled.cs:2102/2140`）＝「所有 `Outpost` 子類的 `WorldObjectDef`」清單，post-load 由 `FindOutposts` 建。**我們的 `pas_archival_Outpost`（`Outpost_Sampled : Outpost`）已在其中** → 現在已會以一個空 snapshot 項出現在選單，E1 須把它**藏起來**。
- **`Dialog_CreateCamp`**（`:73`）：ctor 迭代 `OutpostsMod.Outposts` 建 `validity` 字典（反射呼 `CanSpawnOnWith`）；`DoWindowContents`（`:129`）在 scroll view 內（高度 = `OutpostsMod.Outposts.Count * 110`）逐項呼 `DoOutpostDisplay`；`DoOutpostDisplay`（`:154`）畫 圖標/label/描述/「Create」鈕＋validity 字串，Create →`WorldObjectMaker.MakeWorldObject(def)`＋設 Name/Tile/Faction＋`Find.WorldObjects.Add`＋逐一 `AddPawn(商隊 pawn)`＋Close＋選取（`:217-228`）。
- **現有封存路徑可複用**：`ArchivalService.Archive`（`Source/ArchivalService.cs:196-204`）已是「直接 `WorldObjectMaker.MakeWorldObject("pas_archival_Outpost")`＋`SetFaction`＋`SetSnapshot`＋`AddPawn`」——E1 的建造邏輯與此同構，可抽共用 helper。
- **`Outpost_Sampled` 是自家類別**：可直接 override `GetGizmos`，不需 Harmony 加註冊鈕；已有 `chosenIconPath` 欄位與 `SetSnapshot`。

## 架構（4 元件）

### 1. 資料模型 `OutpostType` + `GameComponent_ArchivalTypes`
- `OutpostType : IExposable`：`string label`、`string iconPath`、`ProductivitySnapshot snapshot`。
- `GameComponent_ArchivalTypes : GameComponent`：持 `List<OutpostType> types`；`ExposeData` 用 `Scribe_Collections.Look(LookMode.Deep)`。
- 提供 API：`Register(OutpostType)`（同 label 視為覆寫並提示）、`Unregister(OutpostType)`、`All`。
- 註冊到 `1.6/Defs/`（GameComponent 需在某 `GameComponentDef`？否——`GameComponent` 由 `Current.Game.components` 自動實例化，只要類別存在即可；以 `Current.Game.GetComponent<GameComponent_ArchivalTypes>()` 取用）。
- **快照深複製**：`ProductivitySnapshot` 加 `Clone()`（新建各字典、複製值；def key 為共享單例直接帶引用）。註冊時複製來源 snapshot、建造時複製類型 snapshot——兩次都深複製，杜絕共享可變字典。

### 2. 註冊 gizmo（`Outpost_Sampled.GetGizmos` override）
- override `public override IEnumerable<Gizmo> GetGizmos()`：`foreach base` 後 append「註冊哨站類型」`Command_Action`。
- action：以本哨站的 `Name`（label）、`chosenIconPath`（iconPath，空則用 def 預設）、`snapshot.Clone()` 建 `OutpostType`，呼 `GameComponent.Register`；`Messages.Message` 提示成功（或覆寫）。
- 僅在 snapshot 非空時可用（空哨站註冊無意義 → `Disable` 並給 tooltip）。

### 3. VOE 建站選單整合（Harmony patch `Dialog_CreateCamp`）
**機制（推薦：暫時抽換清單 + 自繪自訂列）**：
- **Prefix `Dialog_CreateCamp` ctor**：保存 `OutpostsMod.Outposts` 原清單參考；建一個「對話框用清單」＝原清單**移除 `pas_archival_Outpost` base def** ＋ **每個已註冊類型一個暫態 `WorldObjectDef`**（`new WorldObjectDef{ defName, label, description, worldObjectClass=typeof(Outpost_Sampled), expandingIconTexture=type.iconPath }`，**不註冊進 DefDatabase**），把 `OutpostsMod.Outposts` 指向此清單；同時 stash `Dictionary<WorldObjectDef, OutpostType>`（暫態 def → 類型）。
- **Postfix `Dialog_CreateCamp.PostClose`（或 `Close`）**：把 `OutpostsMod.Outposts` 還原為原清單，清掉 stash。
- **Prefix `Dialog_CreateCamp.DoOutpostDisplay(ref Rect, WorldObjectDef)`**：若 def 是我們的暫態 def → **自繪整列**（圖標/label/速率語意提示/「建立」鈕/「取消註冊」鈕），return false 跳過 VOE 原繪製：
  - 「建立」→ 走共用 founding helper：`MakeWorldObject("pas_archival_Outpost")`、`SetSnapshot(type.snapshot.Clone())`、Name/Tile=商隊 tile/Faction=商隊陣營、`Find.WorldObjects.Add`、逐一 `AddPawn(商隊 pawn)`、關閉對話框、選取。**跳過 CostToMake**。建造前置：商隊至少 1 名人類殖民者，否則「建立」鈕 disable＋提示（空哨站不產出）。
  - 「取消註冊」→ `GameComponent.Unregister(type)`；即時從對話框清單移除該暫態 def（重整）。
- 非我們的 def（VOE 自家哨站、其他 mod 的）→ 不攔，原樣走 VOE 繪製。
- **暫態 def 永不落到存檔**：founding 一律用穩定的 `pas_archival_Outpost`，建出的 `Outpost_Sampled.def` 是真 def；暫態 def 只活在對話框開啟期間、僅供選單渲染。
- **全域狀態風險（已知、可控）**：抽換 `OutpostsMod.Outposts` 是改靜態欄位；對話框為 modal，期間其他碼讀該清單機率極低，ctor↔Close 成對還原。spec 採此法因它讓「ctor 建 validity / scroll 高度 / 迴圈渲染」三處零 transpile 自動吃到自訂列。實作時若發現還原時機有縫，退路＝改 patch 兩個讀取點（ctor 與 `DoWindowContents` 迴圈）走我們的擴充清單，不碰靜態欄位。

### 4. 建造 & 取消註冊
- 建造：見 §3「建立」。共用 founding helper 與 `ArchivalService.Archive` 的建站段抽成 `OutpostFactory.Create(tile, faction, snapshot, name, iconPath, pawns)`，兩處共用避免漂移。
- 取消註冊：見 §3「取消註冊」。只動 GameComponent 清單；**已建好的哨站不受影響**（自帶 snapshot 複本、掛穩定 def）。

## 資料流

```
封存哨站(Outpost_Sampled, 帶 snapshot)
   │  gizmo「註冊哨站類型」
   ▼
GameComponent_ArchivalTypes.types += OutpostType{label,iconPath,snapshot.Clone()}
   │  商隊開 VOE Dialog_CreateCamp（ctor prefix 注入暫態 def）
   ▼
選單顯示：VOE 原生哨站 … ＋ 自訂類型列(建立 | 取消註冊)，base def 隱藏
   │  按「建立」
   ▼
OutpostFactory.Create(商隊 tile, 商隊陣營, type.snapshot.Clone(), …, 商隊 pawns)
   ▼
新 Outpost_Sampled（穩定 def、自帶 snapshot 複本）→ 照常 Produce()
```

## 邊界與錯誤處理

- **註冊空 snapshot**：gizmo disable（tooltip 說明）。
- **同 label 重複註冊**：覆寫既有類型 + `Messages.Message` 告知。
- **iconPath 失效/留空**：建造與選單渲染走 `ContentFinder<Texture2D>.Get(path, false)`，null 時 fallback 到 def 預設圖標（比照 `Outpost_Sampled.ExpandingMaterial` 既有 fail-soft）。
- **snapshot 含已移除 mod 的 def**：`ProductivitySnapshot.ExposeData` PostLoadInit 已 `RemoveAll(key==null)`；`Clone()` 也在移除後的字典上做 → 安全。
- **商隊無殖民者**：「建立」disable。
- **取消註冊不影響存量**：已建哨站不引用類型紀錄，無懸空參照。
- **VOE 未安裝/版本不符**：本 mod 硬相依 VOE/VEF，`Dialog_CreateCamp`/`OutpostsMod` 必存在；patch target 以型別字串解析，缺失時 Harmony 報明確錯（與既有相依假設一致）。

## 測試

- **靜態健檢**：`tests/healthcheck.py`（keyed 齊全、Defs 合法）；`dotnet build` 0/0。
- **新增 keyed**：`CAO.RegisterType`/`.Desc`/`.Registered`/`.Overwritten`/`.EmptySnapshot`、`CAO.Founding.Create`/`.Unregister`/`.NeedColonist`/`.RatePerPawn`/`.RateAbsolute`（en/zh-CN/zh-TW 三語）。
- **實機 E2E checklist**：①封存哨站出現「註冊哨站類型」gizmo、空哨站時 disable；②註冊後商隊開建站選單見到該類型、base 空項已隱藏；③「建立」在商隊 tile 蓋出哨站、商隊 pawn 進駐、不扣資源、snapshot 正確（per-pawn 類型換人數會縮放）；④「取消註冊」後選單移除該項、已建哨站續正常產出；⑤存讀檔：類型清單與已建哨站往返不壞；⑥取消註冊一個類型後，先前用它建的哨站存讀檔仍正常。

## 不在本次範圍（YAGNI）

- 類型的編輯/改名/換圖（先只有 註冊/取消註冊；要改＝取消後重註冊）。
- 跨存檔共享類型（明確 per-save）。
- E2/E3/E4（臨時地圖生成/阻擋襲擊/重新採樣）——獨立後續項。
- 類型專屬地圖佈局（屬 E2）。
