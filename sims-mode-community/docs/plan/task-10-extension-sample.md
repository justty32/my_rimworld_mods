# Task 10: 擴充示範 + PROJECT.md 收尾

> 屬於 `../2026-06-11-implementation-plan.md`。

**Files:**
- Create: `sims-mode-community/docs/examples/extension-sample.xml`
- Modify: `sims-mode-community/PROJECT.md`（完成定義打勾區）

- [ ] **Step 1: 寫 extension-sample.xml（證明可擴充性的示範，放 docs/ 不會被遊戲載入）**

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- 第三方擴充示範：證明不改本 mod C# 即可（1）新增角色（2）改作息（3）標記自訂建築（4）綁派系 profile。
     使用方式：放進你自己 mod 的 Defs/ 與 Patches/。 -->
<Defs>

  <!-- (1) 新角色：酒保——傍晚到深夜在聚會點假工作 -->
  <pas.sims.LifeRoleDef>
    <defName>yourmod_Bartender</defName>
    <requiredFacility>pas_sims_GatherSpot</requiredFacility>
    <schedule>
      <li>
        <from>15</from>
        <to>24</to>
        <duty>pas_sims_Duty_FakeWork</duty>
        <focusFacility>pas_sims_GatherSpot</focusFacility>
      </li>
      <li>
        <from>0</from>
        <to>15</to>
        <duty>pas_sims_Duty_Sleep</duty>
        <focusFacility>pas_sims_Bed</focusFacility>
      </li>
    </schedule>
  </pas.sims.LifeRoleDef>

  <!-- (4) 自訂派系 profile，含新角色（搭配 PatchOperationAddModExtension 掛上你的 FactionDef） -->
  <pas.sims.LifeProfileDef>
    <defName>yourmod_Profile_Cantina</defName>
    <roles>
      <li>
        <role>pas_sims_Guard</role>
        <weight>1</weight>
        <minCount>1</minCount>
      </li>
      <li>
        <role>yourmod_Bartender</role>
        <weight>1</weight>
        <minCount>1</minCount>
      </li>
      <li>
        <role>pas_sims_Resident</role>
        <weight>3</weight>
      </li>
    </roles>
  </pas.sims.LifeProfileDef>

</Defs>
<!-- 以下放你 mod 的 Patches/*.xml：

(2) 改作息：把預設工人的下班時間從 17 改 19
<Operation Class="PatchOperationReplace">
  <xpath>Defs/pas.sims.LifeRoleDef[defName="pas_sims_Worker"]/schedule/li[1]/to</xpath>
  <value><to>19</to></value>
</Operation>

(3) 標記自訂建築為工作台（在你的 ThingDef 上掛 extension）
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/ThingDef[defName="YourFancyForge"]</xpath>
  <value>
    <li Class="pas.sims.FacilityTagExtension">
      <tags><li>pas_sims_Workbench</li></tags>
    </li>
  </value>
</Operation>
-->
```

- [ ] **Step 2: PROJECT.md 的「關鍵文件」段補 implementation-plan 連結；commit**

```
git add sims-mode-community/docs sims-mode-community/PROJECT.md
git commit -m "docs: 第三方擴充示範 + PROJECT.md 收尾"
```
