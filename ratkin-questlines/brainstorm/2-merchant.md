# 腦力激盪 ② 經營者（販賣：原材料／加工品／武器裝備）

> 廣度第一遍（sonnet agent，2026-07-18）。defName 均為 1.6 實際**啟用**內容（已排除 Weapons+ 裡被註解的死代碼）。分類寬泛、待細化。

## ★重要發現
- **`Rakinia_TravelRatkin`（旅鼠聯邦）確實存在**（來自 `[OA]Ratkin Gene Expand` 3300291918）——修正 ④旅店 稿「TravelRatkin 找不到」的判斷。
- ⚠️ **Weapons+ 大量死代碼**：`RK_BattleSuit`/`RK_Cannon`/`RK_RocketLauncher`/`RK_Binocular`/`RK_WhiteCoat`/`RK_Cheese`/`RK_Archoknife`/`RK_MagicWand`/整個 `RK_ResumedWeapon.xml` 都被註解未啟用，**勿用**。

## 原材料商線
### 1. 王國補給契約（完整線）
- 誰：`RK_TraderKind_KingdomBulkGoods`(원자재상)商隊，`RatkinMerchant`+`RatkinSoldier`/`RatkinDefender` 護衛
- 勾子：王國軍工廠鋼鐵告急，大額鋼/玻璃鋼/黃金分批交貨單，限期湊齊
- 內容：`RK_TraderKind_KingdomBulkGoods`、`RatkinMerchant`、`Rakinia`、`RK_StrawberryBeer`(謝禮)
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`(requiredThings 分批)、`SentSignal` 接力、`ChangeGoodwillOfFaction`、`GenerateThingSet`→`DropPods`；調性 官僚補給大單

### 2. 地鼠搬運隊：借道（完整線）
- 誰：`RKU_Faction` 走私運輸隊，`RKU_RadioShop`/`Caravan_RKU_Guerrilla`(庫存 Steel/Plasteel/Uranium/Gold/Jade)
- 勾子：借據點當走私中繼避開王國巡邏；接應/拒絕/舉報
- 內容：`RKU_Faction`、`RKU_RadioShop`、`Caravan_RKU_Guerrilla`、`Rakinia`
- 機制：`DialogTreeDef` 分支、`SetGlobalBool("RKU_smuggling_ally")`、`ChangeGoodwillOfFaction`；調性 灰色地帶政治張力

### 3. 草莓啤酒與硬餅乾的節慶訂單（輕插曲）
- 誰：`RatkinMerchant`/`ZHP_RatkinSalesclerk`；勾子：節慶/遠征臨時大量收購
- 內容：`RK_StrawberryBeer`、`RK_Food_Hardtack`、`ZHP_RatkinSalesclerk`、`ZHP_Faction`
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`(requiredThings)；調性 輕鬆日常

### 4. 布商的挑剔（輕插曲）
- 誰：`RK_TraderKind_KingdomTextileMerchant`(원단상)+`RatkinMerchant`；勾子：收購布料，挑品質壓價
- 內容：`RK_TraderKind_KingdomTextileMerchant`、`RK_Plate`
- 機制：`DialogTreeDef` 分支+requiredThings、`SetGlobalBool`(壓價成交)；調性 議價小插曲

## 加工品商線
### 5. 急救包 A／B 等級採購（完整線）
- 誰：`Rakinia` 軍需官/`RK_TraderKind_KingdomCombatSupplier`；勾子：急需甲級 `RK_HealingPack`，湊不齊可交乙級 `RK_HealingPackNotGood` 但砍價
- 內容：`RK_HealingPack`、`RK_HealingPackNotGood`、`Rakinia`、`RK_TraderKind_KingdomCombatSupplier`
- 機制：`DialogTreeDef`(品質分支 requiredThings)、`SentSignal`、`SetGlobalBool`(供貨誠信 bool 分級)、`ChangeGoodwillOfFaction`；調性 戰時後勤取捨

### 6. 邊緣雜貨鋪要開分店（完整線）
- 誰：`ZHP_Faction` 派 `ZHP_RatkinSalesclerk`；勾子：在據點設分店櫃位，換定期供原料，給貨架/終端機
- 內容：`ZHP_Faction`、`ZHP_RatkinSalesclerk`、`ZHP_ShopShelf_Exquisite`、`ZHP_ATM`(實體家具當開張獎勵—氣氛道具)
- 機制：`DialogTreeDef`、`SetGlobalBool("ZHP_partnership")`、`SentSignal`、`GenerateThingSet`→`DropPods`；調性 商業聯盟「據點真的在做生意」

### 7. 討債的鼠：瑕疵貨退款（輕插曲）
- 誰：憤怒 `RatkinNoble`/`RatkinMerchant` 債主+`RatkinMercenaryLight` 護衛；勾子：先前交付的貨被驗摻假/延誤，要退款或雙倍賠
- 內容：`RatkinNoble`、`RatkinMerchant`、`RatkinMercenaryLight`
- 機制：`DialogTreeDef`(社交/談判檢定+以貨抵債)、`ChangeGoodwillOfFaction`；調性「信譽會被追討」

### 8. 黑市「起司」（輕插曲）
- 誰：落魄 `RK_PawnKind_Pilgrim`(`RK_Faction_Pilgrims`)/流浪 `RatkinMerchant`；勾子：兜售「高級點心」其實是 `AC_RatkinCake`(饑荒鼠鼠蛋糕)，靠眼力判斷是否吃虧
- 內容：`AC_RatkinCake`、`RK_Faction_Pilgrims`、`RK_PawnKind_Pilgrim`
- 機制：`DialogTreeDef`(Trade/Perception 檢定)；調性 黑色幽默

## 武器裝備商線
### 9. 軍閥的軍火單（完整線）
- 誰：`Rakinia_Warlord` 派 `RK_TraderKind_KingdomCombatSupplier`；勾子：戰爭經濟開 `RK_AssaultRifle`/`RK_LMG`/`RK_ChargeRifle` 大單，價優但供應壓榨政權
- 內容：`Rakinia_Warlord`、`RK_TraderKind_KingdomCombatSupplier`、`RK_AssaultRifle`、`RK_LMG`、`RK_ChargeRifle`
- 機制：`DialogTreeDef` 三分支(供軍閥/王國/游擊隊)、`SentSignal`、`ChangeGoodwillOfFaction`(多陣營)、`SetGlobalBool("weapons_sold_to")`；調性 道德抉擇大單、三方政治具體化

### 10. 護送軍火商隊（完整線）
- 誰：`RK_TraderKind_KingdomForge`(Gene Expand)帶 `RK_KiteShield`/`RK_ShieldBelt`/`RK_MachineCrossBow`；勾子：運經危險帶求護送，途中伏擊
- 內容：`RK_TraderKind_KingdomForge`、`RK_KiteShield`、`RK_ShieldBelt`、`RK_MachineCrossBow`
- 機制：`DialogTreeDef`、SW_Camp site+原版 raid、`SentSignal`、`GenerateThingSet`→`DropPods`、`ChangeGoodwillOfFaction`；調性 亂世軍火商隊的危險

### 11. 騎士團的成年禮（輕插曲，可擴充）
- 誰：`RKK_KnightOrders` 使者；勾子：年輕騎士成年禮，委託籌備 `RKK_Apparel_DragonKnightArmor`+`RKK_Weapon_RangerBow`
- 內容：`RKK_KnightOrders`、`RKK_Apparel_DragonKnightArmor`、`RKK_Weapon_RangerBow`（⚠️來自 Ratkin Knights+ `RKK.RatKnights.Core`，不掛此擴充則換 `RK_TwoBladed`/`RK_Flail`）
- 機制：`DialogTreeDef`+requiredThings、`ChangeGoodwillOfFaction`；調性 溫馨儀式委製

### 12. 獨家經銷協議（完整線＋跟進插曲）
- 誰：`Rakinia_TravelRatkin`(旅鼠聯邦)商隊，`RK_TraderKind_KingdomWayfarer`/`RK_TraderKind_KingdomHuntpack`
- 勾子：獨家經銷換穩定折扣；後續競爭對手上門遊說毀約，守約 vs 撕毀
- 內容：`Rakinia_TravelRatkin`、`RK_TraderKind_KingdomWayfarer`/`KingdomHuntpack`、`RK_Rapier`/`RK_TwoBladed`/`RK_Flail`
- 機制：`DialogTreeDef`、`SetGlobalBool("exclusive_travelratkin")`(驅動後續插曲)、`SentSignal`、`ChangeGoodwillOfFaction`；調性 長線經銷關係經營

## defName 來源對照
| 分類 | workshop id | packageId |
|---|---|---|
| 種族/王國基礎(`Rakinia`/`RatkinMerchant`/`RatkinNoble`/`RK_TraderKind_Kingdom*`/`RK_Plate`) | 1578693166 | `Solaris.RatkinRaceMod`(NewRatkinPlus) |
| Faction+(`Rakinia_Warlord`/`AC_RatkinCake`) | 3036302713 | `fxz.ratkinfaction` |
| Weapons+(`RK_AssaultRifle`/`RK_LMG`/`RK_ChargeRifle`/`RK_MachineCrossBow`/`RK_KiteShield`/`RK_ShieldBelt`/`RK_HealingPack`/`RK_HealingPackNotGood`/`RK_Rapier`/`RK_TwoBladed`/`RK_Flail`) | 2779404660 | `bbb.ratkinweapon.morefailure` |
| Misc+/雜貨鋪(`ZHP_Faction`/`ZHP_RatkinSalesclerk`/`ZHP_ShopShelf_*`/`ZHP_ATM`) | 3452928337 | `W.ZHP` |
| Underground+(`RKU_Faction`/`RKU_RadioShop`/`Caravan_RKU_Guerrilla`) | 3613814532 | `RKU.RatkinUnderground` |
| Gene Expand(`Rakinia_TravelRatkin`/`RK_TraderKind_KingdomForge`/`Wayfarer`/`Huntpack`) | 3300291918 | `[OA]Ratkin Gene Expand` |
| Knights+(`RKK_KnightOrders`/`RKK_Apparel_DragonKnightArmor`/`RKK_Weapon_RangerBow`) | 3394862242 | `RKK.RatKnights.Core` |
