# faction-politics P1 實作計畫（索引）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** NPC 派系內具名反叛者 NPC 累積反叛進展，達閾值分裂出新派系（聚落+哨站倒戈、母新敵對），零 Harmony、零硬相依。

**Architecture:** 單一 `WorldComponent_RebellionTracker` 心跳（補發/自癒/推進/觸發）+ `RebelSpawner`（world pawn KeepForever + `previouslyGeneratedInhabitants` 定位橋）+ `FactionSplitter`（hidden 生成→倒戈→揭示→goodwill 敵對七步編排）。軟相容經 `PoliticsBridges` hook：npc-outposts 走 loadFolders 條件 assembly、Rim War 走主 DLL 反射骨架。

**Tech Stack:** C# net48（Krafs.Rimworld.Ref 1.6.*）+ XML Defs；spec `docs/2026-06-11-design.md`；可行性依據 `docs/2026-06-11-feasibility/`。

---

## 已驗證 API 座標（可行性調查階段，反編譯源 `C:\code\mine\pas\projects\rimworld`）

| API | 座標 | 用途 |
|---|---|---|
| `FactionGenerator.NewGeneratedFaction(PlanetLayer, FactionGeneratorParms)` → Faction | FactionGenerator.cs:130；hidden 跳過自動聚落 :175 | 分裂造派系 |
| `FactionManager.Add` | FactionManager.cs:93 | 入列 |
| `Faction.leader`（public 欄位）/ `hidden`（public bool?） | Faction.cs:26 / :54 | 首領替換/揭示 |
| `AllFactionsVisible` 即時 filter | FactionManager.cs:43 | 揭示免 recache |
| `SetRelationDirect` goodwill 防呆 | Faction.cs:643-647 | 敵對走 goodwill 路徑的原因 |
| `HasGoodwill = !Hidden && !temporary` | Faction.cs:202-212 | 揭示後才設敵對 |
| `WorldObject.SetFaction` | FactionGenerator.cs:178 用例 | 倒戈 |
| redress 鏈 | SymbolResolver_Settlement.cs:59 → PawnGroupKindWorker_Normal.cs:67-71 → PawnGenerator.cs:210-220 | 反叛者找得到 |
| redress 過濾＝race+faction | PawnGenerator.cs:371-375 | kind 自由度 |
| 原版從不自動填 `previouslyGeneratedInhabitants`（死碼） | PawnGenerator.cs:236 | 我們是唯一供給者 |
| `Notify_MyMapRemoved` 剪清單 | Settlement.cs:202-209 | 自癒重 Add 的原因 |
| KeepForever→ForceKept 永不 GC | WorldPawns.cs:226-228、WorldPawnGC.cs:212-214 | 反叛者長存 |
| loadFolders `IfModActive` | Verse\ModLoadFolders.cs:53 | 條件 bridge assembly |

## Task 清單（`docs/plan/task-*.md`）

| Task | 檔 | 內容 |
|---|---|---|
| 0 | `task-00-api-verification.md` | 殘餘 API 簽名 grep 驗證（8 項），結果記 session_log |
| 1 | `task-01-scaffold.md` | About.xml + loadFolders.xml + 主 csproj，建置綠 |
| 2 | `task-02-defs-and-resolver.md` | RebellionProfileDef/PoliticsSettingsDef/extensions + resolver + XML defs |
| 3 | `task-03-record-spawner-bridges.md` | RebelRecord + PoliticsBridges + RebelSpawner + Languages keys |
| 4 | `task-04-splitter.md` | FactionSplitter 七步編排 + 分裂 letter |
| 5 | `task-05-tracker.md` | WorldComponent_RebellionTracker（心跳/自癒/觸發/存讀檔） |
| 6 | `task-06-rimwar-bridge.md` | RimWarBridge 反射骨架（偵測+簽名記錄+防呆 no-op） |
| 7 | `task-07-outposts-bridge.md` | 條件 assembly：IsSatellite + 哨站跟隨倒戈（獨立 csproj） |
| 8 | `task-08-healthcheck.md` | tests/healthcheck.py 靜態健檢（8 檢） |
| 9 | `task-09-docs-wrapup.md` | 第三方擴充示範 + session_log 收尾 |
| 10 | `task-10-e2e.md` | 實機 E2E 清單（待 RimWorld 環境） |

## 慣例（沿 sims-mode / npc-outposts）

- 每 task 結束：`dotnet build` 0 警告 0 錯誤（task-8 起加跑 `python tests/healthcheck.py`）→ 單獨 commit（只 add 本 mod 明確路徑，禁 `-A`）。
- commit 訊息結尾：`Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`；直接上 main。
- git 一律 `git -C C:\code\mine\my_rimworld_mods ...`（避免 cwd 漂移）。
- 每 C# 檔 ≤200 行；XML 解析格式：FloatRange/IntRange 用 `~` 分隔。
- 建置產物 DLL 進 git（repo 慣例）。
- 主 `Source/` **禁止** `using pas.outposts` 或引用 RimWar 型別——軟相容不變式（健檢把關）。
