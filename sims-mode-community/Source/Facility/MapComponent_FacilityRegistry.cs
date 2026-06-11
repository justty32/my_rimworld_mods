using System.Collections.Generic;
using Verse;

namespace pas.sims
{
    /// <summary>設施標記唯一資料源。不存檔：載入後/生成時呼叫 RebuildAll 重掃。</summary>
    public class MapComponent_FacilityRegistry : MapComponent
    {
        private readonly Dictionary<FacilityTagDef, List<Thing>> facilities = new Dictionary<FacilityTagDef, List<Thing>>();
        private bool built;

        public MapComponent_FacilityRegistry(Map map) : base(map) { }

        public void RebuildAll()
        {
            facilities.Clear();
            List<FacilityTagDef> tags = DefDatabase<FacilityTagDef>.AllDefsListForReading;
            for (int i = 0; i < tags.Count; i++)
            {
                facilities[tags[i]] = new List<Thing>();
            }
            List<Thing> all = map.listerThings.AllThings;
            for (int i = 0; i < all.Count; i++)
            {
                Thing t = all[i];
                FacilityTagExtension ext = t.def.GetModExtension<FacilityTagExtension>();
                for (int j = 0; j < tags.Count; j++)
                {
                    if (MatchesTag(t, tags[j], ext))
                    {
                        facilities[tags[j]].Add(t);
                    }
                }
            }
            built = true;
        }

        /// <summary>明示 extension 優先於自動偵測。public virtual 供外部覆寫/patch。</summary>
        public virtual bool MatchesTag(Thing t, FacilityTagDef tag, FacilityTagExtension ext)
        {
            if (ext != null)
            {
                return ext.tags.Contains(tag);
            }
            for (int i = 0; i < tag.matchers.Count; i++)
            {
                if (tag.matchers[i].Matches(t))
                {
                    return true;
                }
            }
            return false;
        }

        public List<Thing> Get(FacilityTagDef tag)
        {
            if (!built)
            {
                RebuildAll();
            }
            if (facilities.TryGetValue(tag, out List<Thing> list))
            {
                list.RemoveAll(t => t.DestroyedOrNull() || !t.Spawned);
                return list;
            }
            return new List<Thing>();
        }

        public void Register(FacilityTagDef tag, Thing t)
        {
            if (!built)
            {
                RebuildAll();
            }
            if (!facilities.TryGetValue(tag, out List<Thing> list))
            {
                list = new List<Thing>();
                facilities[tag] = list;
            }
            if (!list.Contains(t))
            {
                list.Add(t);
            }
        }

        public void Unregister(FacilityTagDef tag, Thing t)
        {
            if (facilities.TryGetValue(tag, out List<Thing> list))
            {
                list.Remove(t);
            }
        }
    }
}
