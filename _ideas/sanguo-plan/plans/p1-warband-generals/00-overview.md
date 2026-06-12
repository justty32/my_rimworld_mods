# P1 warband-generals 實作計畫總覽

> 設計權威：`04-mod-warband-generals.md`＋`01-architecture.md`（鐵律：勿動派系級
> `combatAttribute`/`growthAttribute`，逐 warband 局部乘）＋`09-roadmap.md` Phase 1 驗證標準。
> P0 API 契約：`named-officers/PROJECT.md`＋`OfficersApi.cs`（**只消費、不改 P0**）。
> 範本：`npc-outposts-rimwar`（TryPatch fail-soft / ModSettings / WarnOnce / healthcheck）。

## 目標與驗證標準（09 Phase 1）

每支 NPC warband 按機率掛一名具名將領（讀 P0 武力/統率），名將帶兵更能打。

1. 名將 warband 戰鬥點數明顯高於庸將（ResolveCombat_Units 局部乘）。
2. 存讀後將領屬性保留（record 走 P0；綁定走本 mod WorldComponent）。
3. inspect 顯示將領（名字＋武力/統率摘要）。
4. 鐵則：獨立編譯、不破壞前期、實機可觀察、存讀往返不丟資料。

## T0 spike 已定案的關鍵事實（依據反編譯，見 01-t0）

- `CreateWarband`：`public static Warband CreateWarband(int power, RimWarData rwd,
  Settlement parentSettlement, PlanetTile startingTile, WorldObject destination,
  WorldObjectDef worldDef, bool _launched=false, bool _interactable=true, int pointDamage=0)`
  （RW:15467）。`MakeWarband` 為 **private** → 鉤 CreateWarband（public、單一 overload）。
- `ResolveCombat_Units(WarObject attacker, WarObject defender)`（RW:11271）：內部
  `Rand × clamped points × combatAttribute` 算 num4/num5，產物只有
  `attacker.PointDamage += …; defender.PointDamage += …`（RW:11331-11332）
  → **prefix 快照雙方 PointDamage、postfix 對 delta 乘將領比值**（不碰公式內部、不 transpile）。
- `WarObject.GetInspectString`（RW:14860）override 自建 StringBuilder、**不走 comp
  CompInspectStringExtra** → P0 view comp 注入無效，必須 postfix 附行。
  `Warband` 不另 override（13236 起無 GetInspectString）→ patch WarObject 即覆蓋。
- **生命週期硬事實（本計畫核心設計依據）**：warband 交戰即被吸收進
  `BattleSite.Units`（RW:10954/14798）或 `RimWarSettlementComp.AttackingUnits`（RW:10345）
  並 `ImmediateDestroy()`（RW:10327-10338）——實例存活於容器（**兩者皆 LookMode.Deep 深存**，
  RW:8547 units / comp atkos），但 `WorldObject.Destroyed==true`。
  戰後倖存者經 `CreateWarObjectOfType`（RW:15358，dispatch 回 CreateWarband）**重生為新物件**。

## 由上述事實推出的三個設計決策

1. **綁定自有化**：P0 heal 在 `assignedTo.Destroyed` 時即解除指派（OfficerHealer 分支 3），
   warband 一進戰鬥就會被 P0 解綁 → 戰力 postfix 不能依賴 `OfficersApi.GetOfficers(host)`。
   本 mod 自設 `WorldComponent_WarbandGenerals` 存 `host(WorldObject ref)↔recordId(int)` 綁定
   （host 深存於 BattleSite/AttackingUnits → Scribe_References 可解析）。
   P0 的 `CreateOfficer/AssignOfficer` 照常呼叫（吃 G6 上限、事件、清理），但**讀取走自家綁定**。
2. **戰後傳承**：勝方 warband 經 `CreateWarObjectOfType` 重生 → 不傳承則「名將每勝一仗即消失」
   （功能半殘）。加 prefix 設 pending-transfer context（舊 warband 的將領 record），
   CreateWarband postfix 優先消費 context 轉指派、否則才 roll 新將。
3. **退場＝本 mod 心跳清理**（不訂 P0 事件——OfficerUnassigned 觸發時 assignedTo 已 null、
   無法分辨「戰鬥中暫離」與「真消亡」）：每 2500 tick 掃綁定，host 已毀**且**不在任何
   BattleSite.Units / AttackingUnits → `RemoveOfficer`（將領隨軍覆滅退場，G5 由 P0 清 opinion 鍵）。
   pawn 死亡（罕見，懶生成）走 P0 G5 → GetById 回 null → 解綁。

## Harmony patch 清單（全部 TryPatch fail-soft，缺一降級不連坐）

| 目標 | 型式 | 用途 |
|---|---|---|
| `WorldUtility.CreateWarband` | postfix | roll 將領（機率/條件）或消費 transfer context |
| `WorldUtility.CreateWarObjectOfType` | prefix+postfix | 設/清 transfer context（戰後傳承） |
| `IncidentUtility.ResolveCombat_Units` | prefix+postfix | PointDamage delta 局部乘將領比值 |
| `WarObject.GetInspectString` | postfix | 附將領行（名字＋武{0}統{1}） |

**鐵律遵循**：全程不讀寫 `rimwarData.combatAttribute`/`growthAttribute`；
只動單場戰鬥的 PointDamage delta（局部、對稱、clamp）。

## 戰力公式（T3）

```
score = (might + command) / 2                       // 0..100，P0 G2 啟用維
bonus = 1 + (score - 50) / 50 × bonusMax            // bonusMax 預設 0.30 → 0.7x..1.3x
ratio = Clamp(bonusAtk / bonusDef, 0.5, 2.0)
defender.PointDamage delta ×= ratio；attacker.PointDamage delta ÷= ratio
```
雙方皆無將 → 早退零開銷。I 段關係 hook：`GeneralsUtility.RelationFactor`
（`Func<OfficerRecord self, OfficerRecord enemy, float>`，預設 null＝1，P4+ 接）。

## Mod 骨架

- 路徑 `~/repo/my_rimworld_mods/warband-generals/`；packageId `pas.officers.warband`；
  Assembly `WarbandGenerals`；RootNamespace `pas.officers.warband`；前綴 `pas_warband_`。
- hard dep：brrainz.harmony＋Torann.RimWar＋pas.officers.community（modDependencies＋loadAfter）。
- csproj：Krafs.Rimworld.Ref 1.6.* ＋ RimWar.dll/0Harmony.dll（Steam 路徑，可 /p: 覆寫）
  ＋ `../../named-officers/1.6/Assemblies/NamedOfficers.dll`（Private=false）。
- Def：`pas_warband_General`（pas.officers.OfficerRoleDef）。
- ModSettings：`generalChance`（生成率，預設 0.5）、`bonusMax`（加成幅度，預設 0.3）。
- 三語 Keyed（English / ChineseSimplified / ChineseTraditional），key 集合三邊一致。

## 任務拓樸（每任務 build 綠燈再往下；每檔 <200 行）

```
T0 簽章 spike（已完成於計畫期；殘留物＝SignatureSpike.cs 編譯期釘）   → 01-t0
T1 骨架：About/csproj/Defs/Languages/Settings/HarmonyInit(空)＋healthcheck 雛形 → 02-t1
T2 生命週期：WorldComponent 綁定＋CreateWarband 生成＋transfer＋心跳退場   → 03-t2
T3 戰力＋顯示：ResolveCombat_Units 乘成＋GetInspectString 附行          → 04-t3
T4 驗證：healthcheck 全量＋dotnet build 0/0＋E2E checklist             → 05-t4
```

## 不做（明確出界）

- `ResolveCombat_Settlement`（聚落攻防將領加成）→ 後續期（與 P3 守城折算協調）。
- `LaunchedWarband`（空降，走 CreateLaunchedWarband 另一工廠）→ 不掛將領。
- 將領 pawn 真具現/真 Pawn 深存 → P0 已有 Materialize，P1 不主動具現（戰略抽象層）。
- 同陣營多將不和打折 → 只留 RelationFactor hook。
- 不 git commit、不部署。
