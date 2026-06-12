# Rim War × Empire × 三國志化 調查彙整（2026-06-12）

> 一批唯讀源碼調查的結論彙整，供後續挑選實作。各項由背景 subagent 完成、本檔回填。
> 源碼基準：Rim War 反編譯 `~/repo/pas/projects/rimworld_mods/rim-war/decompiled/RimWar.decompiled.cs`（簡記 `RW:`）、
> VOE 反編譯 `~/repo/pas/projects/rimworld_mods/vanilla-outposts-expanded/decompiled-framework/Outposts.decompiled.cs`（簡記 `VOE:`）、
> Empire workshop 源碼、自家 mod `~/repo/my_rimworld_mods/`。

## 進度

| # | 主題 | 狀態 |
|---|---|---|
| A | warband 是否襲擊 VOE outpost | ✅ 完成 |
| B | 如何讓 warband 襲擊 VOE/封存哨站 | ✅ 完成 |
| C | RimWar × Empire 更多聯動機會 | ✅ 完成 |
| D | warband 建聚落變體改建哨站＋降頻 | ✅ 完成 |
| E | warband 三國志式屬性＋將領佔位符 | ✅ 完成 |
| F | 聚落掛領主/官員佔位符 × faction-politics | ✅ 完成 |
| G | 領主/官員屬性影響據點點數成長/削弱 | ✅ 完成 |
| H | Rim War 據點擴充金錢/資源維度 | ✅ 完成 |
| I | 領主/官員 pawn 之間的關係好感度 | ✅ 完成 |
| J | 依領主決策決定建哪些 outpost | ✅ 完成 |
| K | 據點屬性 dict 容器（領主治理） | ✅ 完成 |
| — | 自家 mod 盤點（13 mod 整合藍圖） | ✅ 完成 |
| L | warband 攻佔後行為可選（佔領/劫掠/消滅） | ✅ 完成 |
| M | 城池財富→原版聚落交易清單影響 | ⏳ 進行中 |
| N | 通訊台指揮 RimWar 派兵攻打/救援 | ⏳ 進行中 |

---

## A. Rim War warband **不會**襲擊 VOE outpost（預設）

**結論：不會**（乾淨，無需 patch 即不會誤觸）。

- VOE `Outpost : MapParent`（`VOE:731`），**不是** `Settlement`/`Caravan`/`WarObject`。
- warband 目標查詢 `GetRimWorldSettlementsInRange`（`RW:16154-16168`）對每個世界物件先做硬轉型 `(Settlement)(obj is Settlement ? obj : null)`，`MapParent` 子類直接丟棄。
- comp 過濾閘 `RimWarSettlementComp != null && RimWarPoints > 0`（`RW:9411`）位於 `is Settlement` **之後** → 不可達。
- Scout 廣掃唯一掃非聚落物件，但用 `is Caravan / is WarObject / is Settlement` 三分支派發（`RW:18505-18522`），Outpost 三者皆非 → fall-through。
- 全 Rim War 反編譯**零個** `is MapParent` 泛化處理點。
- 你之前擔心的 `IsValidSettlement` defName 白名單（`RW:16685`）其實**不在目標路徑上**，真正關卡是更早的 `is Settlement` 型別過濾。

---

## B. 如何讓 warband 襲擊 VOE/封存哨站 → 推薦**影子 Settlement 橋接**

**Settlement vs MapParent 硬牆**：Mod 1（`npc-outposts-rimwar`）只靠「XML 注入 `WorldObjectCompProperties_RimWarSettlement` + prefix `ConvertSettlement`」就讓 npc-outposts 被鎖定，是因為 `NpcOutpost : Settlement`（通得過型別過濾）。VOE `Outpost_Sampled : Outpost : MapParent` **不是 Settlement**，同手法搬過去會「注入成功但毫無效果」。

**路徑評比**：
- **A. patch 型別過濾**：transpile/postfix `GetRimWorldSettlementsInRange`（`RW:16154`）。死結＝其回傳型別是 `List<Settlement>`，硬塞 MapParent 會 InvalidCast。中工程、脆弱。
- **B. 影子 Settlement 橋接（★推薦，最低風險）**：在每座 Outpost 同/鄰 tile 掛一個隱形真 `Settlement` 當「戰鬥替身」→ 自動通過所有型別過濾、戰鬥/易主全鏈原生可用；prefix `ConvertSettlement`（仿 Mod 1 `Patch_ConvertSettlement.cs`）偵測影子→改對綁定的 Outpost 做摧毀/易主。**完全複用 Mod 1 已驗證手法，不碰 Rim War 型別系統。**
- **C. Outpost 改繼承 Settlement**：否決（VOE 全框架建於 MapParent 假設）。
- **D. 主動派兵補充**：`WorldUtility.CreateWarband/CreateWarObject`（`RW:15467/15348`，`public static`）可主動指定目標 tile 派兵，作為 B 的加味。

**核心戰鬥鏈幾乎不碰 Settlement 型別、全對 comp 操作**（這是 B 可行的關鍵）：`ResolveWarObjectAttackOnSettlement`（`RW:10340`，守方靠 `defender` comp）→ `ResolveCombat_Settlement`（`RW:11018`）→ `ResolveBattle_Settlement`（`RW:11086`）。**唯一真雷在 captured 分支**（`RW:11167`）：`WorldUtility.ConvertSettlement(SettlementAt(tile),…)`，對 MapParent 的 `SettlementAt(tile)` 回 null → `ConvertSettlement`（`RW:15289`）守衛擋下 → 靜默 no-op，哨站永遠打不掉（傷害累積卻無結局）。影子方案天然解掉此雷（影子是真 Settlement，SettlementAt 找得到）。

**實作骨架（路徑 B）**：①隱形 `WorldObjectDef`（Settlement 子類或 `RimWarSettlement`）＋XML 注入 comp（複製 `NpcOutpostRimWarComps.xml`），隱藏圖示；②WorldComponent 維護 `Dictionary<Outpost, Settlement shadow>`，Outpost spawn 時在**鄰 tile**（比同 tile 安全，避 SettlementAt 衝突）建影子、`SetFaction`、設 `RimWarPoints`（public setter）；Outpost destroy 時移除影子；③prefix `ConvertSettlement` 偵測影子→改對 Outpost `Destroy()`（`VOE:1138`）/`SetFaction()`（`VOE:221`），`return false` skip 原版；④可選 postfix `IncrementSettlementGrowth` 加點。

---

## C. RimWar × Empire 新聯動機會（8 項，按價值/工程量比排序）

> 已排除三 mod 叢集與內建 Patch-RW 已做掉的（可見性/點數置換/攻擊導流/奪城防衛/Vassal鎖/淪陷易主/哨站入戰局/防守 IBattleModifier）。
> **通用陷阱**：Empire 的 Registry 不序列化、`Game.ClearCaches` 每次讀檔會 ClearAll（`CachePatches.cs:36-50`）→ 必須 `EmpireCacheUtil.RegisterCacheInvalidator`（`CachePatches.cs:21`）重註冊（官方 Patch-RW 自己都漏做）。

### 第一梯隊
1. **戰時加稅／被圍困減產（RimWar 局勢→Empire 經濟）★最高比值起手**。Empire 端**全免 Harmony**：減產 `IResourceProductionModifier`（消費 `ResourceFC.cs:338/363/582/645`）、戰時稅 `ITaxTickParticipant.PostSettlementCreateTax`（`WorldSettlementFC.cs:1917`，可 `ref` 改白銀）。局勢訊號純讀：`RimWarSettlementComp.AttackingUnits/nextCombatTick`（`RW:9088`）判圍困、`RimWarData.IsAtWar/WarFactions`（`RW:1462/1506`）判戰爭。補上目前缺的「RimWar→Empire 反向因果」。工程小、零 patch、風險低。
2. **附庸繁榮回饋 RimWar 點數**。`ITaxTickParticipant.PostTaxResolution`（`FactionFC.cs:1696`，免 Harmony）讀收益→`RimWarSettlementComp.RimWarPoints` public setter 加點（尊重上限 50000，`RW:17597`）。注意別與 `Patch_RimWarPoints` getter 雙算（加到別的派系/做盟友紅利）。
3. **RimWar 全事件流→Empire FCEvent（信件匯流排對接）**。RimWar 端單一漏斗 postfix `RW_LetterMaker.Archive_RWLetter`（`RW:7851`，六類事件 def `RW:1772-1782`）；Empire 端免 Harmony `FactionFC.AddEvent`（`FactionFC.cs:1736`）+ XML `FCEventDef`。需節流去重避免事件洪水。

### 第二梯隊
4. **帝國威望/軍力受 RimWar 勝負回饋**。`IThreatScalingContributor`（消費 `ThreatScalingUtil.cs:83-84`，免 Harmony，目前無人用）；訊號讀 `RimWarData.TotalFactionPoints` 趨勢（`RW:1510`）或 postfix `ResolveBattle_Settlement`（`RW:11086`）。需自存戰績滑動視窗。
5. **帝國成為 RimWar 外交一方（宣戰/和談互通）**。RimWar 現成 `public static`：`RimWarFactionUtility.DeclareWarOn/DeclareAllianceWith/EndAllianceWith`（`RW:468/526/598`，免 patch）。Empire 端缺外交入口需自建（gizmo/通訊台）。風險中高：`DeclareWarOn` 直寫 `baseGoodwill=-100`（`RW:489`），與 Empire relations/playerVS 可能衝突。「玩家帝國＝世界一方勢力」願景核心。
6. **Empire squad 介入 NPC vs NPC 戰局（傭兵/代理戰）**。`IAutoDefender`（`FCInterfaces.cs:280`，消費 `MilitaryUtilFC.cs:62/150/206`，免 Harmony）；攻方複用 `SimulateBattleFc.FightBattle`（`SimulateBattleFC.cs:10`，public）+ `ConvertSettlement` 轉給盟友。工程大（合約 UI、squad 路徑）。

### 第三梯隊
7. **世界勢力範圍→附庸可擴張範圍/被攻擊頻率**。`IRaidWeightProvider.GetSettlementRaidWeight`（`FactionFC.cs:709/718`）＋`ISettlementFoundingValidator.CanFoundSettlement`（`CreateColonyWindowFC.cs:130/406`），皆免 Harmony、目前無人用；讀 `WorldComponent_PowerTracker.AllRimWarSettlements`（public）。
8. **用未利用的 `ILifecycleParticipant` 事件對接**（11 事件 Patch-RW 一個沒用）：`OnSettlementUpgraded`/`OnResearchCompleted`（`ResearchPatches.cs:24`）/`OnSquadDeployed`（`SettlementMilitary.cs:1207/1563`）等→RimWar public setter 調點。注意 `OnBattleResolved` 已被 `empire-warfare` 用。一個 `LifecycleParticipantBase` 子類覆寫數方法，零 Harmony。

**Empire 端全免 Harmony 的最乾淨機會**：#1、#2、#4、#7、#8（只需 RimWar 側讀 public 狀態，多數連 RimWar 都不用 patch）。**RimWar 側唯一需 patch ＝#3**（薄 postfix）。#5/#6 用 public static API 外呼免 patch。

---

## D. warband 建聚落變體改建哨站＋降頻 → 推薦 Mod 1 加一個 `CreateSettlement` Prefix

**Rim War 聚落生成鏈（單一線性）**：決策抽籤 `RimWarData.GetWeightedSettlementAction`（`RW:1688`，`Rand.Value <= settlerChance`→`RimWarAction.Settler`）→ 權重 `CalculateFactionBehaviorWeights`（`RW:15977`，受 `createsSettlements` 旗標 gate）→ 每聚落排程 `WorldComponent_PowerTracker.WorldComponentTick`（`RW:17030`）到 `nextEventTick` 抽籤、`< maxFactionSettlements` 時 `AttemptSettlerMission`（`RW:17167`）→ 生 `RW_Settler` WarObject（`CreateSettler RW:15783`）→ 抵達 `Settler.ArrivalAction`（`RW:12302`）呼 `CreateSettlement`（`RW:12574`）→ **`WorldUtility.CreateSettlement`（`RW:15248`，public static）→ `SettleUtility.AddNewHome`（`RW:15259`）建 vanilla Settlement**。

**頻率控制點**：`settlerChance`（`RW:1157`）、behavior 權重（`RW:15989`…）、`createsSettlements` 旗標（派系 def，純資料）、`maxFactionSettlements`（預設 40）、`settlementEventDelay`（預設 50000 tick）——後兩者是 **ModSettings 滑桿**（`RW:7373-7410`），非 Def。

**推薦：路 A — Prefix `WorldUtility.CreateSettlement`（`RW:15248`）**，按設定機率改呼叫 npc-outposts 的 `OutpostPlacer.TryPlaceFor(parent, profile)`（`~/repo/my_rimworld_mods/npc-outposts/Source/World/OutpostPlacer.cs:11`）建 NpcOutpost、`return false` skip 原版。Prefix 拿得到 `warObject`(Settler，可取 `ParentSettlement` 當母聚落)/`tile`/`faction`。profile 由 `OutpostProfileResolver.Resolve(faction)` 解析。**一個 patch 同時達成「建哨站變體」＋「降低建聚落頻率」**（settler→哨站轉換率即淨降聚落生成率）。
- 落點：`TryPlaceFor` 自己在母聚落 `profile.radius` 內重找 tile（語意 OK：從母聚落派衛星）。
- fail-soft：profile 解析不到/placer 回 null/例外 → 放行原版建城（仿 Mod 1 `Patch_ConvertSettlement` 風格）。
- 不碰 enum/dispatch/權重正規化（extension_points.md 標「極高風險」散布區全避開）。
- **歸屬：Mod 1 `npc-outposts-rimwar`**（已 patch 同類 `WorldUtility`、已有 fail-soft 框架/ModSettings/spawner cap）。新增 `Source/Patches/Patch_CreateSettlement.cs` + 1 個 slider `settlerToOutpostChance`（預設 0.6）。
- 路 B（只降頻、靠 npc-outposts 既有 spawner 增生）零成本但無「warband 變體」敘事；可與 A 疊加。

---

## E. warband 三國志式屬性＋將領佔位符 → 推薦 WorldObjectComp（注入 RW_Warband def）

**WarObject 資料模型**：`WarObject : WorldObject`（`RW:14157`）、`Warband : WarObject`（`RW:13236`，同層 Scout/Trader/Settler/Diplomat）。戰力＝純抽象點數 `warPointsInt`/`pointDamageInt`→`EffectivePoints`。**已內建 `private List<Pawn> pawns`（`RW:14208`，深存於 `RW:14557` `LookMode.Deep`）但抽象 warband 生成時為空、閒置**（真 pawn 只在打到地圖時 `GeneratePawnGroup RW:11521/11582` 臨時生）。`rimwarData`（`RW:14475`）是**派系級計算屬性**、含 `combatAttribute`（`RW:1171`）。工廠 `CreateWarObjectOfType RW:15358`→`CreateWarband RW:15467`→`MakeWarband RW:15518`（硬編碼 `RW_Warband` def + `new Warband()`）。

**推薦：WorldObjectComp**（Rim War 原生慣例，存檔相容最佳）——自訂 `CompProperties` 用 Harmony 注入 `RimWarDefOf.RW_Warband.comps`，存將領武力/統率/智力/士氣（`Scribe_Values`）。**子類化不可行**（工廠硬編碼、多呼叫點）。
- **將領 pawn**：(A) 真 Pawn（深存、照 VOE `AddPawn` 清 caravan/WorldPawns/holdingOwner，`VOE:1022`）或 (B) 輕量佔位符（string+ints）。**MVP 走 B**。
- **戰力注入點（核心）**：`IncidentUtility.ResolveCombat_Units`（`RW:11271`，公式 `points × Rand × combatAttribute` 於 `RW:11290-11291`）→ postfix/transpiler 乘將領加成。**勿直接改派系級 `combatAttribute`**（污染全派系），要局部乘。聚落戰用 `ResolveCombat_Settlement RW:11018`。
- **顯示**：`WarObject.GetInspectString`（virtual `RW:14860`）+ comp `CompInspectStringExtra`/gizmo；仿 `Settlement_InspectString_WithPoints_Postfix RW:5977`。
- **可達成度 ~80%**：戰略抽象層三國志（名將帶兵更能打），非戰術單騎。**MVP＝輕量 comp + 注入 ResolveCombat_Units + inspect 顯示**。
- 與聚落領主（F）共用「具名 pawn + 屬性」基礎設施（見下）。

---

## F. 聚落掛領主/官員佔位符 × faction-politics → 擴充 faction-politics、抽共用「具名職官」層

**「colony rebellion」＝本機 `faction-politics`**（`pas.politics.community`，零 Harmony/零硬相依）。現況：NPC 派系生**一個具名反叛者 world pawn** 駐某城，進展累積到閾值 `FactionSplitter` **分裂新派系**（聚落含哨站倒戈）。**它已實作「具名 pawn↔聚落綁定」**——正是領主佔位符的地基：
- `RebelSpawner.cs:13-36`：`GeneratePawn` + `WorldPawns.PassToWorld(KeepForever)`；
- pawn↔城橋 `homeSettlement.previouslyGeneratedInhabitants.Add`（`RebelSpawner.cs:24`，玩家拜訪該城 redress 請回同一 pawn）；
- `RebelRecord.cs`（IExposable：faction/rebel(ref)/homeSettlement(ref)/progress/ratePerDay）；
- `WorldComponent_RebellionTracker`（2500-tick 心跳+自癒）；存檔 pawn 走 `Scribe_References`、record 走 Deep。

**推薦混合承載**（鏡像 Empire 模型）：①**數值層走 WorldObjectComp**（能力值/職位/忠誠，仿 Empire `WorldSettlementFC` 的 unrest/loyalty/prosperity；先例 `RimWarSettlementComp:9078`，掛任何 `Settlement` 子類含 RimWar City/Empire/NpcOutpost）②**具現層走真 Pawn 懶生成**（平時輕量 record，拜訪/攻打時才 `GeneratePawn` + 既有 `previouslyGeneratedInhabitants` 橋）。
- **屬性影響管道**（皆有現成接點）：產出/稅收（Empire `WorldSettlementFC.CreateTax`）、防守（`RimWarPoints` / npc-outposts `defenderPointsFactor`）、叛亂傾向（餵 `RebelRecord.ratePerDay`）。
- **與 rebellion 連動**：`ratePerDay`（`RebellionTracker.cs:51`，源 `RebelSpawner.cs:29`）← 領主忠誠/魅力函數；`TrySplit`（`152-172`）；`FactionSplitter.Split`（`14-51`，反叛者升 `newFaction.leader`）——**「領主帶城叛變獨立」80% 現成**，把「藏匿反叛者」改寫成「公開城池領主」即是三國志式叛變；倒戈通知 `PoliticsBridges.SettlementDefected`（哨站跟隨/RimWar 同步已掛）。
- **歸屬：擴充 faction-politics（P-next），不開全新 mod**（地基已在）。**強烈建議把「具名 pawn + 屬性」抽成共用層**（可獨立 `pas.named-officers` 基礎 mod），**城池領主（F）與 warband 將領（E）共消費同款 comp/record**。
- **可行性高**，主要工作：把 `RebelRecord`/`RebelSpawner` 從「單一反叛者」泛化為「領主＋N 官員 record list」+ 加屬性 comp + 接 `ratePerDay`/`RimWarPoints`/稅收。風險：world pawn 數量控管（懶生成）、comp 掛載時序（沿用軟橋）。

---

## G. 領主/官員屬性影響據點點數成長/削弱 → 領主 mod 獨立 postfix（仿 Mod 1）

**成長節奏**：`WorldComponentTick RW:17030`→每 `rwdUpdateFrequency`(2500)→`UpdateFactions RW:17560`→`IncrementSettlementGrowth RW:17567`。**成長公式（`RW:17622-17631`）**：`num4 = (Rand(2,3)+biome) × num × tech × growthAttribute × settlementGrowthRate`，`RimWarPoints += RoundToInt(Clamp(num4,1,100))`（每城每輪最多 +100）；`bonusGrowthCount`（`RW:9098`）是 RimWar 自己的「逐城 int 加速」先例。
- **關鍵：`growthAttribute`（`RW:1173`）是派系級共享**，動它全派系一起變 → **不能**當單城領主倍率。領主倍率**必須逐城 postfix 補乘**（warband 將領能用 `combatAttribute` 當鉤子、本案不行的根本差異）。
- **衰退**：原版只有 PointDamage 分支（`RW:17616-17620`，需 `PointDamage>0`；`if/else if` → 戰損中的城當輪不成長）；成長分支被 `Clamp(...,1,100)` 鎖死下限 +1，**永不為負**。故「庸主→淨衰退」必走 postfix 扣 `RimWarPoints` 或加 `PointDamage`，不能靠改係數。
- **骨架**：postfix 逐城讀領主 comp `GovernanceFactor`（政務×忠誠，0.5~1.5）；`gov≥1` 補成長、`gov<1` 扣點；鏡像「`PointDamage>0` 跳過」、複用 Mod 1 `GrowthCapFor` 上限鏡像。`RimWarPoints` setter `Max(0,)`、getter 地板 100（`RW:9267`）→ 衰退停在 100，**摧毀城需走 ConvertSettlement 易主**（`RW:11168`），非本注入點。
- **歸屬：領主系統 mod 自己的 postfix**（與 Mod 1 哨站貢獻正交；多 postfix 疊加同方法安全；勿塞進 Mod 1）。`GrowthCapFor` 抽共享 util。
- 風險：上限/地板鏡像漂移、PointDamage 語意、派系級 vs 聚落級混淆（**勿動 `RW:1171/1173/17625` 係數**）、threading（`RW:17062`）。

## H. Rim War 據點擴充金錢/資源維度 → 新增 `SettlementWealthComp`（XML 注入、抽象計數器）

**現況**：Rim War 經濟全建在抽象 `RimWarPoints`，**無真實 silver/wealth/goods 欄位**。silver 只在玩家↔NPC 介面層（`GetPlayerSilver RW:124`、`TributeSilver RW:667` 掃玩家地圖）。`Trader`（`RW:11977`）無 inventory、貨物由 vanilla `ThingSetMaker` 即時生成；trade 結算純 `RimWarPoints` 轉移（`RW:10448/11938`）。**掠奪已存在**：`ResolveBattle_Settlement` sack 分支（`RW:11197-11212`），城破搬 `RimWarPoints×Rand(0.3,0.6)` 給攻方——搬點數非物資，"wealth" 只在信件文案。

**推薦：新增自訂 `WorldObjectComp` `SettlementWealthComp`（XML 注入、勿擴 RimWarSettlementComp）**，存 `silver/food/goods`（int, `Scribe_Values`，仿 `RimWarSettlementComp.PostExposeData RW:9502`）。成長走**自 comp `CompTick` + nextTick 節流**（仿 `RW:9585`，與 Rim War 解耦）；需與點數同步才 postfix `IncrementSettlementGrowth`。
- **互動接點**：戰敗被劫 postfix `ResolveBattle_Settlement` sack 分支（`RW:11197`，複用既有觸發、換搬真資源）；貿易 postfix trade 結算（`RW:10448/11938`）；Empire 稅收 `ITaxTickParticipant.PostSettlementCreateTax`（`ref silverAmount`，免 Harmony）；領主貪腐接 F/G comp。
- **玩法**：劫掠經濟（最契合）、貿易路線、上繳朝貢、領主貪腐。
- **歸屬：獨立 mod，「RimWar 側自有 comp + Empire 側 registry participant」形態**（同 C#1 範式；Empire Registry `ClearCaches` 陷阱同 C）。
- **顯示**：獨立 `GetInspectString` postfix（仿 `RW:6570`），勿改 Rim War postfix。**設計決策**：抽象計數器（推薦，合 Rim War 哲學）vs 真 Thing。

## I. 領主/官員 pawn 之間的關係好感度 → 雙軌（DirectPawnRelation + 自訂 opinion dict）

- **持久關係（結拜/世仇）→ vanilla `DirectPawnRelation` + 自訂 `PawnRelationDef`**：對 world pawn **完全可用**（隨 pawn 存檔，`AddDirectRelation` 無 Spawned 檢查，`Pawn_RelationsTracker.cs:483/292`）。零成本。
- **連續好感度（會漲跌）→ 自訂輕量 opinion dict**：vanilla 動態 opinion **對 world pawn 凍結**（社交想法靠 InteractionsTracker、被 `Pawn.cs:1659` 的 `Spawned` 閘死）。故存 `Dictionary<otherPawnId,int>`（IExposable, `Scribe` value），掛具名職官 record/comp，由既有 2500-tick 心跳演化。
- **玩法接點**：餵叛亂 `ratePerDay`（`RebellionTracker.cs:51`；官員集體厭領主→更快 `TrySplit`）、戰力（`ResolveCombat_Units RW:11271` 兩將不和打折）、治理（`IncrementSettlementGrowth RW:17567` 經 GovernanceFactor）。
- 住 E/F 共用「具名職官」層；複用 faction-politics world pawn 管線與心跳。A 軌（結構關係）+ B 軌（情緒好感度）並用，B 讀 A 當初始 bias。

## J. 依領主決策決定建哪些 outpost → npc-outposts 加 `TypeSelector` hook + Mod 1 權重函數

- **選型唯一處**：`OutpostPlacer.TryPlaceFor`（`npc-outposts/Source/World/OutpostPlacer.cs:11`），第 17-20 行 `profile.types.RandomElementByWeight` 純隨機；`TryPlaceFor` 已有 `type=null` 參數可繞過隨機＝乾淨注入縫。
- **推薦：npc-outposts 加第三個 static hook `TypeSelector`**（仿既有 `GrowthRateMultiplier`/`ParentEligibilityOverride`，`WorldComponent_OutpostSpawner.cs:17/22`）：`type ??= TypeSelector?.Invoke(parent,profile)`；spawner（line 61/89）與 D 兩路徑同時受益、本體 hook=null 零變化。
- **Mod 1 註冊權重函數**：讀母聚落領主 comp 能力值 + RimWar `behavior` + Mod 1 `WorldComponent_OutpostWarMomentum` score → 重加權 `profile.types`。**MVP＝純權重函數，非 FSM**（策略靠現成 momentum/behavior 訊號推導）。
- 串接 D(管道)/G(成長)/H(財富成本)。歸屬：hook 在 npc-outposts 本體、權重函數在 Mod 1。**硬前置：領主屬性 comp（F/E 基礎層）尚未實作，須先建。**

## K. 據點屬性容器（領主治理） → 併入 H 的 `SettlementWealthComp`，typed 主幹 + Def-dict 旁路

- **容器設計**：核心維度用 **typed int 欄位**（`silver/food/goods/defenseLevel/defensePoints`，`Scribe_Values`，與 `RimWarSettlementComp.PostExposeData RW:9502` 同形、最安全）；擴充性預留 `Dictionary<SettlementAttributeDef,float> extraAttributes` 旁路（本機 `OutpostProfileDef` 已驗證 Def 驅動屬性可行）。**純 string-dict 否決**（參與邏輯的屬性需穩定符號）。
- **防禦屬性 ↔ RimWarPoints**：防禦是**獨立維度**，**勿疊進存量**（污染經濟/sack）。守城時透過**降 `PointDamage` 臨時抬高 `EffectivePoints`**（`EffectivePoints=RimWarPoints-PointDamage RW:9277`）參與 `ResolveCombat_Settlement RW:11018`，受其 `num` tier clamp 自動封頂；玩家實打另走 `defenderPointsFactor` 範式（`OutpostTypeDef.cs:10`）。
- **領主治理動作層 = 與 J 共用 `ILordAction` 決策骨架**：同一 per-tick/per-lord 迴圈讀領主 comp，**對內動作**（蓋倉庫/修防禦/徵糧，寫 wealth comp）+ **對外動作**（建哪種 outpost，走 D 的 `CreateSettlement` 接點）；`GovernanceFactor` 調制成敗。歸屬領主系統 mod。
- **與 H 合併成單一 comp**：H 經濟維度+互動接點，本案加防禦維度+守城折算+治理寫入層。

## 自家 mod 盤點（13 mod 整合藍圖）

> 實際 packageId 校準：`npc-outposts`=`pas.outposts.community`、`colony-archival-outpost`=`pas.colonyarchival.outpost`。

**核心地基（複用、勿重造）**：
- **faction-politics**（`pas.politics.community`）：具名 pawn↔聚落綁定（`RebelSpawner.cs` `PassToWorld(KeepForever)` + `previouslyGeneratedInhabitants` 橋）、`RebelRecord`、`WorldComponent_RebellionTracker`(心跳+自癒)、`FactionSplitter`(反叛者升 `newFaction.leader`)。**＝領主系統①地基 + 叛亂⑥**。
- **npc-outposts**（`pas.outposts.community`）：`NpcOutpost:Settlement`、增生引擎、`OutpostTypeDef`/profile、**兩個 public static hook**。**＝哨站擴張③地基**。
- **戰爭叢集 Mod 1/2/3**：RimWar 接線、Empire 契約層、雙向易主——**⑦範式已驗證**。
- **colony-archival-outpost**：封存採樣→抽象產出（**＝⑧，E1 進行中**）。
- **voe-outpost-enhancement**（`justty32.VOEOutpostEnhancement`）：花銀升級 gizmo+扣費+WorldComponent——**＝城池發展②投資範式**。
- **sims-mode-community**（`pas.sims.community`）：角色/作息層——領主拜訪具現呈現。
- 無關（排除）：speakup-context-expansion、cqf-caravan-redemption、body-fortification-hediff、body-hp-x10。

**需新建**：①**共用 `pas.named-officers` 基礎層**（從 faction-politics 抽取泛化：具名 pawn+屬性+關係+懶生成，領主與將領共消費）②warband 將領（E）③聚落領主 comp+點數影響（F/G）④城池財富/防禦（H/K 單一 comp）⑤領主決策層（J/K 的 ILordAction）。

**建議架構**：`pas.named-officers`（基礎）→ 聚落領主 mod（F/G/K/J 決策）＋ warband 將領（E）＋ faction-politics 擴充（叛亂改寫成領主帶城叛變）；戰爭叢集 Mod 1/2/3 已完成、新功能掛載其上勿改寫；城池財富獨立 mod「RimWar comp + Empire registry participant」形態。

## L. warband 攻佔後行為可選（佔領/劫掠/消滅） → Mod 1 加 ResolveBattle prefix 結局選擇器

**原版結局現況（全在 `ResolveBattle_Settlement RW:11086-11269`）**：攻方勝出後依兩道 behavior 加權 Rand 閘分流——
A1 **佔領**（`RW:11148-11169`：`Rand×(Expansionist1.1/Warmonger1.5)>0.5` 且非 Vassal 且點數夠 → `ConvertSettlement RW:11168` 易主）；
A2 **劫掠**（`RW:11197-11213`：`Rand×(Warmonger1.2/Merchant1.4)>0.5` 且守方點數>1000 → 搬點數 0.3~0.6，城留存）；
A3 **摧毀**（fallback `RW:11221-11239`：`parent.Destroy()`）；B 同歸於盡固定摧毀（`RW:11257`）；C 攻方覆滅城不變。守方 Vassal 跳劫掠直接摧毀（`RW:11214`）。**結局由隨機閘決定，無人可選。**
- **做成可選**：prefix `ResolveBattle_Settlement` 換「結局選擇器」（佔領=呼 `ConvertSettlement`、劫掠=sack 迴圈+可搬 H wealth、消滅=`Destroy()`），`return false` skip 原版；**須自行重建部隊回吐+letter**（中等工程）。決定者：①behavior 映射（MVP）③玩家 ModSettings（疊加）②將領決策（留 `Func<WarObject,BattleOutcome>` hook，等 E/named-officers）。
- **共存**：empire-warfare 走旁路（patch 上游 `ResolveWarObjectAttackOnSettlement RW:10340` 異步淪陷、明示避開 ConvertSettlement）→ 選擇器開頭排除 `WorldSettlementFC` 即零衝突；Mod 1 既有 ConvertSettlement prefix 對 NpcOutpost 的 capture/raze 正好複用；**風險**：prefix-skip 後 Mod 1 自己的記帳 postfix 須仍觸發（保留等效副作用＋實測）。
- **歸屬**：MVP 放 Mod 1（已 patch 同方法、有範本/設定框架）；outpost 對應走 B 影子＋Mod 1 範本。

## M. 城池財富→原版聚落交易清單影響 → 掛 H 城池經濟 mod（RegenerateStock postfix）

**原版 stock 生成鏈**（`RimWorld.Planet/Settlement_TraderTracker.cs`）：**惰性生成**——玩家第一次按交易 `StockListForReading`(:27) 才呼 `RegenerateStock()`(:265)；每 30 天（`RegenerateStockEveryDays` protected virtual :21）`TryDestroyStock` 清空待重生。參數：`TraderKind` getter(:39) ＝ `faction.def.baseTraderKinds[|HashOffset| % Count]`（每聚落確定性、永不變）；白銀/貨量全由 TraderKindDef XML 的 `StockGenerator` 決定——**聚落本身無財富參數**（純留白）。stock 隨聚落存檔（`Scribe_Deep` :134）。

- **注入點（推薦）**：**A. postfix `RegenerateStock`**——生成後讀 WealthComp 按 silver/goods/food 增刪物品調白銀（工程小、相容面最窄）。B. `StockGenerator.GenerateThings` 不可行（拿不到 settlement）。C. patch `TraderKind` getter 富/貧換 TraderKindDef（效果最原生但工程大，二期）。價格維度（缺糧高價收糧）走 `TradePriceImprovementOffsetForPlayer`(:109 virtual，全域)或 `PriceTypeFor`（分品類，與改價 mod 衝突面），留二期。
- **玩家交易回寫**：postfix `GiveSoldThingToTrader`(:164 玩家賣→food/goods↑) ＋ `GiveSoldThingToPlayer`(:187 玩家買→goods/silver↓)，皆 virtual、`__instance.settlement` 直達。
- **Rim War 零衝突（grep 確認）**：完全沒碰 Settlement_TraderTracker/TradeDeal；唯一相關 `ThingSetMaker.Generate` postfix（`RW:6089`，MFI shim）→ **勿選 ThingSetMaker 當注入點**，RegenerateStock 在其外層避開。
- **歸屬：H 的城池經濟 mod 同一 mod**（回寫直讀寫 comp；這正是 H 缺的「玩家可感介面」）。一期 stock 數量＋回寫（估 200-400 行）；二期 TraderKind 切換＋品類價格。

## N. 通訊台指揮 RimWar 派兵攻打/救援

⏳ 進行中。完成後回填。

## 三國志化願景（跨全部調查 A–K 的整合圖像）

使用者要的整體感覺：**Rim War 大地圖戰爭 + 具名人物（將領率軍、太守治城）+ 叛亂/勢力消長**，把抽象點數戰爭變成有人物的三國志式大戰略。
- **E（warband 將領）** 與 **F（聚落領主/官員）** 應共用一套「具名 pawn 佔位符 + 屬性」基礎設施（待兩調查確認可共用）。
- **F** 與 faction-politics 的 rebellion 連動 ＝「領主對母派系不滿→帶城叛變」。
- **D** 降低 RimWar 建聚落頻率、改以哨站擴張，讓世界版圖節奏更可控。
- **B** 讓 warband 能打哨站，閉合「哨站也是可攻防的戰爭節點」。
