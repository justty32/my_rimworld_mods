# Task 6b: Roles.xml + Profiles.xml（預設內容）

> 屬於 `../2026-06-11-implementation-plan.md`。接續 Task 6a。

**Files:**
- Create: `sims-mode-community/Defs/LifeRoleDefs/Roles.xml`
- Create: `sims-mode-community/Defs/LifeProfileDefs/Profiles.xml`

- [ ] **Step 1: Roles.xml（四個預設角色，作息表完整版）**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- 衛兵：全天駐守聚落中心（快反部隊，被襲擊不用集結） -->
  <pas.sims.LifeRoleDef>
    <defName>pas_sims_Guard</defName>
    <schedule>
      <li>
        <from>0</from>
        <to>24</to>
        <duty>pas_sims_Duty_Guard</duty>
      </li>
    </schedule>
  </pas.sims.LifeRoleDef>

  <!-- 工人：白天工作台、傍晚聚會、夜間睡床 -->
  <pas.sims.LifeRoleDef>
    <defName>pas_sims_Worker</defName>
    <requiredFacility>pas_sims_Workbench</requiredFacility>
    <schedule>
      <li>
        <from>7</from>
        <to>17</to>
        <duty>pas_sims_Duty_FakeWork</duty>
        <focusFacility>pas_sims_Workbench</focusFacility>
      </li>
      <li>
        <from>17</from>
        <to>22</to>
        <duty>pas_sims_Duty_Social</duty>
        <focusFacility>pas_sims_GatherSpot</focusFacility>
      </li>
      <li>
        <from>22</from>
        <to>7</to>
        <duty>pas_sims_Duty_Sleep</duty>
        <focusFacility>pas_sims_Bed</focusFacility>
      </li>
    </schedule>
  </pas.sims.LifeRoleDef>

  <!-- 農夫：白天田裡、傍晚聚會、夜間睡床 -->
  <pas.sims.LifeRoleDef>
    <defName>pas_sims_Farmer</defName>
    <requiredFacility>pas_sims_FarmPlot</requiredFacility>
    <schedule>
      <li>
        <from>7</from>
        <to>17</to>
        <duty>pas_sims_Duty_FakeWork</duty>
        <focusFacility>pas_sims_FarmPlot</focusFacility>
      </li>
      <li>
        <from>17</from>
        <to>22</to>
        <duty>pas_sims_Duty_Social</duty>
        <focusFacility>pas_sims_GatherSpot</focusFacility>
      </li>
      <li>
        <from>22</from>
        <to>7</to>
        <duty>pas_sims_Duty_Sleep</duty>
        <focusFacility>pas_sims_Bed</focusFacility>
      </li>
    </schedule>
  </pas.sims.LifeRoleDef>

  <!-- 居民：白天在居所附近、傍晚聚會、夜間睡床 -->
  <pas.sims.LifeRoleDef>
    <defName>pas_sims_Resident</defName>
    <schedule>
      <li>
        <from>7</from>
        <to>17</to>
        <duty>pas_sims_Duty_HomeLife</duty>
        <focusFacility>pas_sims_Bed</focusFacility>
      </li>
      <li>
        <from>17</from>
        <to>22</to>
        <duty>pas_sims_Duty_Social</duty>
        <focusFacility>pas_sims_GatherSpot</focusFacility>
      </li>
      <li>
        <from>22</from>
        <to>7</to>
        <duty>pas_sims_Duty_Sleep</duty>
        <focusFacility>pas_sims_Bed</focusFacility>
      </li>
    </schedule>
  </pas.sims.LifeRoleDef>

</Defs>
```

- [ ] **Step 2: Profiles.xml（default + tribal 證明派系維度）**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <pas.sims.LifeProfileDef>
    <defName>pas_sims_Profile_Default</defName>
    <isDefault>true</isDefault>
    <roles>
      <li>
        <role>pas_sims_Guard</role>
        <weight>1</weight>
        <minCount>2</minCount>
      </li>
      <li>
        <role>pas_sims_Worker</role>
        <weight>2</weight>
      </li>
      <li>
        <role>pas_sims_Farmer</role>
        <weight>2</weight>
      </li>
      <li>
        <role>pas_sims_Resident</role>
        <weight>3</weight>
      </li>
    </roles>
  </pas.sims.LifeProfileDef>

  <!-- 部落：無工人（部落聚落少有工作台），更多農夫與居民 -->
  <pas.sims.LifeProfileDef>
    <defName>pas_sims_Profile_Tribal</defName>
    <techLevels>
      <li>Neolithic</li>
    </techLevels>
    <roles>
      <li>
        <role>pas_sims_Guard</role>
        <weight>1</weight>
        <minCount>2</minCount>
      </li>
      <li>
        <role>pas_sims_Farmer</role>
        <weight>3</weight>
      </li>
      <li>
        <role>pas_sims_Resident</role>
        <weight>3</weight>
      </li>
    </roles>
  </pas.sims.LifeProfileDef>

</Defs>
```

- [ ] **Step 3: 建置 + commit（含 Task 6a 檔案）**

Run: `dotnet build sims-mode-community/Source/SimsModeCommunity.csproj -c Release` → Build succeeded。

```
git add sims-mode-community/Source sims-mode-community/Defs sims-mode-community/1.6
git commit -m "feat: 派系 profile 解析鏈 + 角色分配 worker + 預設角色/作息/檔案 XML"
```
