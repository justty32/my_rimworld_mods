# session_log — opening-world-demo

- 2026-07-17 新建：「開局世界工作流」第一步落地——worldbuilder preset + faction-relation-seeder 串成管線。
  - 範圍裁示：使用者要求先只做 worldbuilder + 自制 seed 兩件；yc 下一步、ariandel 等之後。
  - 架構＝數據源/執行層分離：引擎（pas.relations.community）只放型別+component、不帶資料；
    本內容 mod 帶 preset（佈景）+ RelationSeedDef（開局關係）。硬相依引擎、loadAfter worldbuilder。
  - preset「OpeningWorldTwoBlocs」照 ratkin-faction-preset/AcornWorld 形狀，純原版派系、
    不開 saveTerrain/saveBases/saveIdeologies。查證 worldbuilder World_FinalizeInit_Patch.cs:67：
    name/color override 由 saveFactionCustomizations gate（非 saveFactions）→ 已開對。
  - 內容＝藍盟(OutlanderCivil+Rough)/赤盟(TribeCivil+Rough)兩陣營，盟內結盟、兩盟四對交叉敵對；
    Pirate 永久敵不列。顏色藍/紅 + 改名，實機一眼可辨。
  - 順帶重構引擎：移除其 demo Seeds.xml（避免兩份 RelationSeedDef 打架）、放寬 healthcheck、
    PROJECT.md 補「純引擎」說明 + permanentEnemy 守衛的已知限制。
  - ✅ 兩 mod healthcheck 皆 PASS（引擎編譯仍有效，未改 C# 故未重編）。
  - ⏳ 未實機：待啟用三 mod 開新局選 preset，核對 log 播種 6 對 + 外交面板兩盟對峙 + 顏色/改名；
    再驗存讀不重刷、無 worldbuilder 時 preset 消失但關係仍套用。
- 2026-07-17 ✅ **實機 E2E 綠**（使用者回報，搭 seeder 0.1.2）：真實新局 preset+seed 管線跑通，
  零紅字/零 NRE、播種完成、兩盟對峙關係正確。「開局世界工作流」第一步（worldbuilder + seed）達標。
  下一步：接 yc faction editor。
