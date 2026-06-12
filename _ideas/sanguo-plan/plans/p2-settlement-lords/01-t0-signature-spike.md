# T0 — 簽章 spike（計畫期定案；殘留物＝SignatureSpike.cs）

## 目的

把本 mod 依賴的 RimWar 公開介面在**編譯期**釘死：RimWar 改版斷簽章 → build 直接紅，
先於實機；執行期另有 HarmonyInit TryPatch 降級雙保險（同 P1 手法）。

## 已核對事實（反編譯 RimWar.decompiled.cs，2026-06-12）

| 介面 | 位置 | 形態 |
|---|---|---|
| `WorldComponent_PowerTracker.IncrementSettlementGrowth()` | RW:17567 | `public void`、無參數、實例 |
| `WorldComponent_PowerTracker` | RW:16850 | `public class : WorldComponent`，ns `RimWar.Planet` |
| `WorldUtility.GetRimWarDataForFaction(Faction)` | RW:15146 | `public static RimWarData` |
| `RimWarSettlementComp.RimWarPoints` | RW:9228 | `public int` get/set；getter clamp 100..100000、setter Max(0,) |
| `RimWarSettlementComp.PointDamage` | RW:9216 | `public int` get/set |
| `RimWarSettlementComp.isCapitol` | RW:9080 | `public bool` 欄位 |
| `RimWarData.behavior` | — | 公開欄位（Mod 1 已消費） |
| `Settlement.GetInspectString` | vanilla | RimWar 自身 postfix 之（RW:5977→6570）→ 多 postfix 安全 |

## 釘法（SignatureSpike.cs）

- 實例方法不能 method-group 成 static delegate → 用**永不呼叫的 internal static wrapper**：
  `PinIncrementSettlementGrowth(WorldComponent_PowerTracker t) => t.IncrementSettlementGrowth();`
- comp 成員（RimWarPoints get/set、PointDamage get、isCapitol、parent）併一個 wrapper 釘。
- `GetRimWarDataForFaction` 是 static → `Func<Faction, RimWarData>` method-group 釘（同 P1 風格）。
- 全部 internal（非 private）避 CS0414/CS0169 破零警告；零執行期成本。

## 驗收

`dotnet build` 0 警告 0 錯誤（T1 一起驗）。
