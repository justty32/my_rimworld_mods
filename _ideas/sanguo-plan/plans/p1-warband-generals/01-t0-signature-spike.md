# T0 — 簽章 spike（結果定案）

> 來源：`~/repo/pas/projects/rimworld_mods/rim-war/decompiled/RimWar.decompiled.cs`（2026-06-12 核對）。
> spike 殘留物＝`Source/SignatureSpike.cs` 編譯期釘（RimWar 改簽章 → build 直接紅，先於實機炸）。

## 已核對簽章（行號＝反編譯檔）

| 成員 | 行 | 簽章/事實 |
|---|---|---|
| `WorldUtility.CreateWarband` | 15467 | `public static Warband CreateWarband(int power, RimWarData rwd, Settlement parentSettlement, PlanetTile startingTile, WorldObject destination, WorldObjectDef worldDef, bool _launched=false, bool _interactable=true, int pointDamage=0)`；try/catch NRE 回 **null**；`_launched` 時內部即呼 `ArrivalAction()`（postfix 須查 `__result==null || __result.Destroyed`）；同類唯一 overload |
| `WorldUtility.MakeWarband` | 15518 | **private** static → 不可鉤；用 `RimWarDefOf.RW_Warband` 造物＋`SetUniqueId` |
| `WorldUtility.CreateWarObjectOfType` | 15358 | `public static void CreateWarObjectOfType(WarObject warObject, int power, RimWarData rwd, Settlement parentSettlement, PlanetTile startingTile, WorldObject destination, WorldObjectDef worldDef, PlanetTile destinationTile, bool _launched=false, bool _interactable=true, int _pointDamage=0)`；`is Warband` 分支轉呼 CreateWarband（同步、無巢狀） |
| `IncidentUtility.ResolveCombat_Units` | 11271 | `public static void ResolveCombat_Units(WarObject attacker, WarObject defender)`；唯一副作用＝兩側 `PointDamage += RoundToInt(num × num6/num7)`（11331-11332）；`combatAttribute` 只讀不寫（11290-11291） |
| `WarObject.GetInspectString` | 14860 | `public override string GetInspectString()`；自建 StringBuilder、不附 comp extras、不呼 base；`Warband`(13236) 無 override → patch WarObject 即覆蓋 warband |
| `WarObject.PointDamage` | — | public get/set（`defender.PointDamage = …` RW:11257 先例） |
| `RimWarData.behavior` | — | public（Player/Excluded 判別，仿 Mod 1） |
| `RimWarSite.Units` | 8555 | `public List<WarObject>`；**深存** `Scribe_Collections(..., LookMode.Deep)`（RimWarSite.ExposeData） |
| `RimWarSettlementComp.AttackingUnits` | 9088 | 深存（`atkos` LookMode.Deep，RW:9502 區） |

## 生命週期事實（決定 T2 設計）

- 交戰吸收：`ResolveRimWarBattle`（10274）把雙方塞 `BattleSite.Units` 或
  `RimWarSettlementComp.AttackingUnits` 後逐一 `Destroy()`＋移出 WorldObjects（10327-10338）；
  `WarObject.InteractWithSite`（14796）同型：`Units.Add(this); ImmediateDestroy()`。
- 戰鬥輪：`BattleSite.Tick` 每 2500 tick 對 Units 兩兩 `ResolveCombat_Units`（8791）
  → **交戰期間 attacker/defender 是「已 Destroyed 但深存」的同一實例**（綁定 ref 不斷）。
- 戰後重生：`ResolveBattle_Units`（11363）/聚落戰各分支（11165/11209/11234/11252）
  以舊實例為樣板呼 `CreateWarObjectOfType` 造**新物件**，舊實例隨容器丟棄。

## P0 端核對（不改 P0）

- `OfficerHealer.Heal` 分支 3：`assignedTo.Destroyed || Faction != record.faction`
  → `Assign(record, null)`＋`OfficerUnassigned`（觸發時已 null → 事件不可用於辨識戰況）。
- `OfficersApi.CreateOfficer(faction, host, role)`：超 G6 上限(4)/參數壞 → null。
- `OfficerRecord`：`might/command/dead/DisplayName` 公開直讀；record id 穩定（B 軌鍵）。
- `GetById(int)`：record 被 P0 清掉後回 null（pawn 死亡 G5 → 下一心跳移除）。

## SignatureSpike.cs（殘留物規格）

internal static readonly 委派釘三個 static 目標（method-group 轉換＝編譯期簽章驗證）：

```csharp
Func<int, RimWarData, Settlement, PlanetTile, WorldObject, WorldObjectDef, bool, bool, int, Warband>
    = WorldUtility.CreateWarband;
Action<WarObject, WarObject> = IncidentUtility.ResolveCombat_Units;
Action<WarObject, int, RimWarData, Settlement, PlanetTile, WorldObject, WorldObjectDef, PlanetTile, bool, bool, int>
    = WorldUtility.CreateWarObjectOfType;
```

（instance virtual `GetInspectString` 不釘——HarmonyInit TryPatch 找不到時 WarnOnce 降級。
欄位用 internal 非 private，避 CS0414 破 0 警告。）

## 驗證

- `dotnet build` 0 警告 0 錯誤（T1 起每任務重跑）。
- 若未來 RimWar 更新斷簽章：build 紅（spike 釘）或啟動 WarnOnce 降級（TryPatch），雙保險。
