# T2 — SettlementWealthComp＋SettlementAttributeDef＋成長＋LordGovernanceBridge

## 產出檔

```
Source/World/SettlementAttributeDef.cs   # Def 子類（旁路 key；本期不出貨實例）
Source/World/SettlementWealthComp.cs     # comp + props + scribe + CompTick 成長
Source/Bridge/LordGovernanceBridge.cs    # P2 反射橋（soft-optional 唯一接點）
Source/EconomyUtility.cs                 # 補公式/常數（T1 已建檔）
```

## SettlementWealthComp（typed 主幹＋Def-dict 旁路）

- `WorldObjectComp` 子類；欄位（全 `Scribe_Values`，key 加 `pas_cityecon_` 前綴防撞）：
  `int silver/food/goods/defenseLevel/defensePoints`、`bool initialized`、`int nextTick`。
- `Dictionary<SettlementAttributeDef,float> extraAttributes`：可空、懶配置；
  `Scribe_Collections(LookMode.Def, LookMode.Value)`；公開 `GetExtra(def,fallback)/SetExtra`。
- `WorldObjectCompProperties_SettlementWealth : WorldObjectCompProperties`
  （ctor 設 `compClass`），同檔出貨。
- 公開唯讀 `DefenseBonus`：`RoundToInt(Min(defensePoints, defenseLevel×1000) × defenseAmplitude)`
  （settings 0 → 0）——T3 守城與 inspect 共用。

## CompTick 節流（仿 RW:9585 絕對時刻法）

```
nextTick==0 → nextTick = TicksGame + 1800 + parent.ID % 700（錯開 P0:0/P2:600/P1:1200）
TicksGame < nextTick → return；否則 nextTick = TicksGame + 2500，try{ TickEconomy() }catch WarnOnce
```

## TickEconomy（每輪）

1. `rwsc = parent.GetComponent<RimWarSettlementComp>()`；null → return（非 RimWar 城）。
2. `rwd = WorldUtility.GetRimWarDataForFaction(parent.Faction)`；null/Player/Excluded → return。
3. 未播種 → seed（P=RimWarPoints）：silver=P、food=P/2、goods=P/2、defenseLevel=1、
   defensePoints=500；`initialized=true`；本輪結束（首輪不成長）。
4. `rwsc.PointDamage > 0` → return（鏡像 RimWar 療傷分支：圍城/受創不長財富）。
5. `rate = growthRate`；≤0 → return。`gov = LordGovernanceBridge.GovernanceFactorFor(parent as Settlement)`。
6. `unit = Max(1, P/100)`；
   `silver = Min(silver + RoundToInt(unit×gov×rate), P×10)`；
   `food/goods = Min(× + RoundToInt(0.6×unit×gov×rate), P×5)`。
7. 防禦（**不吃 gov**——治理只管經濟）：`defCap = defenseLevel×1000`；
   `defensePoints < defCap` → `+= Max(1, RoundToInt(unit/3×rate))`（clamp defCap）；
   否則 `defenseLevel<5` 且 `silver ≥ 2×cost`（`cost=(defenseLevel+1)×2000`）→
   `silver −= cost; defenseLevel++`（城池自治簡化版；P5 改領主決策驅動）。

主執行緒（WorldObject tick）→ 可用 Rand，但本期公式全確定性、不用。

## LordGovernanceBridge（P2 soft-optional 接法）

- 靜態類；首次使用時解析一次並快取（`resolved` 三態：未試/可用/不可用）：
  `Type wc = AccessTools.TypeByName("pas.officers.settlements.WorldComponent_SettlementLords")`、
  `Type util = AccessTools.TypeByName("pas.officers.settlements.LordsUtility")`、
  `MethodInfo get = wc.GetMethod("Get", static)`、`lordOf = wc.GetMethod("LordOf")`、
  `gov = util.GetMethod("GovernanceFactor", static)`——任一 null → 永久降級。
- `float GovernanceFactorFor(Settlement s)`：不可用/s==null → 1f；
  `comp = get.Invoke(null,null)`；null → 1f；`record = lordOf.Invoke(comp, {s})`；
  `result = (float)gov.Invoke(null, {record})`（P2 對 null record 已回 1f）；
  `Clamp(result, 0.25, 2)`；任何例外 → WarnOnce＋永久降級 1f。
- **不 ref 任何 P0/P2 DLL**；OfficerRecord 全程以 object 傳遞。
- 頻率＝每城每 2500 tick 一次 `MethodInfo.Invoke` → 開銷可忽略，不做 delegate 編譯。

## EconomyUtility 補齊

`WarnOnce`（前綴 `[CityEconomy]`）；常數 `CycleTicks=2500`、`MaxDefenseLevel=5`、
`DefensePointsPerLevel=1000`、`DefenseUpgradeCostPerLevel=2000`、cap 倍數；
貨架因子函式（T4 用）：`StockFactor(int wealth, float baseline)` → `Clamp(w/baseline, 0.25, 2)`。

## 驗收

build 0/0；healthcheck OK；（實機留 T5）：新檔城市 inspect 出現財富行、
存讀往返五欄位＋extraAttributes 不丟、無 P2 時 log 零紅字。
