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

### Task 11: 實機 E2E（待執行）

需要 RimWorld 1.6 本體，依 `docs/plan/task-11-e2e.md` 清單手動驗證（載入/作息切換/部落 profile/翻臉防禦/攻打不受影響/存讀檔/離場）。
