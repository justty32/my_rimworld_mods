# NPC Outposts O1（NPC 派系哨站）Implementation Plan（索引）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. 各 task 內容在 `plan/task-*.md`（checkbox 追蹤在各檔內）。

**Goal:** NPC 派系聚落周圍長出衛星哨站（可拜訪小圖＋sims-mode 作息守軍、可交易、可攻打小圖、擊敗即移除），世界隨時間自然增生哨站。(NPC faction outposts: satellite world objects around NPC settlements — visitable small maps with living defenders, tradeable, attackable, growing over time.)

**Architecture:** `NpcOutpost : Settlement` 白嫖拜訪/交易/送禮/擊敗全套；override `ExtraGenStepDefs` 注入 `GenStep_TrimDefenders`（order 9990）壓低守軍、override float menu/caravan gizmo 把攻打與「進入」換成小圖流程；地圖共用原版 `Base_Faction` 生成線 → sims-mode 活聚落 patch 自動生效。鋪設＝單一 `WorldComponent_OutpostSpawner`：`FinalizeInit` 開局鋪基底（含舊檔自動補鋪），`WorldComponentTick` MTB 增生。「真訪問」ArrivalAction 做在 sims-mode（本 mod 硬相依共用）。零 Harmony。

**Tech Stack:** C# net48（SDK-style csproj + `Krafs.Rimworld.Ref` 1.6）＋ XML Defs/PatchOperation。namespace `pas.outposts`，defName 前綴 `pas_outposts_`，packageId `pas.outposts.community`。硬相依 `pas.sims.community`（assembly 引用 `SimsModeCommunity.dll`）。

> **權威源**：`C:\code\mine\pas\projects\rimworld`（1.6 反編譯）。計畫期已坐實的關鍵 API：
> - `RimWorld.Planet\WorldGenerator.cs:57-67`（世界 gen steps 全跑完才 `FinalizeInit(fromLoad:false)`）＋ `Verse\Game.cs:585`→`World.cs:206-210`（讀檔也呼叫 `FinalizeInit(fromLoad:true)`）→ **WorldGenStep 不需要，WorldComponent 一條路**
> - `RimWorld.Planet\MapParent.cs:33`（`ExtraGenStepDefs` virtual）＋ `Verse\GetOrGenerateMapUtility.cs:26`、`Verse\Game.cs:529`（兩條生圖路徑都 concat 它）→ 守軍 trim 注入點
> - `RimWorld.BaseGen\SymbolResolver_Settlement.cs:10,58`（守軍點數預設 `FloatRange(1150,1600)`，`GenStep_Settlement` 無 XML 鉤子 → 必須 trim 後處理）
> - `RimWorld.Planet\SettlementDefeatUtility.cs:9-67`（`CheckDefeated` 由 `Settlement.TickInterval` 呼叫（Settlement.cs:189-197），子類自動繼承；`HasAnyOtherBase` 掃 `Find.WorldObjects.Settlements`——**哨站存活會讓派系不被判定全滅**，O1 接受並記錄）
> - `RimWorld.Planet\TileFinder.cs:146`（`TryFindPassableTileWithTraversalDistance(rootTile, minDist, maxDist, out result, validator, …)`）＋ `:65`（`IsValidTileForNewSettlement`）
> - `RimWorld.Planet\Settlement.cs:141-144`（ctor new `Settlement_TraderTracker` → 子類交易免費）、`:48-58`（`Visitable`）、`:60`（`Attackable`）、`:313-340`（caravan gizmo 範本）、`:342-360`（float menu 範本）
> - `RimWorld.Planet\SettlementUtility.cs:44-59`（攻擊流程範本；`:61` `AffectRelationsOnAttacked` 為 public static 可複用）
> - `RimWorld.Planet\CaravanArrivalAction_VisitSettlement.cs`（60 行 ArrivalAction 範本；`Arrived` 不生圖——原版拜訪＝停格交易）
> - `RimWorld.Planet\WorldObjectComp.cs:45`（comp 的 `GetFloatMenuOptions(Caravan)` virtual）＋ `WorldObject.cs:639`（迭代注入）→ sims-mode「真訪問」patch 點
> - `Verse\GenStepWithParams.cs:11`（`(GenStepDef, GenStepParams)` 建構）
> - `RimWorld\PlanetLayerDef.cs:54,122`（`worldGenSteps` 是 PlanetLayerDef 私有清單——佐證放棄 WorldGenStep 路線的理由）
>
> **測試現實**：同 sims-mode——`dotnet build`（型別檢查）＋ `python tests\healthcheck.py`（Task 7 建立）＋ 實機 E2E（Task 9 手動清單）。
>
> **commit 規則**：npc-outposts 改動 add `npc-outposts/...`；Task 3（sims-mode 交付）add `sims-mode-community/...` 並獨立 commit。勿 `-A`。訊息附 `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`。
>
> **建置順序**：npc-outposts 引用 `sims-mode-community\1.6\Assemblies\SimsModeCommunity.dll`——先建 sims-mode 再建 npc-outposts（Task 3 之後的每次驗證都建兩個）。
>
> **§7 校準保留**：使用者將提供參考 mod 校準「真訪問」。Task 3 先做最小版（介面已凍結：`CaravanArrivalAction_VisitMap` + size 參數），參考 mod 到位後若有出入，只動 `Arrived` 內部與 letter/善後細節，不動呼叫介面。

---

## spec 修訂（計畫期決策，已回寫 spec §10）

1. **WorldGenStep 砍掉**：`FinalizeInit` 證據如上，單一 WorldComponent 覆蓋開局/補鋪/舊檔三場景。
2. **`OutpostTypeDef.weight` 移除**：權重只存在 `OutpostProfileDef.types`（`OutpostTypeEntry.weight`），單一資料源。
3. **`mapSize` 型別 IntVec2 → IntVec3**：`GetOrGenerateMap` 收 IntVec3（`GetOrGenerateMapUtility.cs:9`），XML 寫 `(150,1,150)`。

## 檔案結構

```
npc-outposts/
├── About/About.xml                        # packageId pas.outposts.community；modDependencies + loadAfter pas.sims.community
├── 1.6/Assemblies/                        # 建置輸出（NpcOutposts.dll）
├── Defs/
│   ├── WorldObjectDefs/Outposts.xml       # pas_outposts_Outpost（worldObjectClass NpcOutpost）
│   ├── OutpostTypeDefs/Types.xml          # pas_outposts_Type_Generic
│   ├── OutpostProfileDefs/Profiles.xml    # pas_outposts_Profile_Default
│   └── GenStepDefs/GenSteps.xml           # pas_outposts_TrimDefenders（order 9990）
├── Languages/
│   ├── English/Keyed/NpcOutposts.xml      # NameFormat / AttackOutpost 等
│   └── ChineseTraditional/Keyed/NpcOutposts.xml
├── Source/
│   ├── NpcOutposts.csproj                 # + Reference SimsModeCommunity (Private=false)
│   ├── OutpostDefOf.cs                    # [DefOf] GenStepDef
│   ├── Defs/
│   │   ├── OutpostTypeDef.cs
│   │   └── OutpostProfileDef.cs           # + OutpostTypeEntry + OutpostProfileExtension + OutpostDisabledExtension
│   ├── Assign/OutpostProfileResolver.cs   # 解析鏈（同 sims-mode 模式，資料源不同故自寫）
│   ├── World/
│   │   ├── NpcOutpost.cs                  # Settlement 子類（menu/gizmo/ExtraGenStepDefs/ExposeData）
│   │   ├── OutpostAttackUtility.cs        # 小圖攻擊流程 + CaravanArrivalAction_AttackOutpost
│   │   ├── OutpostPlacer.cs               # TryPlaceFor(parent, profile)
│   │   └── WorldComponent_OutpostSpawner.cs  # caps 字典 + FinalizeInit + MTB tick
│   └── MapGen/GenStep_TrimDefenders.cs
├── tests/healthcheck.py
├── docs/{2026-06-11-design.md, 本索引, plan/task-*.md}
├── PROJECT.md
└── session_log.md

sims-mode-community/（Task 3 交付）
├── Source/Visit/CaravanArrivalAction_VisitMap.cs      # 生圖進場 ArrivalAction（size 參數化）
├── Source/Visit/WorldObjectComp_VisitMap.cs           # + Properties；float menu「進入」
├── Patches/Settlement_VisitMap.xml                    # comp 掛上原版 Settlement WorldObjectDef
└── Languages/{English,ChineseTraditional}/Keyed/SimsModeCommunity.xml
```

## Task 清單（依序執行；checkbox 在各檔內）

| Task | 檔案 | 內容 | 產出驗證 |
|---|---|---|---|
| 0 | `plan/task-00-api-verification.md` | grep 反編譯源碼坐實殘餘 API（AttackSettlement ArrivalAction、Name setter、icon 路徑、MTBEventOccurs、CaravanArrivalActionUtility 簽名等 9 項） | 偏差記入 session_log |
| 1 | `plan/task-01-skeleton.md` | About.xml（modDependencies）+ csproj（Krafs + sims-mode 引用）+ 空建置 | dotnet build 綠 |
| 2 | `plan/task-02-def-layer.md` | OutpostTypeDef / OutpostProfileDef（+Entry+兩個 Extension）/ OutpostDefOf / resolver | dotnet build 綠 |
| 3 | `plan/task-03-simsmode-visit.md` | **sims-mode 交付**：CaravanArrivalAction_VisitMap + comp + patch + Languages（最小版，待參考 mod 校準） | sims-mode build 綠；commit 到 sims-mode |
| 4 | `plan/task-04-npcoutpost.md` | NpcOutpost 類 + OutpostAttackUtility（+AttackOutpost ArrivalAction）+ WorldObjectDef XML + Languages | 雙 mod build 綠 |
| 5 | `plan/task-05-trim-genstep.md` | GenStep_TrimDefenders + GenSteps.xml + Types.xml + Profiles.xml | dotnet build 綠 |
| 6 | `plan/task-06-spawner.md` | OutpostPlacer + WorldComponent_OutpostSpawner（caps 存檔） | dotnet build 綠 |
| 7 | `plan/task-07-healthcheck.md` | healthcheck.py（雙 mod XML/交叉引用/patch 鏈/DefOf/相依宣告） | healthcheck OK |
| 8 | `plan/task-08-docs-finish.md` | PROJECT.md + 第三方擴充示範 + spec §10 修訂回寫 + session_log | — |
| 9 | `plan/task-09-e2e.md` | 實機 E2E 手動清單（spec §9 十條 + 已接受行為觀察） | 記入 session_log |

## 自我審查結果

- **Spec 覆蓋**：§4 物件全行為（Task 4）、§5 Def 體系（Task 2+5）、§6 鋪設（Task 6；兩段式收斂為 FinalizeInit+tick，行為等價）、§7 真訪問（Task 3）、§9 完成定義逐條對應 Task 9。spec §8 待驗證 7 項：計畫期已坐實 5 項（見索引權威源），餘 2 項（vanilla 貼圖路徑、AttackSettlement ArrivalAction 細節）入 Task 0。
- **型別一致性**：`OutpostTypeDef.mapSize`（IntVec3）在 Task 2 定義、Task 3（size 參數）/Task 4（visit/attack 呼叫）一致；`OutpostPlacer.TryPlaceFor(Settlement, OutpostProfileDef)` 在 Task 6 定義與 spawner 呼叫一致；`NpcOutpost.Setup(OutpostTypeDef, Settlement)` Task 4 定義、Task 6 呼叫。
- **已知開放風險**（不阻塞，Task 9 觀察）：(a) 哨站進 `SettlementBases`/`Settlements` 清單——任務系統可能選中（多為加分 flavor）、派系全滅判定會算上哨站（接受：哨站存活＝派系未滅）；(b) 空投/穿梭機攻擊哨站走原版全尺寸圖（O1 接受，僅 caravan 路徑小圖）；(c) vanilla 貼圖路徑若不符 → 粉紅方塊，E2E 即改；(d) 遊戲中途新建的 NPC 聚落要等下次讀檔才入 caps（接受，O2 可在 tick 內補掃）。
