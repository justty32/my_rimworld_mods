# 任務機制參考庫（可用程式碼/機制積木）

> 本輪（2026-07-18）偵察＋實作驗證出來的「能用的積木」全紀錄，之後照這份拼。
> 標記：✅=本包實作/healthcheck 欄位驗過 ｜ ✔=原版/Kiiro 實機驗過的用法 ｜ ⚠=待實機坐實。

---

## A. CQF Action 調色盤（✅ 本包已用、healthcheck 欄位驗過）

> 全限定名前綴 `QuestEditor_Library.`。抽象列表 `<li>` 必帶 `Class`。
> 目標系統：對話 result 的 targets 有 `Interviewer`(玩家談判者)/`Interviewee`(被談者)；**純 QuestScriptDef 腳本裡 targets 是空 dict**（目標型 action 在腳本裡解不到目標）。

| Action | 欄位 | 情境 | 用途 |
|---|---|---|---|
| `CQFAction_Message` | `message`(Keyed或字面), `type`(MessageTypeDef如 PositiveEvent/NeutralEvent/ThreatBig) | 對話/腳本 | 左上訊息 |
| `CQFAction_SetGlobalBool` | `keyOfBool`, `valueOfBool` | 對話/腳本 | 寫全域 bool（跨任務跨讀檔存活）；任務鏈狀態放這 |
| `CQFAction_SentSignal` | `signal`, `addQuestPrefix`(預設 true) | 腳本 | 發 `Quest{id}.{signal}` 信號，對上原版 `inSignal` |
| `CQFAction_DelayExecute` | `delayTime`(tick), `actions` | 腳本/對話 | 延時執行子 actions（入存檔，跨讀檔不丟） |
| `CQFAction_Spawn` | `targetsText`(如 Interviewer), `datas`(內含 `dataName`/`chance`/`message`/`things`[CQFThingDefCount]) | **對話**(需目標) | 在目標腳下生出物品→給玩家 |
| `CQFAction_ChangeGoodwillOfFaction` | `targetsText`(如 Interviewee), `isIncrease`, `value`, `sendLetter` | 對話 | 改目標派系好感 |
| `CQFAction_Quest` | `quest`(QuestScriptDef defName) | 對話 | 觸發一個 QuestScriptDef（不經說書人；被觸發的設 rootSelectionWeight=0）；points 用 `DefaultParmsNow(GiveQuest)` |
| `CQFAction_Faction` | `targetsText`(如 Interviewee), `faction`(FactionDef) | 對話 | **改目標派系；`faction.isPlayer`→`SetFaction(Faction.OfPlayer)`＝收編成殖民者**（用 `PlayerColony`） |
| `CQFAction_Incident` | `targetsText`(需帶 Map 的目標如 Interviewee), `incident`(IncidentDef) | **對話**(需 Map 目標) | 在目標地圖 fire 原版事件（用 `DefaultParmsNow` 預設 parms）。fire raid＝`incident=RaidEnemy` |
| `CQFAction_Condition` | `conditions`, `actions` | — | 條件全滿足才執行（無 else，反向用 Reversal） |
| `CQFAction_Sequence`/`_Random`/`_Chance`(單數`action`)/`_Loop` | 見 catalog | — | 流程控制 |

**其他可能用得上（catalog 有、本包未用）**：`CQFAction_SetRelation`(建兩 pawn 關係，可做收養/親屬)、`CQFAction_SetDuty`(指 DutyDef 給 pawn，可做「聚焦打某 NPC」)、`CQFAction_GainExperience`/`_GainMood`/`_Hediff`/`_Trait`/`_SetXenotype`(pawn 改造)、`CQFAction_AddDialogManager`(動態綁對話到 pawn)、`CQFAction_StartMentalState`。

## A2. DialogTree 積木（✅ 本包驗過）

- `DialogTreeDef`：`defName`/`title`(Keyed)/`dialogReportKey`(Keyed)/`requireNonHostile`/`curIndex`(=最大key+1)/`nodeMoulds`(Dictionary<int,DialogNode>，key=0 為入口)。
- `DialogNode`：`text`(Keyed)/`extraText`(Keyed list,顯示時隨機挑一句)/`index`/`parentIndex`/`subNodeIndexs`/`options`。
- `DialogOption`：`text`(Keyed)/`hideWhenDisabled`/`removeDialogAfterSelect`(選後移除整個對話,杜絕重複)/`conditions`(DialogCondition)/`results`(DialogResult)/`requiredThings`(CQFThingDefCount,不足自動禁用+提示,選後自動扣料——「付款/交貨」原語)。
- `DialogResult`：`resultName`/`conditions`/`actions`(CQFAction)/`nextIndex`(省略=關窗)。
- `DialogManagerDef`：`defName`/`tags`(供 SpecialPawn 掛)/`trees`(DialogTreeAndConditions)/**`genrationConditions`**(注意拼字,由 SpecialPawnGenerator_AddDialog 逐 pawn 過濾,全滿足才綁)。
- `SpecialPawnGenerateDef`：`commonality`/`generator=SpecialPawnGenerator_AddDialog`(`dialogs`列多個`<tag>`+`commonality`)。**單一 def 掛全 tag，每批訪客只抽一個**（官方 QE_RandomDialog 寫法）。⚠排除 trader 與非人形。

## A3. DialogCondition（✅ 用過的欄位驗過）

| Condition | 欄位 | 判定 |
|---|---|---|
| `DialogCondition_Bool` | `boolName`, `failReason` | 讀全域/任務 bool——**讀 C# 側寫器寫的 `RatkinQL_State_*` 就靠這個** |
| `DialogCondition_Faction` | `targetText`(如 Interviewee), `faction`(FactionDef), `failReason` | 目標屬某派系（`Faction.def==faction`） |
| `DialogCondition_Skill` | `targetText`, `skill`(SkillDef), `level`, `needToBeGreater`, `failReason` | 技能門檻 |
| `DialogCondition_Or`/`_And` | `condition`(List), `failReason` | 組合（Or 任一/And 全部） |
| `DialogCondition_Reversal` | `condition`(單個), `failReason` | 取反（做 else/防重複） |
> 其他 pawn 層級：`_Trait`/`_Age`/`_Hediff`/`_PrisonerOrSlave`/`_Inventory`/`_Thought`/`_CapturedPawn`；殖民地層級只有 `_ColonistCount`(全地圖總和)。⚠ **勿用 `DialogCondition_QuestIsGenerated`**（欄位沒被 Satisfied 用，恆過）。

---

## B. 原版 QuestScriptDef 節點＋信號（✔ F2 已用、F4/F5 要用）

**信號接力（F2 供貨約已驗結構）**：`QuestEditor_Library.QuestNode_DoCQFActions`(`inSignal`/`actions`) 收 CQF 信號；`inSignal` 留空=吃任務 initiate。原版節點的 `inSignal` 與 CQF `CQFAction_SentSignal`(addQuestPrefix=true) 格式 `Quest{id}.xxx` **互通**。

**發實體獎勵（必走原版節點，CQF action 空 targets 發不了）**：
`QuestNode_GetMap`(canBeSpace) → `QuestNode_GenerateThingSet`(`thingSetMaker`/`storeAs`) → `QuestNode_DropPods`(`inSignal`/`contents=$var`/`sendStandardLetter`/`useTradeDropSpot`/`customLetterLabel`/`customLetterText`)。
ThingSetMaker：`ThingSetMaker_Sum` > `options` > `ThingSetMaker_StackCount`(`fixedParams`>`filter`>`thingDefs`+`countRange`)。

**流程/結束**：`QuestNode_Sequence`/`QuestNode_Signal`(`inSignal`+`node` 子樹,延後生效外殼)/`QuestNode_Delay`(`delayTicks`/`inSignalEnable`/`isQuestTimeout`/`outSignalComplete`)/`QuestNode_End`(`inSignal`/`outcome`=Success|Fail/`sendStandardLetter`)/`QuestNode_Set`(`name`/`value`)。

---

## C. ★F4/F5 site 任務模板（✔ 抄自 Kiiro 已上架範本，純 XML 零 C#）

### 模板 C — 攻打清場（F4）｜來源 Kiiro `TradeRouteBanditCamp`
```
Util_RandomizePointsChallengeRating (SubScript, 帶 pointsFactor*)
→ Util_AdjustPointsForDistantFight (SubScript, 原版)
→ QuestNode_GetPawn (取 asker/敵首領，或 QuestNode_GetFaction 取敵派系)
→ QuestNode_GetSiteTile (storeAs=siteTile, preferCloserTiles)
→ QuestNode_GetSitePartDefsByTagsAndFaction
     sitePartsTags=[BanditCamp]   ← 原版 SitePartDef，久經測試
     mustBeHostileToFactionOf=$asker  (或直接指 Rakinia_Warlord 敵派系)
     storeAs=sitePartDefs, storeFactionAs=siteFaction
→ QuestNode_GetDefaultSitePartsParams (tile/faction/sitePartDefs → storeSitePartsParamsAs)
→ QuestNode_GetSiteThreatPoints (sitePartsParams → storeAs=sitePoints)
→ Util_GenerateSite (SubScript, 原版，讀上面 slate → 產出 $site)
→ QuestNode_SpawnWorldObjects (worldObjects=$site)
→ QuestNode_WorldObjectTimeout (worldObject=$site, delayTicks, → Fail)
→ QuestNode_Signal inSignal="site.AllEnemiesDefeated" → QuestNode_End outcome=Success  ← ★原版「清場成功」信號
```

### 模板 B — 摧毀/擊殺特定目標（F5 暗殺）｜來源 Kiiro `ProblemCauser`
同上生 site，但：
- `sitePartsTags` 含 **`QuestConditionCauser`**（原版）＋守衛 site part。
- `Util_GenerateSite` 帶 `hiddenSitePartsPossible=false`（目標一定可見）。
- 目標物掛 **`CompQuestConditionCauser`**（原版 comp，或用帶 `QuestConditionCauser` tag 的 SitePartDef）。
- 成功：`QuestNode_Signal inSignal="conditionCauser.Destroyed"` → `QuestNode_End outcome=Success`。**摧毀/擊殺目標自動廣播此信號，零自寫偵測 C#。**

### 共通（Kiiro 的好設計，可抄）
- **不作為有代價**：`QuestNode_WorldObjectTimeout` 逾時 → 扣友方派系好感＋`QuestNode_DestroyWorldObject` 摧毀附近友方聚落，增加緊迫感。
- **防呆**：`QuestNode_NoWorldObject`(site 被外部毀→End)、`QuestNode_Signal inSignal="faction.BecameHostileToPlayer"`(委託方翻臉→Fail)、`QuestNode_QuestUnique`(唯一性鎖，避免同時多個)。
- ⚠ **dialog 觸發（CQFAction_Quest 空 slate）vs 說書人**：site-gen 要的 `$points` 由 `DefaultParmsNow` 給；`$asker`/敵派系我方要自己補（`QuestNode_GetFaction allowEnemy mustBePermanentEnemy storeAs=siteFaction`，或直接指 `Rakinia_Warlord`）。這是唯一要實機坐實的接點。

---

## D. 原版可複用資產（✔ Kiiro 實證）

- **Site parts（直接引用，免自製）**：`BanditCamp`、`Outpost`（原版，帶守衛）；`QuestConditionCauser`（掛可摧毀目標）；`SitePart_MechCluster`（ParentName 繼承機械巢穴）。
- **完成信號**：`site.AllEnemiesDefeated`（清場）、`conditionCauser.Destroyed`（摧毀目標）、`<worldObject>.TradeRequestFulfilled`（資源請求達成）、`faction.BecameHostileToPlayer`。
- **原版 Util 子腳本**（`QuestNode_SubScript def=...`）：`Util_RandomizePointsChallengeRating`、`Util_AdjustPointsForDistantFight`、`Util_GenerateSite`、`Util_GetDefaultRewardValueFromPoints`、`Util_Raid`（吃 `enemyFaction`/`points`/`arrivalMode` slate）。
- **紀念碑（F4 另一路）**：Royalty `BuildMonument_TimeProtect`——`QuestNode_GenerateMonumentMarker`，marker 完工自動送 `monumentMarker.MonumentCompleted`／被毀 `monumentMarker.MonumentDestroyed`；`QuestNode_Delay inSignalEnable="monumentMarker.MonumentCompleted"` 撐過保護期→Success。
- **加入型**（F3 我方已用更簡潔的 `CQFAction_Faction`）：原版 `QuestNode_Root_WandererJoin`/`RefugeePodCrash`/`RelativeJoins` 邏輯長，Kiiro 是複製原版 C# 換皮——我方避開了。

---

## E. Kiiro 自製 C# QuestNode 清單（參考；若我方日後要自己的 C# quest 層）

> assembly `Kiiro_Event.dll`，命名空間 `Kiiro_Event`。**agent 結論：F4/F5 第一版不需要這層**，只在「跨任務重複查詢」才值得寫。

- 查詢/篩選型（最值得學）：`QuestNode_GetNearbyKiiroSettlement`、`QuestNode_GetSiteTile`(加強版,靠近聚落)、`QuestNode_GetRandomHostileFaction`、`QuestNode_IsKiiroFactionSatisfied`(關係門檻)、`QuestNode_ModSettingOn`(mod 設定逐任務開關)、`QuestNode_IsRightSeason`、`QuestNode_RandomValue`、`QuestNode_FactionGoodwillAffect`(改好感+記 HistoryEvent)、`QuestNode_ResourceRequest_Initiate`(→`TradeRequestFulfilled` 信號)、`QuestNode_ExtraInspectStringAddOn`。
- Root 型（整個任務寫死 C#，XML 只剩殼；`isRootSpecial=true`）：`QuestNode_Root_Kiiro{Beggars,HarvestDay,Hospitality_Refugee,MedicalAssistance,RefugeeBaby,RelativeJoin_WalkIn,WandererJoin_WalkIn}`。
- Site GenStep：`GenStep_ReinforcedOutpost`(`widthRange`/`guardsCountRange`)、`GenStep_PreciousLump`、`GenStep_Pirates`、`GenStep_Signal_EnemyWalkIn`(伏擊觸發)、`GenStep_KiiroSettlement` 等。
- SitePartWorker：`SitePartWorker_HostileFaction`、`_ReinforcedOutpost`、`_ConditionCauser`、`_ThrumboFound`、`_PrisonerFree`。
- 劇情門檻（來自 `AncotLibrary` 共用庫）：`QuestNode_PlayerWealth`/`QuestNode_Colonists`/`QuestNode_DaysPassed`/`QuestNode_LordJob_ExitMapBest`/`QuestNode_EndGame`。

---

## F. C# 側 API（✅ F7 側寫器已用、編譯過）

- **寫/讀 CQF 全域 bool**（讓 C# 與對話 `DialogCondition_Bool` 互通）：
  `QuestEditor_Library.GameComponent_Editor.Component.SetBool(string key, bool value)` / `.GetBool(string key)`。
- **本包 F7 用法**：`MapComponent`（遊戲自動對每張地圖實例化）override `MapComponentTick`，讀 `map.wealthWatcher.WealthTotal`／`map.mapPawns.FreeColonistsSpawned(Count)`／`pawn.skills.skills[i].def/.Level`／`SkillDefOf.*`，寫 `RatkinQL_State_*`。
- **編譯**（build 環境：dotnet10 / mcs / Assembly-CSharp 都在）：
  ```
  mcs -target:library -out:1.6/Assemblies/RatkinQuestlines.dll \
    -r:<Managed>/Assembly-CSharp.dll -r:<Managed>/UnityEngine.CoreModule.dll \
    -r:<workshop>/2978572782/1.6/Assemblies/net48/QuestEditor_Library.dll \
    Source/*.cs
  ```
  `<Managed>`=`~/.local/share/Steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed`。DLL 放 `1.6/Assemblies/`（LoadFolders 1.6 自動載）；package-mod.sh 白名單含 1.6、剝除 Source。

---

## G. 鼠族 defName（✅ 本包已核對存在＋型別）

- **FactionDef**：`Rakinia`(NewRatkinPlus,和平王國)、`Rakinia_Warlord`(軍閥,naturalEnemy→暗殺/來襲敵源)、`RKU_Faction`(游擊隊,mod `RKU.RatkinUnderground`)、`ZHP_Faction`(雜貨鋪,`W.ZHP`)、`Rakinia_TravelRatkin`(旅鼠聯邦,Gene Expand)。隱藏無好感：`RK_Faction_Caravan`/`RK_Faction_Pilgrims`(好感掛母版派系)。
- **ThingDef(物品)**：`RK_Food_Hardtack`、`RK_StrawberryBeer`(⚠`RK_BeerBottle` 是 ToolCapacityDef 非 ThingDef,勿當物品)。
- **PawnKind/戰鬥**（force-provider 稿）：`RatkinCombatantWarlord`/`RatkinMurderer`/`RatkinMercenary`/`RKU_Scout`；騎士團 `RKK_*`（來自 `RKK.RatKnights.Core`）。
- **武器**（Weapons+ `bbb.ratkinweapon.morefailure`，⚠大量註解死代碼勿用）：`RK_AssassinKnife`/`RK_Poison`/`RK_Rapier`/`RK_KiteShield`/`RK_AssaultRifle` 等。
- 原版：`Silver`/`Pemmican`/`MedicineHerbal`/`WoodLog`/`PlayerColony`(isPlayer FactionDef)/`RaidEnemy`(IncidentDef)。

---

---

## H. ★★OberoniaAurea_Frame（OA 共用框架）——本輪最大發現（風雪遺孤/Bert 偵察）

> `OARK.OberoniaAurea.Framework`（DLL `OberoniaAurea_Frame.dll`，命名空間 `OberoniaAurea_Frame`；**已在 2-anime modlist**）。所有 OA 系鼠族 mod 共用。**對我 ratkin 包最直接可用——可依賴它、複用它的鼠族感知 QuestNode，省掉自己寫 C#。**

- **`QuestNode_GetRatkinFaction : QuestNode_GetFaction`**（靠 `faction.IsRatkinFaction()`）——**一個節點把任務限定在鼠族派系**。site/威脅任務取敵源、或取友方鼠族聚落都靠它。
- **`QuestNode_Root_RefugeeBase` ＋ `HospitalityRefugeeBase.xml`／`_Female.xml` 抽象 QuestScriptDef 模板**——現成可 `ParentName` 繼承的「難民/孤兒借宿→有機率加入」quest（含 grammar rules、`allowAssaultColony/allowLeave/allowJoinOffer/childCount` 參數）。**F3 收容/託孤/風雪遺孤 直接繼承這個，比手刻 lodger 邏輯省太多。**
- **世界物件/座標工具**：`QuestNode_GetNearTile`、`QuestNode_GetNearbySettlementOfFaction`、`QuestNode_GetFactionLeader`、`QuestNode_GetMapParent`、`QuestNode_GetWorldObjectTile`、`QuestNode_GetDropSpot`。
- **多陣營世界物件**：`QuestNode_GenerateWorldObjectWithMutiFactions`／`QuestNode_GetMutiFactions`／`QuestNode_SetMutiFactionsForWorldObject`＋`WorldObject_WithMutiFactions`（同一據點涉多派系，如鼠族兩派角力）。
- **可互動世界據點＋商隊到訪對話**：`WorldObject_InteractiveBase`／`WorldObject_MutiInteractiveBase`＋`CaravanArrivalAction_VisitInteractiveObject`＋`LordJob_VisitColonyTalkable`／`LordJob_TravelWithInteraction`／`LordToil_DefendPointWithInteraction`（做「可對話的世界據點/驛站/比武場」）。
- **貿易請求**：`QuestNode_InitiateTradeRequest`／`_InitiateSaleRequest`／`_InitiateCategoryTradeRequest`＋`CategoryTradeRequestComp`。
- **通用信號/計時/外交/信件節點**：`QuestNode_FireIncident`、`QuestNode_EarliestDayPassed`、`QuestNode_SeasonalRestriction`、`QuestNode_MultiSignalCount`、`QuestNode_PawnNegativeSiganl`、`QuestNode_AllFactionsGoodwillChange`、`QuestNode_ChoiceLetter`、`QuestNode_DestroyWorldObject`、`QuestNode_TransToHardcodedSignalWithQuestID`。
- **靜態工具類**：`OAFrame_PawnUtility`(`RemoveFirstHediffOfDef` 等)、`OAFrame_DiaUtility`(`DefaultConfirmDiaNodeTree` 標準彈窗)、`OAFrame_CaravanUtility`、`OAFrame_CollectionUtility`。
- **`FactionTagsExtension : DefModExtension`**（`factionTags`+`HasTag()`）——陣營打標籤（`IsRatkinFaction()` 疑基於此），可擴充鼠族子陣營標記。

## I. ★原版 site 攻打 quest 標準骨架（Kiiro＋Milira 雙重確認，皆用 Ludeon Core 公用 subscript）

> 這套是 RimWorld 原版**所有** site 攻打任務的骨架，**不是自製**，`Util_GenerateSite` 內部已接好 `site.MapGenerated`/`site.AllEnemiesDefeated`/`conditionCauser.Destroyed` 標準信號＋成功/失敗收尾。照抄只需換 `siteFaction`/`sitePartsTags`/`minThreatPoints`。
```
Util_RandomizePointsChallengeRating (SubScript, 1~3星難度)
→ (可選) QuestNode_EvaluateSimpleCurve  (points→威脅點曲線)
→ QuestNode_GetMap / QuestNode_GetSiteTile
→ QuestNode_QuestUnique tag="..."          (同 tag 去重，避免多個同時)
→ QuestNode_ViolentQuestsAllowed → 設 $siteThreatChance  (尊重玩家禁暴力設定)
→ QuestNode_Set siteFaction=<敵派系, 如 Rakinia_Warlord 或 OA QuestNode_GetRatkinFaction 取>
→ QuestNode_Set sitePartsTags={tag:<原版 BanditCamp/Outpost 或自製>, chance:$siteThreatChance}
→ QuestNode_GetSitePartDefsByTagsAndFaction → QuestNode_GetDefaultSitePartsParams
→ QuestNode_SubScript def="Util_GenerateSite"     (★內接標準信號)
→ QuestNode_SpawnWorldObjects
→ QuestNode_WorldObjectTimeout (inSignalDisable="site.MapGenerated" → 逾時 Fail)
→ QuestNode_NoWorldObject → QuestNode_End         (site 被毀/離開收尾)
成功：Util_GenerateSite 依 site part 自動接 `site.AllEnemiesDefeated`(清場) / `conditionCauser.Destroyed`(摧毀目標) → End Success
```
- 範本檔（可照抄）：Milira `xml-notuse/QuestSpecialDef/Script_Milira_Airportattack_VH.xml`（`siteFaction=Milira_AngelismChurch`,`sitePartsTags=TOM_milira_airportattack_VH`,`minThreatPoints=400`）；Kiiro `Script_KiiroPiratesOutpost_Threat.xml`（多了「不作為有代價」＋「派系翻臉自動中止」，見 §C）。
- **SitePartDef 最省寫法**：`workerClass=SitePartWorker`(純原版)＋`wantsThreatPoints=true`＋`minThreatPoints`＋`minMapSize`＋`tags`＋一個 `GenStepDef`(`linkWithSite`)。地圖佈局若要整塊防守基地：Milira 用 **`GenStep_Scatterer`＋`PrefabDef`＋`PrefabUtility.SpawnPrefab`** 貼預製地圖塊（比散置 pawn/turret 省事）；Kiiro 用參數化自製 `GenStep_ReinforcedOutpost`(`guardsCountRange`/`widthRange`)更靈活。

## J. 劇情狀態機模式（風雪遺孤 ScenPart 三件套 ／ Bert WorldComponent）

**模式一「劇本專屬旗標」三件套（風雪遺孤，純原版 API）**：
1. 自製 `ScenPart : ScenPart`——`PostGameStart()` 呼叫 GameComponent 的 `Notify_XActive()`；可在 `PlayerStartingThings()` 程式化塞初始 pawn/寵物（含馴化/Bond）。
2. 存檔級 `GameComponent`——`ExposeData` 持久化狀態（active/protagonist/inProgress/finished…）；`GameComponentTick` 每 15000 tick 輪詢推進劇情；`Notify_*` 狀態轉換 API；結局收尾範式（強制正常速度＋BGM＋`ScreenFader.StartFade` 漸白倒數→`EndGame`）；無主角時跳 `DiaNode` 讓玩家重選（容錯）；內建 Dev Debug Window。
3. 一行 `QuestNode`（override `TestRunInt` 回傳 `GameComponent.Instance.StoryActive`）——當所有專屬 QuestScriptDef 的**守衛節點**（塞在 Sequence 最前），「僅本劇本存檔生效」。

**模式二 Bert `WorldComponent` 劇情核心（多章節劇情最省事骨架）**：
- `storyFlags: Dictionary<string,int>`——全域旗標存讀；XML 端 `QuestNode_BertGlobalFlag`(讀寫)／`QuestNode_BertStoryFlagEquals`(判斷分支)／`QuestNode_BertScheduleStoryQuest`(排程延遲觸發下一章)。
- `storyNpcs: Dictionary<string,Pawn>`——**固定命名 NPC 註冊表**，首次 `GetOrCreateXxxNpc()` 生成鎖姓名/外觀，之後跨任務跨章節複用同一 Pawn。
- **★`ProtectRegisteredStoryNpcsFromGc()`**——防劇情 NPC 被 WorldPawns GC 回收（`PassToWorld(...KeepForever)`＋`ForcefullyKeptPawns.Add`，每 2500 tick 檢查）。**任何用固定命名 NPC 跑多章節劇情都會踩這個坑，值得先抄。**
- 排程佇列 `ScheduleStoryQuest`/`FireDueScheduledStoryQuests`（tick 存檔、到期判 flag 後 `QuestUtility.GenerateQuestAndMakeAvailable`）。

## K. 可複用模式雜項（跨三家）

- **`HediffAbility_HostDialog`＋`ModExtension_DialogHost{dialogManagerDefName}`**（Bert）——給任何 Hediff 掛 CQF 對話樹：pawn 一有此 Hediff 就自動 `GameComponent_Editor.AddDialog(pawn, dm)`，**不用每次寫 quest 節點綁對話**。若鼠族要「某 NPC 身上帶劇情對話」，這個超好用。
- **`QuestNode_JoinColonyOnSignal`（含 QuestPart，Bert）**——收信號時 `SetFaction(Faction.OfPlayer)` 收編＝跟我方 `CQFAction_Faction` 同效，但可掛在 quest 信號上（延後收編）。
- **雙軌 IncidentDef 出現節奏（Bert，純 XML 零自製 Comp）**：同一支線配一對 `IncidentDef`——`category=Misc` 低機率（原版隨機池）＋自訂 `IncidentCategoryDef` 高機率，後者靠自訂 `StorytellerDef`＋`StorytellerCompProperties_CategoryMTB`（`mtbDays`）在「解鎖後編年史模式」高頻出現，`listVisible=false` 動態解鎖。做「稀有支線 vs 解鎖後主線接續」不用寫 C#。
- **一個 IncidentWorker 服務多 IncidentDef（Bert `IncidentWorker_GiveBertQuest`）**：靠 `switch(questDef.defName)` 讀 Mod Settings 開關決定放行；省樣板類（缺點：defName 打錯靜默失效）。
- **`QuestPart 常駐計時器`（風雪遺孤）**——`QuestNode.RunInt()` new 一個 QuestPart 掛 quest 上，長期監聽/生成威脅波次（雪原求生倒數）。
- **Hediff 當劇情階段旗標（風雪遺孤）**——add/remove 互斥的兩顆 Hediff（如「渴望歸鄉/已歸鄉」）代表 quest 階段。
- **`IncidentWorker_MakeGameCondition_*` 子類（風雪遺孤）**——一行子類接自己的天氣 GameConditionDef。
- **加權結果表（Milira）**——WorldObject 抵達後 `List<Pair<Action,float>>`＋`RandomElementByWeight` roll 加權結果（探索/遭遇型輕量事件，不需生地圖）。
- **`AncotLibrary.QuestNode_IsFactionRelationKind`**（`factionRelationKind`/`invert`）——查派系敵對/非敵對的門檻節點（Milira 全用它）。

## L. ⚠ 反編譯內嵌 vs 硬依賴（Bert 的做法，取捨參考）

Bert 把 OA_Frame 的類（`WorldObject_InteractiveBase`/`MapParent_Enterable`/`CaravanArrivalAction_*`/`QuestPart_FireIncident`/`QuestNode_QuestUnique`/`CooldownRecord` 等）**反編譯改 namespace 內嵌**進自己 DLL，繞開硬依賴。→ 我方選擇：**OA_Frame 已在 2-anime，建議直接硬依賴＋引用它的節點**（乾淨、跟隨更新），不必內嵌重編。

---

> **方針（三家＋Kiiro 共同印證）**：
> 1. **CQF 定位＝對話樹引擎，不是 quest 流程引擎**（Bert 明證：其 CQF `Quests/Data|Group|Rule` 全空，quest 流程一律原版 `QuestScriptDef`）。我方沿用此分工。
> 2. **site 攻打/威脅＝原版 Core 公用 subscript 骨架**（§I），`Util_GenerateSite` 已接好完成信號，換 faction/tags 即可，**不再是盲賭**。
> 3. **鼠族專屬邏輯優先複用 `OberoniaAurea_Frame`**（`QuestNode_GetRatkinFaction`/`QuestNode_Root_RefugeeBase`…，§H），而非自寫 C#；只有它沒有的才自己寫（C# 有編譯閘門、可靠）。
> 4. **多章節劇情用 WorldComponent 狀態機＋固定 NPC 註冊表＋GC 保活**（§J 模式二）。

---

## M. F8 新驗積木（2026-07-18，商團首領/武器商弧偵察出）

- **★`CQFAction_Spawn` 能生 pawn（非只物品）**：`datas` = `List<LootData>`；`LootData` 兼含 `things`(物品) 與 **`pawnDatas`(`List<PawnSpawnData>`)**。`PawnSpawnData` 欄位：`kind`(PawnKindDef)/`extraKinds`/`count`(IntRange)/`faction`(可設 `PlayerColony`→直接生成為玩家陣營)/**`dialogManager`(生成後自動 `AddDialog(pawn,dm)` 綁對話)**/`way`(ArrivingWay，DropPod 等)/`spawnType`/`duty`/`hediffs`/`inventoryThings`/**`actions`(per-pawn CQFAction，生成後對該 pawn 跑)**/`spawnMessage`。⚠ 只在**對話** context 可用（有 Interviewer/Interviewee target 定位）；純腳本 `QuestPart_DoCQFActions` 用**空 targets** 跑 action → 目標型 action（含 Spawn）在腳本內生不出東西（坐實了「發實體獎勵必走原版節點」）。
- **自製 CQFAction 可行**：`Class="RatkinQuestlines.CQFAction_SetName"` 這種本包自製 `CQFAction_Target` 子類，Verse 標準 XML 載入吃得下（healthcheck 只驗 `QuestEditor_Library.` 前綴、跳過自製類，正確）。子類 override `RealWork(targets,quest)`（base `Work()` 已把 `targetsText`→targets 解析好）＋`ExposeData`(呼 base+Scribe 自欄位)。**`CQFAction_SetName`**：設 `pawn.Name=new NameTriple(first,nick,last)`＋選填 `gender`("Female"/"Male")→`pawn.gender=`＋`pawn.Drawer.renderer.SetAllGraphicsDirty()`。用於「具名人物加入」正名＋固定性別。
- **具名加入殖民地**：`CQFAction_Faction`(targetsText=Interviewee, faction=`PlayerColony`) 收編＋`CQFAction_SetName` 正名。⚠ 收編的是「當下對話那位訪客」→外形/技能仍隨機；要全固定得改用專屬 PawnKindDef＋PawnSpawnData 生成（未驗證路徑）。
- **★召友軍（原版盟友軍援機制）**：`IncidentDefOf.RaidFriendly.Worker.TryExecute(parms)`，`parms.faction=Rakinia`、`raidStrategy=RaidStrategyDefOf.ImmediateAttackFriendly`、`raidArrivalMode=PawnsArrivalModeDefOf.EdgeWalkIn`（非敵對→內部走 `LordJob_AssistColony(Faction,IntVec3)`）。包成可用道具＝`CompProperties_Usable`(useJob=UseArtifact,showUseGizmo)＋自製 `CompUseEffect`＋`CompProperties_UseEffectDestroySelf`(一次即毀)。
- **編譯**：mcs 用 `KeyValuePair<>` 陣列/字典時要多帶 **`-r:$MANAGED/netstandard.dll`**（否則 `System.ValueType` 型別找不到）；且 mcs 不支援 tuple 解構語法，改 KeyValuePair。
- **DeepSeek 量產坑（本輪新踩）**：偶爾把 `<key>v</key>` 寫成 **`key="v"` INI 風**（合法 XML 但零子元素→key 全缺）；偶爾把**閉標籤大小寫寫錯**(`</Ratkinql_…>`)。parse 後要 `xml.etree` 逐檔驗＋掃 open/close 大小寫一致。
