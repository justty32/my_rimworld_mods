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
