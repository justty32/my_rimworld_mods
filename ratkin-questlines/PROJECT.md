# ratkin-questlines（工作名，可改）— 鼠族世界任務包 設計文件

> 活文件 / 底稿。目標 modlist＝部署側 `2-anime`（511 mod，RW 1.6）。純 CQF＋原版節點，**不碰模擬經營框架**。
> 精華同步在自動記憶 `custom-quest-simmgmt-ratkin`。⚠️ 機制細節有 `【待偵察】` 標記者，等 Explore agent 回報再坐實。

## 1. 北極星

做**一整套任務包，讓玩家感受到「有鼠族的世界是真實、活著的」沉浸感**。
成敗重心在**內容/敘事軸**（鼠族口吻、動機、派系關係可信一致），不只機制串接。
手法：各任務線合起來拼出**鼠族社會全貌**——modlist 的鼠族生態本身就是完整社會（王國 `Rakinia`／騎士團 `RKK_KnightOrders`／游擊隊 `RKU_Faction`／軍閥 `Rakinia_Warlord`／雜貨鋪 `ZHP_Faction`／岩鼠雪鼠旅鼠），派系間天然有張力，玩家遇到的是「社會不同角落的鼠」而非孤立 NPC。

## 2. 核心玩法：據點身份系統（衍生式——由客觀狀態長出來）

玩家＝一個聚居點的**管理者**，據點有「身份/類型」，**複合、可演變**。**身份決定誰上門、觸發什麼事件、給什麼委託。**

**身份主要由「可觀測的據點狀態」自動判定，對話問句只是輔助/微調（非主要來源）。** 判定輸入：

| 輸入 | 例：暗示什麼身份 | 讀取難度（純 XML/CQF？）|
|---|---|---|
| 世界地圖位置 | 交通要衝→商店/旅館；邊陲→據點/庇護所 | 【待查】Tile 可讀，暴露給 CQF 條件？ |
| 地形/生態 biome | 沙漠商道、雪原前哨… | 【待查】BiomeDef 可讀性 |
| 財富 wealth | 高→商店/工廠 | 【待查】WealthWatcher，CQF 條件讀不讀得到 |
| 建築/房間類型與數量 | 大量生產台→工廠；祭壇→信仰中心；牢房→監獄；床位多→旅館 | 【難】RoomRoleDef 計數，恐 C#-only |
| 人口數 | 規模門檻 | 較易讀 |
| 武裝程度 | 高→傭兵據點/兵營 | 【難】需量化，恐 C# |
| **做過的任務累積** | **供貨量→商店；暗殺數→暗殺據點；建造數→建設；收容數→庇護所** | ✅ **易**——我方自己記全域計數器（每條線結算時 +1），完全可控 |

**架構含義（重要）**：以上「我方任務累積」這類由本 mod 自己記帳的指標最好處理（CQF 全域庫存計數/旗標）；但「財富/房間類型/武裝」等**原生殖民地狀態**，CQF 的 `DialogCondition` 未必讀得到 ⇒ **很可能需要一個輕量自製 C# 元件「據點側寫器（colony profiler）」**：一個 `GameComponent` 定期評估上述客觀狀態＋讀我方任務計數，算出身份傾向、寫回身份旗標（供 `SpecialPawnGenerateDef`／`DialogCondition_Bool` 用）。此元件**獨立、只讀殖民地狀態＋寫自己的旗標，不碰模擬經營框架**，符合解耦約束。

**✅ 側寫器偵察結論（2026-07-18）——分界線已定：**
- **「某件事有沒有發生過」→ 純 XML/CQF 全搞得定**（`CQFAction_SetGlobalBool` 記錄＋`DialogCondition_Bool`/`_And`/`_Or`/`_Reversal` 組合判定身份）。
- **「殖民地當下的物理/空間聚合狀態」→ 必須自製 C#**：財富分項、房間角色 `RoomRoleDef` 計數、特定建築/物資盤點、整體武裝、biome/tile、精確整數計數——CQF 22 個 `DialogCondition` 只有 `DialogCondition_ColonistCount`（全地圖人數總和）碰得到殖民地層級；**CQF 無整數計數器**（`QuestData.values` 是死欄位，累積數只能用 bool 溫度計分級近似）。
- **可純 XML 做的成熟/規模門檻快照**：原版 `QuestNode_GetMapWealth`（讀 `wealthWatcher.WealthTotal`）＋`QuestNode_GetFreeColonistsCount`（可拆單一地圖）＋`QuestNode_Greater` 系列比較 → 送信號給 `QuestNode_DoCQFActions` → `CQFAction_SetGlobalBool` 寫門檻旗標。**快照式**（生成當下算一次），要持續重估得靠週期性重生的任務鏈。
- **建議架構**：C# 側寫器只負責「感測殖民地狀態＋寫 CQF bool 旗標」（最小侵入），所有篩訪客/委託邏輯留在 XML。可選再自訂 `CQFAction_SetGlobalInt`/`DialogCondition_IntThreshold` 補整數能力。MVP 可先只用「事件發生過」＋「財富/人口快照門檻」這個純 XML 子集，C# 側寫器列為後續增強。

**★ 採納 VOE 的身份邏輯（2026-07-18，見 `brainstorm/0-settlement-taxonomy.md`）**：VOE 定義據點身份靠「**建築/地形前提 ＋ 派駐 pawn 技能總和 → 產出/服務**」，不是靠標籤。⇒ **側寫器應照此推導身份**：讀殖民地**主導技能**（哪類技能等級總和最高→農/工/戰/醫/學…）＋**關鍵建築**（有無祭壇/工坊/牢房/床位…）＋**biome**，映射到六大功能群子類的身份旗標。並用 VOE 的「**資源型 vs 服務型**」二分決定給該據點**供貨型**還是**契約/戰鬥/外交型**委託。

**⑵ 規模進程軸（2026-07-18 使用者加）**：身份除了「類型」還有「量級」——通用村莊隨規模成長可**村莊→城鎮→城市**，各量級解鎖不同訪客/委託。技術上用上面的財富/人口門檻快照分級即可（純 XML 可做）。其他類型日後也可各有量級。

**輔助來源：對話問句（多聲部、別太早、別單一角色）**
- 提出「你們這是做什麼的？」只是**微調/確認**，非主要判定。
- ⚠️ **時機別太早**：殖民地早期還在發育、很窮，沒有身份可言 ⇒ 側寫器/問句都要**過了成熟門檻（時間/財富/人口）才啟動**。
- ⚠️ **別只用一個角色**：讓不同鼠族訪客在不同時機各自從自己的角度觀察搭話（客商看生意、騎士看武備、難民看安全），多聲部拼出「外界怎麼看你」。

**技術落點**
- 身份旗標（全域庫）→ `SpecialPawnGenerateDef` 篩訪客 ＋ 對話 `DialogCondition_Bool` 篩委託。
- ⚠️ 原版說書人抽任務 `rootSelectionWeight` **讀不到旗標** ⇒ 身份相關任務走「訪客對話依旗標分支 → `CQFAction_Quest` 觸發」；身份**無關**的意外（§4）才交給說書人隨機。

## 3. 聚居點類型 × 誰上門 × 請求什麼

> 使用者起手：村莊、信仰中心、商店、旅館、生產工廠、暗殺據點、傭兵據點。以下擴充。

| # | 據點身份 | 誰會上門 | 上門做什麼／請求什麼 |
|---|---|---|---|
| 1 | **村莊／農業聚落** | 鼠族流民、鄰村長老、收成季商販 | 請求庇護、託孤、換糧、合辦豐收祭、借人手搶收 |
| 2 | **信仰中心／神殿** | 朝聖者、傳教士、異端、遺物掮客 | 求朝聖許可、託你護送聖物、清剿褻瀆者、辯經、供奉 |
| 3 | **商店／貿易站** | 雜貨鋪 `ZHP` 客商、跑單幫、討債人 | 談供貨合約、寄售、護送商隊、追欠款、獨家代理 |
| 4 | **旅館／驛站** | 旅人、信使、通緝犯、密探 | 投宿、寄信、打聽消息、藏匿某位「客人」、識破間諜 |
| 5 | **生產工廠／工坊** | 工會代表、缺料商、學徒 | 大宗代工訂單、原料短缺求援、收學徒、技術外流疑雲 |
| 6 | **暗殺據點／影堂** | 掮客、復仇者、被追殺者 | 派懸賞人頭、買情報、反過來保護目標、清理叛徒 |
| 7 | **傭兵據點／兵營** | 雇主、逃兵、招募官 | 護送/防禦契約、平叛、招兵、贖回被俘同袍 |
| 8 | **醫院／療養所** | 傷患、瘟疫使者、器官掮客 | 求醫、隔離疫病、捐藥、可疑的器官訂單、收容瘋者 |
| 9 | **學院／圖書館** | 求知者、抄書匠、禁書追查者 | 委託研究、譯古卷、藏/交禁書、辦講學 |
| 10 | **監獄／勞改營** | 押解官、越獄者家屬、贖金使 | 代收囚犯、防越獄、談贖金、政治犯託付 |
| 11 | **賭坊／歡場** | 豪賭客、欠債者、掃蕩的官差 | 開局、討賭債、抄查前通風報信、擺平鬧事 |
| 12 | **牧場／獸欄** | 牧人、獸疫官、偷牲賊 | 託養/配種、防獸疫、追偷牲賊、獵食人獸 |
| 13 | **礦業據點** | 探礦者、塌方倖存者、爭礦鄰幫 | 深鑽委託、救塌方、礦權糾紛、走私礦石 |
| 14 | **走私港／黑市** | 私梟、線人、緝私隊 | 過貨、買情報、緝私前脫身、黑吃黑 |
| 15 | **皇家哨站／領地** | 王國 `Rakinia` 貴族、騎士 `RKK`、收稅官 | 納稅、受封、護送貴族、平定游擊隊 `RKU`、王室聯姻信使 |
| 16 | **難民營／收容所** | 一波波流離鼠族、援助團、人口販子 | 收容浪潮、發配物資、揪出人販、族群融合糾紛 |
| 17 | **馬幫／遊商營**（若走機動） | 旅鼠 `TravelRatkin`、路霸、同行 | 結伴同行、買路、情報交換、爭地盤 |
| 18 | **藝匠／戲班** | 表演團、贊助人、踢館者 | 委製藝品、辦演出、藝術競賽、踢館比試 |

> 每格「請求什麼」都可長成 1~N 條任務線；先不求全做，當素材庫。

**⚠️ 分類是暫定、待細化（2026-07-18）**：目前先用 5 大寬泛類別做**廣度第一遍**腦力激盪（各派一個 sonnet agent，鼠族本體內容扎根，每類 8~12 條點子）——① 通用（村莊聚居點，含村莊→城鎮→城市規模進程）② 經營者（原材料/加工品/武器裝備）③ 武力提供者（傭兵/騎士團/刺客）④ 旅店（餐廳+住宿）⑤ 學校（托嬰/訓練中心）。這 5 類**太寬、日後會再細分子類**；第一遍產出當各子類的素材池。結果彙整見 §9。

> **✅ 細化已完成（2026-07-18）→ 見 [`brainstorm/0-settlement-taxonomy.md`](brainstorm/0-settlement-taxonomy.md)**。用 VOE 系列（~30 種 outpost）當骨架，把五大寬泛類切成**六大功能群 × 子類**（初級生產／貿易加工／武力安保／待客娛樂／機構教養／公用醫療），每子類對映鼠族味＋任務家族＋已有點子，並標出未涵蓋的空格（牧場/漁村/戲班/使館/監獄/邊境…）供下輪擴充。規模軸（前哨→村莊→城鎮→城市）獲 VOE `Outpost_Town` 機制驗證。

## 4. 不分身份的「意外」事件（type-agnostic）

不看據點身份，說書人可隨機丟、或旅途中撞上的鼠族插曲：

- 迷路的鼠族幼崽晃進門，找不到家人
- 重傷的鼠族巡邏隊倒在門口求救
- 逃離軍閥 `Rakinia_Warlord` 劫掠的難民衝進來
- 垂死的信使把一個包裹/遺言託給你
- 帶著情報叛逃的鼠族，後面有人追
- 王國 vs 游擊隊的衝突無意間燒到你的地界
- 染上「鼠疫」/瘋症的個體闖入
- 一件鼠族遺物/神器被送錯地方到你手上
- 假冒的收稅官/騙徒上門詐財
- 暴風雪/災害把一群鼠族逼來你這避難
- 鼠族巡邏隊純路過寒暄、留下八卦與世界近況（G 輕插曲）

## 5. 任務線機制積木（CQF ＋ 原版節點）

> **📖 可用程式碼/機制積木全紀錄見 [`brainstorm/quest-mechanics-reference.md`](brainstorm/quest-mechanics-reference.md)**——CQF action/dialog/condition 調色盤（欄位驗過）、原版 quest 節點+信號、★F4/F5 site 任務模板（抄自 Kiiro）、原版可複用 site part/信號/comp、Kiiro C# QuestNode 清單、C# 側 CQF 全域 bool API+編譯法、鼠族 defName 表。

- **客商/角色登門**：`SpecialPawnGenerateDef` 綁鼠族 pawnkind ＋ `DialogManagerDef`/`DialogTreeDef`（右鍵對話）
- **身份宣告/走向選擇/防重複**：`CQFAction_SetGlobalBool` ＋ `DialogCondition_Bool`
- **多階段委託**：多個 `QuestNode_DoCQFActions` ＋ `CQFAction_SentSignal` 信號接力
- **以物易物/付款**：`DialogOption.requiredThings`（不足自動禁用、選後自動扣料）
- **發實體獎勵**：**必用原版節點** `QuestNode_GenerateThingSet` → `QuestNode_DropPods`（CQF action 傳空 targets 發不了獎）
- **好感**：`CQFAction_ChangeGoodwillOfFaction`
- **任務鏈**：`CQFAction_Quest`（不經說書人；被鏈的 `rootSelectionWeight=0`）
- **建造紀念碑＋守衛**：✅ 確認。照抄 Royalty `BuildMonument_TimeProtect`（`Data/Royalty/Defs/QuestScriptDefs/BuildMonument/`）——`QuestNode_GenerateMonumentMarker` 生 marker，marker 完工自動送 `monumentMarker.MonumentCompleted`／被毀送 `monumentMarker.MonumentDestroyed`；`QuestNode_Delay inSignalEnable="monumentMarker.MonumentCompleted"`+`delayTicks` 撐過保護期→Success，保護期內被毀→隨機威脅懲罰→Fail。全信號驅動，與 `CQFAction_SentSignal` 相容。
- **前往 settlement 暗殺/攻打**：✅ 確認、零新 C#。借 Simple Warrants 的 `SitePartDef SW_Camp`＋`GenStepDef SW_Camp/SW_CampPower`（靠 `slate.Set("victim", pawn)` 通用介面讓目標 NPC 駐紮敵營）。完成判定**不用** SW 的送回制，改用原版通用「pawn 被殺→`Thing.Destroy(KillFinalize)` 自動送 `<tag>.Killed` 信號」：`QuestNode_AddTag`（或 `CQFAction_AddQuestTag`）把 `victim` tag 掛目標→`QuestNode_End inSignal="victim.Killed" outcome="Success"`。範本照抄 SW `Defs/QuestScriptDefs/Script_WarrantPawn.xml`，把 `inSignal` 從 `victim.WarrantRequestFulfilled` 換成 `victim.Killed`。前置延後：整段包 `QuestNode_Signal inSignal="AssaultStart"`，由 `CQFAction_SentSignal(signal=AssaultStart)` 點燃。
- **客商遇襲/派兵救援（D）**：✅ 確認（打殖民地版純 XML）。`QuestNode_Raid`（→`QuestPart_Incident`，收 `inSignal` 才 fire `RaidEnemy`）＋`QuestNode_Signal`/`QuestNode_Delay(inSignalEnable)` 包一層延後；CQF `CQFAction_SentSignal` 信號格式 `Quest{id}.xxx` 與原版 `QuestGen.GenerateNewSignal` 一致，可互通。原版近似範本＝Royalty `ShuttleCrash_Rescue`（NPC 墜船+敵襲+防守救援，敘事幾乎一樣）。⚠️ **鎖定只打特定 NPC＝C#-only**（`QuestPart_AssaultThings` 無 XML wrapper）。純 XML 替代二選一：(i) `QuestNode_AssaultColony` 打全殖民地（NPC 只是目標之一）；(ii) `CQFAction_SetDuty`＋自訂 `DutyDef`（仿 Faction War `SrAssaultFactionFirst`，零新 C#）讓敵人聚焦 NPC。落地時使用者選。
- **難民加入/借住/託孤**：【待設計】原版 refugee/lodger/pawn-lend 模式。
- **Faction War 資產結論**：它是「派系 vs 派系」整體交火（`SrFactionWar` 等 IncidentDef、`SrAssaultFactionFirst` 等 DutyDef），**不鎖 pawn 層級**；對「敵人打特定客商」幾乎沒有現成可直接引用的 Def，敵源仍靠指定 `Faction`＋原版 raid 管線。其 DutyDef 結構可當自訂 duty 的抄寫參考。

## 6. 落地策略

**垂直切片優先**（呼應發佈標準：靜態綠不夠，要實機 E2E）：
1. 先做**身份系統開場對話 ＋ A 供貨線**成一條完整可跑的線，實機 E2E 過。
2. 驗證通過後，把它當模板批量複製其餘任務線（多數共用同批積木）。
3. 內容軸：客商口吻參考敘事風格庫＋鼠族既有 lore，flavor 文本可交 agy cli 產、我校對結構。

## 7. 依賴（都在 2-anime）

- CQF `hailuan.customquestframework`（框架）
- 鼠族 `fxz.ratkinfaction`(+`oark.ratkinfaction.*`)（客商皮/pawnkind/派系）
- Faction War `sr.modrimworld.factionalwarcontinued`（戰鬥敵源）
- Simple Warrants `pb3n.simplewarrants`（暗殺/懸賞線，視最終落地決定要不要 requiredMod）
- 使用者授權：需要的話任何 modlist 內 mod 或自製 mod 都可引進

## 8. 開放問題（待使用者定）

1. 各任務線**獨立** vs 串成 **campaign**（客商貫串、旗標鎖前置）？
2. 據點身份是**開場一次選定**、還是**可隨遊戲演變/複選**？（架構支援複選）
3. 菜單/意外清單還要加什麼？
4. 工作名 `ratkin-questlines` 要不要換？

## 9. 任務線腦力激盪（廣度第一遍，2026-07-18）

五個 sonnet agent 各認領一寬泛據點類型，扎根鼠族本體 Defs 產出，共 **57 條點子**。完整內容見 `brainstorm/`：
- [`1-general-village.md`](brainstorm/1-general-village.md) 通用村莊（12 條）
- [`2-merchant.md`](brainstorm/2-merchant.md) 經營者/販賣（12 條）
- [`3-force-provider.md`](brainstorm/3-force-provider.md) 武力提供者（12 條）
- [`4-inn.md`](brainstorm/4-inn.md) 旅店（10 條）
- [`5-school.md`](brainstorm/5-school.md) 學校（11 條）

**✅ 依賴全部在 2-anime（已確認 packageId）**：`Solaris.RatkinRaceMod`(NewRatkinPlus 本體/內容主幹)、`fxz.ratkinfaction`(Faction+，僅加 Warlord+糕)、`bbb.ratkinweapon.morefailure`(Weapons+)、`W.ZHP`(Misc/雜貨鋪)、`RKU.RatkinUnderground`、`RKK.RatKnights.Core`(Knights+)、`cyanobot.toddlers`+`cj.rimtalk.toddlers`+`drati.toddlersplayexpanded`+`wuren.toddlersfeed`(Toddlers 鏈)、`oark.ratkinfaction.geneexpand`+`oark.ratkinfaction.oberoniaaurea`+`oark.oberoniaaurea.framework`(Gene Expand/旅鼠聯邦)、`oark.ratkinknightorderfurniture`。**無依賴缺口。**

**跨稿共通技術發現（彙整）：**
1. **內容主幹在 NewRatkinPlus**（`Solaris.RatkinRaceMod` 1578693166）；`fxz.ratkinfaction` 只加軍閥線＋`AC_RatkinCake`。引用 def 多數來自本體。
2. ⚠️ **隱藏派系無好感**：`RK_Faction_Pilgrims`/`RK_Faction_Caravan` 是 `hidden=true`，`CQFAction_ChangeGoodwillOfFaction` 對它們無效 → 好感掛母版派系（`Rakinia`/`ZHP_Faction`/`RKU_Faction`），隱藏派系只借 pawnkind 當視覺素材。
3. ⚠️ **Weapons+ 大量註解死代碼**：`RK_Cannon`/`RK_RocketLauncher`/`RK_BattleSuit`/`RK_Cheese`/`RK_Archoknife`/`RK_MagicWand`/`RK_Binocular`/`RK_WhiteCoat` 等未啟用，勿用。啟用武器見 `2-merchant.md`。
4. ★ **NewRatkinPlus 自帶「流浪商隊加入」條件系統**：`NewRatkin.PawnKindDefExtension_WanderingCaravanJoin`＋`JoinCondition_ColonyWealth`/`JoinCondition_InjuredPatientCount`（掛 `RK_PawnKind_Nomad`/`Wanderer`）→ **既是旅店「借宿→加入」線核心，也與 §2 身份財富/照護門檻直接相關，可複用**。
5. ⚠️ **無 race=Ratkin 0~3 歲幼崽 pawnkind**（Toddlers 的 `RimTalkToddler_Toddler`/`Child` 是 race=Human）→ 嬰兒線需自建；3 歲以上用既有池/`RKU_Miner`(9+)。
6. ★ **RimTalk Toddlers 已內建鼠族托嬰素材**：`RimTalk_ManyRatkinBabies`/`LiveInRatkinNursery` 心情、鼠族幼崽玩耳朵/尾巴動畫、`RimTalk_ChildrenOuting` 校外聚會——直接可用免自造。
7. ★ **多個原生心情/能力鏈可複用免自造**：`RK_PrayerService`+`RK_PriestPray`/`RK_PriestPrayMood`/`RK_AttendPrayerMeetingMood`（祭司祝福）、`RK_StrawberryBeer`+`RK_BeerBottleStrike`（酒館鬧事）、Toddlers 心情鏈。
8. ★ **RKK 騎士團內建五階試煉** `RKK_TrialFirst`…`RKK_TrialFinal` → 現成大型任務鏈骨架（見 `3-force-provider.md` #8）。
9. ⚠️ **RKK 自有 Class pawnkind 能否被 `SpecialPawnGenerateDef` 直接生成需實測**；耦合過深則同數值 `RatkinEliteDefender` 貼皮改名。
10. ★ **反覆出現的「後果/聲望」模式**（刺客聲望結算、商業信譽、走私立場旗標）全用 `CQFAction_SetGlobalBool` 累積 → **呼應 §2 衍生式身份**；CQF 無整數計數器，只能 bool 分級近似。
11. **需額外機制清單（跨稿）**：敵人鎖定特定 NPC(C#)、1v1 決鬥真判定、主動觸發 `GatheringDef`、race=Ratkin 嬰兒 pawnkind、CQF 開交易視窗(C#)。

## 10. 落地進度

- **2026-07-18｜首發垂直切片《陳皮餅乾的老交情》已建，靜態全綠。**
  - 檔案：`About/About.xml`（依賴 Harmony/CQF/NewRatkinPlus）、`LoadFolders.xml`、`1.6/Defs/DialogTreeDefs/DialogTree_RatkinPeddler.xml`＋`DialogManager_RatkinPeddler.xml`（`genrationConditions` 用 `DialogCondition_Or`+多個 `DialogCondition_Faction` 限鼠族派系，非硬依賴的 RKU/ZHP 用 `MayRequire` 保護）、`1.6/Defs/SpecialPawnGenerateDefs/SpecialPawn_RatkinPeddler.xml`、三語 Keyed、`tests/healthcheck.py`。
  - 機制：鼠族中立訪客登門 → 右鍵對話 → 打聽近況（世界感文本）／講價（社交>6，25 銀）／照價（40 銀）／大方（60 銀＋鼠族好感＋好客旗標）→ 交貨 `RK_Food_Hardtack`×12＋`RK_BeerBottle`×2。三交易 `removeDialogAfterSelect` 杜絕套利。
  - 驗證：`python3 tests/healthcheck.py` **全綠**（型別/欄位含繼承/三語 18 key 一致/6 個 defName 存在）。
  - 打包：`dist/mods/RatkinQuestlines-0.1.0/`（package-mod.sh，healthcheck 閘門）。
- **2026-07-18｜實機 E2E 第 1 輪（部署側，最小依賴包 121 mod）**：載入進主選單、NRE/segfault 0，但抓到 **1 條阻斷 def bug（×3）**：`RK_BeerBottle` **是 `ToolCapacityDef` 不是 `ThingDef`**（啤酒瓶近戰招式），`CQFThingDefCount` 要 ThingDef → cross-ref 紅字。**靜態健檢照不到（def 存在、只是型別錯）**。
  - ✅ **已修**：發貨啤酒 `RK_BeerBottle`→`RK_StrawberryBeer`（真正的啤酒 ThingDef，ParentName DrugBase，對話樹三處 barter）。重打包同路徑。
  - ✅ **healthcheck 補強**：新增型別檢查——`<thing>` 必為 ThingDef、`<faction>` 必為 FactionDef（查型別非只查存在），此 bug 類別以後靜態擋得掉。
  - ⏳ **待第 2 輪**：部署側重跑載入確認紅字歸零，並在桌面用滑鼠驅動對話 E2E（#2 訪客綁定／#3 對話樹分支/技能 gating/扣銀/發貨/好感／#4 CQF 執行期例外）。回信到 `~/repo/.../inbox/`。
  - 📌 教訓：**引用他 mod 的 def 要連型別一起確認，不能只查 defName 存在**；靜態綠≠實機綠，實機 E2E 是硬關（[[publish-bar-realmachine-e2e]]）。
- **2026-07-18｜實機 E2E 第 2 輪**：載入修復確認 PASS（紅字 0），對話樹 #2~#4 **靜態全綠**（結構/Class/Keyed/型別），但 **runtime GUI 未驅動**（使用者裁示本輪只到靜態信心）。使用者決定：**不等 runtime、開始複製**。
- **2026-07-18｜Phase-1 複製（F1 純對話邂逅）——擴到 5 條線**：
  - 新增 4 條：迷途的信使（指路打賞/近況/贈糧）、門口的流浪鼠（身世/施捨/銅板/趕走）、報恩的過客（收禮/問恩/婉拒）、朝聖的香客（供餐/朝聖/祝禱）。全 generic 鼠族訪客觸發、同一 proven 積木。
  - **架構升級**：單一 `SpecialPawnGenerateDef RatkinQL_EncounterSpawn` 掛全部 tag（官方 QE_RandomDialog 寫法）；每線一個 combined Def 檔（tree+manager）＋每線獨立三語 Keyed。旗標規約 `RatkinQL_Rep_*`（Helpful/Charitable/Kind/Hospitable）＋ peddler 的 HaggledHard/GenerousHost。
  - **文案量產管線驗證**：結構自寫 → prose 交 **agy**（batch1: 信使/流浪鼠）與 **DeepSeek API**（batch2: 報恩/香客）→ 自己校對＋healthcheck 驗 key 覆蓋。**比較**：DeepSeek 中文更穩（正確保留 Rakinia、自發長出鼠族信仰宇宙觀「地母/深土/祖靈祠」）；agy 會亂譯專有名詞（Rakinia→杜撰名，需校）。→ 之後量產優先 DeepSeek。
  - healthcheck 全綠（75 Keyed key 三語齊、型別檢查通過）。已重打包同路徑，寄信 `ratkin-questlines-e2e-5lines` 請部署側跑載入＋這輪驅動對話 runtime。
- **2026-07-18｜E2E 第 3 輪**：5 條線**載入乾淨 PASS**（0 紅字、0 Keyed 警告、NRE 0，75 key×三語全命中，單一 SpecialPawn 掛 5 tag 都在）。runtime GUI 部署側**交回使用者親驗**（登進其 WAIT_USER）。⇒ 批量複製架構在**載入層完全驗證**；runtime 行為待使用者點測。使用者指示：繼續。
- **2026-07-18｜Phase-1 批 3——擴到 8 條線**：新增示警的斥候（打賞/詳情/領情）、走失的幼崽（搜尋/經過/安撫，框成尋子家長以在 generic 訪客成立）、收路錢的（付/問/看破[智識8+]/回絕）。文案走 **DeepSeek**（parser 切塊寫檔，品質很好，con-artist 幽默到位）。healthcheck 逮到 1 漏：`RatkinQL_Toll_ReasonInt`（技能檢定 failReason）漏在 prompt 沒列 → 已補。📌 教訓：**prose prompt 要記得列出 failReason 這類非對白 key**。全綠（120 Keyed key 三語齊）。已重打包同路徑（未另送信，待使用者 runtime 回饋一起併入下輪 E2E）。
- **2026-07-18｜Phase-1 批 4——擴到 12 條線（F1 層完成）**：新增醉話的老兵（請酒講古）、走唱的伶人（點曲/民間傳說）、採藥的郎中（買草藥發 MedicineHerbal/討偏方）、收舊貨的（收木料換銀/翻舊貨）。全 DeepSeek 文案（parser 切塊，品質穩，DeepSeek 自發長出鼠族民間傳說/神話）。healthcheck 全綠（**168 Keyed key** 三語齊、型別含 MedicineHerbal/WoodLog）。已重打包。
  - **F1「純對話邂逅」層視為完成——12 條，調性/機制譜完整**：交易(peddler)、指路(courier)、施捨(vagabond)、受恩(grateful)、朝聖(pilgrim)、示警(scout)、尋子(lostkit)、識騙(toll)、老兵(veteran)、賣藝(minstrel)、醫藥(healer)、收荒(junk)。涵蓋暖/苦/諧/緊/信仰/市井。全建在同一 proven 積木＋單一 SpecialPawn 多 tag 架構。
  - **文案量產管線定型**：結構自寫 → DeepSeek 產三語（parser 自動切塊寫檔）→ healthcheck 驗 key 覆蓋+型別。**DeepSeek 為量產首選**（中文穩、自發世界觀、無亂譯）。
  - ⏳ **仍待 runtime E2E**（12 條同一條未眼驗管線；使用者的 WAIT_USER 待辦）。後續機制家族 F2-F7 見 §11，建議 runtime 綠燈後再投入（heavier、各需自己的 proof+E2E）。
- **2026-07-18｜F2 proof-slice——《鼠族供貨約》（使用者選「不等 runtime、續攻 F2」）**：
  - 三件：`ThingSetMakerDefs/ThingSetMaker_RatkinQL.xml`（`RatkinQL_ContractPay`→Silver 40）、`QuestScriptDefs/QuestScript_SupplyContract.xml`（`RatkinQL_SupplyContract`，信號接力兩期投放結算，仿 GuideCommission/EchoBeacon）、`DialogTreeDefs/Line_Contract.xml`（接約：交 Pemmican 30→`CQFAction_Quest` 觸發+好感+`RatkinQL_Ident_Supplier` 身份旗標）。
  - **新機制**（F2/F4/F5/F6 共用的骨幹）：對話→`CQFAction_Quest`→原版 `QuestScriptDef`；多階段 `QuestNode_DoCQFActions`+`CQFAction_DelayExecute`+`CQFAction_SentSignal`；發獎原版 `QuestNode_GetMap`→`GenerateThingSet`→`DropPods`。healthcheck 全綠（CQF 欄位全驗、180 key）。
  - **healthcheck 補強**：型別檢查改「只驗 `QuestEditor_Library.` 前綴的 CQF 型別，原版 QuestNode 交實機」（否則 `QuestNode_GetMap` 等被誤判未知型別）。
  - ⚠️ **這是全新、未 runtime 驗的機制面**（quest 鏈/DelayExecute/DropPods/ThingSetMaker）。已重打包 0.1.0（13 對話線+1 QuestScript）。**強烈建議此時做一次 runtime E2E 再往 F3-F7**（F4/5/6 都疊在這條 quest 鏈上，先驗省得後面跨家族修）。
- **2026-07-18｜E2E 第 4 輪＋F3**：13 線＋F2 quest 鏈**載入乾淨 PASS**、F2 信號鏈**靜態結構驗證正確**（部署側判定「結構層面可放心往上疊」）；runtime 仍待使用者（他當時在外、不在桌機）。使用者指示：繼續 → 做 **F3 收容/加入《請求庇護的鼠族難民》**：
  - `DialogTreeDefs/Line_Refugee.xml`。**關鍵發現＋採用**：`CQFAction_Faction`（`targetsText=Interviewee`，`faction=PlayerColony`；因 `faction.isPlayer` → `SetFaction(Faction.OfPlayer)`）＝**把對話中的鼠族當場收編成殖民者**。⇒ 「難民請求庇護→加入」**純對話乾淨做，零原版 lodger 依賴**。另有給補給送走（charity）/問身世/拒絕。記 `RatkinQL_Ident_Sanctuary` 身份旗標。
  - healthcheck 全綠（CQFAction_Faction 欄位驗、PlayerColony=FactionDef、194 key）。已重打包（**14 對話線+1 QuestScript**）。
  - **進度盤點**：F1（12 對話邂逅）+F2（供貨委託）+F3（難民收編）＝**「和平/社交」層完整**，全靜態綠。剩 **F4 紀念碑守衛 / F5 暗殺 / F6 來襲**（戰鬥層，各需原版 site/monument/raid 節點，更重更險）＋ **F7 身份系統（C# 子專案）**。這些疊在 F2 quest 鏈上——**建議 runtime 綠燈後再攻戰鬥層**（風險最集中處）。
- **2026-07-18｜F6 來襲＋F7 身份系統（C# capstone）**（使用者「一路做完」；連續 5 輪載入 PASS，runtime 持續卡使用者不在桌機）：
  - **F6《被追殺的鼠族》**（`Line_Hunted`）：迎戰＝`CQFAction_Incident`（`incident=RaidEnemy`,targetsText=Interviewee）純對話 fire raid，避開原版 raid 節點 plumbing。載入 PASS。記 `RatkinQL_Ident_Mercenary`。
  - **★F7 據點側寫器（C#，編譯驗證通過）**：`Source/MapComponent_RatkinColonyProfiler.cs` → `1.6/Assemblies/RatkinQuestlines.dll`（mcs 編譯，exit 0，對 Assembly-CSharp+QuestEditor_Library 引用全解析）。每 2500 tick 讀 `wealthWatcher.WealthTotal`/`FreeColonistsSpawnedCount`/主導技能，用 `GameComponent_Editor.Component.SetBool` 寫 `RatkinQL_State_*`（Established/Hamlet/Town/City/Wealthy＋按主導技能 Farming/Crafter/Trading/Martial/Clinic/Academy/Kitchen）。**採 VOE「身份＝功能（技能總和）」邏輯**，獨立、只感測+寫旗標、不碰模擬經營框架。
  - **F7 迴路示範＋§2 成熟門檻**：供貨約 `Line_Contract` 的 genrationConditions 加 `DialogCondition_Bool(RatkinQL_State_Established)`＝客商只找已成熟殖民地談約。C# 是**真編譯閘門**驗證，跟盲寫 XML 不同。
  - healthcheck 全綠。已重打包（**15 對話線+1 QuestScript+1 DLL**，Source 剝除）。
  - **★機制家族進度：F1/F2/F3/F6/F7 完成（5/7）**。剩 **F4 紀念碑、F5 暗殺**＝唯二需**原版 site/monument 生成機制**的——盲寫（無 runtime）易「靜默失敗」（清單選得到、實機生不出、無紅字），healthcheck 也擋不到，**誠實信心低，建議有 runtime 迴路時正確迭代**。
- **2026-07-18｜Kiiro quest 生態偵察（使用者指路）→ F4/F5 解鎖，改為「照抄實機驗證過的範本」，不再盲寫**：
  - Kiiro（`Ancot.KiiroStoryEventsExpanded`）有 ~24 個原版 QuestScriptDef＋自製 `Kiiro_Event.dll`，是同類（二次元種族）已上架的完整 quest 生態。深挖報告存 agent transcript。
  - **★F4「攻打/清場」正解＝模板 C（Kiiro `TradeRouteBanditCamp`，純 XML 零 C#）**：`Util_RandomizePointsChallengeRating`→`Util_AdjustPointsForDistantFight`→`QuestNode_GetSiteTile`→`QuestNode_GetSitePartDefsByTagsAndFaction`(**原版 `BanditCamp`/`Outpost` SitePartDef**,`mustBeHostileToFactionOf`)→`Util_GenerateSite`→`QuestNode_SpawnWorldObjects`→`QuestNode_WorldObjectTimeout`(Fail)→**`QuestNode_Signal inSignal="site.AllEnemiesDefeated"`→`QuestNode_End Success`**。完成判定靠**原版信號**，不用自寫偵測。
  - **★F5「摧毀/擊殺特定目標」正解＝模板 B（Kiiro `ProblemCauser`，原版 `QuestConditionCauser`）**：目標物掛 `CompQuestConditionCauser`（或 `QuestConditionCauser` tag 的 SitePartDef）→ 摧毀它**自動廣播 `conditionCauser.Destroyed`**→`QuestNode_End Success`。原版基礎設施，零自寫 C#。
  - **結論（agent）：F4/F5 第一版不需要 C# QuestNode 層**——純 XML＋原版節點/site part/信號足矣。C# 只在「重複查詢己方聚落/讀 mod 設定/陣營關係門檻」才值得（Kiiro 只寫了那幾個小節點）。⚠️ 待解：dialog 觸發（`CQFAction_Quest` 空 slate）vs 說書人觸發，site-gen 需要的 `$points`/`$asker` 等 slate 要自己補（`CQFAction_Quest` 用 `DefaultParmsNow` 給 points；hostile faction 可 `QuestNode_GetFaction` 或直接指 `Rakinia_Warlord`）。
  - **對照：我方 F3 收編（`CQFAction_Faction`）比 Kiiro 簡潔**（它們複製原版 C# 換皮，我一個 action 解決）。
  - **內容金礦（擴充靶，57 條之外）**：節慶（天燈/秋收/篝火→鼠族磷光菌信標節/囤糧節/地底集市）、礦坑重啟（F4 輕量原型）、圖騰獸現蹤（不戰而降）、瘟疫求醫、新巢穴選址、貿易節/常駐商戶、「不作為有代價」（逾時→友方聚落被毀）、主角畢業劇情（`AncotLibrary.QuestNode_PlayerWealth/Colonists/DaysPassed` 門檻）。
- **2026-07-18｜F4/F5 做掉＋F1 擴充→整包 7/7 家族到齊**（使用者「開 agent 把 F4/F5 做掉＋先擴充更多」）：
  - **F5《攻打軍閥據點》**（agent 做，healthcheck 綠）：`QuestScript_RaidOutpost.xml` 照 §I 骨架（GetSiteTile→`QuestNode_GetFaction`(allowEnemy/mustBePermanentEnemy)→sitePartsTags=原版 `Outpost`→`Util_GenerateSite`→`site.AllEnemiesDefeated` 清場→`ThingSetMaker RatkinQL_BountyReward` Silver 200~350）＋`Line_RaidOutpost.xml`(掮客懸賞,接下記 `RatkinQL_Ident_Mercenary`)。
  - **F4《建碑守衛》**（agent 做，healthcheck 綠）：`QuestScript_Monument.xml` 照 Royalty `BuildMonument_TimeProtect`（GetMap→`GetLargestClearArea`/`GetMonumentSketch`→`GenerateMonumentMarker`→空投給玩家建→`monumentMarker.MonumentCompleted` 撐 7 天保護期→發獎 Success／`MonumentDestroyed`→`Util_Raid` 懲罰 Fail）＋`Line_Monument.xml`(委託,記 `RatkinQL_Ident_Faithful`)。agent 查證這幾個節點只需 `$map`、不需 asker/points，dialog 觸發 medium-low 風險。
  - **F1 擴充 3 條**（我做，DeepSeek 文案）：《節慶的邀請》《講古的老者》《報喪的過客》——文化/世界感深化。
  - **healthcheck 也修好一個真 bug**（F5 agent 修）：`<faction>$siteFaction</faction>` 這種 slate 變數不再被當字面 defName 查（加 `$` 排除）。
  - **★整合完成**：SpecialPawn 註冊 20 tag（＝20 DialogManager，完美對齊）、整合 healthcheck 全綠（270 key）、重打包。**產物＝20 對話線＋3 QuestScript（SupplyContract/RaidOutpost/Monument）＋3 ThingSetMaker＋1 C# DLL＋三語各 20 檔**。
  - **⏳ 未 runtime 驗（誠實標記）**：F5 site 生成/清場信號/dialog 觸發 GetFaction 撈敵源；F4 monument marker 非說書人路徑生成後完工信號是否觸發/保護期計時；獎勵平衡。皆需 2-anime 實機一輪坐實（發佈標準）。

**首發垂直切片**：已定《陳皮餅乾的老交情》（見 §10），E2E 測試中。

## 11b. 續作指南（post-compact 從這裡接上，2026-07-18）

**現況**：7/7 機制家族全實作、**20 對話線＋3 QuestScript（SupplyContract/RaidOutpost/Monument）＋3 ThingSetMaker＋1 C# DLL（F7 側寫器）**，整合 healthcheck 全綠、已打包 `dist/mods/RatkinQuestlines-0.1.0/`。**靜態＋載入＋C# 實例化 實機全綠（E2E 第 6 輪）**；唯一未驗＝runtime 行為，卡 portal 輸入 wedge（機器問題，待使用者回機/KWin reset）。

**★新增一條 F1 對話邂逅線的可重複配方（最常做的擴充）：**
1. 寫結構 `1.6/Defs/DialogTreeDefs/Line_<Name>.xml`（照任一既有 Line_*.xml：3 節點 tree＋DialogManager，`genrationConditions` 照抄鼠族派系 Or 篩選；用已驗證積木見 [`brainstorm/quest-mechanics-reference.md`](brainstorm/quest-mechanics-reference.md) §A）。前綴 `RatkinQL_<Name>`，flag 用 `RatkinQL_Rep_*`(聲望)/`RatkinQL_Ident_*`(身份傾向)。
2. 文案交 **DeepSeek**：寫 prompt（範本 `tools/EXAMPLE_prompt.txt`，格式規則照它）→ `source ~/.zshrc; python3 tools/ds_call.py <prompt.txt> > out.txt` → 在 `tools/parse_write.py` 的 `LINE_MAP` 加 `"<HEADER>": "RatkinQL_<Name>"` → `python3 tools/parse_write.py out.txt` 寫三語 Keyed。⚠️ prompt 要列全 key（含 failReason 等非對白 key）；grep 掃 `{PAWN/{0/{1` 未授權 placeholder；DeepSeek 偶爾漏 ```xml fence（parser 已 fence-agnostic）。
3. 在 `1.6/Defs/SpecialPawnGenerateDefs/SpecialPawn_RatkinEncounters.xml` 的 dialogs 加 `<li><tag>RatkinQL_<Name></tag><commonality>0.4</commonality></li>`。
4. `python3 tests/healthcheck.py` 直到全綠（驗 XML/CQF 欄位/三語 key 一致/def 型別）→ `./dist/package-mod.sh ratkin-questlines RatkinQuestlines 0.1.0` 重打包。

**委託/戰鬥線（F2/F4/F5 型）**：多一個 `QuestScript_<Name>.xml`（照 §B 信號接力 或 §I site 骨架 或 §D monument），對話用 `CQFAction_Quest` 觸發。**site/monument 用原版節點＋`Util_GenerateSite`＋`site.AllEnemiesDefeated`/`conditionCauser.Destroyed` 完成信號**（§C/§I），不盲寫。可依賴 `OberoniaAurea_Frame`（已在 modlist）複用 `QuestNode_GetRatkinFaction`/`QuestNode_Root_RefugeeBase`（§H）。

**擴充內容靶（優先）**：①據點子類空格（牧場/漁村/戲班/使館/監獄/邊境，見 `brainstorm/0-settlement-taxonomy.md`）②Kiiro 內容金礦（節慶/礦坑重啟/圖騰獸/瘟疫求醫/新巢穴/貿易節/「不作為有代價」，見 §11c 下方）③57 條腦力激盪裡未落地的（`brainstorm/1-5*.md`）。**用鼠族社會派系織身份**（王國/騎士團/游擊隊/軍閥/雜貨鋪）＝北極星。

## 11. 建置路線圖（2026-07-18 規劃）

**核心策略：按「機制家族」推進，不按據點類型。** 建置成本/風險由**機制**決定，不由主題決定；同一機制家族內的線＝廉價內容複製。先為每個機制家族做一條 proof-slice、實機 E2E 過，再批量填該家族內容。

**✅ DLC 全在 2-anime**：Royalty／Ideology／Biotech／Anomaly——F4 紀念碑（Royalty）、F3 幼崽成長（Biotech）、信仰內容（Ideology）皆無 DLC 缺口。

**機制家族（把 57 條重新歸類）：**
| 家族 | 機制 | 涵蓋的線 | 成本 |
|---|---|---|---|
| **F1 純對話邂逅** | `SpecialPawn`+`DialogTree`（barter/技能檢定/發獎/好感/旗標） | 大量輕插曲（村莊/旅店/經營大半＋部分武力/學校）| 最低（過 F1 後純內容複製）|
| **F2 多階段委託** | 對話→`CQFAction_Quest`→`QuestScriptDef` 信號接力＋`requiredThings` 分批 | 供貨合約/軍火單/急救包/獨家經銷 | 中 |
| **F3 收容/加入** | 原版 refugee/lodger＋對話（可複用 NewRatkinPlus 的 `WanderingCaravanJoin` 條件系統）| 難民庇護/託孤/收養/學徒見習 | 中 |
| **F4 建紀念碑＋守衛** | Royalty `BuildMonument_TimeProtect`＋信號 raid | 建設守衛線 | 中高 |
| **F5 攻打 site/暗殺** | Simple Warrants `SW_Camp`＋`victim.Killed` | 暗殺/懸賞/清剿/護送遇襲 | 中高 |
| **F6 來襲/防禦** | `QuestNode_Raid` on signal | 遇襲救援/據點防守/守衛契約 | 中（與 F4/F5 積木重疊）|
| **F7 據點身份系統** | C# colony profiler＋全域旗標＋篩訪客/委託 | 橫切基礎建設（唯一要 C#）| 最高 |

**分階段路線：**
- **Phase 0（進行中）**：F1 proof＝《陳皮餅乾》實機 E2E。
- **Phase 1**：F1 過後 → 批量複製 ~20+ 條純對話插曲（跨五類）＋同時建下方「共用基礎建設」。
- **Phase 2**：F2 proof（如《王國補給契約》）→ E2E → 批量供貨/委託線。
- **Phase 3**：F3 proof（如《孤兒的新家》）→ E2E → 批量人情線。
- **Phase 4**：F4/F5/F6 戰鬥家族 proof（紀念碑守衛、暗殺各一）→ E2E → 批量戰鬥線。⚠️「敵人只鎖定特定 NPC」需 C# 或接受打全殖民地。
- **Phase 5（可並行）**：F7 身份系統——把整包從「零散任務」升級成「活世界」的關鍵，最花工。

**共用基礎建設（做一次、全包複用）：**
1. **鼠族訪客對話母版**：用 Abstract `DialogManagerDef`（`ParentName`+`Abstract="True"`）把「`genrationConditions` 鼠族派系篩選」抽成共用父，每條對話線繼承它、只改 tag＋tree ⇒ 免 57 份 copy-paste 派系篩選。
2. **共用獎勵 `ThingSetMaker`**：鼠族風味分級獎勵組（小/中/大＋鼠族物品）。
3. **全域旗標命名規約**：身份傾向 `RatkinQL_Ident_*`、聲望/後果 `RatkinQL_Rep_*`、防重複旗標——集中一張表，供 F7 側寫器與各線共用（本切片已用 `RatkinQL_HaggledHard`/`RatkinQL_GenerousHost`，納入規約）。
4. **鼠族派系→好感掛點對照表**：隱藏派系無好感 → 統一掛母版派系的規約（見 §9 發現 #2）。

**F7 C# colony profiler 子專案規劃：**
- 一個 `GameComponent`/`MapComponent`，定期讀殖民地狀態（wealth 分項、`RoomRoleDef` 計數、武裝、biome、人口、囚犯數）＋我方任務計數，算身份分數，寫回 `RatkinQL_Ident_*` 全域 bool（供 XML `DialogCondition_Bool` 消費）。
- 可選自訂 `CQFAction_SetGlobalInt`/`DialogCondition_IntThreshold` 補整數計數（供貨量/暗殺數精確累積）。
- 建置：net48，引用 CQF DLL＋RimWorld 本體（照 CQF tutorial §5 骨架）。**獨立、只感測＋寫旗標，不碰模擬經營框架**（解耦約束）。

**規模化資料夾/命名：** 每條線前綴 `RatkinQL_<Line>`；對話線 `DialogTreeDefs/`、委託線 `QuestScriptDefs/`、獎勵 `ThingSetMakerDefs/`、訪客 `SpecialPawnGenerateDefs/`。Keyed 線多了按類拆檔。brainstorm/ 57 條落地時對應線 ID。

**規劃層待確認/風險：**
- ⚠️ Ideology 是凍結策展 → 碰 ideo/goodwill 的線留意（多數不受影響）。
- ⚠️「敵人只打特定 NPC」C#-only → 戰鬥線二選一。
- ⚠️ race=Ratkin 0~3 歲幼崽 pawnkind 需自建 → 嬰兒線前置。
- ⚠️ RKK 自有 Class pawnkind 能否 `SpecialPawn` 直生需實測。
- 文案量產交 agy/DeepSeek（見開發區 `workflows/tooling/text-generation.md`），結構/def/信號自己寫。
- **類型細化**（VOE 分類，§3 備註）可在 Phase 1 前做，讓內容複製有更準的子類靶。

## 11e. F8 建置日誌（2026-07-18 晚）— 劇情弧＋善惡名聲軸

**敘事透鏡（使用者定調）**：每條線先問「在講誰的故事」「玩家聚居點扮演什麼角色」。並要善惡值/分類名聲當**軟門檻**（用幾率、別限死）＋**具名人物加入殖民地**。

**新增子系統 F8＝善惡值/分類名聲帳本**（`Source/GameComponent_RatkinLedger.cs`）：對話 `CQFAction_SetGlobalBool` 設事件旗標 `RatkinQL_Ev_*` → 帳本每 500t 消費→累加 `karma`/`rep[type]` → 回寫階層 bool `RatkinQL_Karma_*`/`RatkinQL_Rep_*` 供對話 gate；軟 gate `RatkinQL_Soft_OrphanOk` 每 2500t 擲骰=f(善惡)×(暗殺名聲抑制)。詳見 [`REVIEW-2026-07-18-F8.md`](REVIEW-2026-07-18-F8.md)。

**任務線 A《商團首領佩林》**（3 對話章 + 1 site 子任務）：佩林．胡桃＝母鼠、鄉下小貴族三女；分章 bool 閂；Ch3 分「風光重逢送厚禮」vs「破產求收留→加入殖民地」（`CQFAction_Faction` 收編＋自製 `CQFAction_SetName` 正名＋固定母性）。
**任務線 B《鼠族王國軍械採購》**（信使→軍需官收貨）：講王國派系故事、據點角色＝軍械工坊（gate `State_Crafter/Martial`）；交 3 長劍換「王國號角」`RatkinQL_KingdomTally`（`CompUseEffect_RatkinAid` 召友軍 `RaidFriendly`+`ImmediateAttackFriendly`）。

**新可複用機制**：`CQFAction_SetName`(+gender)、`CompUseEffect_RatkinAid`、F8 帳本。新積木紀錄見機制庫 §M。
**產物**：+6 Defs +3 C# +10 三語 Keyed；healthcheck 全綠（112 檔）；打包 `dist/mods/RatkinQuestlines-0.2.0/`。**全 runtime 待實機**（portal wedge 未解）。
**待使用者拍板**：①佩林加入保真度 A(名字+性別) vs B(專屬母鼠 PawnKind 全固定) ②F8 回接既有 20 線？ ③武器商品質門檻/號角延遲限次/可重複？（見 REVIEW §六）

## 11f. 下一階段規劃（2026-07-19，規劃階段）

**觸發機制轉向（部署側 E2E 拍板）**：主線任務從「隨機掛訪客」改為 **信件→接受/拒絕→接受才召帶對話的訪客**（自製 `QuestNode_SpawnRatkinEnvoy`＋`GameComponent_Editor.AddDialog`）。現有對話機器全留、只換召喚方式。已坐實：自製 C# 子類能載入＋執行（令狀召友軍 runtime 成功）。

**善惡/名聲原則**：不立專線；普通任務裡「部分選項記 `Ev_*`、累積回頭改寫分支/來客/任務出現」，自然體現。

**下一批＝兩個據點類型家族**：**鐵匠屋**（層級委託系統＋預定義具名客戶池，每名一線＋滿意度/blocked）＋**傭兵團**（待細化）。完整大綱與進度接點見 [`brainstorm/6-forge-and-company.md`](brainstorm/6-forge-and-company.md)。鼠族設定硬約束（1:10 女多/moe/年輕）見 [`WORLDVIEW.md`](WORLDVIEW.md)。

**接續**：擬鐵匠屋客戶名冊 → 定案 → 地基（信件觸發＋F8 分級名聲/per-客戶狀態擴充）＋T1 試點 → 送測。

## 11g. 鐵匠屋地基＋T1 試點建置日誌（2026-07-19）

名冊審核通過（[`brainstorm/6a-forge-client-roster.md`](brainstorm/6a-forge-client-roster.md)，使用者「過」＝全預設；T1×4/T2×4/T3×1+選配、N=3、折扣改「定期送好貨」、覆滅走機率+延遲+愧疚、信件先做鐵匠屋 T1）。**地基＋T1 試點已建成、healthcheck 全綠、mcs 編譯 exit 0，全 runtime 待實機。**

- **C# 信件觸發地基**：`Source/QuestNode_SpawnRatkinEnvoy.cs`——`QuestNode_SpawnRatkinEnvoy`（RunInt 建 `QuestPart_SpawnRatkinEnvoy`，inSignal 留空＝吃 quest initiate＝接受時；接受→`PawnGroupMakerUtility.GeneratePawns`(Peaceful) 生 Rakinia 訪客團＋`GenSpawn.Spawn`＋`LordMaker.MakeNewLord(LordJob_VisitColony)`＋`GameComponent_Editor.Component.AddDialog(leader, dm)`；保底 basicMemberKind）＋守衛節點 `QuestNode_RequireGlobalBool`（TestRunInt 讀全域 bool，false→說書人不提供該任務；`QuestNode_Sequence` 任一子節點 Test 失敗即擋，已驗）。仿 `QuestNode_DoCQFActions` 信號掛法＋`IncidentWorker_VisitorGroup` 生訪客邏輯（反編譯源核對簽名）。
- **F8 帳本擴充**（`GameComponent_RatkinLedger.cs`）：分級名聲 bool `Forge_T1/T2/T3Unlocked`（門檻常數 `ForgeM1=40`/`ForgeM2=120`）＋per-客戶關係狀態（`forgeSat`/`forgeBlocked` dict，ExposeData 持久化）：每 tick 消費 `Ev_ForgeWellDone_<id>`（滿意度+1＋weaponMerchant rep+12）與 `Ev_ForgeBlocked_<id>`（blocked=true＋rep−10＋karma−4），回寫 `Forge_<id>_Available`（層解鎖且未 blocked）/`Forge_<id>_RewardReady`（滿意度≥N=3）。客戶名冊＝`ForgeClientTier` static dict，新增客戶只加一列。
- **T1 試點（橡實村·村長板栗）**：`QuestScript_ForgeAcornVillage.xml`（rootSelectionWeight=1、守衛 gate `State_Crafter`+`Forge_AcornVillage_Available`、接受→SpawnEnvoy→Delay 1 天→End Success）＋`Line_ForgeAcornVillage.xml`（單場對話：交 4 把現貨 `Bow_Short`[複用 `DialogCondition_ThingQuality`+`CQFAction_ConsumeQualityThings`，minQuality=Awful＝不挑品質]→`CQFAction_Spawn` 發 Silver90+RK_Food_Hardtack15＋`Ev_ForgeWellDone_AcornVillage`＋王國好感；抬價敲詐→`Ev_ForgeBlocked_AcornVillage`＋好感−；詳情/回絕分支）＋三語 19 Keyed（板栗＝年輕元氣村長、抱記帳板算錢精、塞炒栗子反差萌，遵 WORLDVIEW）。信件制客戶 Manager 不需 genrationConditions（AddDialog 直掛不經隨機訪客篩選）。
- **mcs 編譯**：`-r:Assembly-CSharp.dll -r:UnityEngine.CoreModule.dll -r:netstandard.dll -r:QuestEditor_Library.dll Source/*.cs`（⚠ 需 netstandard.dll，否則 CS0012 ValueType；0Harmony.dll 不在 Managed，本包未用可略）。
- **⚠ 待實機 E2E**：訪客團生成/`AddDialog`/到訪 Lord/守衛擋任務/交貨扣料/per-客戶狀態回寫 皆第一次上機。

### 11g 續：T1 全層擴充（2026-07-19 同日，使用者連續設計，healthcheck 全綠、mcs exit 0）

使用者在試點建成後連發多則設計，全數落地（設計權威＝[`brainstorm/6b-forge-design-increment.md`](brainstorm/6b-forge-design-increment.md)）：

- **武器改鼠族原生**：查 `ratkin-weapons-catalog.md`（三 mod ~67 件可用武器編目）→ 板栗現貨改 **RK_Crossbow×3＋RK_Spear×2**（遠程+矛防野獸，雙條件 AND 交貨）。
- **科技藍圖**（新 C#）：`TechBlueprint.cs`＝`CompUseEffect_UnlockResearch`（用了解鎖研究清單）＋`QuestNode_DropTechBlueprintOnce`（每層首個委託接受時空投、gateBool 只送一次）。T1 藍圖 ThingDef `RatkinQL_TechBlueprint_ForgeT1` 解 `RK_Research_Carpentry`+`RK_Research_SwordAndShield`。板栗＋匿名散客任務皆掛（同 gateBool）。
- **彈性貨品需求**（新 C#）：`WeaponValueDelivery.cs`＝`DialogCondition_WeaponValue`（count `IntRange`/minQuality/value 門檻＝絕對或 refThing×refCount 基準式+靜態快取/weaponFilter）＋`CQFAction_ConsumeWeaponsByValue`。
- **分級後果**：板栗加 band-2「精品交貨」（弩+矛皆 Good+→村莊完美擊退、厚禮+名聲+`Ev_Charity` karma）；band 由多個不同門檻交貨選項實現。
- **匿名散客**（新，不記忠誠）：`QuestScript/Line_ForgeWanderingBuyer`＝慕名而來散客、彈性價值需求（4 把/Normal+/值≥4 把鋼鐵普通長劍）、只給 `Ev_WeaponDeal`。三語 Keyed。
- **名號階梯**（四層 鐵匠→鐵匠鋪→名匠鋪→軍械名家）：v1 每層文案用該層稱謂，板栗/散客（T1）稱你「鐵匠」。
- **✅ 交貨挑選視窗 C 已建（2026-07-19）**：`Source/ForgeDelivery.cs`＝`Dialog_ForgeDelivery`（Window：列玩家武器+勾選+市值、價值進度條標 1×/2×/3× band 刻度、數量 min..max 約束、確認→消費勾選+發該 band 獎勵+set Ev 名聲旗標+訊息）＋`CQFAction_OpenDeliveryWindow`（對話「交貨」選項開視窗，帶需求+bands 設定）＋`ForgeDeliveryBand`（valueMultiple/silver/extraThings/evFlags/message）。**無 Harmony、零交易 mod 相容風險**。已接匿名散客（4~6 把、band1 1×→280 銀、band2 2×→520 銀+揚名）；板栗維持簡單對話交貨＝兩種交貨並存。mcs exit 0、DLL 30k（需引用 UnityEngine.IMGUIModule+TextRenderingModule）。
- **✅ 交貨 B 原生交易已建（2026-07-19）**：`Source/ForgeTradeDelivery.cs`＋`HarmonyInit.cs`（本 mod 首次用 Harmony）。`CQFAction_OpenTradeDelivery`＝把訪客設 trader（`Visitor_Outlander_Standard` traderKind＋生商品）+登記 `PendingForgeTrade`+開原生 `Dialog_Trade`；`Patch_TradeDeal_TryExecute` 交易成立後掃 `PlayerSells` 武器→評 band→補名聲/額外物（**不補 band.silver，因玩家已從交易拿市值銀**）。已接匿名散客第二交貨選項（C 視窗/B 交易並存）。
  - **★教訓（2026-07-19 code review 抓到、0.3.2 修）**：postfix 讀交易資料**炸**——`TradeDeal.TryExecute` 成交後 `ResolveTrade()`→`Reset()`（`tradeables.Clear()`+`AddAllTradeables()`，CountToTransfer 歸 0、賣出物已離場）才 return，故 postfix 讀到**空資料**→B 永不發獎。**改法＝Prefix 快照**（Reset 前記玩家意圖賣出的武器件數/值）＋Postfix 僅在 `__result && actuallyTraded` 消費快照。**凡要偵測「交易賣出了什麼」＝Prefix 快照，不能 Postfix 讀 deal。** 另修：letterText 半填 NRE、refStuff 缺省紅字（`GenStuff.DefaultStuffFor` 兜底）、ConsumeWeaponsByValue 過量扣料。
  - ⚠**B 全靠 runtime**：trader 轉換/交易流程/pending 靜態非持久（中途存檔邊界）皆待實機。相容：Prefix/Postfix 唯讀低風險（見 §6.8）。`CQFAction_OpenTradeDelivery` 的 XML `targetsText` 須含 `Interviewee`+`Interviewer`（否則 no-op，已配）。
- **待議/待建**：①善惡值＝服務對象×態度（§6.7，待專門討論）②名匠傳奇武器（§6.9，T3 未來）。
- **C# 檔（11 個）**：`GameComponent_RatkinLedger`/`MapComponent_RatkinColonyProfiler`/`QualityDelivery`/`CQFAction_SetName`/`CompUseEffect_RatkinAid`/`QuestNode_SpawnRatkinEnvoy`/`TechBlueprint`/`WeaponValueDelivery`/`ForgeDelivery`/`ForgeTradeDelivery`/`HarmonyInit`。mcs exit 0、DLL 34k。healthcheck：131 XML、三語各 412 Keyed key。
- **⚠ 編譯指令更新**（新增 UI＋Harmony 引用）：`mcs -target:library -out:1.6/Assemblies/RatkinQuestlines.dll -r:Assembly-CSharp.dll -r:UnityEngine.CoreModule.dll -r:UnityEngine.IMGUIModule.dll -r:UnityEngine.TextRenderingModule.dll -r:netstandard.dll -r:<QEL>/QuestEditor_Library.dll -r:<2009463077>/Current/Assemblies/0Harmony.dll Source/*.cs`
