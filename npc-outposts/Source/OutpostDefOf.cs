using RimWorld;
using Verse;

namespace pas.outposts
{
    [DefOf]
    public static class OutpostDefOf
    {
        public static GenStepDef pas_outposts_TrimDefenders;

        static OutpostDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(OutpostDefOf));
        }
    }
}
