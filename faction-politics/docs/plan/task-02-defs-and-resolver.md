# Task 2: Def 體系 + Resolver + XML Defs

**Files:**
- Create: `faction-politics/Source/Defs/RebellionProfileDef.cs`
- Create: `faction-politics/Source/Assign/RebellionProfileResolver.cs`
- Create: `faction-politics/Defs/PoliticsDefs/Settings.xml`
- Create: `faction-politics/Defs/PoliticsDefs/Profiles.xml`

- [ ] **Step 1: Source/Defs/RebellionProfileDef.cs**

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.politics
{
    /// <summary>派系如何反叛。解析鏈見 RebellionProfileResolver。</summary>
    public class RebellionProfileDef : Def
    {
        public List<FactionDef> factionDefs;
        public List<TechLevel> techLevels;
        public bool isDefault;
        /// <summary>每日反叛進展（每個反叛者生成時擲定一次，存進 record）。</summary>
        public FloatRange progressPerDay = new FloatRange(0.2f, 0.6f);
        public float threshold = 100f;
        /// <summary>倒戈聚落比例（母派系保底留 1 個）。</summary>
        public FloatRange defectFraction = new FloatRange(0.3f, 0.5f);
        /// <summary>反叛者死後重生冷卻（天）。</summary>
        public float respawnDelayDays = 20f;
        /// <summary>派系至少幾個聚落才養反叛者（<2 會讓分裂無聚落可分）。</summary>
        public int minSettlements = 2;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (threshold <= 0f)
            {
                yield return "threshold must be > 0";
            }
            if (minSettlements < 2)
            {
                yield return "minSettlements must be >= 2";
            }
            if (defectFraction.min <= 0f || defectFraction.max >= 1f)
            {
                yield return "defectFraction must be within (0,1)";
            }
        }
    }

    /// <summary>全域設定，恰好 1 個實例（健檢把關）。</summary>
    public class PoliticsSettingsDef : Def
    {
        public int maxDynamicFactions = 5;
        public int checkIntervalTicks = 2500;
    }

    /// <summary>FactionDef 直綁 profile（解析鏈最高優先）。</summary>
    public class PoliticsProfileExtension : DefModExtension
    {
        public RebellionProfileDef profile;
    }

    /// <summary>停用某派系的反叛系統。</summary>
    public class PoliticsDisabledExtension : DefModExtension
    {
    }
}
```

- [ ] **Step 2: Source/Assign/RebellionProfileResolver.cs**（小 public static 方法，沿 npc-outposts 模式）

```csharp
using RimWorld;
using Verse;

namespace pas.politics
{
    /// <summary>解析鏈：Disabled→null；Extension ?? FactionDef ?? TechLevel ?? Default。</summary>
    public static class RebellionProfileResolver
    {
        public static RebellionProfileDef Resolve(Faction faction)
        {
            if (faction?.def == null || Disabled(faction))
            {
                return null;
            }
            return ByExtension(faction) ?? ByFactionDef(faction) ?? ByTechLevel(faction) ?? Default();
        }

        public static bool Disabled(Faction faction)
        {
            return faction.def.HasModExtension<PoliticsDisabledExtension>();
        }

        public static RebellionProfileDef ByExtension(Faction faction)
        {
            return faction.def.GetModExtension<PoliticsProfileExtension>()?.profile;
        }

        public static RebellionProfileDef ByFactionDef(Faction faction)
        {
            foreach (RebellionProfileDef def in DefDatabase<RebellionProfileDef>.AllDefsListForReading)
            {
                if (def.factionDefs != null && def.factionDefs.Contains(faction.def))
                {
                    return def;
                }
            }
            return null;
        }

        public static RebellionProfileDef ByTechLevel(Faction faction)
        {
            foreach (RebellionProfileDef def in DefDatabase<RebellionProfileDef>.AllDefsListForReading)
            {
                if (def.techLevels != null && def.techLevels.Contains(faction.def.techLevel))
                {
                    return def;
                }
            }
            return null;
        }

        public static RebellionProfileDef Default()
        {
            foreach (RebellionProfileDef def in DefDatabase<RebellionProfileDef>.AllDefsListForReading)
            {
                if (def.isDefault)
                {
                    return def;
                }
            }
            return null;
        }
    }
}
```

- [ ] **Step 3: Defs/PoliticsDefs/Settings.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <pas.politics.PoliticsSettingsDef>
    <defName>pas_politics_Settings</defName>
    <label>faction politics settings</label>
    <maxDynamicFactions>5</maxDynamicFactions>
    <checkIntervalTicks>2500</checkIntervalTicks>
  </pas.politics.PoliticsSettingsDef>
</Defs>
```

- [ ] **Step 4: Defs/PoliticsDefs/Profiles.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <pas.politics.RebellionProfileDef>
    <defName>pas_politics_Profile_Default</defName>
    <label>default rebellion profile</label>
    <isDefault>true</isDefault>
    <progressPerDay>0.2~0.6</progressPerDay>
    <threshold>100</threshold>
    <defectFraction>0.3~0.5</defectFraction>
    <respawnDelayDays>20</respawnDelayDays>
    <minSettlements>2</minSettlements>
  </pas.politics.RebellionProfileDef>
</Defs>
```

- [ ] **Step 5: 建置驗證**

Run: `dotnet build C:\code\mine\my_rimworld_mods\faction-politics\Source\FactionPolitics.csproj`
Expected: 0 Warning(s) 0 Error(s)

- [ ] **Step 6: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add faction-politics/Source faction-politics/Defs faction-politics/1.6
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: faction-politics Def 體系 + profile resolver

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
