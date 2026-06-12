using RimWorld;
using Verse;

namespace pas.officers
{
    [DefOf]
    public static class OfficersDefOf
    {
        /// <summary>P0 通用測試角色；領主/將領角色由 P1/P2 各自以 XML 增訂。</summary>
        public static OfficerRoleDef pas_officers_Generic;

        /// <summary>A 軌：結拜（對稱，opinion bias +60）。</summary>
        public static PawnRelationDef pas_officers_SwornBrother;

        /// <summary>A 軌：世仇（對稱，opinion bias -60）。</summary>
        public static PawnRelationDef pas_officers_BloodFeud;

        static OfficersDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(OfficersDefOf));
        }
    }
}
