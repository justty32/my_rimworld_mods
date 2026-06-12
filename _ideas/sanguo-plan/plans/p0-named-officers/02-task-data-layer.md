# T2 Def 層 ＋ T3 資料層 `OfficerRecord`

## T2 — Def 層（0.25d）

**Create:**
- `Source/Defs/OfficerRoleDef.cs`、`Source/Defs/OfficersSettingsDef.cs`、`Source/OfficersDefOf.cs`
- `Defs/OfficersDefs/Roles.xml`、`Defs/OfficersDefs/Settings.xml`

### OfficerRoleDef（職位走 Def，先例 `OutpostTypeDef`/`RebellionProfileDef`——消費 mod 可自加 XML 角色）

```csharp
namespace pas.officers
{
    /// <summary>職位（領主/將領/官員…）。消費 mod 以 XML 增訂自己的角色。</summary>
    public class OfficerRoleDef : Def
    {
        public int displayPriority;          // inspect 排序
        public bool leaderLike;              // P2 領主類（每物件至多一名）
    }
}
```

### OfficersSettingsDef（仿 faction-politics `PoliticsSettingsDef` 單例 Settings def）

```csharp
public class OfficersSettingsDef : Def
{
    public int checkIntervalTicks = 2500;       // 心跳（02 設計指定 2500-tick 範式）
    public int maxOfficersPerObject = 4;        // G6 數量控管
    public int opinionDriftPerHeartbeat = 1;    // B 軌向 bias 回歸步長
    public IntRange initialAttributeRange = new IntRange(20, 80);  // 擲屬性
}
```

XML：`Roles.xml` 出貨一個通用角色 `pas_officers_Generic`（P0 dev 測試用；
領主/將領角色由 P1/P2 各自定義）；`Settings.xml` 一個 `pas_officers_Settings`。
`OfficersDefOf` 用 `[DefOf]` 掛 `pas_officers_Generic`。

**驗證**：build 過；healthcheck（T8 完成後回掃）抓 defName 交叉引用。

## T3 — `OfficerRecord : IExposable`（0.5d）

**Create:** `Source/Data/OfficerRecord.cs`（泛化 `RebelRecord.cs`，逐欄對照）

```csharp
namespace pas.officers
{
    /// <summary>一名具名職官（權威資料；pawn 只是懶生成的具現）。
    /// 泛化自 faction-politics RebelRecord（單一反叛者 → 通用職官）。</summary>
    public class OfficerRecord : IExposable
    {
        // —— 身份 ——
        public int id;                    // registry 發號，關係 dict 的 key（見下）
        public Faction faction;           // Scribe_References
        public WorldObject assignedTo;    // 泛化 homeSettlement: Settlement → WorldObject
        public OfficerRoleDef role;       // Scribe_Defs
        public Pawn pawn;                 // 懶生成；可 null（Scribe_References）
        public string nameCached;         // pawn==null 時的顯示名（T0 決議的生成途徑）

        // —— 屬性（G2：七維全建、MVP 啟用五維）——
        public int might;        // 武力（P1 將領讀）
        public int command;      // 統率（P1 將領讀）
        public int polity;       // 政務（P2 領主讀）
        public int charisma;     // 魅力（P2 領主讀）
        public int loyalty;      // 忠誠（P2/P4 讀）
        public int intellect;    // 智力（預留）
        public int morale;       // 士氣（預留）

        // —— 關係 B 軌（I：vanilla opinion 對 world pawn 凍結，自存自演化）——
        /// <summary>key = 對方 OfficerRecord.id（非 thingIDNumber——pawn 可死後換體，record id 不變）。</summary>
        public Dictionary<int, int> opinions = new Dictionary<int, int>();

        // —— 生命週期 ——
        public bool dead;                 // G5：死亡標記，下個心跳廣播後清理

        public string DisplayName => pawn?.LabelShortCap ?? nameCached ?? role?.label ?? "officer";

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref assignedTo, "assignedTo");
            Scribe_Defs.Look(ref role, "role");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref nameCached, "nameCached");
            Scribe_Values.Look(ref might, "might");
            // …其餘六維同形 Scribe_Values …
            Scribe_Collections.Look(ref opinions, "opinions", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref dead, "dead");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && opinions == null)
                opinions = new Dictionary<int, int>();
        }
    }
}
```

設計要點：
- **關係 key 用 record id 而非 pawnId**（設計檔寫 `Dictionary<otherPawnId,int>`，但 pawn 懶生成
  可能晚於關係建立、且死亡換體會斷鍵——record id 跨具現穩定。回寫設計檔備註）。
- opinion 為**非對稱**（甲對乙 ≠ 乙對甲），各存各的。
- `assignedTo` 用 `WorldObject` 而非 `Settlement`：將領掛 warband（`WarObject : WorldObject`）、
  領主掛 Settlement，同一型別涵蓋（P0 不 ref RimWar，僅用基類）。
- record 本體**不含玩法數值換算**（GovernanceFactor 等住消費 mod）。

**驗證**：build 過；寫一個最小 round-trip 心智檢查列入 T9 E2E（存→讀→七維/dict 不丟）。
