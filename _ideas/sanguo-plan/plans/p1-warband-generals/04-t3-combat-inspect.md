# T3 — 戰力注入＋inspect 顯示

## 1. `Source/Patches/Patch_ResolveCombatUnits.cs` — prefix(快照)+postfix(乘成)

**鐵律**：不讀寫 `rimwarData.combatAttribute`；只動本場 delta。

- `struct DamageSnapshot { int atk; int def; }`（避免 net48 ValueTuple 依賴疑慮）。
- prefix `(WarObject attacker, WarObject defender, ref DamageSnapshot __state)`：
  快照雙方 `PointDamage`（null 防衛 → 0）。
- postfix `(WarObject attacker, WarObject defender, DamageSnapshot __state)`，try/catch WarnOnce：

```
ga = comp.GeneralOf(attacker)，dead → 視為無將；gd 同
ga、gd 皆 null → return（零開銷路徑）
bAtk = GeneralsUtility.CombatBonus(ga)；bDef = CombatBonus(gd)
  CombatBonus(r)：r==null → 1f；score=(might+command)/2f；
  1f + (score-50)/50 × settings.bonusMax（0.3 預設 → 0.7..1.3）
RelationFactor hook（I 段預留）：非 null →
  bAtk ×= Safe(RelationFactor(ga, gd))；bDef ×= Safe(RelationFactor(gd, ga))
  （Safe：NaN/≤0 → 1，clamp 0.5..2，try/catch WarnOnce）
ratio = Mathf.Clamp(bAtk / bDef, 0.5f, 2f)；|ratio-1| < 0.001 → return
defDelta = defender.PointDamage - __state.def
  defDelta > 0 → defender.PointDamage = __state.def + RoundToInt(defDelta × ratio)
atkDelta = attacker.PointDamage - __state.atk
  atkDelta > 0 → attacker.PointDamage = __state.atk + Max(0, RoundToInt(atkDelta / ratio))
```

對稱語意：自家將領強 → 對方多掉血、自家少掉血；上下限 2x/0.5x 防爆。
雙方都被別的 mod 動過 PointDamage 也安全（只縮放本次 delta）。

- `GeneralsUtility` 增：`public static System.Func<OfficerRecord, OfficerRecord, float> RelationFactor`
  （self, enemy）→ 額外乘到 self 側 bonus；預設 null。**P4/關係 mod 註冊用，本期不實作內容**。

## 2. `Source/Patches/Patch_WarObjectInspectString.cs` — postfix

`(WarObject __instance, ref string __result)`，try/catch WarnOnce：

```
record = comp.GeneralOf(__instance)；null 或 dead → return
line = "pas_warband_InspectGeneral".Translate(record.DisplayName, record.might, record.command)
__result = IsNullOrEmpty(__result) ? line : __result + "\n" + line
```

覆蓋面：Warband（無自身 override，T0 核對）；Scout/Trader 等若另 override 則不顯示
（本期不綁將領於彼，無影響）。交戰中 warband 已離世界圖（吸收進 BattleSite），
無 inspect 對象——符合戰略抽象層定位。

## 3. HarmonyInit 掛載

```
TryPatch(IncidentUtility, "ResolveCombat_Units", prefix+postfix)
TryPatch(WarObject, "GetInspectString", postfix)
```

## 驗證

- `dotnet build` 0/0；healthcheck OK（接點字串 ResolveCombat_Units / GetInspectString）。
- 實機（T4 統一）：dev 把某 warband 將領 `SetAttribute` 武/統 100 vs 另一支 0，
  觀察多輪 BattleSite 戰鬥傷害不對稱；select warband → inspect 末行顯示將領。
