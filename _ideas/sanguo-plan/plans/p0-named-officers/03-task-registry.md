# T4 — `WorldComponent_OfficerRegistry`（0.5d）

**Create:** `Source/World/WorldComponent_OfficerRegistry.cs`
（泛化 `WorldComponent_RebellionTracker.cs`：去掉叛亂語意，留心跳/自癒/存讀骨架）

## 儲存設計（關鍵決策）

權威儲存 = **扁平 `List<OfficerRecord>`**（`Scribe_Collections` `LookMode.Deep`，
仿 `RebellionTracker.cs:212`）。WorldObject↔officers 的綁定**不直接 scribe dictionary**
（Reference-key dict 存讀脆弱），而是：
- record 自帶 `assignedTo` ref（隨 Deep record 存）；
- 執行期索引 `Dictionary<WorldObject, List<OfficerRecord>> index` **讀檔後/變更時重建**，不存檔。

```csharp
public class WorldComponent_OfficerRegistry : WorldComponent
{
    private List<OfficerRecord> records = new List<OfficerRecord>();
    private int nextId = 1;                                   // record id 發號器（Scribe_Values）
    private Dictionary<WorldObject, List<OfficerRecord>> index;  // 執行期重建，不 scribe

    public WorldComponent_OfficerRegistry(World world) : base(world) { }

    public static WorldComponent_OfficerRegistry Get()
        => Find.World.GetComponent<WorldComponent_OfficerRegistry>();

    public override void FinalizeInit(bool fromLoad) { /* base + RebuildIndex() */ }
    public override void WorldComponentTick() { /* 心跳節流 + HealAll + EvolveOpinions */ }
    public override void ExposeData() { /* Deep records + nextId + PostLoadInit 清 null */ }

    // —— 內部 CRUD（T7 OfficersApi 是唯一對外門面）——
    internal OfficerRecord Create(Faction f, WorldObject host, OfficerRoleDef role);
    internal void Assign(OfficerRecord r, WorldObject host);   // 更新 assignedTo + index
    internal void Remove(OfficerRecord r);                     // 含 index 與他人 opinions 的鍵清理
    internal List<OfficerRecord> For(WorldObject host);        // index 查詢（null→空表）
    internal OfficerRecord ById(int id);
    internal IReadOnlyList<OfficerRecord> AllForDebug => records;
}
```

## 心跳（照抄 RebellionTracker 範式，`:29-59`）

```csharp
public override void WorldComponentTick()
{
    if (Find.TickManager.TicksGame % Settings.checkIntervalTicks != 0) return;
    for (int i = records.Count - 1; i >= 0; i--)
    {
        try { Heal(records[i], i); }
        catch (System.Exception e) { WarnOnce(records[i], e); }   // 逐 record 例外隔離
    }
    OpinionEvolver.EvolveAll(records);   // T6：B 軌演化
}
```

- 節流用 `OfficersSettingsDef.checkIntervalTicks`（2500）；`WarnOnce` 照抄
  `RebellionTracker.cs:61-72`（同 key 只警告一次，防 log 洪水）。
- **P0 不自動鋪官**（無 `EnsureRebels` 對應物）：record 只由 API/dev action 建立（G6）。
  消費 mod 在自己的心跳裡呼 `OfficersApi.CreateOfficer` 補官。

## 自癒 `Heal`（泛化 `RebellionTracker.Heal :102-150`，逐分支對照）

1. **pawn 死亡/Destroyed**：反叛者是「歸零+冷卻重生」；職官改為（G5）
   `dead=true` → 廣播 `OfficersApi.OfficerDied` → 下一輪心跳 `Remove(record)`
   （留一輪讓消費 mod 讀遺言：繼任/復仇邏輯在它們那邊）。
2. **pawn 在世但掉出 world pawn 清單**：照抄 `:125-138` —
   `!Spawned` 且不在 `WorldPawns` → `PassToWorld(KeepForever)`；
   在但掉 forced-keep（拜訪 redress 副作用）→ `ForcefullyKeptPawns.Add`（冪等）。
3. **assignedTo 消失**（null/Destroyed/易主）：職官**不自動搬家**（反叛者會換城；
   職官的調動是消費 mod 的玩法）→ `assignedTo=null` 留 record、廣播 `OfficerUnassigned`。
4. **inhabitants 橋補鋪**：`assignedTo is Settlement s && pawn!=null &&
   !s.previouslyGeneratedInhabitants.Contains(pawn)` → Add（照抄 `:144-148`）。
5. **faction 滅亡**：record 不強刪（將領可仕新主——消費 mod 決定）；僅 faction==null 時清。

## 存讀（仿 `RebellionTracker.ExposeData :209-227`）

```csharp
Scribe_Collections.Look(ref records, "pas_officerRecords", LookMode.Deep);
Scribe_Values.Look(ref nextId, "pas_officerNextId", 1);
if (Scribe.mode == LoadSaveMode.PostLoadInit)
{
    records ??= new List<OfficerRecord>();
    records.RemoveAll(r => r == null);     // faction null 不清（見 Heal 5）
    RebuildIndex();
}
```

## 驗證步驟

- build 過；開新世界 → log 無錯；存檔 → 檔內出現 `pas_officerRecords` 節點。
- **舊檔中途裝 mod**：拿任一現存存檔載入 → `FinalizeInit(fromLoad)` 不炸、組件就位。
- **移除 mod 回讀**：去掉 mod 載舊檔 → 只有無害 warning（RimWorld 對未知組件的標準行為）。
- index 正確性留 T8 dev action `Dump officer registry` 對帳。
