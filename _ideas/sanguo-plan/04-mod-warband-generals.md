# 04 — warband 將領戰力（E）

> 對應調查：E（:94-103）。依賴：`named-officers`（屬性 comp）。
> P1 首個消費 mod——最小、最快驗證屬性 comp 走通（`01` 建造順序）。

## 職責

每支 warband 掛一名**具名將領**（武力/統率屬性），名將帶兵更能打，
把抽象點數戰鬥變成「有人物的會戰」。**戰略抽象層三國志，非戰術單騎**（可達成度 ~80%，E:102）。

## 資料模型背景（E，:96）

- `Warband : WarObject : WorldObject`（`RW:13236/14157`），戰力＝抽象 `warPointsInt`/`pointDamageInt`→`EffectivePoints`。
- 已內建 `private List<Pawn> pawns`（`RW:14208`，深存 `RW:14557`）但抽象 warband 閒置時為空
  （真 pawn 只在打到地圖時 `GeneratePawnGroup RW:11521/11582` 臨時生）。
- 工廠硬編碼：`CreateWarObjectOfType RW:15358`→`CreateWarband RW:15467`→`MakeWarband RW:15518`（`new Warband()` + `RW_Warband` def）。

## 承載設計（E 推薦，:98）

- **WorldObjectComp**（Rim War 原生慣例、存檔相容最佳）——自訂 `CompProperties`
  Harmony 注入 `RimWarDefOf.RW_Warband.comps`，存將領武力/統率/智力/士氣（`Scribe_Values`）。
- **子類化否決**（工廠硬編碼、多呼叫點，E:98）。
- 將領 pawn：**MVP 走輕量佔位符**（string+ints，住 `named-officers` record）；
  進階才真 Pawn 深存（仿 VOE `AddPawn` 清 caravan/WorldPawns/holdingOwner，`VOE:1022`）。

## 戰力注入點（核心，E:100）

- `IncidentUtility.ResolveCombat_Units`（`RW:11271`，公式 `points × Rand × combatAttribute` 於 `RW:11290-11291`）
  → **postfix/transpiler 局部乘將領加成**。
- 聚落戰用 `ResolveCombat_Settlement RW:11018`。
- **勿直接改派系級 `combatAttribute`**（污染全派系，E:100）——與 G 同鐵律，但 E 可在局部公式乘故較易。
- 兩將不和打折：讀關係 B 軌（I，:146）。

## 顯示（E:101）

- `WarObject.GetInspectString`（virtual `RW:14860`）+ comp `CompInspectStringExtra`/gizmo；
  仿 `Settlement_InspectString_WithPoints_Postfix RW:5977`。

## 存檔策略

- comp 屬性走 `Scribe_Values`；將領 record 走 `named-officers` 層。
- 注入 comp 在工廠生 warband 後即附（Harmony postfix on `MakeWarband` 或 def comps 注入）。

## 風險

- 工廠多呼叫點 → 用 def comps 注入而非逐工廠 patch（涵蓋全生成路徑）。
- transpile `ResolveCombat_Units` 脆弱 → 優先 postfix 乘結果，transpile 為後備。
- 抽象 warband 無真 pawn 期間，將領只是 comp 數值（符合戰略抽象層定位）。
