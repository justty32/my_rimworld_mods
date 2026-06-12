# empire-outposts-war 實作計畫（Mod 3 · 三合一互動膠水層）

> 2026-06-12。構想：`_ideas/2026-06-12-empire-rimwar-war-cluster.md` 的 Mod 3。
> 相依：Empire（Matathias.Empire）＋RimWar（Torann.RimWar）＋npc-outposts（pas.outposts.community）
> ＋Mod1（pas.outposts.rimwar）＋Mod2（pas.empire.warfare）＋Harmony。
> packageId：`pas.empire.outposts.war`。建置順序第三，純膠水。

## 0. 關鍵已核對事實（讀源碼定案）

| 事實 | 來源 | 對設計的影響 |
|---|---|---|
| `WorldSettlementFC : Settlement` | Empire `Worldobjects/WorldSettlementFC.cs` | **附庸本身就是 Settlement**，會出現在 `Find.WorldObjects.Settlements`，npc-outposts 的 spawner 迴圈天然看得到它。 |
| PColony 派系：非 hidden、非 IsPlayer、humanlike、`OutlanderFactionBase` | `Defs/FactionDefs/PColonyFaction.xml` | spawner 的 `settlement.Faction.IsPlayer` 閘**不會**擋掉附庸 → 預設它已可能被鋪 default profile 哨站（不可控）。本 mod 改為「明確 opt-in＋專屬 profile」。 |
| spawner 增生母體＝`Find.WorldObjects.Settlements` 中非 NpcOutpost、`Faction!=null`、`!Faction.IsPlayer`、`Resolve(faction)!=null` 者 | npc-outposts `WorldComponent_OutpostSpawner.InitializeNewSettlements`/`WorldComponentTick` | 要「明確且僅在開關開時」納入附庸，需一個 opt-in 接點，否則無法區分「附庸」與「一般 NPC 聚落」做差異化 profile/平衡。 |
| Empire 契約層：`LifecycleRegistry`（OnSettlementCreated/Removed/BattleResolved…）、`BattleModifierRegistry`（`IBattleModifier.ModifyForce(MilitaryForce, bool isAttacker)`）、`ThreatScalingRegistry`、`TaxTickRegistry`（`ITaxTickParticipant.PostSettlementCreateTax(settlement, ref silverAmount, tithe)`） | `Comps/Interfaces/FCInterfaces.cs`、`Util/Registries/*` | 三項功能能「免 Harmony 對 Empire」全走契約層。 |
| `MilitaryForce`：public `militaryLevel/militaryEfficiency/forceRemaining`、`WorldSettlementFC homeSettlement`、`Faction homeFaction` | `Military/MilitaryForce.cs` | `IBattleModifier.ModifyForce` 拿得到 homeSettlement/homeFaction → 可判「這支力量屬於哪個聚落/派系」加哨站加成。 |
| `MilitaryJobHandler_Capture.OnResolved`：守方＝`CreateMilitaryForceFromSettlement(milComp.WorldSettlement, true)`（attacker＝玩家附庸），敵方＝`CreateMilitaryForceFromFaction(militaryEnemy, false)`（`homeSettlement==null`）；勝利 → `ColonyUtil.CreatePlayerColonySettlement` → `InvokeOnSettlementCreated` | `Military/MilitaryJobHandler_Capture.cs`、`Util/ColonyUtil.cs` | ①Capture 時「敵方力量」的 `homeFaction`＝被攻 NPC 派系、`homeSettlement==null`→ 用 tile 找目標 NPC 聚落數哨站加敵防（功能 2 玩家側）。②奪城成功透過 `OnSettlementCreated` 偵測（功能 3 玩家側）。 |
| Mod2 淪陷走 `LifecycleRegistry.OnBattleResolved`→`pendingFalls`→`VassalFallUtility`（`ColonyUtil.RemovePlayerSettlement`＋`AddNewHome`），會觸發 `InvokeOnSettlementRemoved` | empire-warfare `VassalFallUtility.cs`、Empire `ColonyUtil.cs` | 附庸淪陷時哨站易主 → 接 `OnSettlementRemoved`（功能 3 附庸側）。攻方＝Mod2 在同 tick `AddNewHome` 的派系，用 tile 找新 NPC 聚落當新母。 |
| Mod1 易主只處理 RimWar `WorldUtility.ConvertSettlement` 路徑（NPC 互奪） | Mod1 `Patch_ConvertSettlement.cs` | **去重**：本 mod 只接 Empire 的 `LifecycleRegistry`（Capture/淪陷），與 Mod1 路徑不相交，不會雙觸發。 |

## 1. 三項功能的最終接點

### 功能 1：玩家附庸也長哨站（＋產出/防守加成）
- **增生**：在 npc-outposts 本體加 **一個 opt-in 接點** `WorldComponent_OutpostSpawner.ParentEligibilityOverride`
  （`static Func<Settlement, bool?>`，null 或回傳 null＝零行為變化）。本 mod 註冊：回傳
  `IsPlayerColonyFaction(s.Faction) ? settings.vassalOutpostsEnabled : (bool?)null`。
  → 附庸僅在開關開時被納入，且可給專屬 profile。
- **專屬 profile**：XML Patch 給 PColony 掛 `OutpostProfileExtension`→`pas_empire_war_Profile_Vassal`
  （radius/count/mtb 與 NPC 不同，平衡可調；type 沿用 `pas_outposts_Type_Generic`）。
- **產出加成**：`ITaxTickParticipant.PostSettlementCreateTax` → 每附庸按「同主存活哨站數×perOutpostSilver」加 `ref silverAmount`。
- **防守加成**：併入功能 2 的單一 `IBattleModifier`（附庸防守時 homeSettlement 即該附庸）。

### 功能 2：哨站參與防守與前哨戰（單一 `IBattleModifier`，零新 patch）
`OutpostBattleModifier.ModifyForce(force, isAttacker)`：
- **附庸被攻、附庸防守**（`!isAttacker` 且 `force.homeSettlement` 是玩家附庸）：數其同主、射程內存活哨站，
  `force.militaryLevel += n × defenseLevelPerOutpost`（緩衝層→加防守戰力的抽象等價）。
- **玩家 Capture NPC 聚落**（`!isAttacker` 且 `homeSettlement==null` 且 `homeFaction` 非附庸非玩家＝被攻 NPC 派系）：
  以 `homeFaction` 的「被攻聚落」反查不可得（force 無 tile）→ 改用 **Capture 專用前置**：在 `OnBattleResolved` 不適用（Capture 不發 vassal battle 回呼）。
  **採用**：對 NPC 防方，用 `homeFaction` 找其所有聚落不準。故玩家側削防改走 **CaptureContext**：
  Harmony postfix `MilitaryJobHandler_Capture.OnResolved` 不需要——`ModifyForce` 對 `homeSettlement==null & homeFaction=NPC`
  時，由 `OutpostWarState.CurrentCaptureTargetTile`（在 capture 前置 patch 設）定位目標聚落，數其哨站加敵防。
  → 需要一個**極輕量 Harmony prefix** 於 `MilitaryJobHandler_Capture.OnResolved` 設/清 `CurrentCaptureTargetTile`（fail-soft，找不到方法則玩家側削防降級停用，附庸側照常）。
- 克制：只調 `militaryLevel` 加法，夾上限；無哨站＝零變化。

### 功能 3：征服戰利品——哨站隨聚落易主（雙向，接 Empire LifecycleRegistry）
`OutpostTransferHooks : LifecycleParticipantBase`
- **`OnSettlementCreated(vassal)`**（玩家 Capture 奪下 NPC 聚落 → 同 tile 新建玩家附庸）：
  找「原本屬於該 tile 來源 NPC 聚落」的衛星哨站，改派給 PColony 派系、重掛新附庸為母。
  來源聚落已被 Capture `Destroy`，故以 **tile 鄰近＋原 ParentSettlement.Destroyed** 啟發式認領（記錄窗口由 capture prefix 暫存 tile→舊母引用更精準）。
- **`OnSettlementRemoved(vassal)`**（附庸淪陷，Mod2 已在同 tile `AddNewHome` 建攻方 NPC 聚落）：
  找該附庸的衛星哨站，改派給攻方（用 tile 上的新 NPC 聚落的 Faction＋當新母）。
- **去重**：兩條都只在 Empire 契約層觸發，Mod1 的 ConvertSettlement 路徑不經過 → 無雙觸發。
  另：易主後重掛母並更新 spawner caps（複用 Mod1 已驗證的反射搬鍵手法，fail-soft）。

## 2. 防衛式不變式
- 所有對 Empire/RimWar/Mod1/Mod2 的反射與 patch：簽章探測 fail-soft，缺型別/簽章不符 → **該子功能降級停用**，其餘照常，且只 Warn 一次。
- npc-outposts 本體唯一新增接點 `ParentEligibilityOverride`：null/回傳 null/異常一律視為「無意見」（零行為變化），與既有 `GrowthRateMultiplier` 同款契約。
- Capture prefix 找不到方法 → 玩家側削防＋玩家側哨站認領降級，附庸側（功能 1 防守、功能 3 淪陷）完全不受影響。
- 所有 WorldComponent 狀態可序列化；空集合 PostLoadInit 兜底。

## 3. 對本體 mod 的改動（應極少）
- **npc-outposts**：`WorldComponent_OutpostSpawner` 加 `public static Func<Settlement, bool?> ParentEligibilityOverride` 與兩處 gate 套用（Initialize + tick）。重編 0/0。
- **Mod1 / Mod2**：**零改動**（接點已足夠：Mod1 的 ConvertSettlement 與本 mod 路徑正交；Mod2 的淪陷透過 Empire `OnSettlementRemoved` 自然被本 mod 收到）。

## 4. 交付物
About/、Source/（.csproj＋C#）、Patches/（PColony profile）、Defs/（Vassal profile）、PROJECT.md、本計畫、session_log.md、tests/healthcheck.py、Languages（雙語 Keyed）。

## 5. 已知限制（E2E 待驗）
- 玩家側 Capture 削防依賴 capture prefix 設 tile；若 Empire 改 `OnResolved` 簽章則該子功能降級。
- 奪城認領哨站用 tile 暫存窗口；窗口外（極罕見時序）退化為鄰近啟發式。
- 哨站「射程」用 ApproxDistanceInTiles ≤ profile.radius.max + buffer 近似，非真實戰場緩衝。
