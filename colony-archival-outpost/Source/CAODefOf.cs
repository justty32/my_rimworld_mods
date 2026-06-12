using RimWorld;
using Verse;

namespace ColonyArchivalOutpost
{
    [DefOf]
    public static class CAODefOf
    {
        public static ThingDef CAO_PowerSamplingNode;
        public static ThingDef CAO_PowerOutlet;

        static CAODefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CAODefOf));
        }
    }
}
