# Session Log

## 2026-06-11

### Task 0: API 殘餘驗證（grep 反編譯源碼）

9 項全部命中，3 個對後續 task 的修正/加分發現：

1. `CaravanArrivalAction_AttackSettlement`（CaravanArrivalAction_AttackSettlement.cs:7-72）：與計畫鏡像碼一致（`CanAttack`＝Spawned+Attackable+EnterCooldown；`Arrived`→`SettlementUtility.Attack`）。**修正：原版 `GetFloatMenuOptions` 對中立/友好派系帶 `confirmActionProxy` 確認對話框（`ConfirmAttackFriendlyFaction` key，:64-70）——Task 4 攻擊選單照抄。**
2. `CaravanArrivalActionUtility.GetFloatMenuOptions<T>(Func<FloatMenuAcceptanceReport>, Func<T>, string, Caravan, PlanetTile, WorldObject, Action<Action> confirmActionProxy = null)`（CaravanArrivalActionUtility.cs:9）——比計畫多一個可選參數，相容。
3. `Settlement.Name` 是樸素 get/set（Settlement.cs:28-38）→ `OutpostPlacer` 直接賦值可行。**加分發現：`Settlement.ExpandingIcon => Faction.def.FactionIcon`（:40）——哨站大地圖圖標自動用派系圖標，def 的 `expandingIconTexture` 實際不被讀取（保留作 fallback 無害）。**
4. `AttackCommand = ContentFinder<Texture2D>.Get("UI/Commands/AttackSettlement")`（Settlement.cs:26）——Task 4 gizmo 同路徑。
5. `Rand.MTBEventOccurs(float mtb, float mtbUnit, float ticksSinceLastCheck)`（Rand.cs:509）；`GenDate.TicksPerDay` 常數存在。
6. `WorldObjectCompProperties.compClass`（RimWorld\WorldObjectCompProperties.cs:11，注意 namespace 是 RimWorld 非 RimWorld.Planet）——XML `<li Class="…">` 設子類即可，與計畫一致。
7. `WorldObjectsHolder.AddToCache`：`if (o is Settlement item2) settlements.Add(item2)`（WorldObjectsHolder.cs:233-235）——子類自動入 `Settlements`/`SettlementBases` 清單（交易/擊敗/任務系統可見，spec 已知互動）。
8. 反編譯庫無 vanilla Defs XML（Glob 無命中）→ `texture` 用 `World/WorldObjects/DefaultSettlement` 進 E2E 驗證；expanding icon 因第 3 點自動派系圖標，風險消除。
9. XML 解析：`IntVec3.FromString`（ParseHelper.cs:295）吃 `(150,1,150)`；`IntRange.FromString` 以 `~` 分隔（IntRange.cs:62-74）吃 `1~3`。

結論：計畫可照走；Task 4 攻擊 float menu 補 confirm proxy。
