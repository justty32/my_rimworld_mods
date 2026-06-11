# Task 1: Mod 骨架（About + csproj + 空建置）

> 屬於 `../2026-06-11-implementation-plan.md`（索引含權威源座標、測試現實、commit 規則）。

**Files:**
- Create: `sims-mode-community/About/About.xml`
- Create: `sims-mode-community/Source/SimsModeCommunity.csproj`
- Create: `sims-mode-community/session_log.md`

- [ ] **Step 1: 寫 About.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <packageId>pas.sims.community</packageId>
  <name>Sims Mode Community: Living Settlements</name>
  <author>GuanYu Lu</author>
  <supportedVersions>
    <li>1.6</li>
  </supportedVersions>
  <description>Visit a non-hostile faction settlement and its people actually live: working by day, gathering in the evening, sleeping at night. Fully data-driven (faction profiles, roles, schedules, facility tags) so other mods can patch everything. Phase 1 of the Sims Mode Community project.</description>
</ModMetaData>
```

- [ ] **Step 2: 寫 csproj（Krafs 參考組件，免遊戲安裝）**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>SimsModeCommunity</AssemblyName>
    <RootNamespace>pas.sims</RootNamespace>
    <OutputPath>..\1.6\Assemblies\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
    <LangVersion>latest</LangVersion>
    <DebugType>none</DebugType>
    <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Krafs.Rimworld.Ref" Version="1.6.*" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: 建置驗證**

Run: `dotnet build sims-mode-community/Source/SimsModeCommunity.csproj -c Release`
Expected: Build succeeded，產出 `sims-mode-community/1.6/Assemblies/SimsModeCommunity.dll`。
（若 `Krafs.Rimworld.Ref 1.6.*` 還原失敗，改 `Version="1.6.4518"` 或 `dotnet package search Krafs.Rimworld.Ref` 找最新 1.6 版。）

- [ ] **Step 4: 建 session_log.md（一行：日期 + Task 1 完成）並 commit**

```
git add sims-mode-community/About sims-mode-community/Source sims-mode-community/session_log.md sims-mode-community/1.6
git commit -m "feat: mod 骨架（About + csproj + Krafs 參考組件建置）"
```
