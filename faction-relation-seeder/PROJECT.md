# Faction Relation Seeder（開局派系關係播種器）

> RimWorld 1.6。packageId `pas.relations.community`。**零 Harmony、零硬相依。**
> 緣起：`舞臺三件套`（ariandel＋worldbuilder＋yc-faction-editor）缺「開局派系關係矩陣」——
> worldbuilder 明確不支援關係矩陣，Faction Customizer 能改但純手動無導入導出。
> 本 mod 補上「可發佈、資料驅動、開局自動」的那一塊。分析佐證：
> `analysis/rimworld_mods/faction-customizer/analysis.md`（關係寫法範本）。

## 目標

用 XML 宣告一張「派系對 → 目標善意」表（`RelationSeedDef`），**在新遊戲開局自動套用一次**，
定下開局的政治幾何（誰結盟、誰交戰）。之後不再碰關係，任世界演化層（Rim War 等）自由改動。

**本 mod ＝純引擎（數據源/執行層分離）**：只提供 `RelationSeedDef` 型別 + 播種 component，
**不自帶任何關係資料**。實際的關係表由消費端「內容 mod」提供其 `RelationSeedDef` 實例
（見姊妹 mod `opening-world-demo`：worldbuilder preset 佈景 + 一張配套關係表）。

## 玩家/作者體驗

1. 作者在 XML 寫 `RelationSeedDef`：每條 `<li>` 給 `a`/`b`（派系 defName）+ `goodwill`。
2. 開**新局** → `WorldComponent.FinalizeInit(fromLoad:false)` 觸發一次播種 → 關係即時生效。
3. 缺席的派系（沒裝該 mod / 沒生成）自動略過，永不報錯（soft-optional）。
4. 舊檔中途裝：**不打擾**既有關係（只在全新遊戲播）。
5. dev mode：Debug actions → `pas.relations` → `Re-apply relation seeds` 可手動重播（調參用）。

## 機制（零 Harmony）

- **原生鉤子（零 Harmony）**：`WorldComponent`，引擎自動實例化，無需註冊 Def。
- **觸發時機（option C，延後播種）**：`FinalizeInit` **只標記待播**（新遊戲 `fromLoad=false`），
  實際 `Apply()` 延到 `WorldComponentTick`——因為 FinalizeInit 跑在世界生成期，`Faction.OfPlayer`
  尚未就緒，提早改善意會讓 vanilla 每次撲空記「Could not find player faction.」。tick 守衛：
  `Current.Game != null && Faction.OfPlayer != null`（有 Ideology 再加 `PrimaryIdeo != null`）
  且待播 → 播一次。
- **設關係**：`fa.TryAffectGoodwillWith(fb, target - current, canSendMessage:false, canSendHostilityLetter:false)`
  ——vanilla 對稱套用雙向並重算 kind（比 Faction Customizer 手動雙寫 `baseGoodwill/kind` 更正規）。
  收尾 `Find.GoodwillSituationManager.RecalculateAll(false)` 讓關係即時生效。
- **只播一次 / 冪等**：`seeded` + `pendingSeed` 雙旗標皆 Scribe 持久化；tick 先落旗標再 Apply，
  已播存檔不重播、不重疊。
- **善意閾值**（vanilla）：≤-75 敵對、≥75 結盟、其間中立。

## 檔案結構

| 檔案 | 內容 |
|---|---|
| `About/About.xml` | packageId `pas.relations.community`；零 modDependencies；loadAfter `ferny.Worldbuilder` |
| `loadFolders.xml` | `/` + `1.6` |
| `1.6/Assemblies/FactionRelationSeeder.dll` | 建置產物 |
| `Source/RelationSeedDef.cs` | `RelationSeedDef : Def` + `RelationSeedEntry`（a/b/goodwill）+ ConfigErrors |
| `Source/WorldComponent_RelationSeeder.cs` | 播種器：FinalizeInit 標記待播 → WorldComponentTick 就緒後播一次；逐條例外隔離；ExposeData `seeded`+`pendingSeed` |
| `Source/Dev/RelationSeederDebug.cs` | dev「重新播種」DebugAction |
| `tests/healthcheck.py` | 靜態健檢（XML/交叉引用/零-Harmony 不變式/建置產物） |

## 建置

```
cd Source && dotnet build -c Release      # → ../1.6/Assemblies/FactionRelationSeeder.dll
python3 tests/healthcheck.py              # 靜態健檢
```

## 狀態

- ✅ 編譯（net48 / Krafs.Rimworld.Ref 1.6.*，零警告）、✅ healthcheck OK。
- ✅ **實機 E2E 綠**（2026-07-17，dist 0.1.2）：真實新局零紅字/零 NRE、播種完成印一次、善意正確。
  歷程：0.1.0 世界生成期 NRE → 0.1.1（option A 守衛 RecalculateAll）→ 0.1.2（option C 播種延到
  WorldComponentTick）消除殘留「Could not find player faction.」。

## 已知限制

- **permanentEnemy 覆寫的邊界**：`ApplyEntry` 的 `!HasGoodwill` 守衛讀 `def.permanentEnemy`。
  若某派系原為永久敵、被 worldbuilder 的 `FactionPopulationData.permanentEnemy=false` 改成可交好
  （它 patch 的是 `CanChangeGoodwillFor`，非 `def`），本 seed 仍會略過它。此類派系請改用 yc/遊戲內設，
  或日後把守衛換成呼叫 `CanChangeGoodwillFor`。一般派系不受影響。
