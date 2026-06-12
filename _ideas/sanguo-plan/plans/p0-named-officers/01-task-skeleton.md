# T0 API 驗證 spike ＋ T1 mod 骨架

## T0 — API 驗證（讀碼不寫碼，0.5d）

> 仿 npc-outposts `task-00-api-verification.md` 範式：先在 Krafs ref / 反編譯源確認簽章，
> 把「以為存在」變「驗證存在」，避免 E2E 才踩坑（faction-politics 曾在 Task 0 漏 #7 盲點）。

逐項確認並把行號記進本檔（驗證 = 在 decompile/ref 中找到該成員）：

- [ ] `Settlement.previouslyGeneratedInhabitants` 是 public `List<Pawn>`（1.6 仍為「原版死碼、mod 唯一供給者」，
      見 `RebelSpawner.cs:8-9` 註解；確認 redress 路徑 `PawnGenerator.cs` 仍讀它）。
- [ ] `Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever)` 與
      `ForcefullyKeptPawns` 公開可寫（自癒手法，`WorldComponent_RebellionTracker.cs:127-137`）。
- [ ] `Faction.RandomPawnKind()` 存在（NPC 派系 `basicMemberKind` 全 null 的對策，`RebelSpawner.cs:73-75`）。
- [ ] `PawnGenerationRequest` 具名引數建構（跨版本欄位擴充，必須具名，`RebelSpawner.cs:80-82`）。
- [ ] **人名生成不經 pawn 的途徑**：確認 `PawnBioAndNameGenerator.GeneratePawnName(...)` 或
      `NameGenerator.GenerateName(RulePackDef …)` 哪個能在無 pawn 時產人名；若皆需 pawn，
      fallback 方案=「首次具現前顯示 role label，具現時快取 `pawn.Name`」（T5 用）。
- [ ] `PawnRelationDef` 最小欄位集：`defName/label/opinionOffset/reflexive(對稱)/familyByBloodRelation`；
      `Pawn_RelationsTracker.AddDirectRelation` 無 Spawned 檢查（調查 I:147 已證，複核 1.6）。
- [ ] `[DebugAction]`（LudeonTK）`allowedGameStates = AllowedGameStates.Playing` 下能取
      `Find.WorldSelector.SingleSelectedObject` 操作選中世界物件（T8 用）。
- [ ] `WorldComponent.FinalizeInit(bool fromLoad)` 簽章（1.6 帶參，`WorldComponent_RebellionTracker.cs:23`）。

**驗證步驟**：每項在本檔打勾並附「檔案:行」；任何一項落空 → 回 `00-overview.md` 改對應任務設計再動工。

## T1 — mod 骨架（0.25d）

**Create:**
- `named-officers/About/About.xml`
- `named-officers/Source/NamedOfficers.csproj`
- `named-officers/.gitignore`（內容：`obj/`）

### About.xml（零相依——對照 npc-outposts 拿掉 modDependencies）

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <packageId>pas.officers.community</packageId>
  <name>Named Officers (Community)</name>
  <author>justty32</author>
  <supportedVersions><li>1.6</li></supportedVersions>
  <description>Foundation library: named officer pawns (warlords, governors, generals)
attached to world objects, with stats, relations, and lazy pawn materialization.
No gameplay by itself; consumed by the sanguo mod family. No Harmony, no hard deps.</description>
</ModMetaData>
```

> 不寫 loadFolders.xml（P0 無 Compat 資料夾；root Defs + `1.6/Assemblies` 走原版自動解析，npc-outposts 先例）。
> 消費 mod 之後自行 `loadAfter: pas.officers.community`。

### csproj（仿 `FactionPolitics.csproj`，無任何 mod DLL ref）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>NamedOfficers</AssemblyName>
    <RootNamespace>pas.officers</RootNamespace>
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
  </ItemGroup>
</Project>
```

### 驗證

```bash
dotnet build ~/repo/my_rimworld_mods/named-officers/Source/NamedOfficers.csproj -c Release
```

Expected：0 警告 0 錯誤；產出 `named-officers/1.6/Assemblies/NamedOfficers.dll`；
資料夾內**沒有**任何第三方 DLL（零硬相依驗證）。

### Commit（執行期才做，依 npc-outposts task 範式）

`feat: named-officers 骨架（About + csproj，零硬相依）`
