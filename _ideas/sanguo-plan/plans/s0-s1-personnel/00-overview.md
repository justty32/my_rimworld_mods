# S0+S1 人事系統實作計畫 — 總覽與任務索引（2026-06-12）

> 調查權威：`../../../2026-06-12-rimwar-empire-investigations.md` **P 段（含修正框）**＋
> N 段（通訊台管線）＋ I 段（關係雙軌）＋ F/G 段。
> 現碼權威：`named-officers/`（P0 全源＋PROJECT.md API 契約）、
> `warband-generals/`（P1）、`settlement-lords/`（P2）。
> 計畫慣例：仿 `plans/p0-named-officers/`（任務拆檔、每檔 <200 行、每任務附驗證）。
> **純計畫文件——不寫實作碼、不改 mod 源碼。**

## 範圍一句話

三國志化「人事系統」三塊：**S0**＝向後相容地擴展 P0（status/居住地/職涯 API/三事件），
**S1**＝新 mod `pas.personnel.community`（在野撒種、人才守恆認領、NPC 招攬、玩家徵辟），
**UI**＝聚落人才一覽視窗（在任＋在野、七維屬性、關係 tooltip，MVP 只讀）。

## ⚠ 核心語意定案（使用者拍板，凌駕原調查）

**「在野」＝屬於派系但不擔任官職**：`record.faction`＝居住城（`homeSettlement`）所屬派系、
`status=Idle(在野)`、`assignedTo=null`、`role=null`。**不是 faction=null**。

- 人才隨城易主（城換主→在野人才改隸新主）。
- 主公敗亡（`Faction.defeated`，vanilla `Faction.cs:32`）→ 改隸居住城現任派系＋廣播
  `OfficerOrphaned`，**不刪 record**（修掉 P0 Healer 分支5「派系亡→武將蒸發」反特性）。
- `homeSettlement` 沒了 → 遷居最近聚落；完全無處可去 → `status=Wandering`（流浪，保留舊
  faction 參照——Faction 物件永駐 FactionManager，Scribe_References 安全）。
- **不需 faction-nullable**：faction 欄位永遠有值。
- 備選（記一筆不展開）：建一個與玩家中立的「在野」容器派系收容無主人才——僅當主模型的
  「隨城改隸」在實機產生怪異外交副作用時才回頭評估。

## 命名定案

| 項目 | 值 |
|---|---|
| S0 | 就地改 `named-officers/`（packageId 不變 `pas.officers.community`，**零 Harmony／零硬相依鐵律不破**） |
| S1 mod 目錄 | `my_rimworld_mods/personnel/` |
| S1 packageId | `pas.personnel.community`（使用者指定） |
| S1 Assembly / RootNamespace | `Personnel` / `pas.personnel` |
| S1 defName/key 前綴 | `pas_personnel_` |
| S1 相依 | hard：`brrainz.harmony`、`pas.officers.community`；**soft**：P1 `pas.officers.warband`、P2 `pas.officers.settlements`（橋接類隔離＋ModLister 守衛，見 s1-01） |
| status enum | `OfficerStatus { Serving(任官，含 assignedTo=null 的待命/朝廷職), Idle(在野), Wandering(流浪) }` |

## 任務索引與依賴圖

| # | 檔案 | 內容 | 估時 | 依賴 |
|---|---|---|---|---|
| A0 | `s0-01-fields-naming.md` | S0 spike：defeated 語意／無 pawn 起名 API／名字回寫驗證 | 0.25d | — |
| A1 | `s0-01-fields-naming.md` | record 新欄位＋status enum＋ServiceEntry＋OfficerNamer 預擲名 | 0.5d | A0 |
| A2 | `s0-02-healer-rehome.md` | Healer 分支5 改造（改隸不刪）＋inhabitants 橋泛化＋流浪自癒 | 0.5d | A1 |
| A3 | `s0-03-api-events-queries.md` | Release/Employ/SetRole/MakeIdle/CreateIdle API＋三事件＋查詢 | 0.5d | A2 |
| A4 | `s0-04-roledef-debug-compat.md` | RoleDef rank/scope/quota＋debug actions | 0.25d | A3 |
| A5 | `s0-04-roledef-debug-compat.md` | 相容性驗證：P1/P2/P3 零改動重編＋PROJECT.md/healthcheck 更新 | 0.25d | A4 |
| B0 | `s1-01-skeleton.md` | S1 spike：FactionDialogFor postfix 落點＋RimWar 共存確認 | 0.25d | A3 |
| B1 | `s1-01-skeleton.md` | personnel mod 骨架（About/csproj/Settings/healthcheck） | 0.5d | B0 |
| B2 | `s1-02-seeding.md` | 在野撒種：開局 seed＋trickle 心跳 | 0.5d | B1 |
| B3 | `s1-03-claim-windows.md` | 人才守恆：P1/P2 `ExitInterceptor`（各 <30 行）＋S1 認領橋 | 0.5d | B1,A3 |
| B4 | `s1-04-recruit-ai.md` | P2 `CandidateProvider` hook（<30 行）＋S1 willing 公式 provider | 0.5d | B3 |
| B5 | `s1-05-comms-recruit.md` | 玩家通訊台徵辟（FactionDialogFor postfix） | 0.75d | B2 |
| C1 | `ui-01-carrier.md` | UI 載體決策（gizmo+Window ★ vs Empire 分頁 vs inspect） | 已決 | — |
| C2 | `ui-02-window.md`+`ui-03-tasks.md` | `Window_SettlementTalents` MVP 只讀清單 | 1d | B1,A1 |
| C3 | `ui-03-tasks.md` | 互動鈕（徵辟/關係面板）後補 | 0.5d | C2,B5 |
| V | `99-verification.md` | 全鏈驗證矩陣＋實機 E2E checklist | 0.5d | 全部 |

```
A0→A1→A2→A3→A4→A5
            └→ B0→B1─┬→B2──→B5──┐
                      ├→B3→B4   ├→C3→V
                      └→C2──────┘
（C2 只讀視窗僅依賴 B1 骨架＋A1 欄位，可與 B2~B5 並行；B3/B4 含 P1/P2 各 <30 行小改）
```

**估計總工程量：約 7 天**（S0 ≈2.25d、S1 ≈3d、UI ≈1.5d、驗證 0.5d）。

## 關鍵設計決策（細節見各任務檔）

1. **不走 Remove+Create**：跳槽/升遷一律 `EmployOfficer`/`SetRole` 就地改——
   `Registry.Remove` 會清全網 opinions 鍵（`WorldComponent_OfficerRegistry.Remove`），
   關係網毀＝恩怨歸零，違反三國志核心體驗。
2. **MakeIdle 是底層積木**：orphan 流程、P1/P2 認領、玩家下野共用同一個
   「轉在野」操作（改隸＋卸職＋落籍），事件由外層語意化（Orphaned/Released）。
3. **S1 招攬 MVP「寄生」P2 既有 Scan**：缺位×在野配對不另開迴圈——P2 加
   `CandidateProvider` static hook（仿 npc-outposts 兩 hook 範式），S1 註冊三段式
   provider（待命職官→在野徵辟→回 null 讓 P2 憑空保底）；willing 公式住 provider。
   跨派系挖角/跳槽心跳留 S2。
4. **名字預擲（UI 硬前置）**：在野人才**不可**為了顯示名字而具現 pawn（world pawn 爆量
   鐵律）→ S0 加 `OfficerNamer` 無 pawn 起名（`PawnKindDef.GetNameMaker`→`NameGenerator`
   或 `PawnNameDatabaseShuffled.BankOf` 後備）；`Materialize` 回寫 nameCached 到 pawn.Name
   保「視窗看到的＝拜訪遇到的同一人」。
5. **玩家殖民者不進 record 系統**（調查 P 定案）：徵辟成功＝Materialize→SetFaction(player)
   →入場→`RemoveOfficer` 單向離開；re-home 掃描永不指派玩家派系。
6. **UI 載體＝聚落 gizmo 開自家 Window**（C1 決策，見 ui-01）：覆蓋所有 Settlement 子類
   （RimWar 城/NpcOutpost/Empire），零新相依，雙重自家先例
   （colony-archival `Settlement_GetGizmos_Patch`＋voe-outpost-enhancement）。
   UI 歸屬 **S1 personnel mod**（已有 Harmony＋P0 ref，不另開薄 mod）。
7. **心跳 offset 配置**：P0=0、P2=600、P1=1200、**S1=1800**（2500-tick 模數錯峰慣例）。

## 貫穿鐵律（沿 P0/P1/P2 計畫）

每檔 <200 行；S0 保持零 Harmony／零硬相依（healthcheck 既有規則把關）；
P1/P2 改動各 <30 行且向後相容（hook=null 行為不變）；勿動 RimWar 派系級係數；
存讀往返不丟、中途裝/移除 mod 不炸；所有寫入走 OfficersApi。
