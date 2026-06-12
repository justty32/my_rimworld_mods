# T0 — 簽章 spike（計畫期定案；殘留物＝SignatureSpike.cs）

## 目的

把本 mod 依賴的 RimWar 公開介面在**編譯期**釘死：RimWar 改版斷簽章 → build 直接紅，
先於實機；執行期另有 HarmonyInit TryPatch 降級雙保險（同 P1/P2 手法）。

## 已核對事實（反編譯 RimWar.decompiled.cs，2026-06-12）

| 介面 | 位置 | 形態 |
|---|---|---|
| `IncidentUtility`（ns `RimWar.Planet`） | RW:10233 | `public class` |
| `IncidentUtility.ResolveCombat_Settlement(RimWarSettlementComp, WarObject)` | RW:11018 | `public static void` |
| `IncidentUtility.ResolveBattle_Settlement(RimWarSettlementComp, WarObject, float)` | RW:11108 | `public static void` |
| `RimWarSettlementComp.RimWarPoints` | RW:9228 | `public int` get/set；getter clamp 100..100000、setter Max(0,) |
| `RimWarSettlementComp.PointDamage` | RW:9216 | `public int` get/set，**set 無 clamp（可負）** |
| `RimWarSettlementComp.EffectivePoints` | RW:9277 | `public int` get＝RimWarPoints−PointDamage |
| `RimWarSettlementComp.AttackingUnits` | RW:9131 | `public List<WarObject>` get |
| `RimWarSettlementComp.parent` | 繼承 | `WorldObject` |
| `WarObject.EffectivePoints` | RW:14403 | `public int` get |
| `WorldUtility.GetRimWarDataForFaction(Faction)` | RW:15146 | `public static RimWarData` |
| `RimWarData.behavior` | — | 公開欄位（P1/P2 已消費） |
| vanilla `Settlement_TraderTracker.RegenerateStock()` | :265 | **protected** virtual → 無法編譯期釘，runtime AccessTools 找＋TryPatch 降級 |
| vanilla `GiveSoldThingToTrader/GiveSoldThingToPlayer(Thing,int,Pawn)` | :164/:187 | public virtual → 可釘 |
| vanilla `Settlement_TraderTracker.settlement` | :9 | public 欄位 |
| vanilla `Settlement_TraderTracker.stock` | :11 | **private** `ThingOwner<Thing>` → `AccessTools.FieldRefAccess` |
| P2 `WorldComponent_SettlementLords.Get()/LordOf(Settlement)` | 反射 | **不釘**（soft-optional，缺即 1.0） |
| P2 `LordsUtility.GovernanceFactor(OfficerRecord)` | 反射 | 同上 |

## 釘法（SignatureSpike.cs）

- 兩個 ResolveXXX 是 static → `Action<…>` method-group 釘。
- comp 成員（RimWarPoints get/set、PointDamage get/set、EffectivePoints、AttackingUnits、
  parent）併一個 wrapper 釘；WarObject.EffectivePoints 另一 wrapper。
- `GetRimWarDataForFaction` → `Func<Faction, RimWarData>`；behavior 比對 wrapper。
- vanilla 兩個 GiveSoldThing → 永不呼叫的 wrapper（實例 virtual）；`settlement` 欄位順手釘。
- `RegenerateStock`/`stock` 為 protected/private：**只能 runtime**（HarmonyInit TryPatch
  ＋FieldRefAccess try/catch，缺 → 貨架功能整組降級、不連坐）。
- 全部 internal（非 private）避 CS0414/CS0169 破零警告；零執行期成本。

## 驗收

`dotnet build` 0 警告 0 錯誤（T1 一起驗）→ spike 結果回填 00-overview。
