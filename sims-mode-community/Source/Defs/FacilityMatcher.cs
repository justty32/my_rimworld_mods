using System;
using System.Collections.Generic;
using RimWorld;
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
