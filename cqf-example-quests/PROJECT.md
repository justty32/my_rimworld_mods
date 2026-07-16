# cqf-example-quests — CQF 範例任務集

## 目標

Custom Quest Framework（CQF, `HaiLuan.CustomQuestFramework`）的**純 XML 教學範例 mod**：用最小但彼此不同的三個任務 + 一棵手寫對話樹，把「依託 CQF 做新任務」的主要機制各示範一次。搭配分析報告 `pas/analysis/rimworld_mods/custom-quest-framework/tutorial/02_authoring_new_quests.md`（含 CQFAction 全型錄連結）。

前作 `cqf-caravan-redemption` 只驗證了「自訂 QuestScriptDef 能載入觸發」；本 mod 補上它沒碰的：任務鏈、延時排程、全域旗標、條件分支、信號閘門、以及**手寫 DialogTreeDef**（此前無本地實證）。

## 範圍（三任務一對話樹）

| 內容 | defName | 展示機制 |
|---|---|---|
| 任務 A 回聲信標 | `CQFExample_EchoBeacon` | 說書人抽選、`CQFAction_SetGlobalBool` 全域旗標、`CQFAction_DelayExecute` 排程、`CQFAction_Quest` 任務鏈、`CQFAction_SentSignal`＋`QuestNode_End` 結案 |
| 任務 B 信標的回應 | `CQFExample_EchoBeaconSignal` | 被鏈式觸發（weight=0）、`CQFAction_Condition`＋`DialogCondition_Bool` 讀全域旗標、原版節點發獎（GetMap→GenerateThingSet→DropPods） |
| 任務 C 嚮導的藏匿點 | `CQFExample_GuideCommission` | 由對話接單（weight=0）、CQF 信號閘住原版 `QuestNode_DropPods.inSignal`、延時交付 |
| 對話樹 漂泊嚮導 | `CQFExample_GuideTree`＋`CQFExample_GuideManager`＋`CQFExample_GuideSpawn` | 手寫 DialogTreeDef：3 節點分支跳轉、`extraText` 隨機台詞、技能門檻選項、`requiredThings` 以物易物（30 銀換 10 草藥）、對話接任務、全域旗標防重複；`SpecialPawnGenerateDef` 讓嚮導隨機出現在中立訪客團 |

## 技術棧

- RimWorld 1.6，純 XML（零 C#、零自帶 Harmony patch）
- 相依：Harmony + CQF（`About.xml` modDependencies / loadAfter）
- 欄位 schema 權威：CQF 反編譯源 `pas/projects/rimworld_mods/custom-quest-framework/decompiled/QuestEditor_Library/QuestEditor_Library.decompiled.cs`（各 XML 內註明行號）

## 完成定義

- [x] 所有 XML well-formed（`tests/healthcheck.py`）
- [x] 所有 `Class="QuestEditor_Library.*"` 類名存在於反編譯源；欄位名與類（含基底類）宣告逐一對上（`tests/healthcheck.py` 靜態比對）
- [x] def 交叉引用（quest=/tree=/thingSetMaker=）閉合；翻譯 key 三語齊備
- [x] DialogTree 結構完整（key 0 存在、nextIndex 指向存在節點、curIndex > 最大 key）
- [ ] 實機驗證（未做，留待部署）：對話視窗渲染/跳轉/扣料、任務鏈觸發、延時跨讀檔

## 測試方式

- 離線：`python3 tests/healthcheck.py`
- 實機：開發者模式 → Execute quest 直接觸發 A（B/C weight=0 也可強制觸發驗獎勵段）；對話樹等中立訪客團（嚮導頭上有對話圖示）或用 CQF 遊戲內編輯器熱載驗證

## 關鍵文件

- `1.6/Defs/`：所有 Def（每檔頭部有機制說明與源碼行號）
- `tests/healthcheck.py`：離線 XML/schema/交叉引用/翻譯健檢
- 分析側：`pas/analysis/rimworld_mods/custom-quest-framework/tutorial/02_authoring_new_quests.md`、`details/cqfaction_catalog.md`
