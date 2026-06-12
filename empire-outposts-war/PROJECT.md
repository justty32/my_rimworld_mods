# empire-outposts-war（帝國哨站戰爭・三合一互動膠水層）

戰爭叢集（`_ideas/2026-06-12-empire-rimwar-war-cluster.md`）的 **Mod 3**：把 Empire（玩家附庸帝國）、
Rim War（NPC 大戰略）、npc-outposts（衛星哨站）叢集**接成一體**。純膠水層，最大化複用 Mod 1／Mod 2
與本體既有結構——對 Empire 全走契約層（Registry／介面），對本體只用既有 hook，唯一新增的 Harmony
接點是 Capture 上下文（且 fail-soft）。

- packageId：`pas.empire.outposts.war`
- 硬相依：Harmony（`brrainz.harmony`）＋Empire Refactored（`Matathias.Empire`）＋Rim War（`Torann.RimWar`）
  ＋npc-outposts（`pas.outposts.community`）＋Mod 1（`pas.outposts.rimwar`）＋Mod 2（`pas.empire.warfare`）；loadAfter 同六者。
- 構想出處：`../_ideas/2026-06-12-empire-rimwar-war-cluster.md`（Mod 3，使用者勾選三項三合一互動）。

## 目標 / 範圍（三項功能）

| # | 功能 | 最終接點 | 預設行為 |
|---|---|---|---|
| 1 | **玩家附庸也長哨站**（＋產出/防守加成） | npc-outposts 既有 `WorldComponent_OutpostSpawner.ParentEligibilityOverride`（opt-in）＋專屬 profile（XML）＋`ITaxTickParticipant`（產出）＋`IBattleModifier`（防守） | 附庸僅在開關開時納入，用 `pas_empire_war_Profile_Vassal`（略保守）；每存活同主哨站 +10 白銀稅／稅期、+0.35 軍事等級防守 |
| 2 | **哨站參與防守與前哨戰** | 單一 `IBattleModifier.ModifyForce`（走 `BattleModifierRegistry`，零新 patch on Empire）＋Capture 上下文（唯一 Harmony，fail-soft） | 防守方（附庸／NPC 被 Capture）按存活哨站數加 `militaryLevel`；玩家 Capture NPC 前，目標 NPC 哨站加敵防（拔哨削防）；數量夾上限 8 |
| 3 | **征服戰利品：哨站隨聚落易主（雙向）** | `OutpostTransferHooks : LifecycleParticipantBase`（走 `LifecycleRegistry`） | OnSettlementCreated（玩家 Capture 奪城）→ 來源哨站改派玩家附庸；OnSettlementRemoved（附庸淪陷，Mod 2 同 tile 建攻方聚落）→ 哨站改派攻方；無接收者則摧毀孤兒哨站 |

非目標：新 UI、馳援系統（Empire Patch-RW 已有）、改 RimWar 戰局公式（Mod 1 已做）、Empire 原生襲擊互動。

## 技術棧 / 架構

- RimWorld 1.6 / net48 / Krafs.Rimworld.Ref 1.6.*；Harmony 2。
- **對 Empire 走契約層（B-1，免 Harmony）**：`BattleModifierRegistry`（`IBattleModifier`）、
  `TaxTickRegistry`（`ITaxTickParticipant`）、`LifecycleRegistry`（`LifecycleParticipantBase`）。
  `Game.ClearCaches` 會清空 Registry → 配 `EmpireCacheUtil.RegisterCacheInvalidator` 自動重註冊（同 Mod 2 慣例）。
- **對 npc-outposts 走既有 hook（零本體改動）**：`ParentEligibilityOverride`（`static Func<Settlement,bool?>`，
  null/回傳 null/異常＝零行為變化）。此 hook 由 Mod 1/3 共用，已存在於 npc-outposts 本體（與 `GrowthRateMultiplier` 同款契約）。
- **唯一 Harmony 接點**：`MilitaryJobHandler_Capture.OnResolved` 的 prefix/finalizer 設/清 `CaptureContext`
  （供功能 2 玩家側削防與功能 3 奪城認領定位來源聚落）。簽章探測 fail-soft——方法不存在則玩家側子功能降級、附庸側照常。
- **與 Mod 1 去重**：Mod 1 只攔 RimWar `WorldUtility.ConvertSettlement`（NPC 互奪路徑）；本 mod 只接 Empire
  `LifecycleRegistry`（Capture／淪陷路徑）。兩條路徑正交、不相交，同一次易主不會被兩邊重複改派。

### 檔案導覽

| 檔 | 職責 |
|---|---|
| `Source/OutpostsWarInit.cs` | 入口：註冊 ParentEligibilityOverride、掛 Capture patch、Registry 註冊＋重註冊防護 |
| `Source/OutpostsWarMod.cs` | `Mod`＋`ModSettings`＋設定 UI（五項旋鈕） |
| `Source/OutpostBattleModifier.cs` | 功能 1 防守面＋功能 2：`IBattleModifier`（防守方按哨站數加 militaryLevel） |
| `Source/OutpostTaxParticipant.cs` | 功能 1 產出面：`ITaxTickParticipant`（附庸按哨站數加稅銀） |
| `Source/OutpostTransferHooks.cs` | 功能 3：`LifecycleParticipantBase`（雙向易主） |
| `Source/CaptureContext.cs` | Capture 上下文暫存＋唯一 Harmony patch（prefix/finalizer，fail-soft） |
| `Source/OutpostWarUtility.cs` | 共用：去重警告、數同主哨站、spawner caps 反射搬鍵 |
| `Patches/PColonyOutposts.xml` | 給 PColony 派系掛 OutpostProfileExtension → 附庸專屬 profile（PColony 不存在則靜默跳過） |
| `Defs/OutpostProfileDefs/VassalProfile.xml` | 附庸專屬哨站 profile（`pas_empire_war_Profile_Vassal`） |

## ModSettings 一覽

| 設定 | 預設 | 說明 |
|---|---|---|
| 附庸生成衛星哨站 | 開 | 功能 1 增生開關（opt-in 接點受此控制） |
| 每哨站額外稅收白銀 | 10 | 功能 1 產出加成；0＝停用 |
| 每哨站防守加成（軍事等級） | 0.35 | 功能 2；0＝停用 |
| 單場戰鬥計入哨站數上限 | 8 | 功能 2 防堆疊 |
| 征服時哨站隨之易主 | 開 | 功能 3 雙向總開關 |

## 對本體 mod 的改動

- **npc-outposts**：唯一接點 `WorldComponent_OutpostSpawner.ParentEligibilityOverride`
  （`public static Func<Settlement, bool?>`，null/回傳 null/異常＝零行為變化）＋ `IsEligibleParent` 套用 gate。
  與既有 `GrowthRateMultiplier` 同款契約。重編 0/0。
- **Mod 1 / Mod 2**：**零改動**。Mod 1 的 ConvertSettlement 路徑與本 mod 正交；Mod 2 的淪陷透過 Empire
  `OnSettlementRemoved` 自然被本 mod 收到。

## 防衛式不變式

- 所有對 Empire/RimWar/Mod1/Mod2 的反射與 patch fail-soft：缺型別/簽章不符 → 該子功能降級停用、其餘照常、只 Warn 一次。
- Capture patch 找不到方法 → 玩家側削防＋玩家側哨站認領降級，附庸側（功能 1 防守、功能 3 淪陷）完全不受影響。
- IBattleModifier 只加 `militaryLevel`、夾上限；無哨站＝零變化。
- spawner caps 反射搬鍵讀不到欄位 → 降級停用（哨站照常易主，僅 cap 字典不搬）。

## 建置 / 健檢

```bash
cd Source && dotnet build -c Release   # 0 error 0 warning；輸出 1.6/Assemblies/EmpireOutpostsWar.dll
python3 tests/healthcheck.py           # 離線靜態健檢
```

建置順序：npc-outposts → npc-outposts-rimwar / empire-warfare → 本 mod（依賴前三者的 DLL）。

## 完成定義

- [x] 三項功能照計畫接法落地（接點皆對 Empire/RimWar/本體源碼核章）
- [x] `dotnet build -c Release` 0 error 0 warning（本 mod ＋ 改動後的 npc-outposts ＋ Mod1/Mod2 重編）
- [x] `tests/healthcheck.py` 通過（XML/交叉引用/相依/hook 對齊/雙語/DLL 存在）
- [x] Languages：ChineseTraditional ＋ English Keyed
- [ ] E2E 實機驗證（見下「已知限制」與 docs/plan）

## 已知限制（E2E 待驗）

1. **玩家側 Capture 削防/認領依賴 capture patch 設 tile**：若 Empire 改 `MilitaryJobHandler_Capture.OnResolved`
   簽章，玩家側兩個子功能降級（附庸側不受影響）。
2. **奪城認領哨站**用 CaptureContext 暫存窗口＋來源 ParentSettlement 引用比對；窗口外（極罕見時序）退化為
   tile 鄰近（≤6 tile）啟發式，可能漏認或多認遠處同派系哨站。
3. **哨站「射程」/緩衝層**目前以「同主哨站數」抽象表達加防守戰力，非真實戰場上「哨站先被打」的實體緩衝；
   構想中「周邊哨站先被打」的物理消耗未實作（抽象等價：哨站存活則加防）。
4. **附庸淪陷無接收者時摧毀哨站**：若 Mod 2 未在同 tile 建攻方聚落（如關閉淪陷、或 AddNewHome 失敗），
   哨站會被摧毀以免殘留玩家派系孤兒哨站。
5. **附庸產出/防守加成上限與平衡**未經實機調參；預設值偏保守。

## 關鍵文件

- `docs/plan/2026-06-12-implementation-plan.md`：接點論證＋三項功能設計＋去重
- `session_log.md`：執行記錄
- 上游分析：`~/repo/pas/analysis/rimworld_mods/empire-refactored/details/extension_points.md`（B-1 介面/Registry 表）
- 姊妹 mod：`../npc-outposts-rimwar/`（Mod 1）、`../empire-warfare/`（Mod 2）
