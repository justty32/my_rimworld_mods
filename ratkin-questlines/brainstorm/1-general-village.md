# 腦力激盪 ① 通用村莊（鼠族日常聚落）

> 廣度第一遍（sonnet agent，2026-07-18）。defName 均在真實 Defs 上核對過。分類寬泛、待細化。
> 來源 mod：`fxz.ratkinfaction`(3036302713) + NewRatkinPlus 本體 `Solaris.RatkinRaceMod`(1578693166) + RKK/RKU/ZHP/OA 擴充。

## 12 條點子

### 1. 陳皮餅乾的老交情（輕插曲）
- 誰：`RK_Faction_Caravan`（隱藏商隊）的 `RatkinMerchant`，背景 `Ratkin_CaravanMerchant`
- 勾子：商隊帶硬餅乾與草莓啤酒路過以物易物；玩家可宰一筆或大方換長期折扣
- 內容：`RK_Food_Hardtack`、`RK_StrawberryBeer`/`RK_BeerBottle`、`RK_Recipe_Hardtack`
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`（分支+以物易物）；調性 日常溫馨

### 2. 講道日：搭起佈道台（完整線）
- 誰：`Rakinia` 派下 `RatkinPriest`/`RK_PawnKind_Priest`，背景 `Ratkin_Sister`/`Ratkin_HandMaiden`
- 勾子：牧師借空地辦巡迴佈道，請建造並看守 `RK_Pulpit` 三天，換全聚居點信仰心情加成
- 內容：`RK_Pulpit`、`RK_PriestPray`/`RK_PriestPrayMood`/`RK_AttendPrayerMeetingMood`、`RK_Culture_Virtuard`
- 機制：`QuestNode_DoCQFActions`+`CQFAction_SentSignal` 接力、原版 `BuildMonument_TimeProtect`；調性 溫情神聖

### 3. 田埂上的爭執（輕插曲）
- 誰：村民 `RatkinColonist`，背景 `Ratkin_Farmer` vs `Ratkin_Shepherd`
- 勾子：兩造為灌溉渠/羊群越界到門口評理，玩家當和事佬裁決
- 內容：背景 `Ratkin_Farmer`/`Ratkin_Shepherd`、`RK_Culture_Virtuard`
- 機制：`DialogTreeDef`（技能檢定分支）+`CQFAction_SetGlobalBool`（記公正/偏袒傾向）；調性 幽默生活感

### 4. 雜貨鋪的失竊清單（輕插曲）
- 誰：`ZHP_Faction` 店員 `ZHP_RatkinSalesclerk`
- 勾子：貨單被本地小偷摸走，請玩家抓贓或贖回
- 內容：`ZHP_RatkinSalesclerk`、`RatkinPettyThief`/`Ratkin_PettyTheft`、`ZHP_Faction`
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`+`CQFAction_ChangeGoodwillOfFaction`+`GenerateThingSet`→`DropPods`；調性 輕鬆懸疑

### 5. 深夜的口信：游擊隊借糧（完整線，2階段）
- 誰：`RKU_Faction` 的 `RKU_Scout` 夜訪
- 勾子：游擊隊小隊要通過需悄悄補給，幫或不幫——同族互助 vs 立場敏感
- 內容：`RKU_Scout`、`RKU_Faction`、`RKU_SharedBurdens`（同甘共苦 meme）
- 機制：`DialogTreeDef` 分支+`ChangeGoodwillOfFaction`+`SetGlobalBool("helped_underground")`+`SentSignal`（數日後回禮）；調性 懸疑道德抉擇

### 6. 騎士團的巡禮禮數（輕插曲）
- 誰：`RKK_KnightOrders` 的 `RKK_DragoonKnight`/`RKK_RangerOfTheClarionKnight`
- 勾子：騎士按傳統要求村莊盡待客之道（啤酒熱食過夜），鄉下手忙腳亂
- 內容：`RKK_DragoonKnight`、`RKK_RangerOfTheClarionKnight`、`RK_StrawberryBeer`、`RK_Food_Hardtack`
- 機制：`DialogTreeDef`（以物易物）+原版 lodger；調性 幽默（貴族禮數 vs 鄉下窘迫）

### 7. 孤兒的新家（完整線）
- 誰：孩童難民，背景 `Ratkin_Orphan`/`Ratkin_SlaveKid`；後續失散親人（背景 `Ratkin_Successor` 或 `Rakinia` 尋人使者）
- 勾子：孩子想留下，玩家決定收留；數週後親人上門相認，分支結局
- 內容：`Ratkin_Orphan`、`Ratkin_SlaveKid`、`Ratkin_Successor`、`Rakinia`
- 機制：原版 refugee/lodger 加入+`DoCQFActions`/`SentSignal` 多階段；調性 嚴肅溫情

### 8. 雪鼠商隊困在風雪裡（完整線，單一事件）
- 誰：`Rakinia_SnowRatkin` 旅商，異種型 `OAGene_SnowRatkin`
- 勾子：北方商隊困暴風雪急需禦寒物資熱食，限時救援
- 內容：`Rakinia_SnowRatkin`、`OAGene_SnowRatkin`（⚠需 `[OA]Gene Expand` 3300291918，可能需 Biotech；無則 fallback 純敘事）、`RK_Food_Hardtack`
- 機制：`SpecialPawnGenerateDef`+限時 `DoCQFActions`+`ChangeGoodwillOfFaction`；調性 緊張溫情

### 9. 鐵匠的秘方（輕插曲）
- 誰：流浪鐵匠，背景 `Ratkin_Blacksmith`
- 勾子：借鍛造台打裝備，事成傳授配方或以武器抵工錢
- 內容：`Ratkin_Blacksmith`、`RK_FueledSmithy`/`RK_ElectricSmithy`、`RK_Plate`、`RK_Weapon_Arbalest`
- 機制：`DialogTreeDef`（以物易物）+`GenerateThingSet`→`DropPods`；調性 匠人溫情

### 10. 滾輪壞掉的那一天（輕插曲）
- 誰：鄰村工程師，背景 `Ratkin_Engineer`/`Ratkin_ConstructionWorker`
- 勾子：鄰村 `RK_HamsterWheelGenerator`（倉鼠滾輪發電機）故障全村停電，急需零件人手
- 內容：`RK_HamsterWheelGenerator`、`Ratkin_Engineer`、`Ratkin_ConstructionWorker`
- 機制：`DialogTreeDef`+`GenerateThingSet`→`DropPods`；調性 幽默荒誕可愛

### 11. 戰地修女的巡診（完整線，短）
- 誰：`RatkinBattlefieldPriest`/背景 `Ratkin_BattlefieldSister`，來自 `Rakinia_Warlord` 治下但只為傷兵
- 勾子：不談政治只求捐藥品布料救遠方傷兵——軍閥治下也有想救人的鼠族
- 內容：`RatkinBattlefieldPriest`、`Ratkin_BattlefieldSister`、`Rakinia_Warlord`
- 機制：`DialogTreeDef`+`ChangeGoodwillOfFaction`+`SentSignal`（回禮）；調性 嚴肅溫情

### 12. 門口的醉漢（輕插曲）
- 誰：`RatkinVagabond`，背景 `Ratkin_Vagabond`
- 勾子：門口討硬餅乾/啤酒的說書人隨機小插曲，考驗聚居點人情味
- 內容：`Ratkin_Vagabond`、`RK_Food_Hardtack`、`RK_StrawberryBeer`
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`（極簡）；說書人隨機意外；調性 帶心酸的幽默

## 附註（跨切面重要發現）
- **背景故事池極豐富**：`1578693166` 下 `BackstoryDef_Childhood/Adulthood.xml` 有近 60 個鼠族專屬背景（Farmer/Chef/Librarian/Teacher/Doctor…），適合替訪客套職業背景增真實感，寫對話可引用其 `<title>` 當哏。
- **`RK_Culture_Virtuard`**（Rakinia 王國文化，農耕非侵略）＝「通用村莊」調性根源；反覆呼應「隱身人類視線外、以農耕為主的和平種族」原始設定。
- ⚠️ **隱藏派系無好感**：`RK_Faction_Pilgrims`/`RK_Faction_Caravan` 是 `hidden=true`，`CQFAction_ChangeGoodwillOfFaction` 對它們無效 → 好感獎勵改掛有實體好感的母版派系（`Rakinia`/`ZHP_Faction`/`RKU_Faction`），隱藏派系只借 pawnkind 當視覺/角色素材。
- ⚠️ **Gene Expand 相依**：`OAGene_SnowRatkin`/`RockRatkin`/`TravelRatkin` 來自 `[OA]Ratkin Gene Expand`(3300291918)，modlist 沒裝需 fallback。
