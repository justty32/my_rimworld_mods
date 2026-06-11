# NPC 派系哨站（NPC Outposts Community）

## 衍生目標
NPC 派系聚落周圍長出衛星哨站，讓世界有「派系在經營領地」的血肉感。路線圖 O1（本版）→ O2 功能性節點 → O3 戰略目標 → 延後：哨站增益母聚落數值（待 Rim War / Empire Refactor 參考）。

## 範圍（O1）
- `NpcOutpost : Settlement`：可拜訪（150x150 小圖＋sims-mode 活聚落作息）、可交易（原版繼承）、可攻打（小圖＋原版關係懲罰＋中立確認框）、擊敗即移除。
- 鋪設：單一 `WorldComponent_OutpostSpawner`——`FinalizeInit` 開局/舊檔鋪基底、tick MTB 增生至每聚落上限。
- Def 體系：`OutpostTypeDef` + `OutpostProfileDef`（resolver 鏈 Extension > FactionDef > TechLevel > Default），零寫死。
- 守軍壓低：`ExtraGenStepDefs` 注入 `GenStep_TrimDefenders`（order 9990）。

## 技術棧
C#（net48）＋ XML Defs/PatchOperation；零 Harmony。**硬相依 `pas.sims.community`**（assembly 引用；共用「真訪問」ArrivalAction 與 Base_Faction 地圖生成線）。defName 前綴 `pas_outposts_*`；namespace `pas.outposts`。

## 對應 RimWorld 版本
1.6（反編譯權威源 `C:\code\mine\pas\projects\rimworld`）。

## 完成定義（O1）
見 `docs/2026-06-11-design.md` §9（分布/舊檔增生/拜訪小圖作息/交易/攻打/海盜站/真訪問/存讀檔/缺相依警告/健檢）。

## 關鍵文件
- `docs/2026-06-11-design.md`：O1 設計 spec（權威；§10 計畫期修訂）。
- `docs/2026-06-11-implementation-plan.md`：實作計畫索引（各 task 在 `docs/plan/task-*.md`）。
- `docs/examples/extension-sample.xml`：第三方擴充示範。
- `tests/healthcheck.py`：靜態健檢。
- `session_log.md`：執行記錄。

## 來源報告
- 可行性：`pas/analysis/rimworld_mods/_mod_ideas/world_map_grand_strategy/02_outposts_and_world_objects.md`（VOE 解剖、輕量 WorldObject、lazy 生圖、Settlement 繼承 CP 值）。
- 姊妹案：`sims-mode-community`（活聚落＝哨站地圖的守軍行為引擎；「真訪問」入口由本案 Task 3 交付至該 mod）。
