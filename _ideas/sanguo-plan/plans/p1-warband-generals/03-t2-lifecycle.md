# T2 — 生命週期：綁定 WorldComponent＋生成＋傳承＋退場

## 1. `Source/World/WorldComponent_WarbandGenerals.cs`

權威綁定儲存（00-overview 決策 1）。

- `GeneralBinding : IExposable { WorldObject host; int recordId; }`
  （host `Scribe_References`——warband 在世界圖或深存於 BattleSite/AttackingUnits 皆可解析；
  recordId `Scribe_Values`，經 `OfficersApi.GetById` 懶解析、不存 record 參照）。
- 欄位：`List<GeneralBinding> bindings`（scribe Deep）＋執行期 `Dictionary<WorldObject, GeneralBinding> byHost`
  （FinalizeInit/PostLoadInit 重建，不 scribe——仿 P0 registry index）。
- API（皆 null-safe）：
  - `static Get()`；
  - `OfficerRecord GeneralOf(WorldObject host)`：byHost → GetById；record 不存在 → 順手解綁。
  - `void Bind(WorldObject host, OfficerRecord record)`：先移除同 recordId 舊綁定（傳承時舊 host
    已毀仍掛表上），再 add＋index。
- **心跳清理**（`WorldComponentTick`，`TicksGame % 2500 == 1200`——錯開 P0 的 %==0）：
  倒序掃 bindings，逐項 try/catch＋WarnOnce：
  1. `GetById(recordId) == null` → 解綁（pawn 死亡 G5 已由 P0 清 record）。
  2. `record.dead` → 跳過（遺言窗口，下一輪走 1）。
  3. `host == null` 或（`host.Destroyed` 且 `!GeneralsUtility.InActiveBattle(host)`）
     → **將領退場**：`OfficersApi.RemoveOfficer(record)`＋解綁（隨軍覆滅；勝者已在
     CreateWarband 傳承走、不會走到這）。
- **存檔防懸掛**：`GeneralBinding.ExposeData` Saving 時若 `host.Destroyed &&
  !InActiveBattle(host)` → 以 null 寫出 host（防 load 時 unresolved-ref 警告）；
  load 後 host==null 由心跳分支 3 補退場（record 不漏）。
- `ExposeData` PostLoadInit：`bindings.RemoveAll(b => b == null)`＋重建 byHost（host null 不進 index）。

## 2. `GeneralsUtility.InActiveBattle(WorldObject host)`

掃 `Find.WorldObjects.AllWorldObjects` 中 `RimWarSite.Units.Contains(host)`；
再掃 `Find.WorldObjects.Settlements` 的 `RimWarSettlementComp.AttackingUnits.Contains(host)`。
頻率低（心跳/存檔），線性掃可接受。RimWar 型別缺失防衛：try/catch WarnOnce 回 false。

## 3. `Source/Patches/Patch_CreateWarband.cs` — postfix

參數 `(Warband __result, RimWarData rwd)`（名對齊原簽章）。全身 try/catch WarnOnce。

```
__result null / Destroyed（_launched 即時抵達）→ return
TransferContext.TryConsume(out record) →                       # 傳承優先（00 決策 2）
    AssignOfficer(record, __result)（重建 P0 綁定/索引）＋ comp.Bind(__result, record)；return
rwd null / behavior Player|Excluded → return                   # 仿 Mod 1 慣例
__result.Faction null 或玩家 → return
Rand.Chance(settings.generalChance) 不中 → return
role = GeneralsUtility.GeneralRole（GetNamedSilentFail，null → WarnOnce return）
record = OfficersApi.CreateOfficer(__result.Faction, __result, role)（null＝G6 滿/壞參數 → return）
comp.Bind(__result, record)
```

## 4. `Source/Patches/Patch_CreateWarObjectOfType.cs` — prefix+postfix＋TransferContext

- `TransferContext`：internal static 單槽 `OfficerRecord pending`（Set/Clear/TryConsume）。
  CreateWarObjectOfType→CreateWarband 同步單線呼叫、無巢狀（T0 核對）→ 單槽夠用。
- prefix `(WarObject warObject)`：`warObject is Warband` 且 `comp.GeneralOf(warObject)` 非 null
  非 dead → `TransferContext.Set(record)`。
- postfix：`TransferContext.Clear()`（CreateWarband 失敗回 null/非 Warband 分支時防殘留）。

## 5. HarmonyInit 掛載

```
TryPatch(WorldUtility, "CreateWarband",        postfix: Patch_CreateWarband.Postfix)
TryPatch(WorldUtility, "CreateWarObjectOfType", prefix/postfix: Patch_CreateWarObjectOfType)
```

## 驗證

- `dotnet build` 0/0；healthcheck OK（加：Source 出現 CreateWarband/CreateWarObjectOfType 接點字串）。
- dev 實機（可延後至 T4 一併）：開 RimWar 世界，等/催生 warband → select 後 dev log
  無紅字；存讀檔 → 綁定與 record 保留。
