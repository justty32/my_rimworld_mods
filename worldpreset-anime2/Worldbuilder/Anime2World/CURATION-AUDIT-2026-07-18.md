# 敘事策展稽核 — 二次元定製世界 ideo × 各族 lore（2026-07-18）

> 配 [`CULTURE-PLAN.md`](CULTURE-PLAN.md)。逐派系核對「凍結的信仰／meme」是否貼合該族的 worldview，
> 依據 `~/repo/moddings/rimworld/analysis/narrative-tone/`（`_cross-race-synthesis.md` 全庫綜述 ＋
> `ratkin.md`／`ratkin-submods-partial.md` 深檔）。
>
> 稽核＋修正並行：SnowRatkin／Guild 兩檔已離線捏好（見下），其餘僅提案。**未碰 live、未重組、未跑測。** 修法見末節「機制注意」。

## 結論一句話

27 份指派**多數貼合，有 2 個接錯神、4 個口味偏差**。兩個旗標項（SnowRatkin／Guild）不是「奇特」
而是**RNG 亂滾到『超凡智能（archotech AI）崇拜』**——與該派系 lore 正面衝突。

> ✅ **2026-07-18 已離線捏好兩個信仰**：SnowRatkin→「**风雪之神**」、Guild→「**冒险者盟约**」（世俗）。
> 手法＝**改皮風味字串＋（Guild）乾淨換 structure meme**，兩檔皆 0 殘留、XML 良構。詳見下方各節。
> **真正驗證＝新開局選「二次元定製世界」進遊戲看**（worldpreset ideo headless 測不到）。
>
> ✅ **2026-08-27：四個口味小偏也全部處理完了**，見末節「四個 ⚠️ 小偏」的處理結果表。
> 全 27 檔重驗：XML 良構 27/27、meme defName 全數可解析、`Precept_<ID>` 反向參照 0 斷。

## 範圍界定：哪些派系綁族 lore、哪些是原版原型

| 類別 | 派系 | 稽核依據 |
|---|---|---|
| **二次元族**（有 narrative-tone 深檔，會被 lore 打臉）| Milira×2、Kiiro、Moelotl、DMS×2、Ratkin×8 | 逐族對照 |
| 人類企業／原版原型（無族 lore，看原型自洽即可）| USAC、帝國、邊民×2、太空商會、部落、海盜×3、食人×2、劫掠、LucifersCartel、Ancients | 原型自洽，多已 coherent |
| **本輪不在範圍** | dragonian／nivarian／maru／monolyn／mugirl／vivi／wolfein／yuran | narrative-tone 有分析，但**這包沒有它們的派系** |

## 逐派系判定

| .rid | 派系 | 現行信仰 / memes | 判定 | 依據 |
|---|---|---|---|---|
| `Milira` | Milira_Faction | 新生-正义 / Ideological,Loyalist,Collectivist | ✅ 貼合 | Milira 主流＝正義/新生，把「背叛的教會」對比出來，正好 |
| `MiliraChurch` | Milira_AngelismChurch | 超凡科技之路 / Archist,MaleSupremacy,Loyalist,Collectivist | ✅ 貼合（反派向）| lore 核心＝5486 教會背叛；教會作為 Archist/至上主義反派**是刻意的**，MaleSupremacy 當墮落記號可留 |
| `Kiiro` | Kiiro_Faction | 文化-肉体派 / Ideological,Individualist,FleshPurity | ⚠️ 可調 | Kiiro＝田園治癒＋**無妄之災**，非肉体派主題。FleshPurity 只沾到「自然純樸」邊。CULTURE-PLAN 自己也標「想換純真味可調」|
| `Moelotl` | AxolotlWanderingDynasty | 红船 礼义 / Ideological,FemaleSupremacy,Loyalist,Collectivist,FleshPurity | ✅✅ 極貼合 | 女尊萌系**王朝**＋制度性＋仙俠礼义；FemaleSupremacy/Loyalist/Collectivist 全中 |
| `StellarCorp` | USAC_Faction | 上帝 / Christian,Supremacist,Individualist,Proselytizer,HumanPrimacy | ✅ 貼合 | 企業教會原型自洽 |
| `DMSLegion` | DMS_Army, DMS_AncientCorps | 凡人集体 / Archist,Collectivist,Transhumanist,HumanPrimacy | ✅ 貼合 | 軍團／合成人倫理／行政冷硬；Transhumanist＋HumanPrimacy＝「人指揮合成體」自洽 |
| `OARatkin` | OA_RK_Faction | 金鸢尾兰文化分支 / Elite,Friendly | ✅✅ 完美 | OA＝域外溫暖對照組，友善觀察者；OA 專屬 meme |
| `RatkinUnderground` | RKU_Faction | 鼠族游击队 / Tunneler,SharedBurdens | ✅ 貼合 | 地下游擊＝革命體、共擔 |
| `RatkinWarlord` | Rakinia_Warlord | 加雅缇 / Hindu,MaleSupremacy,Xenophobia,Healthcare | ⚠️ 小偏 | 軍閥＝「王國分裂出的貪」，MaleSupremacy/Xenophobia 貼；**Hindu 屬 RNG 隨機宗教、與鼠族無互文**，可換更中性的 theist |
| `ZHP` | ZHP_Faction | 灵长合作社 / Ideological,Supremacist,Collectivist,HumanPrimacy | ⚠️ 小偏 | 雜貨鋪鼠＝**輕鬆賣萌日常**；Supremacist＋HumanPrimacy 太侵略，與萌系日常調不搭 |
| `RatkinKingdom` | Rakinia, RKK_KnightOrders | 诺布阿尔 / TheistEmbodied,Loyalist,TerritorialHegemony | ✅ 貼合 | 封建王庭＋騎士團之光；已乾淨換過 |
| `RockRatkin` | Rakinia_RockRatkin | 诺布艾尔 / TheistAbstract,Loyalist,Rancher,TerritorialHegemony | ✅ 貼合 | 王庭變體 |
| `SnowRatkin` | Rakinia_SnowRatkin | 阿图拉机械族 / **Archist,MechanoidSupremacy** | ❌ **接錯神** | 雪鼠 lore＝風雪遺孤、喪失鄉愁和解；宗教母題是**風雪教徒崇拜「風雪之神」**。現行卻拜超凡智能／機械族——與雪鼠、與整個 `RK_Culture_Virtuard` 騎士王國全衝突 |
| `TravelRatkin` | Rakinia_TravelRatkin | 万能之主 / Islamic,Cowboys,Light,InsectoidSupremacy | ⚠️ 小偏 | 牛仔游牧＋光明神自洽；**InsectoidSupremacy 突兀**（鼠族不敬蟲），該 drop／換 |
| `Frontier` | OutlanderCivil | 岩仓教 / Buddhist,Rancher,Royal | ✅ 貼合 | 已乾淨換過 |
| `FrontierRough` | OutlanderRough | 基督 / Christian,Supremacist,Rancher | ✅ 貼合 | 原型自洽 |
| `Traders` | TradersGuild | 超凡-防御 / Archist,Shipborn,Bulwark,Egalitarian | ✅ 貼合 | 太空商會原型自洽 |
| `Tribal` | TribeCivil | 树皮树叶 / Bacchanalianism,Supremacist,TreeConnection | ✅ 貼合 | 已乾淨換過 |
| `Imperial` | Empire | 不可知论家庭 / Agnosticism,Loyalist,Collectivist,Aristocratic | ✅ 貼合 | 貴族帝國原型自洽 |
| `Corsair` | Pirate | 盗窃主义 / Authoritarianism,Supremacist,Raider | ✅ 貼合 | 海盜原型 |
| `CorsairYttakin` | PirateYttakin | 劫掠兽群 / Corsair,AnimalPersonhood,Raider | ✅ 貼合 | 獸群海盜原型 |
| `CannibalPirate` | CannibalPirate | 垃圾-掠夺主义 / Scavenger,Supremacist,Cannibal,Raider | ✅ 貼合 | 食人海盜原型 |
| `CannibalTribe` | TribeCannibal | 人肉族 / Atheist,Cannibal,FireWorship | ✅ 貼合 | 食人部落原型 |
| `Bandit` | SettlerSavage | 劫掠主义 / Raider,Sadist,Isolationist | ✅ 貼合 | 劫掠原型 |
| `Guild` | GuildFaction_AdventurersGuild | 超凡科技机组 / **Archist,Shipborn** | ❌ **接錯味** | 冒險者公會被滾成「超凡智能崇拜太空科技邪教」（領袖＝首席超凡科技师）。應是**正派冒險者／傭兵榮譽**味，非 AI 邪教 |
| `LucifersCartel` | PC_Faction_LucifersCartel | 综合之路 / Omnism,Supremacist,Darkness | ✅ 貼合 | 暗黑商團，Darkness 貼路西法 |
| `Ancients` | Ancients, AncientsHostile | 超凡-技术主义 / Archist,Loyalist,Transhumanist,HumanPrimacy | ✅ 貼合 | 超凡遠古人原型自洽 |

## 兩個 ❌ 的修正提案

### 1. SnowRatkin —— 拜錯神（最該修）

- **病**：memes `Archist, MechanoidSupremacy`、信仰名「阿图拉机械族」、描述拜「超凡智能」。
- **雪鼠真實 lore**（`ratkin-submods-partial.md` §一）：風雪遺孤——永續暴風雪荒原、日記抒情、喪失與和解；
  世界裡的宗教勢力是**「風雪教徒」崇拜「風雪之神」**（狂信寡言，視玩家為使者或罪人）。
- **正解（需進遊戲 dump，propose-only）**：做一份**風雪／自然神**信仰——`TheistEmbodied`（把風雪之神當具身神明）
  ＋ 冷地/自然向 meme（如 `Nature`-系或 `Tunneler` 之外的樸素信仰），信仰名走「風雪之神」路線。
- **安全替代（可立即執行的整檔替換，但會犧牲雪鼠獨特性）**：把 `SnowRatkin.rid` 整檔換成 `RockRatkin.rid`
  的內容再改 `<name>`——至少變回**鼠族騎士王國自洽信仰**，比拜機械族好，但雪鼠就沒有自己的味道。
  **建議還是走正解**（進遊戲捏風雪神信仰）。
- ✅✅ **已捏好（2026-07-18，使用者「你不能直接幫我捏」）**：兩步——先整檔複製 `RockRatkin.rid` 拿到
  **連貫的 `TheistAbstract` 骨架**（TheistAbstract/Loyalist/Rancher/OAGene_TerritorialHegemony，RK 騎士王國文化），
  再把**神祇與全部風味字串改皮成「風雪之神」**：牧場女神「诺布艾尔」→「**风雪之神**」（风雪与畜牧之神）；
  信仰名 `雪原-精神主义`；稱謂 风雪信徒／风雪守望者；描述重寫成風雪求生神話（織入雪地犬/畜群＝呼應 Rancher meme、
  番红花榴莲背叛＝呼應鼠族背叛母題、「風雪中前行方抵芳草綠茵」＝取自鼠族 OA 賀信）；葬禮儀式改名「风雪之神哀悼」。
  **memes 一根未動＝零 precept 殘留**；0 機械族/牧場殘留、XML 良構。TheistAbstract＋風雪神＋忍耐求生本就契合，**已是可用正解**。
  （若日後想更硬核，仍可進遊戲把 Rancher 換成純自然向 meme 並重生 precepts；非必要。）

### 2. Guild —— 冒險者公會不該是 AI 邪教

- **病**：memes `Archist, Shipborn`、信仰名「超凡科技机组」、領袖「首席超凡科技师」、描述拜「超凡智能」。
- **想要的味**：正派冒險者／傭兵公會的**榮譽與同袍**，Astropolitan（太空文化）可留。
- ✅✅ **已捏好（2026-07-18，離線改皮）**：查證發現 Guild 的三個「超凡X」precept 背後**都是通用 def**
  （`超凡机械日`＝Festival、`超凡形状`/`超凡科技雕像`＝IdeoBuilding），archotech 味**只在顯示名**；
  且全檔**無任何 Archist 專屬 precept def**。⇒ 兩處安全動作：① meme `Structure_Archist`→`Structure_Ideological`
  （世俗結構 meme，precept 清單不依賴 Archist 故乾淨）；② 全部風味改皮。結果＝世俗冒險者信仰
  「**冒险者盟约**」（memes `Structure_Ideological, Shipborn`——**Shipborn 留**＝星際冒險者正好；icon `Steer` 船舵；
  領袖 公会会长；memberName 冒险者；三個 precept 改名 冒险者庆典/盟约徽记/英雄纪念碑；描述重寫成
  「不問出身、只論胆識與信義／護同伴、留荣光」的世俗信條，去除超凡智能崇拜）。0 殘留、XML 良構。**已是可用正解。**

## 四個 ⚠️ 小偏（口味項，可留可調，全部 propose-only）

**✅ 四項於 2026-08-27 全數處理完畢**（離線；仍未 in-game 驗證）。

| 派系 | 原偏差 | 實際做法 | 動到 meme？ |
|---|---|---|---|
| `ZHP` | Supremacist＋HumanPrimacy 太侵略 | 拔兩個 meme、補 `VME_Egalitarian`；`Skullspike_Desired`→`_Disapproved`、`Execution_RespectedIfGuilty`→`_HorribleIfInnocent`、`IdeoDiversity_Standard`→`_Approved`；移除 `Bonding_Disapproved`（HumanPrimacy 殘留）與 `GladiatorDuel` 鬥技儀式；全套改皮成「常灯之约」雜貨鋪日常 | 是（3 個） |
| `TravelRatkin` | InsectoidSupremacy 突兀（鼠族不敬蟲）| 拔 meme＋它 `requireOne` 的三條：移除 `VME_Insectoids_Exalted`，`VME_InsectJellyEaten_Loved_Classic`→`_Neutral_Classic`、`VME_InsectMeatEating_Sacrilegious`→`_DontCare`；另拔掉兩條 `requiredMemes` 指向它的孤兒（`VME_InsectoidHymnPrecept` 蟲族禮讚儀式、`VME_IdeoRole_InsectoidHerder` 甲蟲巢穴指揮者角色）；改皮「小虫-穆斯林」→「逐光牧人」 | 是（1 個） |
| `RatkinWarlord` | Hindu 屬 RNG、與鼠族無互文 | `Structure_OriginHindu`→`Structure_TheistEmbodied`（該 meme 無 requireOne／requiredPrecepts ⇒ 0 殘留）；移除 `thingStyleCategories` 的 `Hindu` 視覺風格（其餘鼠族信仰本來就沒有）；四神改皮成王國「諾布」神族的軍閥支系（诺布戎／诺布慈／诺布铎／诺布厉）；領袖銜對齊 FactionDef 的「司令」 | 是（1 個） |
| `Kiiro` | FleshPurity 非其治癒主題 | **只改皮、一個 meme 一條 precept 都沒動**——FleshPurity 本身就讀得成「身體如生時所是」，衝突的其實是顯示字串（「文化-肉体派」「血肉者」＋撕下皮下裝置的血腥描述）。全部重寫成田園治癒＋無妄之災的綺羅基調 | 否 |

> **為什麼 Kiiro 只改皮**：`FleshPurity` 的 `requireOne` 是 `DrugUse_Prohibited` / `BodyMod_Abhorrent`，
> Kiiro 已有 `BodyMod_Abhorrent`，換掉 meme 反而要動更多；而 audit 原本的判定就是「不改也不出戲」。
> 真正出戲的是 RNG 生成的顯示字串，那個零風險可改。

### 已知殘留（判定為可接受）

`ZHP` 留下兩個 `requiredMemes` 未滿足的孤兒 precept：`IdeoRole_ProductionSpecialist`（原需 HumanPrimacy）、
`IdeoRole_ShootingSpecialist`（原需 Supremacist）。**不拔**，理由有二：
①「製造專家／彈藥大師」對一間邊境雜貨鋪本來就合理；
②這種孤兒在本包**本來就普遍存在**——未經任何人動過的 `RatkinKingdom`／`RockRatkin`／`SnowRatkin`／`Traders`／`Guild`
都各有 3～4 個，而它們是從真實遊戲 dump 出來的，代表引擎容忍這種狀態
（`requiredMemes`／`requireOne` 都只在**生成期**檢查，載入期不驗——見 `Ideo.PreceptIsRequired` 只被 UI 呼叫、
`ExposeData` 直接 Scribe 兩個清單）。

## 機制注意（哪些離線能做、哪些不能）——本輪實證更新

`.rid` 是**整份 ideo 快照**：`<memes>` 之外還烘焙了一整串 precepts（儀式/規範）。前人「已換掉」三個
（RatkinKingdom／Frontier／Tribal）都是**整檔替換成連貫 dump ideo**，不是裸改 meme。本輪實作後把規則講清楚：

- ✅ **改皮（改顯示字串）完全安全**：神祇 name/type、`<description>`/`<descriptionTemplate>`、ideo `<name>`、
  adjective、memberName、leader title、**precept 的 `<name>`**——全是顯示字串，不影響機制。SnowRatkin 全靠這個。
- ✅ **換 structure/其它 meme——只在「沒有該 meme 專屬 precept」時才乾淨**。Guild 實測：三個「超凡X」precept 背後
  是通用 def（`Festival`/`IdeoBuilding`），全檔無 Archist 專屬 precept ⇒ `Structure_Archist`→`Structure_Ideological` 乾淨無殘留。
- ❌ **裸換有專屬 precept 的 meme 會殘留**（如 SnowRatkin 若裸換 MechanoidSupremacy，机械族/超凡哀悼儀式仍在）
  → 這種只能**整檔替換**或**進遊戲重生 precepts**。
- ⚠️ **换 iconDef/colorDef 要用已驗證存在的 defName**（本 pool 實際用過的清單，Guild 用了 `Steer`）——亂填會報錯。

> **驗證**：worldpreset ideo **headless 測不到**，唯一真驗＝新開局選「二次元定製世界」進遊戲逐派系看。

## 建議執行順序（等使用者回桌面驗證）

1. **SnowRatkin**：✅✅ 已捏好「风雪之神」信仰（改皮，零殘留）。進遊戲確認風味即可。
2. **Guild**：✅✅ 已捏好「冒险者盟约」世俗信仰（換 meme＋改皮，零殘留）。進遊戲確認即可。
3. ~~有空再處理四個 ⚠️ 小偏~~ → **2026-08-27 已全部做完**（ZHP／TravelRatkin／RatkinWarlord／Kiiro），
   一併進遊戲確認風味即可。

> **2026-07-18 動的檔**：`CustomIdeos/SnowRatkin.rid`、`CustomIdeos/Guild.rid`（皆改皮/換乾淨 meme，XML 良構）。
> **2026-08-27 動的檔**：`CustomIdeos/ZHP.rid`、`TravelRatkin.rid`、`RatkinWarlord.rid`、`Kiiro.rid`。
> **兩輪都未碰 live、未重組、未跑測**（worldpreset 本就 headless 測不到）。全在 worldpreset 源碼區，可 git 還原。
> ✅ **2026-08-27 已同步到部署副本**：四份 `.rid` ＋ 本檔 ＋ `CULTURE-PLAN.md` 已複製進
> `~/notes/projects/modding/rimworld/worldpreset-2-anime/`（遊戲 `Mods/worldpreset-2-anime` symlink 指向那裡）。
> 同步前已確認部署側那四份與開發側改動前**逐位元組相同**（沒有蓋掉部署側的在地修改），
> 並整包備份。同步後穿 symlink 從遊戲實際讀取路徑複驗：27 份 `.rid` ＋ `About.xml` ＋ `Preset.xml` 全數良構。
> 開發側專屬的 `PROJECT.md`／`SOURCE-HISTORY.md` **不進**部署副本（維持原本的分工）。
</content>
</invoke>
