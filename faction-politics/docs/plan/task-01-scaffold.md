# Task 1: 腳手架（About / loadFolders / 主 csproj）

**Files:**
- Create: `faction-politics/About/About.xml`
- Create: `faction-politics/loadFolders.xml`
- Create: `faction-politics/Source/FactionPolitics.csproj`

- [ ] **Step 1: About/About.xml**

注意：**無 modDependencies**（零硬相依是本案不變式）；loadAfter 列兩個姊妹 mod 確保 bridge 載入順序。

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <name>Faction Politics (Community)</name>
  <author>justty32</author>
  <packageId>pas.politics.community</packageId>
  <supportedVersions>
    <li>1.6</li>
  </supportedVersions>
  <loadAfter>
    <li>pas.sims.community</li>
    <li>pas.outposts.community</li>
  </loadAfter>
  <description>Named rebel NPCs rise inside NPC factions. Their rebellion grows over time; at the breaking point the faction splits — settlements (and their satellite outposts) defect to a new hostile faction led by the rebel.\n\nVisit a faction's settlement to meet its rebel in person; kill them to crush the plot (a new schemer will rise later). Works on existing saves. No Harmony, no hard dependencies.\n\nRecommended companions: Sims Mode (Community) for peaceful settlement visits, NPC Outposts (Community) for satellite outposts that defect together with their parent settlement. Soft-compatible with Rim War (bridge skeleton; full sync pending calibration).</description>
</ModMetaData>
```

- [ ] **Step 2: loadFolders.xml**

`Compat/NpcOutposts` 只在 npc-outposts 啟用時載入（`IfModActive` 機制：Verse\ModLoadFolders.cs:53）。資料夾本身 task-7 才會有內容，先宣告不報錯（RimWorld 對不存在的 loadFolder 條目只忽略）——若實測會噴 log，將該 li 移到 task-7 再加（記 session_log）。

```xml
<?xml version="1.0" encoding="utf-8"?>
<loadFolders>
  <v1.6>
    <li>/</li>
    <li>1.6</li>
    <li IfModActive="pas.outposts.community">Compat/NpcOutposts</li>
  </v1.6>
</loadFolders>
```

- [ ] **Step 3: Source/FactionPolitics.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>FactionPolitics</AssemblyName>
    <RootNamespace>pas.politics</RootNamespace>
    <OutputPath>..\1.6\Assemblies\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <GenerateDependencyFile>false</GenerateDependencyFile>
    <DebugType>none</DebugType>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Krafs.Rimworld.Ref" Version="1.6.*" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: 建置驗證**

Run: `dotnet build C:\code\mine\my_rimworld_mods\faction-politics\Source\FactionPolitics.csproj`
Expected: Build succeeded. 0 Warning(s) 0 Error(s)；產出 `faction-politics/1.6/Assemblies/FactionPolitics.dll`

- [ ] **Step 5: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add faction-politics/About faction-politics/loadFolders.xml faction-politics/Source faction-politics/1.6
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: faction-politics 腳手架（About/loadFolders/csproj）

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
