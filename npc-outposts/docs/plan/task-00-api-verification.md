# Task 0: API 殘餘驗證（grep 反編譯源碼）

**目的**：索引已坐實大項；本 task 補殘餘 9 項，全部在 `C:\code\mine\pas\projects\rimworld` 用 Grep/Read 確認。任何偏差記入 `npc-outposts/session_log.md` 並回頭修正後續 task 的對應程式碼。

**Files:**
- Modify: `npc-outposts/session_log.md`（建檔，記錄結果）

- [ ] **Step 1: CaravanArrivalAction_AttackSettlement 全文**

Read `RimWorld.Planet\CaravanArrivalAction_AttackSettlement.cs`。確認：
- `CanAttack(Caravan, Settlement)` 的條件（預期：`settlement != null && settlement.Spawned && settlement.Attackable` + `EnterCooldownBlocksEntering` 檢查）
- `GetFloatMenuOptions` 用 `CaravanArrivalActionUtility.GetFloatMenuOptions` 的呼叫形狀
- `Arrived` 是否直接呼叫 `SettlementUtility.Attack`

Task 4 的 `CaravanArrivalAction_AttackOutpost` 照此鏡像（僅 `Arrived` 改走 `OutpostAttackUtility.Attack`）。

- [ ] **Step 2: CaravanArrivalActionUtility.GetFloatMenuOptions 簽名**

Grep `GetFloatMenuOptions` in `RimWorld.Planet\CaravanArrivalActionUtility.cs`。預期泛型：
`GetFloatMenuOptions<T>(Func<FloatMenuAcceptanceReport> acceptanceReportGetter, Func<T> arrivalActionGetter, string label, Caravan caravan, PlanetTile pathDestination, WorldObject revalidateWorldClickTarget)`。

- [ ] **Step 3: Settlement.Name setter**

Read `RimWorld.Planet\Settlement.cs` 的 `Name` 屬性（預期 `nameInt` get/set，set 可自由呼叫）。Task 6 `OutpostPlacer` 用 `outpost.Name = …`。

- [ ] **Step 4: Settlement 的 AttackCommand icon 路徑**

Grep `AttackCommand|UI/Commands/AttackSettlement` in `Settlement.cs`。預期 `ContentFinder<Texture2D>.Get("UI/Commands/AttackSettlement")`。Task 4 的 gizmo 直接用同路徑。

- [ ] **Step 5: Rand.MTBEventOccurs 簽名 + GenDate.TicksPerDay**

Grep `MTBEventOccurs` in `Verse\Rand.cs`。預期 `static bool MTBEventOccurs(float mtb, float mtbUnit, float checkDuration)`；`GenDate.TicksPerDay == 60000`。

- [ ] **Step 6: WorldObjectCompProperties 形狀**

Read `RimWorld.Planet\WorldObjectCompProperties.cs`。確認 `compClass` 欄位名與 XML 寫法（`<li Class="…Properties 類">` 或 `<compClass>`）。Task 3 的 comp patch 照實際寫。

- [ ] **Step 7: WorldObjectsHolder.Settlements 子類註冊**

Grep `Settlements|AddToCache` in `RimWorld.Planet\WorldObjectsHolder.cs`。確認 `is Settlement` 型別歸類（子類自動入列）。

- [ ] **Step 8: vanilla Settlement WorldObjectDef 的貼圖路徑**

先 Glob `projects/rimworld/**/WorldObjects*.xml`（反編譯庫可能無 XML）。找得到 → 抄 `texture`/`expandingIconTexture` 確切值；找不到 → Task 4 XML 先用 `World/WorldObjects/DefaultSettlement` 與 `World/WorldObjects/Expanding/Settlement`，E2E 驗證（錯了頂多粉紅方塊，當場改）。

- [ ] **Step 9: IntVec3/IntRange 的 XML 解析格式**

Grep `ParseHelper` 或既有 mod XML 確認：IntVec3 → `(150,1,150)`；IntRange → `1~3`。

- [ ] **Step 10: 記錄與 commit**

session_log.md 新增「Task 0 API 殘餘驗證」節，逐項記 命中/偏差。

```powershell
git -C C:\code\mine\my_rimworld_mods add npc-outposts/session_log.md
git -C C:\code\mine\my_rimworld_mods commit -m @'
chore: npc-outposts Task 0 API 殘餘驗證記錄

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
