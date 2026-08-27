# A0 spike ＋ A1 資料層：status／homeSettlement／職涯欄位／預擲名

## A0 — 讀碼 spike（0.25d，唯讀不寫碼）

照家規（P1/P2 計畫的 T0 慣例），動手前以反編譯源（`~/repo/projects/rimworld/`）釘死：

1. **`Faction.defeated`**（`RimWorld/Faction.cs:32`，public bool）：確認 vanilla 何時置 true
   （最後聚落被毀時由誰寫入）、defeated 派系是否留在 `Find.FactionManager`（預期：留，
   Scribe_References 對其安全）、`RimWar`/Empire 易主鏈是否會觸發它。
2. **無 pawn 起名路徑**：
   - 首選 `kind.GetNameMaker(gender)`（`Verse/PawnKindDef.cs:360`）→
     `NameGenerator.GenerateName(rulePack)`；kind 取 `faction.RandomPawnKind()`（P0 既用）。
   - 後備 `PawnNameDatabaseShuffled.BankOf(PawnNameCategory.HumanStandard)
     .GetName(PawnNameSlot.First/Last, gender)`（皆 public，已 grep 確認）。
   - 確認兩路徑在 NPC 派系（部落/海盜/帝國）下的回傳樣態與 null 面。
3. **名字回寫**：`pawn.Name = new NameTriple(first, nick, last)` / `NameSingle` 在
   `PawnGenerator.GeneratePawn` 之後設定是否安全（vanilla 多處先例：確認 setter 無副作用）。
4. 確認 `Find.WorldGrid.ApproxDistanceInTiles(int,int)` 簽章（A2 re-home 最近聚落用）。

**驗證**：spike 結論寫進本檔修正框（如有出入），無出入則在任務 log 記「A0 無出入」。

## A1 — `OfficerRecord` 新欄位＋`OfficerNamer`（0.5d）

**Modify:** `Source/Data/OfficerRecord.cs`、`Source/OfficersApi.cs`（僅 enum 宣告處）
**Create:** `Source/Data/ServiceEntry.cs`、`Source/Data/OfficerNamer.cs`

### 新欄位（全部增量、預設值向後相容）

```csharp
public enum OfficerStatus { Serving, Idle, Wandering }   // 任官(含待命/朝廷職)/在野/流浪

// OfficerRecord 增欄：
public OfficerStatus status = OfficerStatus.Serving; // Scribe_Values 預設 Serving → 舊檔自動遷移
public Settlement homeSettlement;                    // 居住地≠任職地；Scribe_References
public int appointedTick = -1;                       // 最近一次任官 tick（UI 任期/AI 遲滯帶）
public List<ServiceEntry> serviceHistory;            // 職涯履歷；LookMode.Deep；懶初始化
```

### `ServiceEntry : IExposable`（輕量、不持 ref——派系/role 可能消亡，存字串快照）

```csharp
public class ServiceEntry : IExposable
{
    public string roleLabel;     // 卸任時的 role.label 快照
    public string factionName;   // 卸任時的 faction.Name 快照
    public int startTick;
    public int endTick;
}
```

- 寫入點：`SetRole`/`EmployOfficer`/`MakeIdle`（A3）卸下舊職時 push 一筆。
- **環形上限 8 筆**（最舊先丟）——防無限成長；常數 `MaxServiceHistory=8` 放 record。

### 不變式（A2 Healer 自癒、A3 API 共同維護）

| status | assignedTo | role | faction |
|---|---|---|---|
| Serving | 可 null（待命/朝廷職 scope=Faction） | 非 null | 非 null |
| Idle（在野） | **null** | **null** | 非 null＝homeSettlement 派系 |
| Wandering | null | null | 非 null（可為 defeated 舊派系） |

`DisplayName` fallback 鏈擴充：`pawn → nameCached → role?.label → "pas_officers_Talent".Translate()`
（新 Keyed：在野者 role=null，現行字面 "officer" 改 keyed「人才」）。

### `OfficerNamer`（靜態 util，UI 硬前置——見 00 決策 4）

```csharp
public static class OfficerNamer
{
    /// <summary>無 pawn 預擲名（冪等：nameCached 已有值直接回）。
    /// kind.GetNameMaker → NameGenerator；後備 NameBank First+Last；再後備 null（DisplayName 落 keyed）。</summary>
    public static string EnsureNameCached(OfficerRecord record);
}
```

- 性別：預擲時 `Rand.Bool` 擲一個、存 record？**不存**——MVP 接受 Materialize 後性別與
  名字風格輕微不搭（記入 backlog）；名字本身由 Materialize 回寫保證一致（A2 下述）。
- `OfficerSpawner.SyncName` 反向補強（A2 範圍但邏輯歸此檔記錄）：
  原本「具現後快取 pawn 名」；改為**雙向**——nameCached 已存在（預擲過）→
  `pawn.Name = new NameSingle(nameCached)`（或 spike 驗證的 NameTriple 拆字），
  nameCached 為空 → 維持原行為快取 pawn 名。

### ExposeData 增量

```csharp
Scribe_Values.Look(ref status, "status", OfficerStatus.Serving);
Scribe_References.Look(ref homeSettlement, "homeSettlement");
Scribe_Values.Look(ref appointedTick, "appointedTick", -1);
Scribe_Collections.Look(ref serviceHistory, "serviceHistory", LookMode.Deep);
// PostLoadInit：serviceHistory null → 留 null（懶初始化，省舊檔空間）
```

**舊檔遷移**：零動作——舊 record 全部落 `Serving`＋`homeSettlement=null`，語意正確
（它們本來就是任官中）；homeSettlement 由 A3 `EmployOfficer` 與 A2 orphan 流程逐步補值。

## 驗證（A1）

1. `dotnet build Source/NamedOfficers.csproj -c Release` → 0 警告 0 錯誤。
2. `python3 tests/healthcheck.py` 通過（零 Harmony/零相依規則未破——本任務純 vanilla API）。
3. 心智檢查列入 99-E2E：舊檔（無 status 欄）讀入 → dump 全部 Serving、不炸。
4. dev：`Dump officer registry` 增列 status/home/appointedTick/history（A4 改 dump，先記）。
