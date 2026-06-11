# Task 2: Def 層（TypeDef / ProfileDef / DefOf / Resolver）

**Files:**
- Create: `npc-outposts/Source/Defs/OutpostTypeDef.cs`
- Create: `npc-outposts/Source/Defs/OutpostProfileDef.cs`
- Create: `npc-outposts/Source/OutpostDefOf.cs`
- Create: `npc-outposts/Source/Assign/OutpostProfileResolver.cs`

- [ ] **Step 1: OutpostTypeDef.cs**

```csharp
using RimWorld;
using Verse;

namespace pas.outposts
{
    public class OutpostTypeDef : Def
    {
        public WorldObjectDef worldObjectDef;
        public IntVec3 mapSize = new IntVec3(150, 1, 150);
        public float defenderPointsFactor = 0.4f;
        public MapGeneratorDef mapGeneratorDef;   // null = 沿用 Settlement.MapGeneratorDef（Base_Faction）
    }
}
```

- [ ] **Step 2: OutpostProfileDef.cs**

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.outposts
{
    public class OutpostTypeEntry
    {
        public OutpostTypeDef type;
        public float weight = 1f;
    }

    public class OutpostProfileDef : Def
    {
        public List<FactionDef> factionDefs;
        public List<TechLevel> techLevels;
        public bool isDefault;
        public IntRange countPerSettlement = new IntRange(1, 3);
        public IntRange radius = new IntRange(2, 4);
        public float spawnMtbDays = 15f;
        public List<OutpostTypeEntry> types;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors()) yield return e;
            if (types.NullOrEmpty()) yield return "no types";
        }
    }

    /// <summary>掛在 FactionDef 上指定 profile（解析鏈最高優先）。</summary>
    public class OutpostProfileExtension : DefModExtension
    {
        public OutpostProfileDef profile;
    }

    /// <summary>掛在 FactionDef 上停用該派系的哨站。</summary>
    public class OutpostDisabledExtension : DefModExtension
    {
    }
}
```

- [ ] **Step 3: OutpostDefOf.cs**

```csharp
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
```

（GenStepDef 在 Task 5 才建 XML——DefOf 在 def 載入後才解析，建置不受影響；Task 5 前勿實機載入。）

- [ ] **Step 4: OutpostProfileResolver.cs**

與 sims-mode `ProfileResolver` 同模式（小 public static 方法，Harmony 好 patch），資料源不同故自寫不共用：

```csharp
using System.Linq;
using RimWorld;
using Verse;

namespace pas.outposts
{
    /// <summary>派系 → OutpostProfileDef。鏈：Disabled → null；Extension > factionDefs > techLevels > isDefault。</summary>
    public static class OutpostProfileResolver
    {
        public static OutpostProfileDef Resolve(Faction faction)
        {
            if (faction == null || faction.def.HasModExtension<OutpostDisabledExtension>())
            {
                return null;
            }
            return ByExtension(faction) ?? ByFactionDef(faction) ?? ByTechLevel(faction) ?? Default();
        }

        public static OutpostProfileDef ByExtension(Faction faction)
        {
            return faction.def.GetModExtension<OutpostProfileExtension>()?.profile;
        }

        public static OutpostProfileDef ByFactionDef(Faction faction)
        {
            return DefDatabase<OutpostProfileDef>.AllDefsListForReading
                .FirstOrDefault(p => p.factionDefs != null && p.factionDefs.Contains(faction.def));
        }

        public static OutpostProfileDef ByTechLevel(Faction faction)
        {
            return DefDatabase<OutpostProfileDef>.AllDefsListForReading
                .FirstOrDefault(p => p.techLevels != null && p.techLevels.Contains(faction.def.techLevel));
        }

        public static OutpostProfileDef Default()
        {
            return DefDatabase<OutpostProfileDef>.AllDefsListForReading.FirstOrDefault(p => p.isDefault);
        }
    }
}
```

- [ ] **Step 5: 建置驗證**

```powershell
dotnet build C:\code\mine\my_rimworld_mods\npc-outposts\Source\NpcOutposts.csproj -c Release
```
Expected: 0 警告 0 錯誤。

- [ ] **Step 6: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add npc-outposts/Source
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: npc-outposts Def 層（TypeDef/ProfileDef/DefOf/resolver）

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
