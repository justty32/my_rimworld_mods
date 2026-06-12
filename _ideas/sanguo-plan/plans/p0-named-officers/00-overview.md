# P0 `pas.named-officers` 實作計畫 — 總覽與任務索引（2026-06-12）

> 設計依據（權威）：`../../02-mod-named-officers.md` + `../../01-architecture.md`（DAG/鐵律）
> + `../../06-relations-and-lord-actions.md`（關係雙軌）+ `../../09-roadmap.md`（P0 驗證標準）。
> 調查依據：`../../../2026-06-12-rimwar-empire-investigations.md` E/F/I 段。
> 泛化來源：`faction-politics/Source/`（`RebelRecord.cs`/`RebelSpawner.cs`/`WorldComponent_RebellionTracker.cs`）。
> **P0 是新 mod，不改 faction-politics**（其改用本層是 P4 的事）。

## 範圍一句話

把 faction-politics 的「單一反叛者」管線抽取泛化為通用**具名職官層**：
record + 註冊表 + 懶生成 + 屬性 + 關係雙軌 + 對外 API。**本身零玩法**，
零 Harmony、零硬相依（不 ref RimWar/Empire/sims-mode），純 RimWorld 1.6 API。

## 命名定案（設計檔未鎖死處，本計畫拍板）

| 項目 | 值 | 依據 |
|---|---|---|
| mod 目錄 | `my_rimworld_mods/named-officers/` | 對齊概念名 |
| packageId | `pas.officers.community` | 家規 `pas.X.community`（見缺口 G1） |
| AssemblyName / RootNamespace | `NamedOfficers` / `pas.officers` | 仿 npc-outposts |
| defName/key 前綴 | `pas_officers_` | healthcheck 防呆慣例 |
| 核心類 | `OfficerRecord`、`OfficerSpawner`、`WorldComponent_OfficerRegistry`、`OfficersApi`、`OfficerRoleDef`、`OfficersSettingsDef`、`OfficerDebugActions` | 02 設計 |

## 任務索引與依賴圖

| # | 檔案 | 內容 | 估時 | 依賴 |
|---|---|---|---|---|
| T0 | `01-task-skeleton.md` | API 驗證 spike（讀碼不寫碼） | 0.5d | — |
| T1 | `01-task-skeleton.md` | mod 骨架：About/csproj/.gitignore/空建置 | 0.25d | T0 |
| T2 | `02-task-data-layer.md` | Def 層：`OfficerRoleDef`+`OfficersSettingsDef`+DefOf | 0.25d | T1 |
| T3 | `02-task-data-layer.md` | `OfficerRecord : IExposable`（屬性/關係 dict/refs） | 0.5d | T2 |
| T4 | `03-task-registry.md` | `WorldComponent_OfficerRegistry`（心跳+自癒+存讀） | 0.5d | T3 |
| T5 | `04-task-materialization.md` | `OfficerSpawner` 懶生成（KeepForever+inhabitants 橋） | 0.5d | T4 |
| T6 | `05-task-relations.md` | 關係雙軌：`PawnRelationDef` XML + opinion 演化 | 0.5d | T4,T5 |
| T7 | `06-task-api.md` | `OfficersApi` 對外 static API + 事件 hook | 0.5d | T4,T5,T6 |
| T8 | `07-task-dev-healthcheck.md` | dev debug actions + `tests/healthcheck.py` | 0.5d | T7 |
| T9 | `08-task-e2e-docs.md` | 實機 E2E 驗證 + Languages + PROJECT.md | 0.5d | T8 |

```
T0 → T1 → T2 → T3 → T4 → T5 ─┬→ T6 → T7 → T8 → T9
                             └────────↗
（T2/T3 可與 T1 尾端並行；T6 需 T5 的具現能力做 A 軌；其餘嚴格串行）
```

**估計總工程量：約 4.5 天**（10 任務、每任務 ≤ 半天、皆含驗證步驟）。

## P0 驗收標準（`09-roadmap.md` Phase 0 + 里程碑鐵則）

1. 能對一個 NPC 派系/世界物件生出具名職官 record（dev action 觸發）。
2. 屬性（武力/統率/政務/魅力/忠誠）可寫可讀、存讀檔往返不丟。
3. 玩家拜訪駐地請回**同一個** pawn（`previouslyGeneratedInhabitants` 橋走通）。
4. 關係 B 軌 dict 由心跳演化、A 軌結拜/世仇可建立且隨 pawn 存檔。
5. 鐵則：獨立編譯 0 警告 0 錯誤；空世界/舊檔中途裝 mod 不炸（`FinalizeInit(fromLoad)`）；
   移除 mod 後舊檔可讀（僅 warning）；healthcheck 通過。

## 設計檔缺口（本計畫的決議，建議回寫設計檔）

- **G1 packageId 未定**：02 用 `pas.named-officers` 當概念名，非合法家規 packageId
  → 拍板 `pas.officers.community`。
- **G2 屬性集出入**：02 列七維（武力/統率/智力/政務/魅力/忠誠/士氣）
  → 七個 typed int 欄位全建，MVP 啟用五維（武力/統率/政務/魅力/忠誠），智力/士氣預留（02 風險「over-design」條款）。
- **G3 屬性承載矛盾**：02 說「屬性層 = WorldObjectComp + Harmony 注入」，但 P0 約束零 Harmony、不 ref RimWar
  → 決議：**record 為屬性唯一真相**（仿 RebelRecord）；P0 只出貨**無狀態 view comp 型別**
  （`WorldObjectComp_OfficersView`，讀 registry 供 inspect），Harmony 注入 def comps 由消費 mod（P1/P2）自做。
  順帶消滅 02 的「comp 掛載時序」風險（P0 無 comp 資料可丟）。
- **G4 A 軌前置條件**：`DirectPawnRelation` 需兩個真 Pawn → 結拜/世仇 API 對未具現職官**按需具現後再建**（02 未明說）。
- **G5 職官死亡政策**：反叛者有 respawn 循環，職官沒定 → P0 決議：死亡=record 標記+事件廣播，
  預設一個心跳後移除；繼任邏輯留給消費 mod（P2 領主、P4 叛亂各有需求）。
- **G6 數量控管無上限數字** → `OfficersSettingsDef.maxOfficersPerObject`（預設 4）+ 只 dev/API 觸發生成（P0 不自動鋪官）。

## 貫穿鐵律（`00-vision.md`/`01-architecture.md`）

每檔 < 200 行（含本計畫各檔）；複用勿重造（管線手法照抄 faction-politics，已驗證）；
P0 不動任何既有 mod；不動派系級係數（P0 無涉，列出供消費期警惕）。
