# 動態派系政治（Faction Politics Community）

## 衍生目標
NPC 派系內的具名反叛者 NPC 隨時間累積反叛進展，達閾值分裂出新派系——聚落（含衛星哨站）倒戈易主、母新敵對。世界地圖長出騎砍/CK 風的動態政治事件。路線圖 P1（反叛→分裂一條龍）→ P2 玩家互動（安撫/煽動）→ P3 同盟/合併/通用生命週期。

## 範圍（P1）
- 反叛者：world pawn KeepForever + `previouslyGeneratedInhabitants` redress 橋（拜訪駐地找得到、殺得掉=鎮壓）。
- 分裂編排：hidden 生成同 def 新派系 → 倒戈 → 揭示 → goodwill 敵對 → letter；動態派系上限防膨脹。
- Def 體系：`RebellionProfileDef` + `PoliticsSettingsDef`（resolver 鏈 Extension > FactionDef > TechLevel > Default），零寫死。
- 軟橋：Rim War（主 DLL 反射，簽名待使用者供檔校準）、npc-outposts（loadFolders 條件 assembly，哨站跟隨倒戈）。

## 技術棧
C#（net48）+ XML Defs；**零 Harmony、零硬相依**（sims-mode 僅推薦安裝）。defName 前綴 `pas_politics_*`；namespace `pas.politics`。

## 對應 RimWorld 版本
1.6（反編譯權威源 `C:\code\mine\pas\projects\rimworld`）。

## 完成定義（P1）
見 `docs/2026-06-11-design.md` §9。

## 關鍵文件
- `docs/2026-06-11-feasibility/`：可行性調查拆檔（含對來源報告的 5 處修正、redress 鏈全驗證、PawnGenerator.cs:236 死碼發現）。
- `docs/2026-06-11-design.md`：P1 spec（權威；§10 設計期修訂）。
- `docs/2026-06-11-implementation-plan.md`：實作計畫索引（各 task 在 `docs/plan/task-*.md`）。

## 來源報告
- `pas/analysis/rimworld_mods/_mod_ideas/world_map_grand_strategy/01_faction_scale_and_lifecycle.md`（idea 7+8；README 定位本案為大戰略叢集「中樞」）。
- 姊妹案：`sims-mode-community`（活聚落＝拜訪體驗）、`npc-outposts`（衛星哨站＝倒戈跟隨對象）。
