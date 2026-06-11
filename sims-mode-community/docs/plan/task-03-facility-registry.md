# Task 3: 設施註冊表（MapComponent）+ Facilities.xml

> 屬於 `../2026-06-11-implementation-plan.md`。

**Files:**
- Modify: `sims-mode-community/Source/Facility/MapComponent_FacilityRegistry.cs`（取代 Task 2b 的殼）
- Create: `sims-mode-community/Defs/FacilityTagDefs/Facilities.xml`

- [ ] **Step 1: 實作 MapComponent_FacilityRegistry**

```csharp
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
```

- [ ] **Step 2: 寫 Facilities.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <pas.sims.FacilityTagDef>
    <defName>pas_sims_Bed</defName>
    <matchers>
      <li Class="pas.sims.FacilityMatcher_ThingClass">
        <thingClass>RimWorld.Building_Bed</thingClass>
      </li>
    </matchers>
  </pas.sims.FacilityTagDef>

  <pas.sims.FacilityTagDef>
    <defName>pas_sims_GatherSpot</defName>
    <matchers>
      <li Class="pas.sims.FacilityMatcher_Table" />
      <li Class="pas.sims.FacilityMatcher_DefNames">
        <defNames>
          <li>Campfire</li>
        </defNames>
      </li>
    </matchers>
  </pas.sims.FacilityTagDef>

  <pas.sims.FacilityTagDef>
    <defName>pas_sims_Workbench</defName>
    <matchers>
      <li Class="pas.sims.FacilityMatcher_ThingClass">
        <thingClass>RimWorld.Building_WorkTable</thingClass>
      </li>
    </matchers>
  </pas.sims.FacilityTagDef>

  <pas.sims.FacilityTagDef>
    <defName>pas_sims_FarmPlot</defName>
    <matchers>
      <li Class="pas.sims.FacilityMatcher_Crop" />
    </matchers>
  </pas.sims.FacilityTagDef>

</Defs>
```

- [ ] **Step 3: 建置 + commit**

Run: `dotnet build sims-mode-community/Source/SimsModeCommunity.csproj -c Release` → Build succeeded。

```
git add sims-mode-community/Source sims-mode-community/Defs sims-mode-community/1.6
git commit -m "feat: 設施註冊表 MapComponent + 四個內建 FacilityTagDef"
```
