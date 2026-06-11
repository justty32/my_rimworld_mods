# 04 軟相容橋：Rim War（反射）與 npc-outposts（條件 assembly）

## 橋接總架構：主 DLL 留 hook，bridge 註冊

主 DLL 內設 `PoliticsBridges` 靜態類：

- `Func<Settlement, bool> IsSatellite`（預設恆 false）——倒戈抽選時排除衛星哨站。
- `Action<Settlement, Faction, Faction> SettlementDefected`（聚落、母派系、新派系）——每筆倒戈後觸發。
- `Action<Faction, Faction> FactionSplit`（母、新）——分裂完成後觸發。

主 DLL 不引用任何第三方 assembly，bridge 在各自載入時用 `[StaticConstructorOnStartup]` 註冊進來。

## npc-outposts bridge：loadFolders 條件載入（機制已驗證）

- `IfModActive` 是原版 loadFolders 機制（Verse\ModLoadFolders.cs:53 解析該 XML 屬性）。
- `loadFolders.xml`：

```xml
<loadFolders>
  <v1.6>
    <li>/</li>
    <li>1.6</li>
    <li IfModActive="pas.outposts.community">Compat/NpcOutposts</li>
  </v1.6>
</loadFolders>
```

- `Compat/NpcOutposts/Assemblies/` 放獨立編譯的 bridge DLL，**編譯期引用我們自己的 `NpcOutposts.dll`**（簽名有保證，同 repo 共演進）＋主 DLL `FactionPolitics.dll`（`Private=false`，比照 npc-outposts 引 sims-mode 的做法）。
- Bridge 行為：
  - `IsSatellite(s) => s is NpcOutpost`——衛星哨站不直接被抽為倒戈對象。
  - `SettlementDefected(s, mother, nf)`：遍歷 `Find.WorldObjects.Settlements`，`NpcOutpost o && o.ParentSettlement == s` → `o.SetFaction(nf)`（哨站跟著母聚落倒戈）。

## Rim War bridge：反射呼叫（本機無 DLL，唯一可行路徑）

**環境事實**：`pas/projects` 在本機無 `rimworld_mods`（Rim War 反編譯源在使用者另一台機器）、Steam workshop 目錄不存在 → 無法編譯期引用，loadFolders 條件 assembly 方案對 Rim War 不可用。

**落地方案**：主 DLL 內 `RimWarBridge`（純 `System.Reflection`，零 Harmony）：

1. 啟動時 `GenTypes.GetTypeInAnyAssembly("RimWar.Planet.WorldUtility")`——以型別存在性偵測 Rim War（比 packageId 字串穩，不必猜 id）。
2. 未偵測到 → 全部 no-op，零成本。
3. 偵測到 → 反射快取目標方法，掛進 `PoliticsBridges`：
   - 倒戈落地：`WorldUtility.ConvertSettlement`（public static，rim-war 反編譯 :15289）——觸發 Rim War 認可的易主，RimWarData 同步。
   - 新派系納管：不必主動呼叫——Rim War 的 `CheckForNewFactions`（:17302）週期自動接管新 Faction（extension_points 既有結論）。
4. **簽名防呆**：反射找不到方法、或參數數/型別與預期不符 → `Log.Warning` 一次後永久 no-op（不炸、不刷 log）。原版 `SetFaction` 已先行，Rim War 不同步只是戰力資料滯後，非致命。

**待校準**：`ConvertSettlement` 確切參數表本機無從查證。使用者已承諾提供 Rim War 檔案（DLL 或反編譯 .cs）——屆時校準參數綁定並解除 no-op。P1 交付的是「偵測＋防呆＋綁定點」骨架，校準是一次小改。

## sims-mode：不需要 code 橋

「找得到反叛者」走 vanilla redress（`03`），任何會生聚落地圖的路徑都觸發——sims-mode 真訪問、原版攻打皆可。sims-mode 只影響體驗品質（活聚落作息、和平拜訪入口），About.xml 以建議性 `loadAfter` + 描述推薦安裝，**無 modDependencies 硬相依**（與 npc-outposts 的硬相依不同）。
