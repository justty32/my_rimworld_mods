# Task 1: Mod 骨架（About + csproj + 空建置）

**Files:**
- Create: `npc-outposts/About/About.xml`
- Create: `npc-outposts/Source/NpcOutposts.csproj`
- Create: `npc-outposts/.gitignore`

- [ ] **Step 1: About.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <packageId>pas.outposts.community</packageId>
  <name>NPC Outposts (Community)</name>
  <author>GuanYu Lu</author>
  <supportedVersions>
    <li>1.6</li>
  </supportedVersions>
  <modDependencies>
    <li>
      <packageId>pas.sims.community</packageId>
      <displayName>Sims Mode Community</displayName>
    </li>
  </modDependencies>
  <loadAfter>
    <li>pas.sims.community</li>
  </loadAfter>
  <description>NPC factions build satellite outposts around their settlements. Visit them (small living maps via Sims Mode), trade with them, or raid them. The world grows new outposts over time.</description>
</ModMetaData>
```

- [ ] **Step 2: csproj（Krafs + sims-mode DLL 引用）**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>NpcOutposts</AssemblyName>
    <RootNamespace>pas.outposts</RootNamespace>
    <OutputPath>..\1.6\Assemblies\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <GenerateDependencyFile>false</GenerateDependencyFile>
    <DebugType>none</DebugType>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Krafs.Rimworld.Ref" Version="1.6.*">
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
    <Reference Include="SimsModeCommunity">
      <HintPath>..\..\sims-mode-community\1.6\Assemblies\SimsModeCommunity.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

`Private=false`：不把 sims-mode 的 DLL 複製進本 mod 的 Assemblies（執行期由 RimWorld 載入順序提供）。

- [ ] **Step 3: .gitignore**

```
obj/
```

- [ ] **Step 4: 建置驗證**

```powershell
dotnet build C:\code\mine\my_rimworld_mods\sims-mode-community\Source\SimsModeCommunity.csproj -c Release
dotnet build C:\code\mine\my_rimworld_mods\npc-outposts\Source\NpcOutposts.csproj -c Release
```
Expected: 兩個都 0 警告 0 錯誤；產出 `npc-outposts/1.6/Assemblies/NpcOutposts.dll`，且該資料夾**沒有** SimsModeCommunity.dll。

- [ ] **Step 5: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add npc-outposts/About npc-outposts/Source/NpcOutposts.csproj npc-outposts/.gitignore
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: npc-outposts 骨架（About + csproj，硬相依 sims-mode）

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
