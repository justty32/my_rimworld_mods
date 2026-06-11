# Task 5: DutyDefs（五個純 XML think node）

> 屬於 `../2026-06-11-implementation-plan.md`。

**Files:**
- Create: `sims-mode-community/Defs/DutyDefs/Duties.xml`

> 每個 duty 頂層結構統一：打敵人 → 滿足基本需求 → 角色專屬行為 → 回 duty 點遊蕩。
> Class 不帶 namespace 的是 Verse.AI / RimWorld 原版類（GenTypes 自動解析）；自訂類用全名。
> 若 Task 0 發現 `JobGiver_AIFightEnemies` / `maxDistToDutyTarget` 名稱有出入，以 Task 0 結果為準替換。

- [ ] **Step 1: 寫 Duties.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- 假工作（工人/農夫共用，focus 不同設施） -->
  <DutyDef>
    <defName>pas_sims_Duty_FakeWork</defName>
    <thinkNode Class="ThinkNode_Priority">
      <subNodes>
        <li Class="JobGiver_AIFightEnemies">
          <targetAcquireRadius>50</targetAcquireRadius>
          <targetKeepRadius>60</targetKeepRadius>
        </li>
        <li Class="ThinkNode_Subtree">
          <treeDef>SatisfyBasicNeeds</treeDef>
        </li>
        <li Class="pas.sims.JobGiver_FakeWork" />
        <li Class="JobGiver_WanderNearDutyLocation" />
      </subNodes>
    </thinkNode>
  </DutyDef>

  <!-- 聚會社交 -->
  <DutyDef>
    <defName>pas_sims_Duty_Social</defName>
    <socialModeMax>SuperActive</socialModeMax>
    <thinkNode Class="ThinkNode_Priority">
      <subNodes>
        <li Class="JobGiver_AIFightEnemies">
          <targetAcquireRadius>50</targetAcquireRadius>
          <targetKeepRadius>60</targetKeepRadius>
        </li>
        <li Class="ThinkNode_Subtree">
          <treeDef>SatisfyBasicNeeds</treeDef>
        </li>
        <li Class="ThinkNode_ConditionalCloseToDutyTarget">
          <maxDistToDutyTarget>10</maxDistToDutyTarget>
          <subNodes>
            <li Class="JobGiver_StandAndBeSociallyActive" />
          </subNodes>
        </li>
        <li Class="JobGiver_WanderNearDutyLocation" />
      </subNodes>
    </thinkNode>
  </DutyDef>

  <!-- 夜間睡覺 -->
  <DutyDef>
    <defName>pas_sims_Duty_Sleep</defName>
    <thinkNode Class="ThinkNode_Priority">
      <subNodes>
        <li Class="JobGiver_AIFightEnemies">
          <targetAcquireRadius>50</targetAcquireRadius>
          <targetKeepRadius>60</targetKeepRadius>
        </li>
        <li Class="pas.sims.JobGiver_SleepAtDutyFocus" />
        <li Class="ThinkNode_Subtree">
          <treeDef>SatisfyBasicNeeds</treeDef>
        </li>
        <li Class="JobGiver_WanderNearDutyLocation" />
      </subNodes>
    </thinkNode>
  </DutyDef>

  <!-- 衛兵駐守（快反部隊；focus=聚落中心） -->
  <DutyDef>
    <defName>pas_sims_Duty_Guard</defName>
    <thinkNode Class="ThinkNode_Priority">
      <subNodes>
        <li Class="JobGiver_AIFightEnemies">
          <targetAcquireRadius>65</targetAcquireRadius>
          <targetKeepRadius>75</targetKeepRadius>
        </li>
        <li Class="ThinkNode_Subtree">
          <treeDef>SatisfyBasicNeeds</treeDef>
        </li>
        <li Class="JobGiver_AIDefendPoint" />
        <li Class="JobGiver_WanderNearDutyLocation" />
      </subNodes>
    </thinkNode>
  </DutyDef>

  <!-- 居家（在 focus 附近過日子） -->
  <DutyDef>
    <defName>pas_sims_Duty_HomeLife</defName>
    <thinkNode Class="ThinkNode_Priority">
      <subNodes>
        <li Class="JobGiver_AIFightEnemies">
          <targetAcquireRadius>50</targetAcquireRadius>
          <targetKeepRadius>60</targetKeepRadius>
        </li>
        <li Class="ThinkNode_Subtree">
          <treeDef>SatisfyBasicNeeds</treeDef>
        </li>
        <li Class="JobGiver_WanderNearDutyLocation" />
      </subNodes>
    </thinkNode>
  </DutyDef>

</Defs>
```

- [ ] **Step 2: Commit**

```
git add sims-mode-community/Defs
git commit -m "feat: 五個生活 DutyDef（純 XML think node）"
```
