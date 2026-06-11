# Task 0: API 殘餘驗證（grep 反編譯源碼）

> 屬於 `../2026-06-11-implementation-plan.md`（索引含權威源座標、測試現實、commit 規則）。

**Files:** 無（只讀 `C:\code\mine\pas\projects\rimworld`）

設計引用了少數未逐字坐實的成員。逐條 grep，若結果與預期不符，**在後續 task 用實際名稱替換**並在 session_log.md 記錄。

- [ ] **Step 1: 驗證 think node 類別與欄位**

```
rg "class JobGiver_AIFightEnemies" C:\code\mine\pas\projects\rimworld --files-with-matches
rg "maxDistToDutyTarget" C:\code\mine\pas\projects\rimworld -l
rg "class ThinkNode_ConditionalCloseToDutyTarget" C:\code\mine\pas\projects\rimworld -l
rg "class JobGiver_WanderNearDutyLocation" C:\code\mine\pas\projects\rimworld -l
rg "class ThinkNode_Subtree" C:\code\mine\pas\projects\rimworld -A 5
```

預期：四個類別都存在；`ThinkNode_Subtree` 有 `treeDef` 欄位。若 `JobGiver_AIFightEnemies` 不存在改用 `JobGiver_AIFightEnemy` 的具體子類（rg "JobGiver_AIFight" 列出全部）。

- [ ] **Step 2: 驗證 Lord/Pawn 雜項成員**

```
rg "ForcedToJoinOtherLord" C:\code\mine\pas\projects\rimworld\Verse.AI.Group -l
rg "public Map Map" C:\code\mine\pas\projects\rimworld\Verse.AI.Group\Lord.cs C:\code\mine\pas\projects\rimworld\Verse.AI.Group\LordToil.cs
rg "AllowRestingInBed" C:\code\mine\pas\projects\rimworld\Verse.AI.Group\LordToil.cs -A 1
rg "SpawnedPawnsInFaction" C:\code\mine\pas\projects\rimworld\Verse\MapPawns.cs
rg "ParentFaction" C:\code\mine\pas\projects\rimworld\Verse\Map.cs
rg "public bool Sowable" C:\code\mine\pas\projects\rimworld\Verse\PlantProperties.cs
rg "public bool IsTable" C:\code\mine\pas\projects\rimworld\Verse\ThingDef.cs
rg "RandomElementByWeight" C:\code\mine\pas\projects\rimworld\Verse\GenCollection.cs -l
rg "class TransitionAction_WakeAll" C:\code\mine\pas\projects\rimworld -l
```

預期：全部命中；`LordToil.AllowRestingInBed` 預設 `true`（若預設 false，`LordToil_SettlementLife` 需 override 為 true）。`Map` property 在 `LordToil` 上不存在的話改用 `lord.Map`（`Lord.cs` 有）。

- [ ] **Step 3: 驗證 IsValidBedFor 簽名可被外部呼叫**

```
rg "public static bool IsValidBedFor" C:\code\mine\pas\projects\rimworld\RimWorld\RestUtility.cs -A 2
```

預期：public static，參數 `(Thing bedThing, Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool allowMedBedEvenIfSetToNoCare = false, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)`。

- [ ] **Step 4: 把任何偏差記入 session_log.md，commit**

```
git add sims-mode-community/session_log.md
git commit -m "chore: Task 0 API 驗證結果"
```
