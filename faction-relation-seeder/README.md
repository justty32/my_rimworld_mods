# Faction Relation Seeder — 使用說明

**這是「引擎」mod，本身不做任何事**——它只提供 `RelationSeedDef` 這個 XML 型別，
和一個在開局套用它的 component。實際的「開局關係表」由**內容 mod** 提供
（例如 `Opening World: Two Blocs (Demo)` / `pas.openingworld.demo`）。

## 它做什麼

當一個內容 mod 宣告了 `RelationSeedDef`（一張「派系對 → 目標善意」表），本引擎會在
**新遊戲開局時自動套用一次**，設定派系間的初始敵友，然後就不再干涉——之後任世界演化
（Rim War 等）自由改動關係。用的是原版 `TryAffectGoodwillWith`，**零 Harmony**。

- 只在**新遊戲**播種（`fromLoad=false`）；載入舊存檔不打擾既有關係。
- 播種只發生一次（存檔記 `seeded` 旗標）。
- 缺席的派系（沒裝對應 mod / 沒生成）自動略過，不報錯。

## 怎麼用

1. 裝這個引擎 mod。
2. 裝（或自己寫）一個提供 `RelationSeedDef` 的內容 mod，載入順序排在本引擎**之後**。
3. 開新遊戲即生效。

自己寫一張關係表（放在你內容 mod 的 `Defs/` 下任意 XML）：

```xml
<Defs>
  <pas.relations.RelationSeedDef>
    <defName>MyOpeningRelations</defName>
    <relations>
      <li><a>FactionDefA</a><b>FactionDefB</b><goodwill>-100</goodwill></li>  <!-- 敵對 -->
      <li><a>FactionDefA</a><b>FactionDefC</b><goodwill>90</goodwill></li>    <!-- 結盟 -->
    </relations>
  </pas.relations.RelationSeedDef>
</Defs>
```

- `a` / `b`：派系 FactionDef 的 defName。
- `goodwill`：目標善意，`[-100,100]`。**≤-75 敵對、≥75 結盟、其間中立**（原版閾值）。

## 相依

- **硬相依**：無（純引擎）。
- 內容 mod 若要 XML 引用 `pas.relations.RelationSeedDef`，需硬相依本引擎（`pas.relations.community`）
  且載入在後，否則型別無法解析。

## Dev 工具

Debug actions → `pas.relations` → **Re-apply relation seeds**：手動重新套用一次（調參用）。

## 已知限制

若某派系原為永久敵（`permanentEnemy`）、被 Worldbuilder 的 `permanentEnemy=false` 覆寫成可交好，
本引擎的守衛仍會略過它（守衛讀 `def.permanentEnemy`，讀不到 Worldbuilder 的執行期覆寫）。
這類派系請改用遊戲內工具設關係。一般派系不受影響。
