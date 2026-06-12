# P2 settlement-lords 實作計畫總覽

> 設計權威：`03-mod-settlement-lords.md`＋`01-architecture.md`（鐵律：勿動派系級
> `combatAttribute`/`growthAttribute`，領主倍率逐城 postfix 補乘/扣點）＋`09-roadmap.md`
> Phase 2 驗證標準。
> P0 API 契約：`named-officers/PROJECT.md`＋`OfficersApi.cs`（**只消費、不改 P0**）。
> 範本：P1 `warband-generals`（csproj ref／HarmonyInit TryPatch fail-soft／SignatureSpike／
> healthcheck 鐵律 guard／自有綁定 WorldComponent）＋ Mod 1 `npc-outposts-rimwar`
> `Patch_IncrementSettlementGrowth`/`GrowthCapFor`（**勿改 Mod 1**，上限鏡像自帶一份）。

## 目標與驗證標準（09 Phase 2）

每座 RimWar 追蹤的 NPC 聚落按機率掛一名具名領主（太守，讀 P0 政務/忠誠），
治理能力影響該城 RimWarPoints 成長/衰退。

1. 賢主城點數成長加速、庸主城淨衰退（衰退停在 getter 地板 100）。
2. 不污染同派系他城（逐城 postfix，全程不碰派系級係數——healthcheck 鐵律 guard）。
3. inspect 顯示領主（名字＋政務/忠誠＋治理係數）。
4. 鐵則：獨立編譯、不破壞前期、實機可觀察、存讀往返不丟資料。

## T0 spike 已定案的關鍵事實（依據反編譯，見 01-t0）

- `WorldComponent_PowerTracker.IncrementSettlementGrowth()`：**public 實例方法、無參數**
  （RW:17567）→ postfix 不需要 `__instance` 之外的注入；本 mod 自掃自家綁定即可。
- 成長公式（RW:17622-17631）：`num4=(Rand(2,3)+biome)×num×tech×派系成長係數×settlementGrowthRate`，
  `RimWarPoints += RoundToInt(Clamp(num4,1,100))`——下限鎖 +1 **永不為負** →
  「庸主淨衰退」只能 postfix 直接扣 `RimWarPoints`，不能靠改係數。
- **PointDamage>0 走療傷分支、當輪不成長**（RW:17616-17620，`if/else if`）→ postfix 鏡像跳過。
- 成長上限（RW:17597-17612）：基礎 50000、`City_Citadel` +5000、首都 +5000（Vassal +1000）
  → 鏡像為自帶 `GrowthCapFor`（抄 Mod 1 `OutpostRimWarUtility.cs:38`，**不 ref Mod 1 DLL**）。
- `RimWarSettlementComp.RimWarPoints`：getter clamp 100..100000（Vassal 100..10000，RW:9267 附近）、
  setter `Max(0,value)` → 衰退自然停在地板 100；摧毀城走 `ConvertSettlement`，**非本注入點**。
- **threading（RW:17062）**：`Settings.threadingEnabled` 時 `UpdateFactions()`（含本 postfix）
  跑在 tasker 背景執行緒 → postfix 不用 `Rand`、不發信件/Message、
  綁定取 snapshot、整體 try/catch（與 RimWar 自身在該執行緒讀寫 comp 同等風險面）。
- `Settlement.GetInspectString`：RimWar 自己就 postfix 它（RW:5977→6570）、Mod 1 未碰
  → 多 postfix 疊加安全，各 append 自己段。
- `WorldUtility.GetRimWarDataForFaction(Faction)`（RW:15146，public static）拿 `rwd.behavior`
  過濾 Player/Excluded（Mod 1 同款）。

## 由上述事實推出的設計決策

1. **綁定自有化（仿 P1 決策 1）**：本 mod 自設 `WorldComponent_SettlementLords` 存
   `host(Settlement, Scribe_References)↔recordId(int)`。host=Settlement 是穩定 WorldObject
   （不像 warband 會被吸進戰鬥容器），但仍自有化：P0 heal 在易主
   （`assignedTo.Faction != record.faction`）時會搶先解除指派（OfficerHealer 分支 3）、
   我方需要自己的易主偵測窗口與 G5 政策，不能依賴 `OfficersApi.GetOfficers(host)`。
2. **指派走心跳掃描**（聚落無單一生成工廠可鉤——世界生成期+RimWar `CreateSettlement`+
   Empire 各有來源）：每 2500 tick（offset 600，錯開 P0 的 0 與 P1 的 1200）掃
   `Find.WorldObjects.Settlements`，候選＝有 `RimWarSettlementComp`、Faction 非 null 非玩家、
   rwd.behavior 非 Player/Excluded、未綁定；按 `lordChance` 機率指派
   （`OfficersApi.CreateOfficer(faction, settlement, lordRole)`），每心跳至多
   `MaxNewLordsPerHeartbeat=5`（內建節流常數）。語意：機率控制「上任速度」，
   長期收斂為每城有太守（三國志語境合理）；0＝停用指派（既有領主照常運作）。
3. **易主/被毀處置（G5 政策）**：心跳逐綁定 heal——
   record 已被 P0 清（pawn 死亡 G5 收尾）→ 解綁（該城變無主、之後心跳自然補新太守＝繼任）；
   host Destroyed → `RemoveOfficer`＋解綁（城亡人去）；
   host.Faction != record.faction（易主）→ **先廣播 `LordEvents.LordLostSettlement(record, host)`
   （P4 叛變消費窗口）再 `RemoveOfficer`＋解綁**（預設政策＝退場；P4 要留人就在事件裡接走）。
4. **治理影響＝獨立 postfix**（與 Mod 1 哨站貢獻 postfix 疊加同方法、互不知情、各自鏡像
   PointDamage 跳過與上限）：逐綁定算 `GovernanceFactor`，`gov≥1` 補成長（clamp 上限鏡像）、
   `gov<1` 且衰退開關開 → 扣 `RimWarPoints`（setter Max(0,)、getter 地板 100 自然托底）。

## Harmony patch 清單（全部 TryPatch fail-soft，缺一降級不連坐）

| 目標 | 型式 | 用途 |
|---|---|---|
| `WorldComponent_PowerTracker.IncrementSettlementGrowth` | postfix | 逐城治理補成長/扣點 |
| `Settlement.GetInspectString` | postfix | 附領主行（名字＋政{0}忠{1}＋係數） |

**鐵律遵循**：全程不讀寫派系級 `combatAttribute`/成長係數（C# 源碼連字串都不得出現，
healthcheck guard 同 P1）；只動單城 `RimWarPoints`。

## 治理公式（T3）

```
score = 0.7 × polity + 0.3 × loyalty                  // 0..100，P0 G2 啟用維
gov   = Clamp(1 + (score - 50) / 50 × govAmplitude, 0.25, 2)   // 預設 0.5 → 0.5x..1.5x
delta = RoundToInt((gov - 1) × GovPointsScale)        // GovPointsScale=30（常數，每輪點數擺幅）
delta > 0：RimWarPoints = Min(points + delta, GrowthCapFor(...))（points≥cap 不動）
delta < 0 且 decayEnabled：RimWarPoints = Max(0, points + delta)（地板 100 由 getter 托）
```
量級核對：原版每輪成長 `Clamp(num4,1,100)`、低科技常態 1~5 點/輪；
amplitude=0.5 極端 ±15 點/輪（每日 24 輪＝±360）→ 賢主加速明顯、庸主必然淨衰退。
record null/dead → gov=1（不動）。

## Mod 骨架

- 路徑 `~/repo/my_rimworld_mods/settlement-lords/`；packageId `pas.officers.settlements`；
  Assembly `SettlementLords`；RootNamespace `pas.officers.settlements`；前綴 `pas_settlement_`。
- hard dep：brrainz.harmony＋Torann.RimWar＋pas.officers.community（modDependencies＋loadAfter）。
- csproj：Krafs.Rimworld.Ref 1.6.* ＋ RimWar.dll/0Harmony.dll（Steam 路徑，可 /p: 覆寫）
  ＋ `../../named-officers/1.6/Assemblies/NamedOfficers.dll`（Private=false）。
- Def：`pas_settlement_Lord`（pas.officers.OfficerRoleDef，leaderLike=true）。
- ModSettings：`lordChance`（指派機率，預設 0.25）、`govAmplitude`（治理幅度，預設 0.5）、
  `decayEnabled`（衰退開關，預設 true）。
- 三語 Keyed（English / ChineseSimplified / ChineseTraditional），key 集合三邊一致。

## 任務拓樸（每任務 build 綠燈再往下；每檔 <200 行）

```
T0 簽章 spike（計畫期完成；殘留物＝SignatureSpike.cs 編譯期釘）        → 01-t0
T1 骨架：About/csproj/Defs/Languages/Settings/HarmonyInit(空)＋healthcheck 雛形 → 02-t1
T2 生命週期：WorldComponent 綁定＋心跳指派＋易主/被毀處置＋LordEvents      → 03-t2
T3 治理＋顯示：IncrementSettlementGrowth postfix＋GetInspectString 附行    → 04-t3
T4 驗證：healthcheck 全量＋dotnet build 0/0＋E2E checklist               → 05-t4
```

## 不做（明確出界）

- 官員（領主以外的 N 名屬官）→ 後續期（G6 上限已留空間）。
- 稅收/防守/叛亂傾向管道（F 的另三管道）→ P3/P4。
- 領主 pawn 主動具現 → P0 已有 Materialize＋inhabitants 橋（玩家拜訪自然請回），P2 不主動呼。
- NpcOutpost 特判 → 不特判（它是 Settlement＋有 comp＝合法候選；衛星小城也有主簿，合語境）。
- 不 git commit、不部署。
