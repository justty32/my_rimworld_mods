# Task 7: npc-outposts 條件 assembly bridge（哨站跟隨倒戈）

**Files:**
- Create: `faction-politics/SourceBridgeOutposts/FactionPoliticsOutpostsBridge.csproj`
- Create: `faction-politics/SourceBridgeOutposts/OutpostsBridge.cs`

機制（可行性 `04`）：`Compat/NpcOutposts/` 只在 `pas.outposts.community` 啟用時被 loadFolders 載入（task-1 已宣告）。bridge DLL 編譯期引用我們自己的 `NpcOutposts.dll`（同 repo，簽名有保證）與主 DLL。**主 `Source/` 維持不認識 `pas.outposts`**。

- [ ] **Step 1: SourceBridgeOutposts/FactionPoliticsOutpostsBridge.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>FactionPoliticsOutpostsBridge</AssemblyName>
    <RootNamespace>pas.politics.outposts</RootNamespace>
    <OutputPath>..\Compat\NpcOutposts\Assemblies\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <GenerateDependencyFile>false</GenerateDependencyFile>
    <DebugType>none</DebugType>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Krafs.Rimworld.Ref" Version="1.6.*" />
    <Reference Include="FactionPolitics">
      <HintPath>..\1.6\Assemblies\FactionPolitics.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="NpcOutposts">
      <HintPath>..\..\npc-outposts\1.6\Assemblies\NpcOutposts.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

- [ ] **Step 2: SourceBridgeOutposts/OutpostsBridge.cs**

```csharp
using System.Collections.Generic;
using pas.outposts;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.politics.outposts
{
    /// <summary>npc-outposts 啟用時：衛星哨站不被當倒戈對象/駐地/聚落數，且跟隨母聚落易主。</summary>
    [StaticConstructorOnStartup]
    public static class OutpostsBridge
    {
        static OutpostsBridge()
        {
            PoliticsBridges.IsSatelliteResolver = (Settlement s) => s is NpcOutpost;
            PoliticsBridges.SettlementDefected += OnSettlementDefected;
        }

        private static void OnSettlementDefected(Settlement defector, Faction mother, Faction newFaction)
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (all[i] is NpcOutpost outpost && outpost.ParentSettlement == defector
                    && outpost.Faction == mother)
                {
                    outpost.SetFaction(newFaction);
                }
            }
        }
    }
}
```

- [ ] **Step 3: 雙建置驗證**

Run:
```powershell
dotnet build C:\code\mine\my_rimworld_mods\faction-politics\Source\FactionPolitics.csproj
dotnet build C:\code\mine\my_rimworld_mods\faction-politics\SourceBridgeOutposts\FactionPoliticsOutpostsBridge.csproj
```
Expected: 各 0 Warning(s) 0 Error(s)；產出 `faction-politics/Compat/NpcOutposts/Assemblies/FactionPoliticsOutpostsBridge.dll`

- [ ] **Step 4: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add faction-politics/SourceBridgeOutposts faction-politics/Compat
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: faction-politics npc-outposts 條件 bridge（哨站跟隨倒戈）

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
