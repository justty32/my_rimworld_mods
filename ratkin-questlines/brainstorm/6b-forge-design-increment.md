# 鐵匠屋設計增補（2026-07-19 使用者連續指示，待確認後實作）

> 承 [`6-forge-and-company.md`](6-forge-and-company.md)＋名冊 [`6a`](6a-forge-client-roster.md)。地基＋T1 試點（板栗）已建、靜態綠。
> 本份收錄使用者在試點建成後追加的四項設計，**部分待拍板**（見文末問題），拍板後併入實作。

## §6.1 玩家鐵鋪名號階梯（稱謂隨名聲升）

使用者：「軍械名匠」是名聲最高層才有的稱呼；名號應隨名聲升級：**鐵匠 → 鐵匠鋪 → 名匠鋪 → …**

- 名號綁 `weaponMerchant` 名聲層（對齊 T1/T2/T3 解鎖）。低名聲你只是個「鐵匠」；越做越大 → 「鐵匠鋪」→「名匠鋪」→（頂層名號待定）。
- **v1 實作（省 C#）**：每層客戶文案直接用該層稱謂——T1 客戶叫你「鐵匠」、T2「鐵匠鋪師傅」、T3「名匠/名匠鋪」。因為高層客戶只在高名聲出現，稱謂自然對得上層級。
- **後續增強**：動態注入 token（同一具名客戶隨你升級改口）＝自製對話 token/C#，先不做。
- ✅ 已改：板栗（T1）從「軍械名匠」改叫「鐵匠」（三語）。

## §6.2 科技藍圖空投（每層「第一個任務」）

使用者：村莊(T1)/城鎮(T2)/王國(T3) 各自的**第一個委託**，在**接受的瞬間**就直接送來（或空投）該層武器**所需科技的科技藍圖**——玩家才做得出貨。

- **為何**：客戶要 `RK_Crossbow`/`RK_Spear`，但玩家可能還沒研究出來 → 首單附上 know-how，閉合「接單→能造→交貨」迴路。敘事上＝客戶「先墊技術」。
- **T1 所需研究（已查證，NewRatkinPlus 研究，皆存在）**：
  - `RK_Crossbow` → `RK_Research_Carpentry`
  - `RK_Spear` → `RK_Research_SwordAndShield`
  - ⇒ **T1 藍圖解此二項研究**。
- **每層只送一次**：帳本 bool `RatkinQL_Forge_T1TechGifted`/`T2`/`T3`（首次接受該層任務→空投＋set true）。
- **實作候選**：自製「科技藍圖」ThingDef ＋ `CompUseEffect_UnlockResearch`（使用→`Find.ResearchManager.FinishProject(研究)`），接受首個該層任務時 `QuestNode_DropPods`/`CQFAction` 空投。
- **待查**：T2/T3 各委託武器對應研究（用 `ratkin-weapons-catalog.md` 逐件查 `researchPrerequisite`）。

## §6.3 兩類客戶：具名 vs 匿名隨機

使用者：這些任務線都是寫死的具名客戶；還要多加**隨機任務——不算忠誠度**，那種**不知哪來的小村/遊商/慕名而來**的散客，想採購/定做。

| | **具名客戶**（名冊 6a） | **匿名隨機客戶**（新） |
|---|---|---|
| 身份 | 橡實村板栗、赤爪傭兵團…有名有姓 | 不知哪來的小村/遊商/慕名散客 |
| 忠誠度 | ✅ 記滿意度/blocked/專屬獎勵 | ❌ 不記，一次性 |
| 對話 | 手寫、每名一線 | 通用模板（可少量變體） |
| 觸發 | **信件制**（可靠，主線） | **隨機訪客掛通用對話**（bonus，偶爾不出現無妨） |
| 武器 | 固定組合（板栗＝弩+矛）或後續隨機 | **隨機**（從該名聲層武器池抽） |
| 名聲 | `Ev_ForgeWellDone_<id>` | 只 `Ev_WeaponDeal`（給名聲、不記客戶） |
| 意義 | 北極星沉浸、回頭客關係 | 世界活水、隨機性、慕名而來的成就感 |

- **匿名線實作**：複用「隨機鼠族訪客掛對話」機制（`SpecialPawnGenerator_AddDialog`，即現有 Peddler 那套）。gate `State_Crafter`；**名聲越高→散客越多/單越大/「慕名而來」**（genrationConditions 加名聲層 bool）。這正是舊 ArmsEnvoy 那套的正確用途——bonus 線可靠性要求低。
- 匿名散客名字＝鼠族名庫隨機生成（訪客本來就有隨機名），不必自寫。

## §6.4 委託武器隨機

- **匿名線**：隨機武器（該名聲層 catalog 池抽）。實作＝對話多選項各認一種武器（玩家有哪種交哪種），或自製 C# 動態選 def（較重）。
- **具名線**：固定組合（板栗＝3 弩+2 矛，防野獸遠近兼備）；日後可加隨機。
- 使用者原問「不能隨機嗎」＝由匿名線承接隨機，具名線保手寫記憶感。

---

## §6.5 彈性貨品需求（具名客戶定製，別寫死 defName）【使用者定案】

使用者：具名客戶現有節點的貨品需求都寫死了。希望**除了第一個、最後一個、或途中重要節點**（那些可指定招牌/特定貨），**其餘節點的需求別訂死**，改用參數化條件：

> 例：**四把武器，品質需良好(Good)以上，武器總價值需超過「四把鋼鐵鼠族良好品質長劍的價值總和」（以此為基準）。**

- **語意**：不指定 defName，只要求「數量 N ＋ 每件品質 ≥ minQ ＋ 交付總價值 ≥ 門檻」。玩家用任何武器湊滿即可 → 打造自由、鼓勵做高品質好料。
- **價值基準**：門檻＝`refCount × 某參考武器(refThing 用 refStuff、refQuality)的 MarketValue`。作者只需寫「以 4 把鋼鐵良好長劍為基準」，無需硬算銀數；參考武器改版也自動跟隨。
- **實作（C#，可複用）**：
  - `DialogCondition_WeaponValue`：閂交貨選項。掃玩家地圖武器，取品質 ≥ `minQuality` 者，若數量 ≥ `count` 且其中最值錢的 `count` 件 MarketValue 加總 ≥ 門檻 → Satisfied。門檻由 `minTotalValue`（絕對）或 `refThing/refStuff/refQuality/refCount`（基準式，runtime 算＋快取）擇一。可選 `weaponFilter`（近戰/遠程/不限）。
  - `CQFAction_ConsumeWeaponsByValue`：交貨時消費那 `count` 件（品質達標、值最高的 count 件）。
  - 值計算＝`ThingMaker.MakeThing(def,stuff)`＋`CompQuality.SetQuality`讀 `MarketValue`，靜態快取避免每 tick 重算。
- **節點分工**：首/末/重要節點＝可續用寫死的 `DialogCondition_ThingQuality`（指定招牌武器，如 T3 王國要 Gunlance）；中間常態節點＝用本彈性條件。T1 現貨（板栗＝弩+矛）屬「簡單現貨/首單」性質，維持指定 defName 合理（現貨＝特定平價存貨）。**彈性條件主要服務 T2+ 定製**。

### §6.5.1 數量上下限（愈多愈好）【使用者追加】

- 需求的**數量可為區間 `count: min..max`**：交 ≥min 才成立、上限 max，**交越多（到 max）越好**。不是每次都固定 N。
- `DialogCondition_WeaponValue` 的 `count` 改吃 `IntRange`（min 為門檻、max 為封頂消費量）；`CQFAction_ConsumeWeaponsByValue` 消費「實際可交且達標的件數（min≤n≤max，取值最高者）」。
- 與 §6.6 分級後果扣合：實際交付**件數**與**總價值**共同決定結果等級。

## §6.6 分級後果：交越多/越精良 → 越好的結局【使用者定案】

使用者：依交付**武器總價值**（與件數）決定該任務節點的**結果**，且要「很有趣」。例：

> - 交付總價值達需求 **2 倍**以上 → 村莊**完美擊退敵軍**，謝禮更多、名聲提高更多。
> - 給王國的物品總價值超過 **3 倍** → 王國**國力強盛**（特殊後果）。

- **語意**：交貨不再是過/不過二元，而是**分級（band）**：1×（達標，基本結局）／2×（優異，村莊完美擊退＋厚禮＋名聲多）／3×（極致，王國強盛等招牌後果）。慷慨與精良被獎勵。
- **實作（純 CQF，複用彈性條件）**：交貨節點放**多個 `hideWhenDisabled` 交貨選項，各以不同 band 門檻的 `DialogCondition_WeaponValue` 閂**（1×/2×/3× 門檻）。玩家看到自己夠得到的最高 band，**自選要傾注到哪一級**（給越多換越好結局 vs 留貨）＝有趣的取捨。每個 band 選項＝該級的獎勵量級＋`Ev_*` 名聲多寡＋敘事後果訊息（完美擊退／國力強盛）。
- **後果可延伸（越有趣越好，先記後補）**：村莊 band3＝該村民日後來投／送特產事件；王國 band3＝大幅好感＋開放助戰契約／授榮銜／頂層名號「軍械名家」提前解。這些接 §6.2 神祕獎勵與名號階梯。

## §6.7 善惡值＝服務對象 × 態度【✅ 使用者定案 2026-07-19】

> **拍板**：①三分陣營＋karma 掛法＝通過。②**只有「超額」（band2+）才記 karma**——達標(band1)中性不記；正派超額→`Ev_Charity`(善)、邪派超額→`Ev_Cruelty`(惡，越好兵器給壞人越重)。③惡名守衛（正派客戶加 `Karma_NotEvil` gate）**T1 先不加**（鐵匠屋暫無邪派客戶＝無 forge 負 karma 來源，現在加只增測試干擾）；**待 T2 邪派/髒活客戶登場時連同其 Karma gate 一起加**，閉環才完整。
> **T1 現況已符合**：板栗 band1(現貨)只記 rep、band2(精品超額)才記 `Ev_Charity`；匿名散客中性不記 karma。**無需改 code**。往後客戶只要「達標 band 不掛 karma flag、超額 band 按陣營掛 Charity/Cruelty」即自動遵循。


使用者想法：karma 增減由「**是否接某客戶的單 ＋ 對該客戶的態度（是否超額滿足）**」決定：
- **正派客戶（村莊/城鎮）**：對他們好、超額滿足 → **善值＋**。
- **邪派客戶（盜匪/叛軍/軍閥/惡徒）**：為他們打造軍械 → **惡值＋**（賣兇器給壞人）。

- **與現有系統吻合**：F8 帳本已有 `Ev_Charity`(karma+10)、`Ev_Cruelty`(karma−8)、`Ev_Betray`(−12)、`Ev_Assassin`(−8)。只需在各客戶對話按其陣營善惡屬性掛對應 `Ev_*`。
- **已示範（good 側）**：板栗 band-2 精品交貨已設 `Ev_Charity`（超額滿足正派村莊→善值＋）。**evil 側**待 T2「赤爪傭兵團／武裝惡徒」等客戶登場（賣軍火給邪派→`Ev_Cruelty`）。
### 提案（2026-07-19 開發側草擬，供使用者拍板）——★幾乎純資料層、無需新 C#

核心洞見：**karma 方向＝客戶陣營善惡；karma 大小＝交付慷慨度**——而這**完全由現有 `ForgeDeliveryBand.evFlags` 承載**（每個 band 設哪些 `Ev_*`）。所以不必寫新機制，只是「按客戶陣營填對 evFlags＋加 karma 守衛節點」。

1. **客戶陣營三分**（設計時標，名冊 6a 加一欄 `alignment`）：
   - **正派 Good**：村莊/城鎮/王國正統採購（橡實村板栗、螢石塢可可、黑曜城懸鈴、金合歡商行、王國採購官映山紅、慕名散客正派版）。
   - **中性 Neutral**：賣命/逐利的灰色（赤爪傭兵團、遊商）。
   - **邪派 Evil**：軍閥/盜匪/黑市惡徒（T2+ 才登場：軍閥採購、黑市散客、髒活掮客）。
2. **karma 由 band evFlags 承載（資料驅動）**：
   | 客戶陣營 | 達標(band1) | 超額(band2+) |
   |---|---|---|
   | 正派 | 中性（公平買賣，可不加） | `Ev_Charity`（善＋）——為好人多做一分 |
   | 中性 | 中性 | 小善（可 `Ev_Charity`）或不加 |
   | 邪派 | **`Ev_Cruelty`（惡＋）**——光武裝惡徒就有道德代價 | **再 `Ev_Cruelty`（惡更＋）**——給越好兵器給壞人越糟 |
   → 板栗（正派村莊）band-2 已設 `Ev_Charity`＝此提案 good 側的活範例。
3. **「接單」vs「交貨」記 karma**：建議**交貨才記**（沒交貨＝沒真武裝他們，接了可反悔）。故 karma 全走交貨 band 的 evFlags，接單不記。
4. **下游回寫（接 §3 名聲回寫／F8，用現成 bool）**：帳本已回寫 `Karma_Good/Evil/NotEvil`。→ 正派客戶任務加守衛節點 `QuestNode_RequireGlobalBool(RatkinQL_Karma_NotEvil)`：**惡名在外時村莊/城鎮少來**；邪派/髒活客戶反之（低 karma 才出現，或機率隨惡值升）。這是「壞名聲招髒活、嚇跑好客」的閉環。
5. **需要的最小新東西**：只有①名冊加 alignment 欄（純文件）②各客戶 band 填對 evFlags（authoring）③正派/邪派任務加 Karma 守衛節點（XML）。**零新 C#**（守衛節點 `QuestNode_RequireGlobalBool`、evFlags、Karma bool 全現成）。

**給使用者的問題**：①三分陣營＋上表 karma 掛法可否？②達標即記惡（邪派 band1 就 `Ev_Cruelty`）還是只超額才記？③惡名回寫要不要現在就給正派客戶加 `Karma_NotEvil` 守衛（會讓高惡值玩家的村莊委託變少）？

## §6.8 交貨挑貨 UI：兩種都做（C 自製視窗 ＋ B 原生交易）【使用者定案＋相容評估】

使用者：交貨要能**讓玩家挑要交哪些物品**（現況對話自動扣最值錢的，玩家不能選）。「兩個都做」＝自製挑選視窗(C) ＋ 原生交易界面(B)；並要評估「原生交易易被其他 mod patch」的相容風險。

### 相容評估（原生交易 B）——查證 1.6 反編譯源
- **偵測點**：Harmony **postfix** 掛 `RimWorld.TradeDeal.TryExecute(out bool actuallyTraded)`（`TradeDeal.cs:150`，交易執行點）。`actuallyTraded` 為真時掃 `TradeSession.deal.tradeables`，取 `ActionToDo == TradeAction.PlayerSells` 且 `ThingDef.IsWeapon` 者＝玩家這次賣出的武器 → 回填當前委託判定。
- **風險＝低**：本 patch **唯讀觀察、不改交易行為/價格**。Harmony postfix **可疊加**（各 mod 對同方法的 postfix 都會跑），故：①交易 UI/排版 mod（多 patch `Dialog_Trade` 繪製）不影響我；②定價 mod（patch `Tradeable.GetPriceFor`/`StatWorker_MarketValue`）改的是銀數，**不影響我數件數/品質**的判定。
- **殘留風險**：①**完全取代 vanilla 交易流程**（繞過 `TradeDeal.TryExecute` 自寫交易）的 mod → 我的 postfix 不觸發、該 mod 下 B 失效（少見；多數交易 mod 只 patch 繪製/定價）。②訪客設為 trader 可能與 trader 生成 mod 互動（中）。
- **緩解**：**優雅退場**——B 偵測不到（postfix 沒觸發/沒賣武器）時委託維持開啟，玩家仍可用 C/對話交貨。B 永不硬壞，只退化成 C/A。
- **C 無交易 mod 相容風險**：完全自製 `Window`、不碰交易系統 → 零受 patch 影響。**故 C＝穩健主力、B＝熟悉 UI 備選（帶安全網）**。

### 實作方向
- **C（自製交貨挑選視窗）**：`Window` 子類，列玩家武器（品質/市值/勾選）＋**價值進度條**（標 1×/2×/3× band 刻度，讓 §6.6 分級後果一目了然）＋數量 min..max 提示；確認→消費勾選項→判定 band→結果。由對話「交貨」選項開啟（取代現有自動扣料）。複用 `WeaponValueUtil`。
- **B（原生交易交貨）**：訪客設 trader（買武器的 TraderKindDef）＋ Harmony postfix 記錄賣出武器＋交易後判定＋補委託 bonus（rep/band/獎勵）。**新增 Harmony 依賴**（本 mod 首次用）。帶退場安全網。
- **順序**：先 C（穩健主力、無新依賴、貼委託邏輯），再 B（熟悉 UI 備選）。

## §6.9 名匠傳奇武器：交出頂級武器→命名→出名→衍生任務【使用者定案，T3 未來工作】

使用者：**名匠(T3/頂層)** 委託可要求**最高/次高品質**武器＋**總值很高**。招牌機制——**交出去的那把武器會出名、影響後續任務**：
> 例：交一把超強鋼鐵長劍 → 任務結算：「客戶將其命名為『XX』，後續你或許會聽到它的傳說。」→ 之後衍生一些與這把武器相關的隨機任務。

- **機制拆解**：
  1. **頂級門檻交貨**：`DialogCondition_WeaponValue`（minQuality=Excellent/Masterwork、value 門檻極高）＝已有機制，調參即可。
  2. **命名＋登記**：交貨時把那把武器（或其複製）記進「傳奇武器名冊」（新 C#：`GameComponent` 存 List of {隨機武器名, defName, stuff, quality, 誕生日, 客戶}）。命名走鼠族武器命名風格（catalog 有青龍/朱雀等靈獸賦名傳統可借）。
  3. **衍生隨機任務**：說書人依名冊隨機挑一把已命名武器，生相關任務信（「聽聞你鑄的『XX』現於某處/某人手中/被劫/揚威沙場……」）——複用信件觸發地基。
- **與現有系統接點**：交貨→命名可在 C 挑選視窗/交貨 action 內做（記錄玩家勾選/交出的那把的 def/stuff/quality）；名冊持久化同 F8 帳本模式；衍生任務同信件觸發 QuestScript。
- **狀態**：T3 未來工作，先記。與 §6.6 分級後果（頂 band＝命名觸發）、名號階梯頂層「軍械名家」呼應。

## 拍板結果（2026-07-19，使用者定案）

1. **名號階梯＝四層**：`鐵匠 → 鐵匠鋪 → 名匠鋪 → 軍械名家/御用鐵鋪`（頂＝王庭認可，T3 王國大單做到頂才解）。v1 每層客戶文案用該層稱謂。
2. **科技藍圖＝可用道具**：空投一件藍圖、玩家自選時機用了才解研究；**解整層武器研究**（T1＝`RK_Research_Carpentry`+`RK_Research_SwordAndShield`）。
3. **匿名隨機客戶＝也信件化**（可靠；每變體寫任務信＋通用對話）。
4. **本輪範圍＝T1 全層一起送測**：具名（板栗）＋匿名散客＋科技藍圖＋名號＋彈性條件/分級後果 infra。

## 實作順序（本輪）

1. **C# 核心（compile 閘門先驗）**：
   - `CompUseEffect_UnlockResearch`（科技藍圖：使用→`Find.ResearchManager.FinishProject` 指定研究清單）。
   - `DialogCondition_WeaponValue`（count `IntRange` min 門檻/max 封頂、`minQuality`、value 門檻＝絕對或 refThing/refStuff/refQuality 基準式＋靜態快取、可選 weaponFilter）＋`CQFAction_ConsumeWeaponsByValue`（消費 min≤n≤max 達標且值最高者）。
   - 帳本 `Forge_T1/T2/T3TechGifted` bool（每層藍圖只送一次）。
2. **XML/Keyed**：T1 科技藍圖 ThingDef（CompUsable+CompUseEffect_UnlockResearch+CompUseEffectDestroySelf）＋板栗任務接受首單空投藍圖（首層 gate）＋板栗交貨加**分級後果**（1×/2×，弩+矛品質帶更好結局）＋匿名 T1 散客（信件+通用採購對話+值/品質彈性需求+隨機武器）＋名號文案。
3. **healthcheck + package + 送部署側 E2E**。

> 分級後果（§6.6）＋數量上下限（§6.5.1）的完整鋪陳主要在 T2+ 定製；T1 先把 infra 建好、板栗用「1×/2× 品質 band」小示範，匿名散客用「值+品質彈性需求」示範，驗過再於 T2 全開。
