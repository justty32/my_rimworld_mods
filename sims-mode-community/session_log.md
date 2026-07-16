# Session Log

## 2026-06-11

### Task 0: API 殘餘驗證（grep 反編譯源碼）

全部命中，**零偏差**，後續 task 程式碼照計畫使用：

- `JobGiver_AIFightEnemies : JobGiver_AIFightEnemy`（RimWorld\JobGiver_AIFightEnemies.cs:6）；基類有 `protected float targetAcquireRadius = 56f` / `targetKeepRadius = 65f`（XML 可設 protected 欄位）
- `ThinkNode_ConditionalCloseToDutyTarget` 有 `private float maxDistToDutyTarget = 10f`（XML 可設）
- `JobGiver_WanderNearDutyLocation`（Verse.AI）、`JobGiver_AIDefendPoint`、`JobGiver_StandAndBeSociallyActive`、`TransitionAction_WakeAll` 全部存在
- `ThinkNode_Subtree` 有 `private ThinkTreeDef treeDef`（XML 可設）
- `PawnLostCondition.ForcedToJoinOtherLord` 存在
- `LordToil.Map`（LordToil.cs:18）與 `Lord.Map`（Lord.cs:58）都存在；`LordToil.AllowRestingInBed` 預設 `true`（LordToil.cs:26）→ 無需 override
- `MapPawns.SpawnedPawnsInFaction(Faction)` 存在；`Map.ParentFaction`（Map.cs:266）存在
- `PlantProperties.Sowable`（RimWorld\PlantProperties.cs:181——注意在 RimWorld namespace，非 Verse；不影響程式碼）、`ThingDef.IsTable`（ThingDef.cs:1008）存在
- `GenCollection.RandomElementByWeight<T>(IEnumerable<T>, Func<T, float>)` 存在
- `RestUtility.IsValidBedFor` 簽名逐字相符（RestUtility.cs:162）：`(Thing bedThing, Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool allowMedBedEvenIfSetToNoCare = false, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)`

### Task 1: Mod 骨架

About.xml + csproj（Krafs.Rimworld.Ref 1.6.*）建立，`dotnet build -c Release` 成功（0 警告 0 錯誤），產出 `1.6/Assemblies/SimsModeCommunity.dll`。

### Task 2–10: 實作完成

照計畫逐 task 執行，每 task 建置綠 + commit。與計畫的偏差只有兩處 using 補充（計畫程式碼漏列，API 本身無誤）：

- `FacilityMatcher.cs` 補 `using RimWorld;`（`Plant` 在 RimWorld namespace）
- `JobDriver_FakeWork.cs` 補 `using RimWorld;`（`RandomSocialMode` 在 RimWorld namespace）

驗證結果：
- `dotnet build -c Release`：0 警告 0 錯誤
- `python tests/healthcheck.py`：healthcheck OK（首跑即綠）
- 所有 C# 檔案 ≤ 94 行（最大 `MapComponent_FacilityRegistry.cs`），符合 ≤200 行約束

### 程式碼自我審查（對照反編譯源碼），修了 3 個 bug

1. **防禦卡死（高）**：`Trigger_PawnHarmed` 不過濾加害者（野獸傷害也觸發；Trigger_PawnHarmed.cs:37-60），而 life→defend 後唯一回程 `Trigger_BecameNonHostileToPlayer` 只在「曾敵對→解除」時觸發——非玩家來源傷害會讓聚落永久卡在防禦。修：新增 `Trigger_CalmNonHostile`（繼承原版 `Trigger_TicksPassedAndNoRecentHarm`，加「未與玩家敵對」條件），defend|assault → life，平靜 5000 ticks 回歸生活。零 Harmony。
2. **攻打中立聚落偏離原版（中）**：`SettlementUtility.AttackNow` 先生成地圖才翻敵對（SettlementUtility.cs:47→50），攻打中立聚落時本 mod GenStep 也會接管；守軍會被 `Trigger_BecamePlayerEnemy` 立刻切 defend（行為正確），但原 defend→assault 只有損失 20% 一個觸發，原版有 7 個（LordJob_DefendBase.cs:55-65）。修：整組觸發＋進攻警告訊息照抄原版（delayBeforeAssault=25000 同 SymbolResolver_Settlement.cs:45）。
3. **存讀檔 null key 紅字（中）**：pawn 死亡/離隊後 `roleAssignments` 殘留引用，銷毀後讀檔印 "Null key while loading dictionary"（Scribe_Collections.cs:400-405）。修：override `LordJob.Notify_PawnLost`（LordJob.cs:71 virtual，Lord.cs:700 會呼叫）清字典；另補 `base.ExposeData()` 呼叫（對齊原版慣例）。

附帶確認無誤的點：`JobMaker.MakeJob(JobDefOf.LayDown, bed)` 與原版 JobGiver_GetRest 同寫法；`Trigger_TicksPassed` 計數只在來源 toil 活躍時累積、從非來源 toil 進入會歸零（語意正確）；MapComponent 子類由引擎自動為每張地圖建構。

已知但接受的行為（記錄備查）：
- `pas_sims_Bed` matcher 用 `Building_Bed` 會掃到動物床——`IsValidBedFor` 會擋下，pawn fallback 睡地上（外觀小瑕疵，不爆錯）。
- life→defend 的 `Trigger_PawnHarmed()` 全靈敏度（被野獸咬也全聚落警戒 2 小時）——有了回歸退路後可接受，寧可過度反應。

驗證：`dotnet build` 0 警告 0 錯誤；`healthcheck OK`。

### Task 11: 實機 E2E（待執行）

需要 RimWorld 1.6 本體，依 `docs/plan/task-11-e2e.md` 清單手動驗證（載入/作息切換/部落 profile/翻臉防禦/攻打不受影響/存讀檔/離場）。新增驗證點：野獸傷害觸發防禦後約 2 小時內應自動回歸生活。
- 2026-07-10 規劃偵察完成：「NPC 據點人物生活」= 本 mod 的 P2（Talk 對話/租房留宿/工作板，R5 B 區），P1 代碼已完成僅剩 task-11 實機 E2E；具名家族 NPC 整合（idea 8 + named-officers OfficersApi + previouslyGeneratedInhabitants redress）是 P1→P2 的缺口；地基定調＝本 mod 的 VisitMap 套件（不用 workshop Visit Settlements）。另 city-economy 同日已接線完成（ModsConfig 已加、build/healthcheck 綠）待使用者實機 T5。下一步＝先跑 task-11 E2E，再開 P2 拆工單。
