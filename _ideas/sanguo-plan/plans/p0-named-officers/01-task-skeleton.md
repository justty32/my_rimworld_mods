# T0 API 驗證 spike ＋ T1 mod 骨架

## T0 — API 驗證（讀碼不寫碼，0.5d）

> 仿 npc-outposts `task-00-api-verification.md` 範式：先在 Krafs ref / 反編譯源確認簽章，
> 把「以為存在」變「驗證存在」，避免 E2E 才踩坑（faction-politics 曾在 Task 0 漏 #7 盲點）。

逐項確認並把行號記進本檔（驗證 = 在 decompile/ref 中找到該成員；
decompile 源：`~/repo/pas/projects/rimworld/`，Krafs ref 1.6.4850。2026-06-12 簽收）：

- [x] `Settlement.previouslyGeneratedInhabitants` 是 public `List<Pawn>`
      （`RimWorld.Planet/Settlement.cs:14`；redress 路徑仍讀它：`Verse/PawnGenerator.cs:212-213`，
      原版唯一寫入點是玩家地圖生成 `:237`——NPC 聚落仍是死碼，mod 為唯一供給者）。
- [x] `Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever)`
      （`RimWorld.Planet/WorldPawns.cs:200`）；`ForcefullyKeptPawns` 為 get-only property
      但回傳可變 `HashSet<Pawn>`（`:71`），`.Add` 可行（faction-politics 既有手法成立）。
- [x] `Faction.RandomPawnKind()` 存在（`RimWorld/Faction.cs:452`）。
- [x] `PawnGenerationRequest` 具名引數建構可行：唯一必填參 `kind`，其餘全有預設值
      （`Verse/PawnGenerationRequest.cs:151`）。
- [x] **人名生成不經 pawn**：`PawnBioAndNameGenerator.GeneratePawnName` 需 pawn（`:297`）；
      `TryGetRandomUnusedSolidName(Gender,…)`（`:219`）可無 pawn 但需預先擲性別且只出 solid name；
      `NameGenerator.GenerateName(RulePackDef…)`（`RimWorld/NameGenerator.cs:11/16`）出字串非人名專用
      → **採 fallback 方案 B**：建 record 時 `nameCached=null` 顯示 role label，首次具現後快取 `pawn.Name`（T5）。
- [x] `PawnRelationDef` 欄位齊：`importance(float):14`/`reflexive(bool):18`/`opinionOffset(int):20`/
      `familyByBloodRelation(bool):28`（`RimWorld/PawnRelationDef.cs`）；
      `Pawn_RelationsTracker.AddDirectRelation`（`:483`）無 Spawned 檢查，僅防 implied/self/duplicate
      （duplicate 只 Log.Warning——呼叫前自查 `DirectRelationExists :422`）；
      預設 `workerClass = typeof(PawnRelationWorker)`（`PawnRelationDef.cs:9`），
      `OnRelationCreated` 為 no-op（`PawnRelationWorker.cs:74`）——XML 不填 worker 安全。
- [x] `[DebugAction]` 在 `LudeonTK`（`LudeonTK/DebugActionAttribute.cs:8`，
      `allowedGameStates` 欄位 `:14` 預設即 Playing）；`Find.WorldSelector.SingleSelectedObject`
      public `WorldObject` property（`RimWorld.Planet/WorldSelector.cs:79`）。
- [x] `WorldComponent.FinalizeInit(bool fromLoad)` 1.6 帶參簽章
      （`RimWorld.Planet/WorldComponent.cs:31` virtual）。
- 加驗（T7 view comp 用）：`WorldObjectComp.CompInspectStringExtra()` virtual
  （`RimWorld.Planet/WorldObjectComp.cs:64`）、`WorldObjectCompProperties.compClass`
  （`RimWorld/WorldObjectCompProperties.cs:11`）。

**結論：8/8 全過，無一落空，不需回改任務設計。名字策略定案方案 B。**

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
