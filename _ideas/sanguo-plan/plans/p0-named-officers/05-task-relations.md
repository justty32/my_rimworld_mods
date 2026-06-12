# T6 — 關係雙軌（0.5d）

> 設計：`02`（雙軌定義）+ `06-A`（玩法消費接點，P0 只鋪骨架不接玩法）+ 調查 I。

**Create:**
- `Defs/PawnRelationDefs/Relations.xml`（A 軌兩個 def）
- `Source/Data/OpinionEvolver.cs`（B 軌演化）
- `Source/Data/RelationsUtility_Officers.cs`（A 軌包裝；命名避開 vanilla `RelationsUtility`）

## A 軌 — 持久關係（結拜/世仇）＝ vanilla `DirectPawnRelation`

對 world pawn 完全可用（`Pawn_RelationsTracker.cs:483/292` 無 Spawned 檢查，調查 I 已證）、
隨 pawn 自動存檔、零維護成本。

### Relations.xml

```xml
<Defs>
  <PawnRelationDef>
    <defName>pas_officers_SwornBrother</defName>
    <label>sworn brother</label>
    <opinionOffset>60</opinionOffset>
    <reflexive>true</reflexive>           <!-- 對稱：互為結拜 -->
    <importance>120</importance>
  </PawnRelationDef>
  <PawnRelationDef>
    <defName>pas_officers_BloodFeud</defName>
    <label>sworn enemy</label>
    <opinionOffset>-60</opinionOffset>
    <reflexive>true</reflexive>
    <importance>120</importance>
  </PawnRelationDef>
</Defs>
```

（欄位名以 T0 對 1.6 `PawnRelationDef` 的驗證為準；`opinionOffset` 是 A 軌餵 B 軌
初始 bias 的數值來源——見下。）

### A 軌包裝（G4：需真 Pawn → 按需具現）

```csharp
public static class RelationsUtility_Officers
{
    /// <summary>建立持久關係；兩端未具現則先 Materialize（G4 決議）。失敗回 false。</summary>
    public static bool AddPersistent(OfficerRecord a, OfficerRecord b, PawnRelationDef def)
    {
        Pawn pa = OfficerSpawner.Materialize(a);
        Pawn pb = OfficerSpawner.Materialize(b);
        if (pa == null || pb == null) return false;
        if (!pa.relations.DirectRelationExists(def, pb))
            pa.relations.AddDirectRelation(def, pb);   // reflexive：vanilla 自動補對向
        return true;
    }
    public static bool HasPersistent(OfficerRecord a, OfficerRecord b, PawnRelationDef def);
}
```

## B 軌 — 連續好感度 ＝ 自存 `opinions` dict（record 內，T3 已建欄位）

vanilla 動態 opinion 對 world pawn 凍結（`Pawn.cs:1659` Spawned 閘，調查 I）→ 自存自演化。

### OpinionEvolver（由 T4 心跳每 2500-tick 呼叫）

```csharp
public static class OpinionEvolver
{
    /// <summary>每心跳：各 record 對「同宿主同僚」的 opinion 向 bias 回歸一步。
    /// bias = A 軌加總（結拜 +60 / 世仇 -60 / 無關係 0）——B 讀 A 當初始偏置（02 設計）。</summary>
    public static void EvolveAll(List<OfficerRecord> records)
    {
        // 1. 只演化同 assignedTo 的配對（O(宿主內 n²)，宿主官數 ≤ maxOfficersPerObject=4，可控）
        // 2. 缺鍵 → 以 bias 初始化：opinions[other.id] = BiasOf(a, b)
        // 3. 有鍵 → 每步 ±opinionDriftPerHeartbeat 向 bias 收斂；clamp [-100, 100]
        // 4. 對方 record 已 Remove → 鍵由 registry.Remove 統一清（T4），此處跳過
    }
    public static int BiasOf(OfficerRecord a, OfficerRecord b);   // 讀 A 軌（雙方未具現→0，不強制具現）
}
```

設計要點：
- **演化只是回歸骨架**：事件式漲跌（戰功/羞辱/賞賜）是消費 mod 經
  `OfficersApi.OffsetOpinion` 寫入的脈衝，骨架負責緩慢回歸 bias——P0 不發明玩法事件。
- `BiasOf` **不觸發具現**（只查已具現雙方的 DirectPawnRelation）；
  避免心跳路徑悄悄生 world pawn（爆量風險鐵律）。
- 跨宿主關係不演化但保留既有鍵值（將領調防後舊怨仍在，P1 讀「兩將不和打折」用）。

## 驗證步驟

1. build 過；healthcheck 抓 Relations.xml defName/類交叉引用。
2. dev action：同一聚落建兩官 → `Add sworn brothers` → dump 顯示
   `DirectRelationExists=true`、下個心跳後 `opinions[other]` 從 0 爬向 +60。
3. `Offset opinion -100` 脈衝 → 數個心跳觀察回歸 bias 方向收斂。
4. 存讀檔 → A 軌（隨 pawn）與 B 軌（dict）皆不丟；結拜雙方 pawn 互看 opinion 含 +60 offset
   （vanilla social 卡只在 pawn 上地圖時可見——以 dump 為準）。
