# 腦力激盪 ⑤ 學校（托嬰／訓練／傳承）

> 廣度第一遍（sonnet agent，2026-07-18）。defName 均在真實 Defs 核對。分類寬泛、待細化。

## ★重要發現
- **RimTalk Toddlers 相容鏈**（`cyanobot.toddlers`+`cj.rimtalk.toddlers`，workshop 3659064387，⚠️需確認在 2-anime）**已內建大量「鼠族托嬰」素材**：
  - 心情：`RimTalk_ManyRatkinBabies`(+3)、`RimTalk_LiveInRatkinNursery`(+5) → 遊戲本來就承認「鼠族托嬰所」概念
  - 家具(可當 requiredThings)：`RimTalk_ToyBlockPile`/`RimTalk_ToyRockingHorse`/`RimTalk_ToyPuzzleTable`
  - 鼠族幼崽專屬動畫：`RimTalk_ToddlerSelfPlay_Ratkin_PlaywithOwnEar`/`PlaywithOwnTail`、`RimTalk_ToddlerObserveAdultWork`
  - 校外活動：`RimTalk_ChildrenOuting`+`RimTalk_AttendedChildrenOuting`(+8)/`RimTalk_OrganizedChildrenOuting`(+10)
  - 語言成長：`RimTalk_ToddlerLanguageLearning`(需 Biotech)、`RimTalk_BabyBabbling`；哭鬧 `RimTalk_WantToBeHeld`(MentalState_BabyCry)
- ⚠️ **無 race=Ratkin 的 0~3 歲學步兒 pawnkind**：`RimTalkToddler_Toddler`/`RimTalkToddler_Child` 是 race=Human 通用模板。要生成純鼠族嬰兒需照模板自建 race=Ratkin 版（需額外機制）。**3 歲以上**可用 `RKU_Miner`(9+)或原版 lodger/refugee 池，不受此限。
- **豐富童年背景**：`Ratkin_Orphan`、`Ratkin_Student`、`Ratkin_GuardenerStudent`、`Ratkin_ChefStudent`、`Ratkin_LittleLibrarian`、`Ratkin_ServentChild`、`Ratkin_SlaveKid`、`Ratkin_PoorChild`、`Ratkin_KidnapVictim`(強制50% Psychopath)、`Ratkin_Punk`；Underground+ 專屬：`Ratkin_GuerrillaCJ`(瘟疫孤兒被游擊隊收養)、`Ratkin_GuerrillaCT`(學徒機械師)、`Ratkin_GuerrillaCP`(小礦工，對應 `RKU_Miner`)。

## 11 條點子
### 1. 搖籃裡的瘟疫孤兒（完整線）
- 誰：`RKU_Faction` 使者帶 `Ratkin_GuerrillaCJ`(瘟疫孤兒)幼崽；勾子：地下鼠瘟爆發請學校暫代托孤
- 機制：lodger 收養、requiredThings 檢查玩具家具(沒有→對話拉低好感)、`SentSignal`(數十天後回訪)、`SetGlobalBool`(收養vs交還)；`RimTalk_ManyRatkinBabies` 自然觸發；調性 溫情託付

### 2. 滿窩的小訪客（輕插曲）
- 誰：`Rakinia` 難民代表；勾子：襲擊迫疏散，數名幼崽暫托數日
- 機制：多重 lodger、`RimTalk_ManyRatkinBabies`(+3)/`LiveInRatkinNursery`(+5)、`GenerateThingSet`→`DropPods`；調性 溫馨熱鬧

### 3. 雜貨鋪的算盤學徒（完整線）
- 誰：`ZHP_Faction` 送 `ZHP_RatkinSalesclerk` 背景(13~24)少年見習；勾子：學做生意學成回鋪頂班
- 機制：`DialogTreeDef`(Social/交易檢定)、`SentSignal`(畢業考)、`SetGlobalBool`、`ChangeGoodwillOfFaction`；分支 回鋪 vs 留下；調性 溫馨成長

### 4. 聖刃騎士團的見習生（完整線）
- 誰：`RKK_SaberKnightOrder` 導師+見習騎士；勾子：借據點磨練非戰鬥歷練(醫療/農務/待人)，完訓授勳
- 機制：lodger、觀察日對話(可包 `RimTalk_ChildrenOuting` 敘事)、`QuestNode_Raid`(延後,測臨場)、`ChangeGoodwillOfFaction`；調性 溫情帶張力

### 5. 礦坑裡長大的孩子（完整線）
- 誰：`RKU_Faction` 的 `RKU_Miner`(`Ratkin_GuerrillaCP` 小礦工)；勾子：指揮官私下拜託「教他點別的，他還小」
- 機制：多階段培訓延時信號、`SetGlobalBool`；分支 轉行留下 vs 帶技能回礦坑(諷刺寫實)；調性 感傷寫實

### 6. 圖書館員之子（完整線）
- 誰：`Rakinia` 使者帶 `Ratkin_LittleLibrarian` 遺孤；勾子：圖書館員過世，王國望延續其識字教養
- 機制：lodger 收養、`SentSignal`(閱讀里程碑)、`SetGlobalBool`；分支 留下當學者 vs 回王國圖書館；調性 溫情知識傳承

### 7. 被鼠商人買走的孩子（完整線，黑暗支線）
- 誰：不明商隊/`Rakinia_Warlord` 側商人送 `Ratkin_KidnapVictim`(強制50% Psychopath)；勾子：商人說「調教賣相好」，發現是拐賣受害者
- 機制：`DialogTreeDef`(Social/Medicine 看穿創傷)、`SetGlobalBool`、`QuestNode_Raid`(選保護→追討支線)；分支 配合出貨(黑暗) vs 收留尋親；調性 張力道德抉擇

### 8. 半夜哭鬧的小訪客（輕插曲）
- 誰：任一寄養幼崽(承接收養線)；勾子：`RimTalk_WantToBeHeld` 半夜哭鬧考驗照顧者
- 機制：`DialogTreeDef`(Social/Medicine 安撫檢定)；調性 溫馨幽默

### 9. 校外教學委託（輕插曲）
- 誰：`Rakinia`/`ZHP_Faction`；勾子：讓孩子出去見世面，委託組織出遊
- 機制：⚠️主動觸發 `RimTalk_ChildrenOuting` GatheringDef 需額外機制/需查證，否則延時信號+敘事模擬；`RimTalk_OrganizedChildrenOuting`(+10)、`GenerateThingSet`→`DropPods`；調性 溫馨

### 10. 薪火相傳的老師傅（完整線，旗艦級招牌）
- 誰：商隊/`Rakinia_Warlord` 送 `Ratkin_GuardenerStudent`(苦修見習)少年；勾子：據點「老師傅」收徒，見證見習→出師完整成長弧
- 機制：多階段培訓延時信號(敘事描述非數值)、`QuestNode_Raid`(危機考驗獨當一面)、`SetGlobalBool`、`ChangeGoodwillOfFaction`；三分支結局(接衣缽/自立/回軍閥)；調性 溫情完整成長弧

### 11. 世襲僕役之子的自由路（完整線）
- 誰：不明貴族/商隊代理人送 `Ratkin_ServentChild`(逃離世襲命運)少年；勾子：偷逃出來拜師學藝，主人隨後上門討人
- 機制：難民收留、短期培訓、`DialogTreeDef`(主人對峙:贖金vs拒還)、`ChangeGoodwillOfFaction`、`SetGlobalBool`；調性 張力抉擇後溫情
