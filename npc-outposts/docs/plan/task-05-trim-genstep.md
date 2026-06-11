# Task 5: GenStep_TrimDefenders + 內容 Defs XML（GenStep/Type/Profile）

**Files:**
- Create: `npc-outposts/Source/MapGen/GenStep_TrimDefenders.cs`
- Create: `npc-outposts/Defs/GenStepDefs/GenSteps.xml`
- Create: `npc-outposts/Defs/OutpostTypeDefs/Types.xml`
- Create: `npc-outposts/Defs/OutpostProfileDefs/Profiles.xml`

- [ ] **Step 1: GenStep_TrimDefenders.cs**

守軍點數無 XML 鉤子（`SymbolResolver_Settlement.cs:58` 預設 1150-1600），生成後按比例削減。order 9990 → 跑在 sims-mode `GenStep_SettlementLife`（9999）之前，角色分配看到的是削減後人口。Destroy 會自動通知 Lord 移除成員。

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace pas.outposts
{
    /// <summary>哨站地圖生成後，按 OutpostTypeDef.defenderPointsFactor 比例削減守軍人數。
    /// 僅對 NpcOutpost 地圖生效（經 ExtraGenStepDefs 注入，其他地圖不會跑到）。</summary>
    public class GenStep_TrimDefenders : GenStep
    {
        public override int SeedPart => 612873451;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!(map.Parent is NpcOutpost outpost) || outpost.Faction == null)
            {
                return;
            }
            float factor = outpost.TypeDef?.defenderPointsFactor ?? 1f;
            if (factor >= 1f)
            {
                return;
            }
            List<Pawn> defenders = map.mapPawns.SpawnedPawnsInFaction(outpost.Faction)
                .Where(p => p.RaceProps.Humanlike).ToList();
            int keep = Mathf.Max(1, Mathf.CeilToInt(defenders.Count * factor));
            int removeCount = defenders.Count - keep;
            for (int i = 0; i < removeCount; i++)
            {
                Pawn victim = defenders.RandomElement();
                defenders.Remove(victim);
                victim.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
```

- [ ] **Step 2: Defs/GenStepDefs/GenSteps.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <GenStepDef>
    <defName>pas_outposts_TrimDefenders</defName>
    <order>9990</order>
    <genStep Class="pas.outposts.GenStep_TrimDefenders" />
  </GenStepDef>
</Defs>
```

- [ ] **Step 3: Defs/OutpostTypeDefs/Types.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <pas.outposts.OutpostTypeDef>
    <defName>pas_outposts_Type_Generic</defName>
    <label>generic outpost</label>
    <worldObjectDef>pas_outposts_Outpost</worldObjectDef>
    <mapSize>(150,1,150)</mapSize>
    <defenderPointsFactor>0.4</defenderPointsFactor>
  </pas.outposts.OutpostTypeDef>
</Defs>
```

（`mapGeneratorDef` 不寫 → null → 沿用 Settlement 的 `Base_Faction`，sims-mode patch 自動生效。）

- [ ] **Step 4: Defs/OutpostProfileDefs/Profiles.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <pas.outposts.OutpostProfileDef>
    <defName>pas_outposts_Profile_Default</defName>
    <isDefault>true</isDefault>
    <countPerSettlement>1~3</countPerSettlement>
    <radius>2~4</radius>
    <spawnMtbDays>15</spawnMtbDays>
    <types>
      <li>
        <type>pas_outposts_Type_Generic</type>
        <weight>1</weight>
      </li>
    </types>
  </pas.outposts.OutpostProfileDef>
</Defs>
```

- [ ] **Step 5: 建置驗證**

```powershell
dotnet build C:\code\mine\my_rimworld_mods\npc-outposts\Source\NpcOutposts.csproj -c Release
```
Expected: 0 警告 0 錯誤。

- [ ] **Step 6: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add npc-outposts/Source npc-outposts/Defs
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: 守軍 trim GenStep + 泛用哨站 Type/Profile Defs

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
