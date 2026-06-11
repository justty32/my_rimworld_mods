# Task 2b: 角色/派系 Def 類 + DefOf + 暫時殼

> 屬於 `../2026-06-11-implementation-plan.md`。接續 Task 2a。

**Files:**
- Create: `sims-mode-community/Source/Defs/LifeRoleDef.cs`
- Create: `sims-mode-community/Source/Defs/LifeProfileDef.cs`
- Create: `sims-mode-community/Source/SimsDefOf.cs`
- Create: `sims-mode-community/Source/Assign/RoleAssignmentWorker.cs`（殼，Task 6a 補實作）
- Create: `sims-mode-community/Source/Facility/MapComponent_FacilityRegistry.cs`（殼，Task 3 補實作）

- [ ] **Step 1: LifeRoleDef.cs（角色 + 作息表）**

```csharp
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace pas.sims
{
    public class ScheduleEntry
    {
        public int from;                       // 起始時辰（含），0-23
        public int to;                         // 結束時辰（不含）；from > to 表跨夜（如 22-7）
        public DutyDef duty;
        public FacilityTagDef focusFacility;   // 可空：空 → fallback 聚落中心

        public bool Contains(int hour)
        {
            if (from <= to)
            {
                return hour >= from && hour < to;
            }
            return hour >= from || hour < to;
        }
    }

    public class LifeRoleDef : Def
    {
        public FacilityTagDef requiredFacility;            // 地圖上沒有此設施 → 不分配此角色
        public List<PawnKindDef> fixedRoleForPawnKinds;    // 這些 pawnKind 一律此角色
        public List<ScheduleEntry> schedule = new List<ScheduleEntry>();

        public ScheduleEntry EntryAt(int hour)
        {
            for (int i = 0; i < schedule.Count; i++)
            {
                if (schedule[i].Contains(hour))
                {
                    return schedule[i];
                }
            }
            return null;
        }
    }
}
```

- [ ] **Step 2: LifeProfileDef.cs（派系維度 + assignmentWorker 鉤子）**

```csharp
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.sims
{
    public class LifeRoleEntry
    {
        public LifeRoleDef role;
        public float weight = 1f;
        public int minCount;       // 保底人數（如衛兵至少 2）
    }

    public class LifeProfileDef : Def
    {
        public List<FactionDef> factionDefs;       // 明示匹配派系
        public List<TechLevel> techLevels;         // 按科技層匹配
        public bool isDefault;                     // 全域 fallback
        public List<LifeRoleEntry> roles = new List<LifeRoleEntry>();
        public Type assignmentWorker = typeof(RoleAssignmentWorker);   // 可被 patch 換實作

        [Unsaved] private RoleAssignmentWorker workerInt;

        public RoleAssignmentWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (RoleAssignmentWorker)Activator.CreateInstance(assignmentWorker);
                }
                return workerInt;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            if (roles.NullOrEmpty())
            {
                yield return "LifeProfileDef " + defName + " has no roles.";
            }
            if (assignmentWorker != null && !typeof(RoleAssignmentWorker).IsAssignableFrom(assignmentWorker))
            {
                yield return "LifeProfileDef " + defName + " assignmentWorker is not a RoleAssignmentWorker.";
            }
        }
    }
}
```

- [ ] **Step 3: SimsDefOf.cs**

```csharp
using RimWorld;
using Verse;

namespace pas.sims
{
    [DefOf]
    public static class SimsDefOf
    {
        public static JobDef pas_sims_FakeWork;

        static SimsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SimsDefOf));
        }
    }
}
```

- [ ] **Step 4: 兩個暫時殼（讓本 task 可編譯，後續 task 取代）**

`Source/Assign/RoleAssignmentWorker.cs`：

```csharp
using System.Collections.Generic;
using Verse;

namespace pas.sims
{
    public class RoleAssignmentWorker
    {
        public virtual Dictionary<Pawn, LifeRoleDef> Assign(List<Pawn> pawns, LifeProfileDef profile, Map map, MapComponent_FacilityRegistry registry)
        {
            return new Dictionary<Pawn, LifeRoleDef>();
        }
    }
}
```

`Source/Facility/MapComponent_FacilityRegistry.cs`：

```csharp
using Verse;

namespace pas.sims
{
    public class MapComponent_FacilityRegistry : MapComponent
    {
        public MapComponent_FacilityRegistry(Map map) : base(map) { }
    }
}
```

- [ ] **Step 5: 建置驗證**

Run: `dotnet build sims-mode-community/Source/SimsModeCommunity.csproj -c Release`
Expected: Build succeeded。

- [ ] **Step 6: Commit**

```
git add sims-mode-community/Source sims-mode-community/1.6
git commit -m "feat: 四層資料模型 Def 類（FacilityTag/LifeRole/LifeProfile + matcher 管線）"
```
