# C2 `Window_SettlementTalents` 視窗設計（線框級）

## 線框

```
┌─────────────────────────────────────────────────────────────┐ 720×560
│ 西涼城 — 人才一覽                    派系：馬騰軍（中立）   │ ← Text.Font=Medium 標題行
│─────────────────────────────────────────────────────────────│
│ ▌在任職官（2）                                              │ ← section 頭（灰底）
│  名字        官職    武  統  政  魅  忠   狀態   任期       │ ← 表頭（Tiny font）
│  馬騰        太守    71  80  55  62  88   任官   2.1 年     │ ← 行高 26f，hover 高亮
│  龐德        將軍    93  77  30  41  85   任官   0.4 年     │
│ ▌在野人才（3）                                              │
│  賈詡        —      35  48  95  70  22   在野    —         │
│  …                                                          │ ← BeginScrollView 區
│─────────────────────────────────────────────────────────────│
│ [重新整理]                          [徵辟…]C3   [關閉]      │ ← 底欄 36f
└─────────────────────────────────────────────────────────────┘
```

慣例骨架照 `Dialog_ArchivalConfirm`：`InitialSize=(720,560)`、`doCloseX=true`、
`forcePause=false`（看 NPC 城不必停錶）、`closeOnClickedOutside=true`、
手動 y 疊版＋`Widgets.BeginScrollView(outer, ref scroll, view)`。

## 資料來源與快取

- 開窗（ctor/PreOpen）一次拉取：
  `serving = OfficersApi.GetOfficers(settlement)`（在任，含 P2 太守/未來駐城官）＋
  `idle = OfficersApi.GetIdleAt(settlement)`（A3 查詢，homeSettlement==此城）。
- 逐筆 `OfficerNamer.EnsureNameCached`（**不具現 pawn**——名字預擲，A1 前置）。
- 快取成 `List<Row>`（record ref＋格式化字串），每幀只畫不查；
  `[重新整理]` 鈕＋開窗時重拉（registry 心跳變動不即時反映，可接受）。

## 列規格

| 欄 | 寬 | 內容 |
|---|---|---|
| 名字 | 140f | `DisplayName`；已具現且 pawn 在世 → 名字後綴小圖示（可拜訪暗示） |
| 官職 | 90f | `role?.label ?? "—"`（在野）；scope=Faction 加「朝」字綴 |
| 武/統/政/魅/忠 | 34f×5 | 五個啟用維；數值著色（≥80 綠、≤30 紅，仿技能面板） |
| 狀態 | 60f | 任官／待命（Serving+assignedTo null）／在野／流浪（理論不出現在城清單） |
| 任期 | 70f | `(now-appointedTick)` 換算年；在野 → "—" |

- 排序（MVP 固定）：在任區 `role.rank` 降序→`displayPriority`；在野區屬性和降序。
  可點表頭排序＝backlog（C3 之後）。
- 智力/士氣（預留維）**不佔欄**，進 tooltip——七維全示但版面只擺啟用五維。

## 行 tooltip（`TooltipHandler.TipRegion`，整行熱區）

```
賈詡（在野）
武 35  統 48  政 95  魅 70  忠 22  智 —  士 —
履歷：195年 任 太守＠長安（董卓軍）／196年 下野
關係：對 馬騰 -40（世仇⚔）、對 龐德 +12
居住：西涼城（隨城易主而仕）
```

- 履歷：`serviceHistory` 倒序至多 3 筆（roleLabel/factionName/起迄 tick→年）。
- 關係：對**本清單其他 record** 的 `GetOpinion`，|值|≥20 才列、至多 3 筆；
  A 軌標記：`HasPersistent(SwornBrother/BloodFeud)`（僅雙方已具現時可查——
  未具現顯示 B 軌數值即可，**不為 tooltip 觸發具現**，爆量鐵律）。
- tooltip 字串開窗時預組（非每幀組串）。

## 互動入口（C3 範圍，本檔先定位置）

- 底欄 `[徵辟…]`：在野區有人且玩家有通訊台 → 開 B5 同款流程（直呼
  `RecruitService.TryRecruit`，免走通訊台對話）；無通訊台/敵對 → 灰＋原因 tooltip。
  MVP（C2）此鈕不出現。
- 在野行尾 per-row 小鈕 `[辟]`（C3）：單人徵辟捷徑，同上守衛。
- `[關係]`／點名字 → backlog：跳 Dialog_InfoCard（需 pawn，會觸發具現）——
  C3 評估後再定，MVP 不做。

## gizmo（`Patch_SettlementGizmo.cs`）

```
postfix Settlement.GetGizmos：yield 原 gizmos；
if (serving.Count + idle.Count > 0)            // 無料不顯示（C1 擁擠緩解）
    yield Command_Action { label="人才", icon=自繪/借 vanilla, 
                           action=() => Find.WindowStack.Add(new Window_SettlementTalents(s)) }
```

玩家自家聚落（faction.IsPlayer）不顯示（殖民者不進 record 系統，必空）。

## Keyed 字串

`pas_personnel_TalentsGizmo/WindowTitle/SectionServing/SectionIdle/ColName...`
三語（En/ChT/ChS）齊備（家規）。

## 驗證
（任務切分與驗收步驟見 ui-03）
