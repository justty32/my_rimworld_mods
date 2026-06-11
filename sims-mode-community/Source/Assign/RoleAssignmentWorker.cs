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
