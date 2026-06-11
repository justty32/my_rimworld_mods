# Task 9: 第三方擴充示範 + 收尾

**Files:**
- Create: `faction-politics/docs/examples/extension-sample.xml`
- Modify: `faction-politics/session_log.md`（補 Task 1-9 執行記錄）

- [ ] **Step 1: docs/examples/extension-sample.xml**（不被遊戲載入，純示範）

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- 第三方擴充示範：不改本 mod C# 即可（1）派系專屬反叛 profile（2）停用某派系反叛（3）extension 直綁。
     使用方式：放進你自己 mod 的 Defs/ 與 Patches/。本檔在 docs/ 下，不被遊戲載入。 -->
<Defs>

  <!-- (1) 派系專屬 profile：反叛更快、倒戈更狠 -->
  <pas.politics.RebellionProfileDef>
    <defName>yourmod_Profile_Unstable</defName>
    <label>unstable rebellion profile</label>
    <factionDefs>
      <li>YourEmpireFaction</li>
    </factionDefs>
    <progressPerDay>0.5~1.2</progressPerDay>
    <threshold>80</threshold>
    <defectFraction>0.4~0.7</defectFraction>
    <respawnDelayDays>10</respawnDelayDays>
    <minSettlements>2</minSettlements>
  </pas.politics.RebellionProfileDef>

</Defs>
<!-- 以下放你 mod 的 Patches/*.xml：

(2) 停用某派系的反叛系統
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/FactionDef[defName="SomeMonolithicFaction"]</xpath>
  <value>
    <li Class="pas.politics.PoliticsDisabledExtension" />
  </value>
</Operation>

(3) 或用 extension 直接綁 profile（解析鏈最高優先）
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/FactionDef[defName="YourEmpireFaction"]</xpath>
  <value>
    <li Class="pas.politics.PoliticsProfileExtension">
      <profile>yourmod_Profile_Unstable</profile>
    </li>
  </value>
</Operation>

(4) 調全域上限（動態派系最多 8 個、輪詢加快）
<Operation Class="PatchOperationReplace">
  <xpath>Defs/pas.politics.PoliticsSettingsDef[defName="pas_politics_Settings"]/maxDynamicFactions</xpath>
  <value><maxDynamicFactions>8</maxDynamicFactions></value>
</Operation>
-->
```

- [ ] **Step 2: session_log.md 補 Task 1-9 記錄**

格式照 npc-outposts/session_log.md：執行偏差（含原因）、雙建置結果、健檢結果、各 C# 檔行數（≤200 確認）、Task 10 待執行註記。

- [ ] **Step 3: 健檢重跑（examples 不在掃描路徑，應仍綠）**

Run: `python C:\code\mine\my_rimworld_mods\faction-politics\tests\healthcheck.py`
Expected: `healthcheck OK`

- [ ] **Step 4: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add faction-politics/docs/examples faction-politics/session_log.md
git -C C:\code\mine\my_rimworld_mods commit -m @'
docs: faction-politics 第三方擴充示範 + session log 收尾

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
