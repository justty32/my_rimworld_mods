# P3 city-economy 實作計畫總覽

> 設計權威：`05-mod-city-economy.md`（H+K）＋`01-architecture.md`（鐵律：勿動派系級
> `combatAttribute`/`growthAttribute`；防禦勿疊進 RimWarPoints 存量）＋`09-roadmap.md`
> Phase 3 驗證標準。調查依據：investigations H/K/M 段。
> 範本：P1 `warband-generals`＋P2 `settlement-lords`（csproj／HarmonyInit TryPatch
> fail-soft／SignatureSpike／healthcheck 鐵律 guard／三語 keyed）。
> **P2 為 soft-optional**：領主在 → 財富成長吃 `GovernanceFactor`；不在 → 中性 1.0。

## 目標與驗證標準（09 Phase 3）

給 RimWar 追蹤的 NPC 聚落補真實經濟維度（silver/food/goods）與防禦維度
（defenseLevel/defensePoints），單一 `SettlementWealthComp` 承載。

1. 城有真資源存量（inspect 可見、隨點數規模成長、存讀往返不丟）。
2. 戰敗被劫真資源（sack 分支同步搬走財富；RimWar 原點數搬移保留不雙算）。
3. 守城防禦影響 EffectivePoints（降 PointDamage 臨時抬高，戰後還原，tier clamp 封頂）。
4. 貨架介面：聚落交易 stock 數量隨財富縮放；玩家買賣回寫財富。
5. 領主治理（P2 在場時）影響財富成長；P2 不在 → 一切照常、係數 1.0。
6. 鐵則：獨立編譯、不破壞前期、實機可觀察、存讀往返不丟資料。

> 偏離備忘：設計檔 H 提及 Empire `ITaxTickParticipant` 對接——本期任務範圍
> （劫掠/守城/貨架/治理）未含，**延後**（出界清單）。

## T0 spike 已定案的關鍵事實（依據反編譯，見 01-t0）

- comp 注入：RimWar 官方走 XML `PatchOperationAdd`（`v1.6/Patches/RimWarCompsx.xml`）
  xpath `*/WorldObjectDef[worldObjectClass = "Settlement"]/comps`（另對 `Cities.City`／
  `FactionColonies.WorldSettlementFC` 走 PatchOperationSequence+Test 防缺）。
  **鏡像同一組 xpath** ＝「只掛會被 RimWar 追蹤的聚落 def」。
- `RimWarSettlementComp.CompTick`（RW:9585）：`nextCombatTick >= TicksGame` 早退、
  過了就 `nextTick = TicksGame + 2500` ——本 comp 仿同款絕對時刻節流，
  初始 offset `1800 + parent.ID % 700`（錯開 P0 的 0／P2 的 600／P1 的 1200）。
- `EffectivePoints = RimWarPoints - PointDamage`（RW:9277）；`PointDamage` set 無 clamp
  （可暫時為負 → EffectivePoints 高於存量）；`RimWarPoints` getter clamp 100..100000。
- `IncidentUtility.ResolveCombat_Settlement(RimWarSettlementComp defender, WarObject attacker)`
  （RW:11018，public static）：tier clamp `num`=200/500/2000/4000 按攻方點數分檔，
  `num2 = Clamp(defender.EffectivePoints, 0, num)` → **防禦加成自動受 clamp 封頂**。
  雙方 `PointDamage += …`；任一 `EffectivePoints<=0` → 內呼 `ResolveBattle_Settlement`。
  呼叫源＝`RimWarSettlementComp.CompTick`（主執行緒，且 CompTick 守
  `EffectivePoints > 0` 才開打 → 還原時 clamp 至 `RimWarPoints-1` 防戰鬥停擺）。
- `IncidentUtility.ResolveBattle_Settlement(defender, attacker, float pointClamp)`（RW:11108）
  四結局：①佔領（ConvertSettlement，城換主）②sack（**唯一倖存分支**：
  `PointDamage = RimWarPoints - 1`＋`AttackingUnits.Clear()`，parent 不毀、不換派系）
  ③焚毀（同 sack 指紋但 `parent.Destroy()`）④守軍勝（只 `AttackingUnits.Remove`）。
  → **sack 偵測指紋**：parent 活著＋派系未變＋`PointDamage == RimWarPoints-1`
  ＋`AttackingUnits.Count == 0`＋攻方 `EffectivePoints > 0`。
- vanilla `Settlement_TraderTracker`：`RegenerateStock()` protected virtual（:265，
  Harmony 可 patch；private 欄位 `stock` 走 `AccessTools.FieldRefAccess`）；
  `GiveSoldThingToTrader/GiveSoldThingToPlayer` 皆 public virtual、`__instance.settlement`
  直達。RimWar 對此類零接觸（唯一相關是 ThingSetMaker.Generate postfix RW:6089
  → **勿選 ThingSetMaker 當注入點**，healthcheck guard 禁字串）。
- P2 介面（reflection 目標）：`pas.officers.settlements.WorldComponent_SettlementLords`
  `static Get()`／實例 `LordOf(Settlement)→OfficerRecord`；
  `pas.officers.settlements.LordsUtility` `static GovernanceFactor(OfficerRecord)→float`
  （null/dead record → 1f，內含 clamp 0.25~2）。

## 設計決策

1. **soft-optional 接 P2 ＝ 反射橋**（`LordGovernanceBridge`）：不 ref
   `SettlementLords.dll`/`NamedOfficers.dll`（csproj healthcheck guard）。啟動時
   `AccessTools.TypeByName` 解析一次、快取 MethodInfo；任一缺/拋例外 → 永久降級 1.0
   ＋WarnOnce。About 只 `loadAfter`（非 modDependencies）。
2. **防禦獨立維度**：守城戰 prefix 降 `PointDamage`（可為負）抬 EffectivePoints，
   postfix 還原並 clamp ≤ `RimWarPoints-1`（防 sack 後二度疊傷與戰鬥停擺）；
   parent 已毀 → 不還原（comp 隨城亡）。**絕不寫 RimWarPoints**。
3. **sack 搬真資源**＝postfix 偵測指紋後按 `sackLossRatio` 扣 silver/food/goods、
   defensePoints 折半；**保留** RimWar 原點數搬移（信件/點數不動 → 不雙算：
   點數歸 RimWar、實財歸本 mod）。佔領/焚毀分支：comp 隨 ConvertSettlement/Destroy
   消失，財富歸零＝合理戰損，一期不轉移。
4. **成長自 comp CompTick**（不 postfix IncrementSettlementGrowth——財富不需與點數同輪）：
   每 2500 tick 一輪；鏡像 `PointDamage > 0` 跳過（圍城/受創不長財富）；
   首輪只播種不成長。
5. **貨架一期**＝stock 數量/白銀縮放＋交易回寫；價格、TraderKind 切換出界（M 段二期）。
6. typed int 主幹＋`Dictionary<SettlementAttributeDef,float> extraAttributes` 旁路
   （可空、懶配置；本期出貨 Def 類別不出貨實例，P5 治理動作用）。

## 數值公式（T2/T3/T4 實作依據）

```
seed（首輪）：silver=P、food=P/2、goods=P/2、defenseLevel=1、defensePoints=500（P=RimWarPoints）
unit = Max(1, P/100)；gov = LordGovernanceBridge（1.0 中性）；rate = growthRate 設定
每輪：silver += unit×gov×rate（cap P×10）；food/goods += 0.6×unit×gov×rate（cap P×5）
防禦：defCap = defenseLevel×1000；defensePoints += Max(1,unit/3)×rate → 到頂後
      若 defenseLevel<5 且 silver ≥ 2×cost（cost=(defenseLevel+1)×2000）→ 扣 cost 升級
守城加成 bonus = RoundToInt(Min(defensePoints, defCap) × defenseAmplitude)
sack：silver/food/goods 各 −floor(value×sackLossRatio)；defensePoints /= 2
貨架因子：silverF=Clamp(silver/P,0.25,2)；foodF=Clamp(food/(P×0.5),…)；goodsF 同 goods
回寫：賣給城：銀 +count；食 +BaseMarketValue×count；他 goods 同——買走則反向扣（Max 0）
```
量級核對：P=1000 城 gov=1 → +10 銀/輪＝240/日；cap 10000；sack 0.45 → 掉一半、
約兩週回滿。defensePoints 1000~5000 vs tier clamp 200~4000 → 中後期守城顯著。

## Harmony patch 清單（全 TryPatch fail-soft，缺一降級不連坐）

| 目標 | 型式 | 用途 |
|---|---|---|
| `IncidentUtility.ResolveCombat_Settlement` | prefix+postfix | 守城：降 PointDamage 臨時抬 EffectivePoints，戰後還原 |
| `IncidentUtility.ResolveBattle_Settlement` | prefix+postfix | 劫掠：sack 指紋偵測 → 搬真資源 |
| `Settlement_TraderTracker.RegenerateStock` | postfix | 貨架：按財富縮放 stock 數量 |
| `Settlement_TraderTracker.GiveSoldThingToTrader` | postfix | 玩家賣 → 城財富回寫（銀↑/食貨↑） |
| `Settlement_TraderTracker.GiveSoldThingToPlayer` | postfix | 玩家買 → 城財富回寫（銀↓/食貨↓） |
| `Settlement.GetInspectString` | postfix | 附財富/防禦行（獨立 postfix，與 RimWar/P2 疊加） |

## Mod 骨架

- 路徑 `~/repo/my_rimworld_mods/city-economy/`；packageId `pas.sanguo.cityeconomy`；
  Assembly `CityEconomy`；RootNamespace `pas.sanguo.cityeconomy`；前綴 `pas_cityecon_`。
- hard dep：brrainz.harmony＋Torann.RimWar（modDependencies＋loadAfter）；
  `pas.officers.community`/`pas.officers.settlements` **只 loadAfter**（soft-optional）。
- csproj：Krafs.Rimworld.Ref 1.6.*＋RimWar.dll/0Harmony.dll（Steam 路徑可 /p: 覆寫）；
  **禁 ref NamedOfficers/SettlementLords**（healthcheck guard）。
- XML：`Patches/CityEconomyComps.xml` 注入 `WorldObjectCompProperties_SettlementWealth`
  （鏡像 RimWar 三組 xpath）。
- ModSettings：`growthRate`(0~3,預設1)／`sackLossRatio`(0~1,預設0.45)／
  `defenseAmplitude`(0~2,預設1)／`traderEconomyEnabled`(bool,預設true)。
- 三語 Keyed（English / ChineseSimplified / ChineseTraditional），key 集合三邊一致。

## 任務拓樸（每任務 build 綠燈再往下；每檔 <200 行）

```
T0 簽章 spike（殘留物＝SignatureSpike.cs 編譯期釘）                     → 01-t0
T1 骨架：About/csproj/Patches XML/Languages/Settings/HarmonyInit(空)＋healthcheck 雛形 → 02-t1
T2 comp：SettlementWealthComp＋SettlementAttributeDef＋成長＋LordGovernanceBridge → 03-t2
T3 戰爭：守城 prefix/postfix＋sack prefix/postfix                       → 04-t3
T4 貨架＋顯示：三個 trader postfix＋GetInspectString                    → 05-t4
T5 驗證：healthcheck 全量＋dotnet build 0/0＋E2E checklist              → 06-t5
```

## 不做（明確出界）

- Empire `ITaxTickParticipant` 稅收對接（設計檔有列；本期任務未含 → P6 前補）。
- 價格維度／TraderKind 富貧切換（M 二期）。
- 佔領時財富轉移給新主（ConvertSettlement 重建 comp，一期歸零）。
- 領主主動投資/徵糧/修防（`ILordAction` 決策層 → P5；本期防禦升級為城池自治簡化版）。
- 玩家實打地圖戰 `defenderPointsFactor` 範式（哨站專屬，B 影子 Settlement 期協調）。
- 不 git commit、不部署。
