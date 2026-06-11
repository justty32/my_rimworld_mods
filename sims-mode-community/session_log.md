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
