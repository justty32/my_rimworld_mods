# 模擬市民社區（Sims Mode Community）

## 衍生目標
落地 `analysis/rimworld/others/life_politics/sims_mode_community.md` 的願景：把 RimWorld 從「上帝視角管理」轉向「世界中的參與者」。整體拆成 P1–P6 多階段路線圖（見 `docs/2026-06-11-design.md` §1），本資料夾承載整個大計畫。

**P1（v1）＝活的聚落**：玩家商隊訪問非敵對派系聚落（1.6 本體 visit settlement）生成地圖時，聚落 pawn 不再傻站（原版全員只掛 `LordJob_DefendBase`），而是按「派系 → 角色 → 作息表」過生活：白天各司其職、傍晚聚會社交、夜間回床睡覺、被襲擊切防禦。

## 範圍（v1）
- 注入＝**方案 C**：PatchOperation 往聚落 MapGeneratorDef 附加 `GenStep_SettlementLife`（零 Harmony 目標）。
- 行為引擎＝RimCities 範式（`LordJob` → 單 `LordToil` → `UpdateAllDuties` 查表發 duty）＋ 原版 Trigger 切防禦。
- **四層 Def 化、Def 優先但不教條**（核心設計約束）：`LifeProfileDef`（派系維度）→ `LifeRoleDef`（角色＋作息表）→ `FacilityTagDef`（設施標記，matcher 可插拔＋DefModExtension 明示）→ 原版 `DutyDef`（行為，純 XML think node）。內容知識供其他 mod patch；複雜邏輯可留 C# 但須好 patch（worker 化、public/virtual、單一資料源）。
- 「工作」＝假動作（走到設施前播動畫，不真產出）——報告 05 §3.5 定論。

### v1 砍掉（YAGNI）
玩家側互動（Talk/租房/工作板，P2）、自訂聚落布局生成（P3）、公會任務（P4）、玩家殖民地引入他派系自主 pawn（P5）、權威/徵調系統（P6）、真實工作產出、敵對（攻打）行為改動。

## 技術棧
- C#（net48）＋ XML Defs/PatchOperation；零 Harmony（v1 目標）。
- 無 mod 相依。defName 前綴 `pas_sims_*`；namespace `pas.sims`。

## 對應 RimWorld 版本
1.6（反編譯權威源 `C:\code\mine\pas\projects\rimworld`）。

## 完成定義（v1）
見 `docs/2026-06-11-design.md` §7（角色作息就位、進場時辰正確、翻臉切防禦、攻打不受影響、可 patch 性驗證、雙 profile、存讀檔、實機端到端）。

## 關鍵文件
- `docs/2026-06-11-design.md`：v1 設計 spec（權威）。
- `docs/2026-06-11-implementation-plan.md`：實作計畫索引（各 task 細節在 `docs/plan/task-*.md`）。
- `docs/examples/extension-sample.xml`：第三方擴充示範（新增角色/改作息/標記建築/綁派系 profile，不被遊戲載入）。
- `tests/healthcheck.py`：靜態健檢（XML well-formed、交叉引用、patch 鏈、DefOf 防呆）。
- `session_log.md`：執行記錄（API 驗證結果、E2E 結果）。

## 來源報告
- 願景：`analysis/rimworld/others/life_politics/`（`sims_mode_community.md` ＋ `rpg_quest_system.md`、`authority_leadership_system.md`）
- 可行性坐實：`analysis/rimworld_mods/_mod_ideas/world_map_grand_strategy/05_settlement_npc_life_and_interaction.md`（Lord/Duty 範式、RimCities 借鑑）、`02_outposts_and_world_objects.md` §6（visit settlement）
