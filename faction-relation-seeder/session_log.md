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

- 2026-07-17 收尾殘留紅字（option C，0.1.2）：0.1.1 的 option A 只擋了收尾 RecalculateAll，但整個
  Apply() 仍掛在 FinalizeInit（世界生成期）跑——此時 Faction.OfPlayer 未就緒，迴圈裡每次
  TryAffectGoodwillWith 都讓 vanilla 撲空記「Could not find player faction.」（6 對×2~3 查≈15 條）。
  改：FinalizeInit 只設 `pendingSeed=true`（新遊戲且未播）、不碰 goodwill；新增 `WorldComponentTick`
  守衛（`Current.Game!=null && Faction.OfPlayer!=null`，有 Ideology 再等 `PrimaryIdeo!=null`）就緒後
  播一次，先落 `pendingSeed=false;seeded=true` 再 Apply（冪等）。`pendingSeed` 也 Scribe 持久化。
  ApplyEntry 邏輯未動，option A 守衛/log 保留。重編 0 警告、healthcheck OK、DLL 確認含
  WorldComponentTick/pendingSeed/IdeologyActive。打包 dist 0.1.2、symlink 重指、dist/README 更新。未 push。

- 2026-07-17 修世界生成期 NRE（實機回報）：`Apply()` 的 `RecalculateAll` 在世界生成階段跑，此時
  玩家派系 ideo 尚未指派 → vanilla `GoodwillSituationWorker_SameIdeo.GetNaturalGoodwillOffset` 對 null
  PrimaryIdeo 解參考 NRE（並伴「Could not find player faction.」×16）。關係本身已由 `TryAffectGoodwillWith`
  套好，RecalculateAll 只是便利收尾。採 A 案：加守衛 `applied>0 && Current.Game!=null &&
  Faction.OfPlayer?.ideos?.PrimaryIdeo!=null` + try/catch，且「播種完成」log 移到守衛外保證必印；失敗
  交遊戲自然重算。改 WorldComponent_RelationSeeder.cs 約 6→18 行，未動 ApplyEntry。重編 0 警告、
  healthcheck OK、DLL 內確認含 PrimaryIdeo/OfPlayer/get_Game 新參照。commit a889722；打包 dist
  FactionRelationSeeder-0.1.1 並把佈署 symlink（~/rimworld_mods/FactionRelationSeeder）指向 0.1.1；
  dist/README 產物表更新。未 push。

- 2026-07-17 重構為純引擎：新建消費端 `opening-world-demo`（worldbuilder preset + RelationSeedDef）
  時，把本 mod 的 demo Seeds.xml 移除（數據源/執行層分離、避免兩份 RelationSeedDef 打架）、
  放寬 healthcheck（不再要求自帶 RelationSeedDef）、PROJECT.md 補「純引擎」+ permanentEnemy 守衛
  的已知限制（worldbuilder 用 CanChangeGoodwillFor patch 改 permanentEnemy，本 seed 的 HasGoodwill
  守衛讀不到→該類派系會略過）。C# 未動、DLL 仍有效。實機驗證改由 opening-world-demo 承載。
