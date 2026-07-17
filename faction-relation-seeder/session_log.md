# session_log — faction-relation-seeder

- 2026-07-17 新建：補「舞臺三件套」缺的開局派系關係矩陣（緣起見 PROJECT.md／
  analysis/rimworld_mods/faction-customizer/analysis.md）。
  - 設計：`RelationSeedDef : Def`（List<RelationSeedEntry a/b/goodwill>，defName 軟參照）
    + `WorldComponent_RelationSeeder`（FinalizeInit 原生鉤子、只新局播一次、`seeded` Scribe）。
  - 設關係走 vanilla `TryAffectGoodwillWith(delta, canSendMessage/HostilityLetter:false)`
    ＋`GoodwillSituationManager.RecalculateAll(false)`；比 Faction Customizer 手動雙寫更正規。
  - 零 Harmony、零硬相依；缺席派系軟略過；舊檔中途裝不打擾。dev 有「重新播種」DebugAction。
  - ✅ dotnet build（net48/Krafs 1.6.*，零警告）→ 1.6/Assemblies/FactionRelationSeeder.dll；
    ✅ tests/healthcheck.py OK。範例 Seeds.xml 用 vanilla defName（OutlanderCivil↔TribeCivil 敵對、
    OutlanderRough↔OutlanderCivil 結盟），開新局可實機驗。
  - ⏳ 未部署未實機——待開新局於 Factions 面板/世界視圖/dev dump 核對；再驗舊檔載入不被打擾。

- 2026-07-17 重構為純引擎：新建消費端 `opening-world-demo`（worldbuilder preset + RelationSeedDef）
  時，把本 mod 的 demo Seeds.xml 移除（數據源/執行層分離、避免兩份 RelationSeedDef 打架）、
  放寬 healthcheck（不再要求自帶 RelationSeedDef）、PROJECT.md 補「純引擎」+ permanentEnemy 守衛
  的已知限制（worldbuilder 用 CanChangeGoodwillFor patch 改 permanentEnemy，本 seed 的 HasGoodwill
  守衛讀不到→該類派系會略過）。C# 未動、DLL 仍有效。實機驗證改由 opening-world-demo 承載。
