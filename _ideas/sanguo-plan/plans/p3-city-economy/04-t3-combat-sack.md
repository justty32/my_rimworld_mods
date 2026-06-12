# T3 — 戰爭接點：守城折算＋劫掠搬真資源

## 產出檔

```
Source/Patches/Patch_ResolveCombatSettlement.cs   # 守城 prefix+postfix
Source/Patches/Patch_ResolveBattleSettlement.cs   # 劫掠 prefix+postfix
Source/HarmonyInit.cs                             # 補兩條 TryPatch
```

## 守城：Patch_ResolveCombatSettlement（RW:11018）

鐵律：防禦**勿疊進 RimWarPoints 存量**——只動 `PointDamage`（K 段定案）。

- `Prefix(RimWarSettlementComp defender, ref int __state)`：
  `bonus = comp.DefenseBonus`（comp 缺/未播種/amplitude 0 → 0）；bonus≤0 → return；
  `defender.PointDamage -= bonus`（set 無 clamp，可為負 → EffectivePoints 高於存量，
  `num2=Clamp(EffectivePoints,0,num)` tier clamp 自動封頂——**鏡像 clamp 由原方法承擔**）；
  `__state = bonus`。
- `Postfix(RimWarSettlementComp defender, int __state)`：`__state≤0` → return；
  `parent == null || parent.Destroyed` → return（佔領 ConvertSettlement/焚毀後不還原，
  comp 隨城亡）；否則 `defender.PointDamage += __state`，再
  `PointDamage = Min(PointDamage, RimWarPoints - 1)`——雙保險：
  ①sack 分支已把 PointDamage 設為 `RimWarPoints-1`（倖存指紋），還原疊上去會讓
  EffectivePoints < 0 → CompTick `EffectivePoints>0` 守門 → 戰鬥停擺，clamp 防之；
  ②正常回合不會觸頂（傷害遠小於存量）。
- 整體 try/catch＋WarnOnce；不發信件、不用 Rand（與原方法同呼叫緒——
  RimWarSettlementComp.CompTick 主執行緒）。

## 劫掠：Patch_ResolveBattleSettlement（RW:11108）

sack 分支指紋（T0 定案）：**唯一倖存分支**＝parent 活著＋派系未變＋
`PointDamage == RimWarPoints - 1`＋`AttackingUnits.Count == 0`＋攻方 `EffectivePoints > 0`。
（焚毀分支同設 PointDamage 但 `parent.Destroy()`；佔領分支換派系；守軍勝只 Remove 攻方。）

- `Prefix(RimWarSettlementComp defender, ref Faction __state)`：
  `__state = defender?.parent?.Faction`（佔領偵測快照）。
- `Postfix(RimWarSettlementComp defender, WarObject attacker, Faction __state)`：
  1. settings `sackLossRatio ≤ 0` → return；
  2. comp 缺/未播種 → return；
  3. 指紋全中 → 搬資源：
     `silver -= Floor(silver×ratio)`；food/goods 同；`defensePoints /= 2`（城防殘破）；
  4. **不動 RimWarPoints/信件**——RimWar 原 sack 點數搬移保留（點數歸 RimWar、
     實財歸本 mod，不雙算）。
- 整體 try/catch＋WarnOnce。

## HarmonyInit 補丁清單（TryPatch fail-soft）

```
IncidentUtility.ResolveCombat_Settlement  → prefix+postfix（守城）
IncidentUtility.ResolveBattle_Settlement  → prefix+postfix（劫掠）
```

缺 method（RimWar 改版）→ WarnOnce＋該功能降級，不連坐成長/貨架。

## 驗收

build 0/0；healthcheck OK（接點字串 guard 過）；實機（T5）：dev 觀察被圍城戰——
有防禦城撐更久；城破 sack 信件出現後 inspect 財富明顯下降、defensePoints 折半。
