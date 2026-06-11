# Task 8: 文件收尾（PROJECT.md / 擴充示範 / spec 修訂回寫 / session_log）

**Files:**
- Create: `npc-outposts/PROJECT.md`
- Create: `npc-outposts/docs/examples/extension-sample.xml`
- Modify: `npc-outposts/docs/2026-06-11-design.md`（§10 修訂節，若 brainstorm 階段尚未寫入）
- Modify: `npc-outposts/session_log.md`

- [ ] **Step 1: PROJECT.md**

```markdown
# NPC 派系哨站（NPC Outposts Community）

## 衍生目標
NPC 派系聚落周圍長出衛星哨站，讓世界有「派系在經營領地」的血肉感。路線圖 O1（本版）→ O2 功能性節點 → O3 戰略目標 → 延後：哨站增益母聚落數值（待 Rim War / Empire Refactor 參考）。

## 範圍（O1）
- `NpcOutpost : Settlement`：可拜訪（150x150 小圖＋sims-mode 活聚落作息）、可交易（原版繼承）、可攻打（小圖＋原版關係懲罰）、擊敗即移除。
- 鋪設：單一 `WorldComponent_OutpostSpawner`——`FinalizeInit` 開局/舊檔鋪基底、tick MTB 增生至每聚落上限。
- Def 體系：`OutpostTypeDef` + `OutpostProfileDef`（resolver 鏈 Extension > FactionDef > TechLevel > Default），零寫死。
- 守軍壓低：`ExtraGenStepDefs` 注入 `GenStep_TrimDefenders`（order 9990）。

## 技術棧
C#（net48）＋ XML Defs/PatchOperation；零 Harmony。**硬相依 `pas.sims.community`**（assembly 引用；共用「真訪問」ArrivalAction 與 Base_Faction 地圖生成線）。defName 前綴 `pas_outposts_*`；namespace `pas.outposts`。

## 對應 RimWorld 版本
1.6（反編譯權威源 `C:\code\mine\pas\projects\rimworld`）。

## 完成定義（O1）
見 `docs/2026-06-11-design.md` §9（分布/舊檔增生/拜訪小圖作息/交易/攻打/海盜站/真訪問/存讀檔/缺相依警告/健檢）。

## 關鍵文件
- `docs/2026-06-11-design.md`：O1 設計 spec（權威）。
- `docs/2026-06-11-implementation-plan.md`：實作計畫索引（各 task 在 `docs/plan/task-*.md`）。
- `docs/examples/extension-sample.xml`：第三方擴充示範。
- `tests/healthcheck.py`：靜態健檢。
- `session_log.md`：執行記錄。

## 來源報告
- 可行性：`pas/analysis/rimworld_mods/_mod_ideas/world_map_grand_strategy/02_outposts_and_world_objects.md`（VOE 解剖、輕量 WorldObject、lazy 生圖、Settlement 繼承 CP 值）。
- 姊妹案：`sims-mode-community`（活聚落＝哨站地圖的守軍行為引擎）。
```

- [ ] **Step 2: docs/examples/extension-sample.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- 第三方擴充示範：不改本 mod C# 即可（1）新增哨站類型（2）派系專屬 profile（3）停用某派系哨站。
     使用方式：放進你自己 mod 的 Defs/ 與 Patches/。 -->
<Defs>

  <!-- (1) 新類型：要塞哨站——更大的圖、更強守軍 -->
  <pas.outposts.OutpostTypeDef>
    <defName>yourmod_Type_Fortress</defName>
    <label>fortress outpost</label>
    <worldObjectDef>pas_outposts_Outpost</worldObjectDef>
    <mapSize>(200,1,200)</mapSize>
    <defenderPointsFactor>0.8</defenderPointsFactor>
  </pas.outposts.OutpostTypeDef>

  <!-- (2) 派系專屬 profile：哨站更多更密 -->
  <pas.outposts.OutpostProfileDef>
    <defName>yourmod_Profile_Warlord</defName>
    <factionDefs>
      <li>YourWarlordFaction</li>
    </factionDefs>
    <countPerSettlement>3~5</countPerSettlement>
    <radius>2~6</radius>
    <spawnMtbDays>8</spawnMtbDays>
    <types>
      <li>
        <type>pas_outposts_Type_Generic</type>
        <weight>2</weight>
      </li>
      <li>
        <type>yourmod_Type_Fortress</type>
        <weight>1</weight>
      </li>
    </types>
  </pas.outposts.OutpostProfileDef>

</Defs>
<!-- 以下放你 mod 的 Patches/*.xml：

(3) 停用某派系的哨站
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/FactionDef[defName="SomePacifistFaction"]</xpath>
  <value>
    <li Class="pas.outposts.OutpostDisabledExtension" />
  </value>
</Operation>

(2b) 或用 extension 直接綁 profile（解析鏈最高優先）
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/FactionDef[defName="YourWarlordFaction"]</xpath>
  <value>
    <li Class="pas.outposts.OutpostProfileExtension">
      <profile>yourmod_Profile_Warlord</profile>
    </li>
  </value>
</Operation>
-->
```

- [ ] **Step 3: spec §10 修訂回寫**

確認 `docs/2026-06-11-design.md` 末尾有「## 10. 計畫期修訂」節（WorldGenStep 砍掉、TypeDef.weight 移除、mapSize IntVec3）；沒有就補上（內容照索引「spec 修訂」三條）。

- [ ] **Step 4: session_log 補記 + Commit**

session_log.md 記錄 Task 1-8 執行結果（建置/健檢輸出、與計畫的偏差）。

```powershell
git -C C:\code\mine\my_rimworld_mods add npc-outposts/PROJECT.md npc-outposts/docs npc-outposts/session_log.md
git -C C:\code\mine\my_rimworld_mods commit -m @'
docs: npc-outposts PROJECT.md + 第三方擴充示範 + spec 修訂回寫

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
