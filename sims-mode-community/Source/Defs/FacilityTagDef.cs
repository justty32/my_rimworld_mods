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
