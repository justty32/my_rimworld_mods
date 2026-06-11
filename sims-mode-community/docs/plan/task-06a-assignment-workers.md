# Task 6a: 角色分配 C#（ProfileResolver + RoleAssignmentWorker）

> 屬於 `../2026-06-11-implementation-plan.md`。

**Files:**
- Create: `sims-mode-community/Source/Assign/ProfileResolver.cs`
- Modify: `sims-mode-community/Source/Assign/RoleAssignmentWorker.cs`（取代 Task 2b 的殼）

- [ ] **Step 1: ProfileResolver.cs（解析鏈：extension → factionDefs → techLevels → default）**

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.sims
{
    /// <summary>派系 → 生活檔案解析鏈。小顆 public static 方法，供 Harmony patch 個別環節。</summary>
    public static class ProfileResolver
    {
        public static LifeProfileDef Resolve(Faction faction)
        {
            if (faction?.def == null)
            {
                return null;
            }
            return ByExtension(faction)
                ?? ByFactionDef(faction)
                ?? ByTechLevel(faction)
                ?? Default();
        }

        public static LifeProfileDef ByExtension(Faction faction)
        {
            return faction.def.GetModExtension<LifeProfileExtension>()?.profile;
        }

        public static LifeProfileDef ByFactionDef(Faction faction)
        {
            List<LifeProfileDef> all = DefDatabase<LifeProfileDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].factionDefs != null && all[i].factionDefs.Contains(faction.def))
                {
                    return all[i];
                }
            }
            return null;
        }

        public static LifeProfileDef ByTechLevel(Faction faction)
        {
            List<LifeProfileDef> all = DefDatabase<LifeProfileDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].techLevels != null && all[i].techLevels.Contains(faction.def.techLevel))
                {
                    return all[i];
                }
            }
            return null;
        }

        public static LifeProfileDef Default()
        {
            List<LifeProfileDef> all = DefDatabase<LifeProfileDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].isDefault)
                {
                    return all[i];
                }
            }
            return null;
        }
    }

    /// <summary>派系 mod 作者在自家 FactionDef 上掛這個 extension 直接指定 profile（解析鏈第一優先）。</summary>
    public class LifeProfileExtension : DefModExtension
    {
        public LifeProfileDef profile;
    }
}
```

- [ ] **Step 2: RoleAssignmentWorker.cs 完整實作（取代殼）**

```csharp
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace pas.sims
{
    /// <summary>預設分配：pawnKind 直綁 → minCount 保底 → 權重隨機。public virtual，profile.assignmentWorker 可整個換掉。</summary>
    public class RoleAssignmentWorker
    {
        public virtual Dictionary<Pawn, LifeRoleDef> Assign(List<Pawn> pawns, LifeProfileDef profile, Map map, MapComponent_FacilityRegistry registry)
        {
            var result = new Dictionary<Pawn, LifeRoleDef>();
            List<LifeRoleEntry> available = profile.roles
                .Where(e => e.role != null && (e.role.requiredFacility == null || registry.Get(e.role.requiredFacility).Count > 0))
                .ToList();
            if (available.Count == 0)
            {
                return result;
            }
            var pool = new List<Pawn>(pawns);

            // 1. pawnKind 直綁
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                Pawn p = pool[i];
                LifeRoleEntry fixedEntry = available.FirstOrDefault(e =>
                    e.role.fixedRoleForPawnKinds != null && e.role.fixedRoleForPawnKinds.Contains(p.kindDef));
                if (fixedEntry != null)
                {
                    result[p] = fixedEntry.role;
                    pool.RemoveAt(i);
                }
            }

            // 2. minCount 保底
            foreach (LifeRoleEntry entry in available)
            {
                int have = result.Values.Count(r => r == entry.role);
                while (have < entry.minCount && pool.Count > 0)
                {
                    Pawn p = pool[pool.Count - 1];
                    pool.RemoveAt(pool.Count - 1);
                    result[p] = entry.role;
                    have++;
                }
            }

            // 3. 權重隨機
            foreach (Pawn p in pool)
            {
                result[p] = available.RandomElementByWeight(e => e.weight).role;
            }
            return result;
        }
    }
}
```

- [ ] **Step 3: 建置驗證**

Run: `dotnet build sims-mode-community/Source/SimsModeCommunity.csproj -c Release` → Build succeeded。
（commit 與 Task 6b 一起。）
