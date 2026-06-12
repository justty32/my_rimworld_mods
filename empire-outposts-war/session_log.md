# session_log — empire-outposts-war

## 2026-06-12 — 初版實作（Mod 3 三合一互動膠水層）

### 背景精讀
- 構想：`_ideas/2026-06-12-empire-rimwar-war-cluster.md`（Mod 3，三項三合一互動全勾）。
- Mod 1 `npc-outposts-rimwar`：哨站貢獻 RimWarPoints（postfix IncrementSettlementGrowth）、
  ConvertSettlement 攔截處理「RimWar 路徑」哨站易主、戰局動態增減、`GrowthRateMultiplier` hook。
- Mod 2 `empire-warfare`：附庸淪陷走 `LifecycleRegistry.OnBattleResolved`＋`ColonyUtil.RemovePlayerSettlement`＋
  `AddNewHome` 建攻方聚落；`IBattleModifier` stub 預留；收復走 Capture＋OnSettlementCreated。
- npc-outposts 本體：`WorldComponent_OutpostSpawner` 已含 `ParentEligibilityOverride`
  （`static Func<Settlement,bool?>`）與 `GrowthRateMultiplier` 兩個 hook；`OutpostProfileResolver` 有 default profile。

### 讀源碼定案（Empire）
- `IBattleModifier.ModifyForce(MilitaryForce, bool isAttacker)`：唯一生產呼叫點在 `SimulateBattleFc.FightBattle`
  （MFA=attacker, MFB=defender，modifier 在戰前先跑）。`MilitaryForce` 有 public `militaryLevel/
  militaryEfficiency/forceRemaining/homeSettlement/homeFaction`。
- 附庸防守：defender force 的 `homeSettlement` 即該附庸 → 可數同主哨站。
- 玩家 Capture：attacker=玩家附庸（CreateMilitaryForceFromSettlement），defender=NPC
  （CreateMilitaryForceFromFaction，`homeSettlement==null`，僅 homeFaction）→ 用 CaptureContext 暫存目標 tile/聚落定位。
- `LifecycleRegistry.InvokeOnSettlementCreated` 從 `ColonyUtil.CreatePlayerColonySettlement` 觸發
  （Capture 勝利路徑）；`InvokeOnSettlementRemoved` 從 `RemovePlayerSettlement`（Mod 2 淪陷路徑）觸發。
- `ITaxTickParticipant.PostSettlementCreateTax(settlement, ref silverAmount, tithe)` 可加稅銀。
- Registry 不序列化、`EmpireCacheUtil.InvalidateAll`（ClearCaches/Dispose）會 ClearAll → 需 RegisterCacheInvalidator 重註冊。

### 實作（純膠水，最大化複用）
- 功能 1：註冊 `ParentEligibilityOverride`（opt-in，PColony 依設定納入）＋ XML patch 給 PColony 掛
  專屬 profile `pas_empire_war_Profile_Vassal`＋`ITaxTickParticipant` 加稅銀＋`IBattleModifier` 防守加成。
- 功能 2：單一 `IBattleModifier`（防守方按哨站數加 militaryLevel；附庸案例讀 homeSettlement、Capture 案例讀 CaptureContext）。
  唯一 Harmony＝Capture prefix/finalizer 設/清上下文，fail-soft。
- 功能 3：`OutpostTransferHooks : LifecycleParticipantBase` 雙向易主（OnSettlementCreated 奪城認領、
  OnSettlementRemoved 淪陷改派攻方）。spawner caps 反射搬鍵複用 Mod 1 手法。
- 去重：本 mod 全走 Empire 契約層，與 Mod 1 的 RimWar ConvertSettlement 路徑正交。

### 對本體改動
- npc-outposts：`ParentEligibilityOverride` hook（與既有 GrowthRateMultiplier 同款契約，null=零行為變化）。
- Mod 1 / Mod 2：零改動。

### 建置 / 健檢結果
- npc-outposts：Build succeeded, 0 Warning(s) 0 Error(s)。
- npc-outposts-rimwar（Mod 1，重編驗證）：0/0。
- empire-warfare（Mod 2，重編驗證）：0/0。
- empire-outposts-war（clean rebuild）：0/0。
- `tests/healthcheck.py`：healthcheck OK（修掉 prior session 殘留的尾端 `</parameter>` 雜訊行後）。
- 四個連帶 mod 的 healthcheck 皆 OK。

### 已知限制
見 PROJECT.md「已知限制」與 docs/plan §5。核心：玩家側 Capture 子功能依賴 capture patch；
緩衝層為抽象等價（哨站存活加防，非實體先被打）；平衡未實機調參；全部待 E2E。
