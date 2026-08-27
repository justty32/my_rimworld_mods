# 二次元定製世界 — 文化（信仰）現況 v2

> 配 `Preset.xml`。**v2（2026-07-17）：文化已就位。** 27 份 `.rid` 覆蓋全部 30 派系，
> 開局 0 個退回隨機。全部取自使用者 dump 的世界 `危宿三-IV`，逐派系「凍結」。

## v2 做法（為什麼是逐派系凍結，不是共用原型）

dump 揭露一件關鍵事：**每個種族派系都有自己 required 的 CultureDef**——Kiiro 用
`KiiroTraditionalCulture`、Milira 用 `MiliraCulture`、OA 鼠用 `OA_RK_RatkiniaTraditionCulture`…
所以最初「13 個共用泛型原型」的設計是錯的（會把 Rustican 之類的錯文化套到鼠族/精靈頭上，
pawn 命名全亂）。正解 = 讓每個派系用**它自己那份文化正確的信仰**。

dump 裡遊戲已為每個派系滾好一份文化正確的 ideo。多數已連貫，直接凍結；少數亂滾到垃圾
meme 的手動換成 pool 裡更合適的（見下表備註）。`.rid` 是遊戲產物，直接複製零脆弱風險。

## 現行對映（CustomIdeos/<key>.rid → 派系）

| .rid key | 派系 | 信仰名 | culture | memes | 備註 |
|---|---|---|---|---|---|
| `Milira` | Milira_Faction | 新生-正义 | MiliraCulture | Ideological, Loyalist, Collectivist | |
| `MiliraChurch` | Milira_AngelismChurch | 超凡科技之路 | Milira_ChurchCulture | Archist, MaleSupremacy, Loyalist, Collectivist | |
| `Kiiro` | Kiiro_Faction | 本色之约 | KiiroTraditionalCulture | Ideological, Individualist, FleshPurity | **2026-08-27 改皮**（meme 不動）：肉体派→「本色之约」田園純真味 |
| `Moelotl` | AxolotlWanderingDynasty | 红船 礼义 | MoeLotlTraditionCulture | Ideological, FemaleSupremacy, Loyalist, Collectivist, FleshPurity | 女尊萌系王朝，讚 |
| `StellarCorp` | USAC_Faction | 上帝 | USAC_Culture | Christian, Supremacist, Individualist, Proselytizer, HumanPrimacy | 企業教會，coherent |
| `DMSLegion` | DMS_Army, DMS_AncientCorps | 凡人集体 | DMS_Nara_Culture | Archist, Collectivist, Transhumanist, HumanPrimacy | 兩支機甲軍團共用 |
| `OARatkin` | OA_RK_Faction | 金鸢尾兰文化分支 | OA_RK…Culture | Elite, Friendly（OA 專屬 meme） | 完美貼合 |
| `RatkinUnderground` | RKU_Faction | 鼠族游击队 | …Culture_Und | Tunneler, SharedBurdens | 地下游擊，貼合 |
| `RatkinWarlord` | Rakinia_Warlord | 诺布戎 | …Warlord_PLUS | **TheistEmbodied**, MaleSupremacy, Xenophobia, Healthcare | **2026-08-27 換神**：印度教起源→中性具象神論，神系接回王國的「諾布」神族（軍閥＝分裂出去那一支）|
| `ZHP` | ZHP_Faction | 常灯之约 | ZHP_Culture | Ideological, Collectivist, **VME_Egalitarian** | **2026-08-27 去侵略性**：拔 Supremacist／HumanPrimacy，改走「灯不熄、门不锁」的雜貨鋪日常 |
| `RatkinKingdom` | Rakinia, RKK_KnightOrders | 诺布阿尔 | RK_Culture_Virtuard | TheistEmbodied, Loyalist, TerritorialHegemony | **已換掉原本機械族亂滾**；王國＋騎士團共用王庭信仰 |
| `RockRatkin` | Rakinia_RockRatkin | 诺布艾尔 | RK_Culture_Virtuard | TheistAbstract, Loyalist, Rancher, TerritorialHegemony | |
| `SnowRatkin` | Rakinia_SnowRatkin | 风雪之神 | RK_Culture_Virtuard | TheistAbstract, Loyalist, Rancher, TerritorialHegemony | **2026-07-18 已改皮修好**（原本 RNG 滾到機械族崇拜，與雪鼠 lore 正面衝突）|
| `TravelRatkin` | Rakinia_TravelRatkin | 万能之主 | RK_Culture_Virtuard | Islamic, Cowboys, Light | **2026-08-27 去蟲**：拔 InsectoidSupremacy 及其三條 requireOne precept＋蟲族禮讚儀式＋甲蟲巢穴指揮者角色；牛仔／光明神原樣保留 |
| `Frontier` | OutlanderCivil | 岩仓教 | VFES_Frontier | Buddhist, Rancher, Royal | **已換掉原本蟲神膜拜** |
| `FrontierRough` | OutlanderRough | 基督 | VFES_Frontier | Christian, Supremacist, Rancher | |
| `Traders` | TradersGuild | 超凡-防御 | Astropolitan | Archist, Shipborn, Bulwark, Egalitarian | 太空商會，貼合 |
| `Tribal` | TribeCivil | 树皮树叶 | Rustican | Bacchanalianism, Supremacist, TreeConnection | **已換掉原本「摧毀之道」**；改自然/TreeConnection |
| `Imperial` | Empire | 不可知论家庭 | Sophian | Agnosticism, Loyalist, Collectivist, Aristocratic | 貴族帝國，貼合 |
| `Corsair` | Pirate | 盗窃主义 | Kriminul | Authoritarianism, Supremacist, Raider | |
| `CorsairYttakin` | PirateYttakin | 劫掠兽群 | Kriminul | Corsair, AnimalPersonhood, Raider | |
| `CannibalPirate` | CannibalPirate | 垃圾-掠夺主义 | Kriminul | Scavenger, Supremacist, Cannibal, Raider | |
| `CannibalTribe` | TribeCannibal | 人肉族 | Corunan | Atheist, Cannibal, FireWorship | |
| `Bandit` | SettlerSavage | 劫掠主义 | Kriminul | Raider, Sadist, Isolationist | |
| `Guild` | GuildFaction_AdventurersGuild | 超凡科技机组 | Astropolitan | Archist, Shipborn | ⚠️ 太空船員味，公會口味待定——想要「正派冒險者」味就進遊戲改 |
| `LucifersCartel` | PC_Faction_LucifersCartel | 综合之路 | Corunan | Omnism, Supremacist, Darkness | 暗黑商團（Darkness meme 貼「路西法」） |
| `Ancients` | Ancients, AncientsHostile | 超凡-技术主义 | KiiroTraditionalCulture | Archist, Loyalist, Transhumanist, HumanPrimacy | 超凡遠古人；culture 名恰是 Kiiro（無傷，遠古人少露臉） |

## 想微調文化的兩條路

- **改個別派系味道**：進遊戲信仰編輯器改好 → 另存 → 覆蓋 `CustomIdeos/<key>.rid`（檔名別變）。
- **整批重滾**：再 dump 一個你更滿意的世界給我，我重新挑一輪。

標 ⚠️ 的（SnowRatkin 機械族、Guild 太空船員）最值得先調。

## 部署（尚未做，避開 ~/notes 上另一個 agent）

1. 讓遊戲看到：把 `worldpreset-2-anime/` symlink 進遊戲 `Mods/`（比照本機部署慣例）。
2. 啟用：把 `justty32.worldpreset.anime2` 加進 `pack-2-anime.xml` activeMods（排 `ferny.worldbuilder` 之後）。
3. 新開局 → Worldbuilder 世界預設選擇頁 → 選「二次元定製世界」→ 各派系即用固定信仰。

> ⚠️ 步驟 2 動到 pack 清單（STATUS 管的數字）。等另一個 agent 收工／使用者點頭再做。
