# 腦力激盪 ④ 旅店（餐廳＋住宿）

> 廣度第一遍（sonnet agent，2026-07-18）。defName 均在真實 Defs 上核對，未核對到者標「(需查證 defName)」。分類寬泛、待細化。
> 根：Ratkin Faction+ 只是 Warlord 擴充，內容主幹在硬依賴 **NewRatkinPlus**（`Solaris.RatkinRaceMod` 1578693166）。

## ★跨切面重要發現
- **本體自帶「旅人加入殖民地」條件系統**：`NewRatkin.PawnKindDefExtension_WanderingCaravanJoin`（掛在 `RK_PawnKind_Nomad`/`RK_PawnKind_Wanderer`），加入條件本身就是敘事鉤子：`JoinCondition_ColonyWealth`（財富達標）、`JoinCondition_InjuredPatientCount`（照顧過的傷患數）。→ 既是旅店線核心機制，也與 §2 身份系統的財富/照護門檻直接相關，可複用。
- `RK_StrawberryBeer` 掛原版醉酒鏈（`AlcoholHigh`/`Hangover`/`AlcoholAddiction`），且設計上就是「拿來砸人的啤酒瓶」（`RK_BeerBottleStrike` 近戰招式＋拋擲彈道）→ 酒館鬧事現成素材。
- ⚠️ `TravelRatkin` 派系在此環境**未搜到**，判斷誤記或未安裝 → 改用 `RK_Faction_Caravan`（`RK_PawnKind_Nomad`/`RK_PawnKind_Wanderer`）頂替「旅鼠」。

## 10 條點子
### 1. 空位上的旅人（完整線）
- 誰：`RK_PawnKind_Nomad`/`RK_PawnKind_Wanderer`（隱藏派系 `RK_Faction_Caravan`）；勾子：投宿觀察你待客，最後鼓勇氣問能否留下
- 內容：上述 `WanderingCaravanJoin` 加入系統、`RK_Food_Hardtack`(旅途乾糧)
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`、lodger、`SetGlobalBool`(記待客好壞)、`GenerateThingSet`→`DropPods`；調性 溫情

### 2. 同一屋簷下的兩個信（完整線）
- 誰：`RatkinCombatantWarlord`(軍閥,`naturalEnemy`) 與 `RKU_Scout`(游擊隊) 同夜各自投宿
- 勾子：兩人互不知情，玩家決定排房/是否洩漏行蹤；挑撥→深夜衝突，調停→平安離開
- 內容：`Rakinia_Warlord`(壓迫立國、鼠民年年逃亡)、`RKU_Faction`/`RKU_Commissar`
- 機制：`SpecialPawnGenerateDef`×2、`DialogTreeDef`(social 檢定)、`SetGlobalBool`、`QuestNode_Raid`(小規模僅兩人)、`ChangeGoodwillOfFaction`；調性 懸疑

### 3. 聖罗勒叶騎士的一夜借宿（輕插曲）
- 誰：`RKK_KnightOrders` 成員（`RKK_OcimumSanctem`/`RKK_BloodKnights`；具體騎士 pawnkind 需查證如 `RKK_KnightOfTheOcimum`）
- 勾子：巡遊借宿，要安靜房與不摻肉簡餐，回禮傳授劍術
- 內容：RKK 各具名騎士團、贖罪/榮譽調性
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`(requiredThings 付費)、`GenerateThingSet`；調性 莊重溫情

### 4. 邊緣雜貨鋪的推銷員（輕插曲）
- 誰：`ZHP_Faction` 店員/商旅(pawnkind 需查證)；勾子：借大廳擺一晚地攤賣奇怪小東西順便打聽情報
- 內容：`ZHP_Faction`(「奇怪小東西」「與店員互動觸發特殊事件」)
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`(requiredThings)、`DoCQFActions`+`SentSignal`；調性 幽默

### 5. 草莓啤酒與拳頭（輕插曲）
- 誰：非戰鬥客(`RatkinMerchant`/`RatkinVagabond`)喝多 `RK_StrawberryBeer`；勾子：醉客口角，出面平息或看戲
- 內容：`RK_StrawberryBeer`(低度傳統酒)、`RK_BeerBottleStrike`(砸瓶招式)、醉酒鏈
- 機制：`DialogTreeDef`(social 檢定)、`DoCQFActions`(倒地小事件非死亡)；調性 幽默

### 6. 手腳不乾淨的房客（輕插曲）
- 誰：`RatkinPettyThief`(裝束 `RK_Sack`+`RK_Cardigan`+`RK_Muffler`+`RK_WoolenHat`)；勾子：順走小東西被抓包，處理決定去留（他只是餓怕了）
- 內容：`RatkinPettyThief`/`RatkinThief` 背景，寒酸低質裝束
- 機制：`DoCQFActions`+`SentSignal`(失竊事件)、`DialogTreeDef` 三分支、`SetGlobalBool`；調性 幽默略心酸

### 7. 贖罪的貴族（輕插曲）
- 誰：`RK_PawnKind_NoblePilgrim`(`RK_Faction_Pilgrims`/實 `Rakinia`)：衣衫襤褸(itemQuality Awful)但暗藏高階植入(techHediffs 500~1000)
- 勾子：自稱普通朝聖者，身分是否被拆穿看玩家有沒有留意細節
- 內容：`RK_PawnKind_NoblePilgrim`(專為「貴族贖罪故意穿寒酸」設計)、`RK_Backstory_Noble`
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`(social/medicine 檢定)、`SetGlobalBool`、`GenerateThingSet`；調性 感傷

### 8. 忌口的信眾（輕插曲）
- 誰：`RatkinPriest`(`Rakinia`)/`RK_PawnKind_Priest`(`RK_Faction_Pilgrims`)，`RK_SistersVeil` 面紗；勾子：借大廳辦小型祈禱聚會為房客祝禱
- 內容：`RK_PrayerService`(祭司祝福能力)、`RK_PriestPray`/`RK_PriestPrayMood`/`RK_AttendPrayerMeetingMood`(既有心情鏈，直接套)
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`、`DoCQFActions`(觸發原版能力/心情)；調性 溫情

### 9. 來路不明的劍客（輕插曲，可留伏筆）
- 誰：`RatkinVagabond`(官方註「鼠族版黑衣人」)，帶 `MedicineHerbal`(剛受傷暗示)；勾子：獨行客只租一晚，追問身世語帶保留
- 內容：`RatkinVagabond`(謎樣獨行客定位)
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef` 多分支、`SetGlobalBool`(記錄揭露的身世版本供後續呼應)；調性 懸疑

### 10. 寧靜夜裡的通緝令（完整線，可與 #2/#9 串「軍閥壓迫」主題）
- 誰：`RatkinMurderer`(高階植入 techHediffs 4000~5000，戰力偏高)偽裝旅客；勾子：遭 `Rakinia_Warlord` 通緝的要犯，隔天追兵盤查，窩藏與否
- 內容：`RatkinMurderer`(`aiAvoidCover` 亡命之徒)、`Rakinia_Warlord`(國內鎮壓、鼠民逃亡的世界觀貼合)
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`、`SetGlobalBool`、`ChangeGoodwillOfFaction`、`DoCQFActions`(追兵盤查)；調性 懸疑

## 調味細節素材（非獨立任務）
- `AC_RatkinCake`(鼠族糕，黑色幽默飢荒暗示「可能用誰家孩子的肉」)→ 旅店「來歷不明特餐」，客人吃完問這是什麼肉，純調味。
- `RK_Food_Hardtack`→ 送行乾糧小動作，任何投宿線結尾可用。
- `RK_Ability_MeleeMoraleBooster`(旗手 `RK_PawnKind_StandardBearer` 士氣技)→「旅店請來的助興表演者」引子（非戰鬥情境能否觸發需查證，否則僅台詞引用）。

## 需查證 defName
ZHP 店員/商旅 pawnkind、RKK 各騎士團旗下騎士確切 1.6 defName、Warlord「信使」pawnkind。
