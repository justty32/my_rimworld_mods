# T4 — 貨架介面＋inspect 顯示

## 產出檔

```
Source/Patches/Patch_TraderStock.cs            # RegenerateStock postfix
Source/Patches/Patch_TraderGiveSold.cs         # GiveSoldThingToTrader/Player 兩 postfix
Source/Patches/Patch_SettlementInspectString.cs
Source/HarmonyInit.cs                          # 補四條 TryPatch
```

## 貨架縮放：Patch_TraderStock（vanilla Settlement_TraderTracker.RegenerateStock :265）

M 段定案：postfix 在 ThingSetMaker 之外層 → 避開 RimWar 唯一相關 patch（RW:6089）。

- `stock` 是 private → 啟動時 `AccessTools.FieldRefAccess<Settlement_TraderTracker,
  ThingOwner<Thing>>("stock")` try/catch 快取；失敗 → 貨架縮放整組降級（WarnOnce）。
- `Postfix(Settlement_TraderTracker __instance)`：
  1. settings `traderEconomyEnabled` false → return；
  2. `comp = __instance.settlement?.GetComponent<SettlementWealthComp>()`；
     缺/未播種 → return（玩家城/非 RimWar 城自然跳過）；
  3. 因子（P=RimWarPoints，EconomyUtility.StockFactor，clamp 0.25~2）：
     `silverF = silver/P`、`foodF = food/(P×0.5)`、`goodsF = goods/(P×0.5)`；
  4. 逐 stock item：Pawn 跳過（奴隸/動物不縮放）；`def==ThingDefOf.Silver` → silverF；
     `def.IsNutritionGivingIngestible` → foodF；其餘 → goodsF；
     `stackCount = Max(1, RoundToInt(count×factor))`（不刪 thing、不生 thing——
     一期只調數量，min 1 避免空 stack 邊角）。
- 不重呼 `StockListForReading`（防 stock 空時遞迴 RegenerateStock）。

## 交易回寫：Patch_TraderGiveSold（:164/:187，皆 public virtual）

兩 postfix 都：settings 關 → return；comp 缺/未播種 → return；
`value = (def==Silver) ? count : RoundToInt(def.BaseMarketValue × count)`
（用 `toGive.def`＋`countToGive`——原方法已 SplitOff，不碰 thing 實體）。

- `GiveSoldThingToTrader`（玩家賣給城；含買貨付的銀）：
  Silver → `silver += count`；食 → `food += value`；其餘 → `goods += value`；
  clamp 各自 cap（P×10／P×5）。
- `GiveSoldThingToPlayer`（玩家從城買走；含城找零）：
  Silver → `silver = Max(0, silver−count)`；食 → `food = Max(0, food−value)`；
  其餘 → `goods = Max(0, goods−value)`。

買賣對偶自洽：玩家買貨＝goods↓＋（付款經 GiveSoldThingToTrader）silver↑；
玩家賣貨＝goods/food↑＋（收款經 GiveSoldThingToPlayer）silver↓。

## 顯示：Patch_SettlementInspectString（獨立 postfix，仿 RW:6570／P2）

- comp 缺/未播種 → return（玩家城無行）；append 一行：
  `pas_cityecon_InspectLine`.Translate(silver, food, goods, defenseLevel, DefenseBonus)。
- RimWar 自身＋P2 都 postfix 同方法 → 多 postfix 疊加安全，各 append 自己段。
- try/catch＋WarnOnce。

## HarmonyInit 補丁清單（TryPatch fail-soft）

```
Settlement_TraderTracker.RegenerateStock      → postfix（protected：AccessTools.Method 字串找）
Settlement_TraderTracker.GiveSoldThingToTrader → postfix
Settlement_TraderTracker.GiveSoldThingToPlayer → postfix
Settlement.GetInspectString                    → postfix
```

## 驗收

build 0/0；healthcheck OK；實機（T5）：富城 stock 銀/貨多於窮城；和 NPC 城交易後
inspect 財富變化方向正確；關 `traderEconomyEnabled` 後 stock 回原版行為。
