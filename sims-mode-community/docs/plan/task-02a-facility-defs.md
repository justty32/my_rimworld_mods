# Task 2a: 設施標記 Def 類（matcher 管線）

> 屬於 `../2026-06-11-implementation-plan.md`（索引含權威源座標、測試現實、commit 規則）。

**Files:**
- Create: `sims-mode-community/Source/Defs/FacilityMatcher.cs`
- Create: `sims-mode-community/Source/Defs/FacilityTagDef.cs`

- [ ] **Step 1: FacilityMatcher.cs（抽象 + 四內建，全 public 供外部繼承）**

```csharp
using System;
using System.Collections.Generic;
using Verse;

namespace pas.sims
{
    /// <summary>設施偵測規則。其他 mod 可繼承並在 FacilityTagDef 的 matchers 清單以 Class= 引用。</summary>
    public abstract class FacilityMatcher
    {
        public abstract bool Matches(Thing t);
    }

    public class FacilityMatcher_ThingClass : FacilityMatcher
    {
        public Type thingClass;

        public override bool Matches(Thing t)
        {
            return thingClass != null && thingClass.IsAssignableFrom(t.GetType());
        }
    }

    public class FacilityMatcher_DefNames : FacilityMatcher
    {
        public List<string> defNames = new List<string>();

        public override bool Matches(Thing t)
        {
            return defNames.Contains(t.def.defName);
        }
    }

    /// <summary>栽培作物（聚落農田）：可播種的植物。</summary>
    public class FacilityMatcher_Crop : FacilityMatcher
    {
        public override bool Matches(Thing t)
        {
            return t is Plant && t.def.plant != null && t.def.plant.Sowable;
        }
    }

    /// <summary>桌子（聚會點）。</summary>
    public class FacilityMatcher_Table : FacilityMatcher
    {
        public override bool Matches(Thing t)
        {
            return t.def.IsTable;
        }
    }
}
```

- [ ] **Step 2: FacilityTagDef.cs（含 DefModExtension）**

```csharp
using System.Collections.Generic;
using Verse;

namespace pas.sims
{
    public class FacilityTagDef : Def
    {
        public List<FacilityMatcher> matchers = new List<FacilityMatcher>();
    }

    /// <summary>建築 mod 作者在自家 ThingDef 上掛這個 extension 明示標記，優先於自動偵測。</summary>
    public class FacilityTagExtension : DefModExtension
    {
        public List<FacilityTagDef> tags = new List<FacilityTagDef>();
    }
}
```

- [ ] **Step 3: 先不建置（`LifeProfileDef` 等在 Task 2b 才齊），直接進 Task 2b**
