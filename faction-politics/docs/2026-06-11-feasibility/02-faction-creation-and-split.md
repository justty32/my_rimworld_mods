# 02 途中造派系與分裂編排（API 全驗證）

## 途中造派系：可行，但要走對入口

| API | 簽名（已驗證） | 用途 |
|---|---|---|
| `FactionGenerator.CreateFactionAndAddToManager(FactionDef)` | `public static void`（FactionGenerator.cs:106） | ❌ 不用——void 拿不到新派系引用，且必造自動聚落 |
| `FactionGenerator.NewGeneratedFaction(PlanetLayer, FactionGeneratorParms)` | `public static Faction`（:130） | ✅ 主路徑 |
| `Find.FactionManager.Add(faction)` | `public`（FactionManager.cs:93，會 RecacheFactions + 通知各地圖） | ✅ 接在後面 |

`NewGeneratedFaction` 途中呼叫的行為（:130-188，逐行核對）：

1. 建 Faction、取 loadID、配色（:134-137）
2. `faction.hidden = parms.hidden`（:138）← **關鍵開關**
3. humanlike → 生 ideo（:139-146）
4. 取名（factionNameMaker，避撞名，:147-170）
5. 對**所有既有派系**建立雙向初始關係（:171-174，O(N)，無條件執行——hidden 也建）
6. **`if (!faction.Hidden && !factionDef.isPlayer)` 才自動造一個隨機 tile 的聚落**（:175-185）
7. `TryGenerateNewLeader()` 無條件造首領 world pawn（:186）

→ **`parms.hidden = true` 生成＝跳過自動聚落、其餘全套照走**。這是「分裂時完全自控據點歸屬」的正解。
（來源報告 §3.1 建議的 `NewGeneratedFactionWithRelations` 是錯路：它內部直接呼叫 `NewGeneratedFaction`（:210），跳不過自動聚落。）

## 事後揭示：安全

- `Faction.hidden` 是 `public bool?`（Faction.cs:54），`Hidden => hidden ?? def.hidden`（:200）。
- `FactionManager.AllFactionsVisible` 是即時 LINQ filter（FactionManager.cs:43：`allFactions.Where(fa => !fa.Hidden)`）→ 翻轉 `hidden = false` 即生效，**無 recache 需求**。

## 首領替換

- `Faction.leader` 是 public 欄位（Faction.cs:26）→ `newFaction.leader = rebelPawn` 直接賦值。
- 反叛者改隸：`rebelPawn.SetFaction(newFaction)`。
- `TryGenerateNewLeader` 自動生成的首領（:186）被替換後仍是 KeepForever world pawn。P1 接受此殘留（上限 5 次分裂＝最多 5 個閒置 pawn，與原版量級無感）；若 task-0 驗得 `WorldPawns` 有安全棄置 API（如 `RemoveAndDiscardPawnViaGC`）則順手清掉。

## 敵對設定：順序敏感（本調查新發現）

`HasGoodwill = !Hidden && !temporary`（Faction.cs:202-212）。兩個 `HasGoodwill` 派系之間 `SetRelationDirect` 會被防呆拒絕（:643-647，已驗證），只能走 goodwill 路徑。但 hidden 期間新派系 `HasGoodwill == false`，goodwill 路徑（`TryAffectGoodwillWith`）對它無效。

**採用順序**：先揭示（`hidden = false`）→ 再 `newFaction.TryAffectGoodwillWith(mother, newFaction.GoodwillToMakeHostile(mother), canSendMessage: false, canSendHostilityLetter: false, …)`（`GoodwillToMakeHostile` 是原版現成的「拉到敵對所需差值」helper，Faction.cs:550）→ 最後發自訂分裂 letter（壓掉原版敵對信避免重複通知）。

不採用的替代路：hidden 期間 `SetRelation(FactionRelation)`——雖然防呆不觸發，但它的雙向寫入**不複製反向 baseGoodwill**（Faction.cs:443-449，反向筆 kind 對、goodwill 留預設），且 kind 與 goodwill 不一致時 `CheckReachNaturalGoodwill` 的漂移行為未驗證。風險高於收益。

## 聚落倒戈

- `WorldObject.SetFaction(faction)`——原版造派系自己就用它掛聚落（FactionGenerator.cs:178），Settlement 是 WorldObject 子類。
- 倒戈集合從 `Find.WorldObjects.Settlements.Where(s => s.Faction == mother)` 抽選，比例由 profile 定，**母派系保底留 1 個**（避免瞬間 defeated）。
- 衛星哨站（npc-outposts 的 `NpcOutpost` 也在 Settlements 清單）不直接參與抽選、改為跟隨母聚落易主——經 bridge 謂詞排除/處理（詳 `04`）。

## 分裂編排全序（P1 規格）

```
1. newFaction = NewGeneratedFaction(Find.WorldGrid.Surface,
       new FactionGeneratorParms(mother.def, default, hidden: true))   // 同 def、無自動聚落
2. Find.FactionManager.Add(newFaction)
3. newFaction.leader = rebel; rebel.SetFaction(newFaction)             // （自動首領殘留，見上）
4. 倒戈：抽 mother 聚落（排除衛星）× defectFraction，逐一 SetFaction(newFaction)
       每筆觸發 bridge hook（哨站跟隨 + Rim War 同步）
5. newFaction.hidden = false                                           // 揭示（即時生效）
6. newFaction.TryAffectGoodwillWith(mother, GoodwillToMakeHostile(mother),
       canSendMessage:false, canSendHostilityLetter:false)             // 敵對
7. Find.LetterStack.ReceiveLetter(分裂信, lookTarget: 反叛者/倒戈聚落)
```

PlanetLayer：P1 固定 `Find.WorldGrid.Surface`（報告 §8 低風險項；軌道層派系不在 P1 範圍）。
