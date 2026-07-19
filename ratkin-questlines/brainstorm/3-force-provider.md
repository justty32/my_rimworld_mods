# 腦力激盪 ③ 武力提供者（傭兵／騎士團／刺客）

> 廣度第一遍（sonnet agent，2026-07-18）。defName 均在真實 Defs 上核對。分類寬泛、待細化。
> 來源：`fxz.ratkinfaction`、Ratkin Weapons+、Ratkin Knights+、Ratkin Underground+、Ratkin Misc+/ZHP、NewRatkinPlus。

## 派系/兵種調查摘要
| 派系 | 性質 | 戰鬥 pawnkind |
|---|---|---|
| `Rakinia` | 王國，農耕非侵略 | `RatkinKnight`、`RatkinKnightCommander`、`RatkinEliteDefender` |
| `Rakinia_Warlord` | 軍閥，`naturalEnemy=true`，戰爭經濟 | `RatkinCombatantWarlord`、`RatkinDefenderWarlord`、`RatkinBattlefieldPriest`、`RatkinWonderSoldier`(堑壕突击兵)、`RatkinInvader` |
| `RKU_Faction` | 十年前被王國擊敗的革命軍殘部，鑽地游擊隊 | `RKU_Commissar`/`RKU_EliteCommissar`、`RKU_Scout`、`RKU_Invader`、`RKU_Miner` |
| `RKK_KnightOrders` | `raidsForbidden=true`、純劇本隱形派系，掛 7 騎士團 | 見下 |
| `ZHP_Faction` | 軌道貿易/雜貨鋪，`permanentFriendlyToPlayer=true` | `ZHP_RatkinSalesclerk`、`ZHP_TraumaTeam_Rifle`(賽博創傷小組) |

**七騎士團**（`RKK_KnightOrderDef`）：`RKK_NormalKnightOrder`、`RKK_SaberKnightOrder`(圣刃/Dragon Knight，招募需100榮譽+Avalon封印之力)、`RKK_ConstitutionalKnights`(冠军侯，Holy Shield)、`RKK_BloodKnights`(赎罪之血，Spinning Slash 吸血斬)、`RKK_ChampionKnights`(宪律铁骑，Psychic Support)、`RKK_RangerOfTheClarion`(号角游侠，Arrow Rain)、`RKK_OcimumSanctem`(圣罗勒叶)。另 `RKK_CursedExileKnight`(寻迹罪骑，Cursed Alchemy Fog 毒霧——被放逐追獵的罪騎，鉤子極好)。招募經濟＝榮譽點（40招一般/100招龍騎；`RKK_KnightRefuseArrest` 扣好感）。**內建五階試煉**：`RKK_TrialFirst`…`RKK_TrialFinal`。
底層/黑市：`RatkinPettyThief`、`RatkinMurderer`、`RatkinVagabond`、`RatkinMercenaryLight`、`RatkinMercenary`、`RatkinDemonMan`(爆破兵)。
刺客武器（Weapons+）：`RK_AssassinKnife`/`RK_AssassinKnifes`、`RK_Poison`(hediff)。騎士武器：`RK_Rapier`、`RK_TwoBladed`、`RK_Flail`、`RK_KiteShield`、`RK_Bible`。重火力：`RK_AssaultRifle`、`RK_LMG`、`RK_ChargeRifle`、`RK_Cannon` 等。

## 傭兵路線
### 1. 軍閥的戰爭債券（完整線）
- 誰：`Rakinia_Warlord` 募兵官（`RatkinCombatantWarlord`）；勾子：按人頭算錢，圍剿東線 `RKU_Faction` 鑽地叛軍；可中途倒戈援游擊隊走另一分支
- 內容：`Rakinia_Warlord`、`RKU_Faction`（弱勢殘部）、`RatkinCombatantWarlord`、`RKU_Scout`/`RKU_Invader`
- 機制：`DialogTreeDef`+`SpecialPawnGenerateDef`；`DoCQFActions`+`SentSignal` 接力；`GenerateThingSet`→`DropPods`；`ChangeGoodwillOfFaction` 雙向；調性 冷硬道德掙扎（幫壓迫者打弱者）

### 2. 穿越軍閥國境線（完整線）
- 誰：`Rakinia`/`RK_Faction_Caravan` 或 `ZHP_Faction`；勾子：護商隊穿越 Warlord 邊境
- 內容：`RK_Faction_Caravan`(hidden)、`RatkinInvader`、`RatkinWonderSoldier`
- 機制：護送用原版 defend；`QuestNode_Raid` 收信號 fire；⚠️「劫匪只打商隊」需額外機制；調性 熱血公路

### 3. 雜貨鋪的邊防外包（完整線）
- 誰：`ZHP_Faction`（`ZHP_RatkinSalesclerk`）；勾子：軌道站點被 Warlord 盯上，蹲點防守
- 內容：`ZHP_Faction`、`RatkinDemonMan`(爆破攻城變數)
- 機制：`QuestNode_Raid`+`QuestNode_Delay`；`GenerateThingSet`→`DropPods`；調性 黑色幽默+防守戰

### 4. 創傷小組的加班費（輕插曲）
- 誰：`ZHP_TraumaTeam_Rifle`(PowerArmor 賽博創傷小組)；勾子：借人手撐十分鐘、他們負責醫療，短防守即撤
- 內容：`ZHP_TraumaTeam_Rifle`（Cyberpunk Trauma Team 山寨梗）
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`(技能檢定)；短 defend；`DropPods`；調性 黑色幽默輕插曲

## 騎士團路線
### 5. 贖罪之血騎士的逃亡（完整線）
- 誰：逃亡 `RKK_BloodKnight`，母團追獵隊隨後；勾子：窩藏 vs 交人，分支硬仗
- 內容：`RKK_BloodKnights`(Spinning Slash 吸血贖罪)、`RKK_KnightOrders`
- 機制：`SpecialPawnGenerateDef`（RKK 自有 Class 能否直接生成需實測，否則 `RatkinEliteDefender` 貼皮改名）；`SentSignal` 觸發追獵；`QuestNode_Raid`；`ChangeGoodwillOfFaction`；調性 道德掙扎悲劇

### 6. 號角游俠團的護送（完整線）
- 誰：`RKK_RangerOfTheClarion` 游俠+`RKK_RangerOfTheClarionSentry`；勾子：護送聖物穿越危險帶
- 內容：`RKK_RangerOfTheClarionKnight`/Sentry(Arrow Rain 弓系)
- 機制：原版 defend；`QuestNode_Raid`(可設 Warlord 劫聖物，政治張力)；`GenerateThingSet`；調性 熱血冒險

### 7. 冠軍侯的榮譽比武（輕插曲）
- 誰：`RKK_ConstitutionalKnight`(冠军侯，Holy Shield)；勾子：不動武器徒手/近戰較高下，輸贏都算榮譽
- 內容：`RKK_ConstitutionalKnight`、`RKK_ConstitutionalKnights`(招募需40榮譽彩蛋)
- 機制：`SpecialPawnGenerateDef`+`DialogTreeDef`(技能檢定)；真1v1判定需額外機制，骰值比較則純XML；調性 熱血榮譽

### 8. 龍騎士的試煉（完整線，大型）
- 誰：`RKK_SaberKnightOrder` 使者，帶 Avalon 封印之力認可；勾子：改編官方五階試煉成五段委託鏈，通關獲 `RKK_DragoonKnight` 級盟友
- 內容：`RKK_SaberKnightOrder`/`RKK_DragoonKnight`(Transform 變身)、五階命名取自 `RKK_Translations.xml`：`RKK_TrialFirst`(Prelude of the Forest Horn)…`RKK_TrialFinal`(Vow of the Eternal Moon)
- 機制：`DoCQFActions`+`SentSignal` 五階接力；混用 `QuestNode_Raid`(防守)、`QuestNode_AddTag`+`QuestNode_End inSignal`(護送/擊殺)；終局 `ChangeGoodwillOfFaction` 大加成；調性 史詩儀式

## 刺客路線
### 9. 暗巷裡的軍閥密探（完整線）
- 誰：黑市線人(`RatkinVagabond`/`RatkinMercenaryLight`)；勾子：暗殺藏身 Warlord 陣營的情報販子（複用 SW_Camp）
- 內容：`RK_AssassinKnife`/`RK_Poison`(接單需持有增代入感)、`Rakinia_Warlord`
- 機制：SW_Camp 暗殺模板（`victim.Killed`）；`ChangeGoodwillOfFaction`(不曝光不扣)；調性 冷硬暗面

### 10. 雜貨鋪的特殊服務（完整線）
- 誰：`ZHP_Faction`「特殊服務」窗口（`ZHP_RatkinSalesclerk` 開場，暗示鸢萝主使）；勾子：以「特殊服務」包裝清除威脅生意的對象
- 內容：`ZHP_Faction`（原文「提供特殊服務」的曖昧鉤子）
- 機制：`DialogTreeDef` 多輪試探分支；`SetGlobalBool`(與ZHP暗面往來旗標)；`DropPods`；調性 黑色幽默包裝的冷硬

### 11. 告密者（輕插曲）
- 誰：`RKU_Commissar`(游擊隊政委)；勾子：清除通敵告密者，對象可能是被逼無奈的小人物
- 內容：`RKU_Faction`(內鬥張力，告密者或為脫隊舊同志)、`RKU_Commissar`
- 機制：`DialogTreeDef`(社交/搜查揭動機分支)；SW_Camp 模板；`ChangeGoodwillOfFaction`；調性 沉重道德掙扎

### 12. 血債的重量（完整線，系統性收束）
- 誰：無固定訪客——靠 `SetGlobalBool` 累積的「殺手聲望」系統本身
- 勾子：接數個暗殺(9/10/11)後旗標達閾值，觸發 `Rakinia`（非侵略王國）使者警告/斷交，或 `RKK_KnightOrders` 因榮譽有損拒絕往來
- 內容：`Rakinia`(非侵略定位 vs 刺客血腥的張力)、`RKK_KnightOrders`(榮譽經濟 `RKK_KnightRefuseArrest` 邏輯挪用)
- 機制：`SetGlobalBool` 累積；`QuestNode_Signal` 收閾值信號；`ChangeGoodwillOfFaction` 結算；調性「你的據點是不是壞人」總結算

## 需額外機制清單
- 「劫匪/追獵隊只鎖定特定NPC」（#2、#5）→ C#-only，暫退化打全部，或自訂 DutyDef
- 「1v1決鬥真判定」（#7）→ 需額外機制；骰值比較純XML可
- RKK 自有 Class pawnkind 能否被 `SpecialPawnGenerateDef` 直接生成需實測；耦合過深則同數值代打+改名（`RatkinEliteDefender` 貼皮）

## 關鍵檔案位置
- `294100/3036302713/1.6/Defs/FactionDefs/Factions_Warlord.xml`（Rakinia_Warlord）
- `294100/1578693166/1.6/Defs/FactionDefs/Factions_Misc.xml`（Rakinia）
- `294100/3613814532/Defs/FactionDefs/Factions_Undergrounds.xml`（RKU_Faction）
- `294100/3394862242/1.6/Defs/KnightOrderDef/KnightOrderDefs.xml`（RKK 七騎士團）
- `294100/3452928337/1.6/Defs/FactionDefs/FactionDefs.xml`（ZHP_Faction）
- `294100/2779404660/1.6/Defs/ThingsDefs/RK_ResumedWeapon.xml`（RK_AssassinKnife/RK_Poison）
