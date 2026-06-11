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
