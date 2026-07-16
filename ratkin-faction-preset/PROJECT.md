# ratkin-faction-preset — 橡實大遠征商會 + Worldbuilder 世界預設

## 目標
RimWorld 1.6 派系 mod：新增一個鼠族（NewRatkinPlus）派系「橡實大遠征商會」，
並隨 mod 攜帶 Worldbuilder 世界預設「橡實之路」。純 XML、零 C#、零 Harmony。

- **沒裝 Worldbuilder**：普通派系 mod，`RKP_Faction_AcornGuild` 走原版世界生成自然出現
  （`requiredCountAtGameStart=1`，新世界預設帶 1 個）。
- **有裝 Worldbuilder**：建立世界時多一個現成預設「橡實之路」可選
  （鼠族王國 ×1＋商會 ×2＋原版基本盤，森林權重上調）。

## 派系一句話定位
把「搬橡實回家」當成畢生偉業的快遞／囤貨商會——認真努力、常常出包、天然呆的可愛；
與既有「鼠族王國」（隱居農耕王權）區隔：商會滿地圖跑商、能開多個分會、不會圍城。

## 參照素材
| 素材 | 路徑 |
|---|---|
| NewRatkinPlus 本體（packageId `Solaris.RatkinRaceMod`） | `~/.steam/steam/steamapps/workshop/content/294100/1578693166/` |
| 王國/商隊 FactionDef 範本 | 上述 mod `1.6/Defs/FactionDefs/Factions_Misc.xml` |
| 命名 RulePack 範本＋詞庫 | `1.6/Defs/RulePack/RulePacks_Namers_Factions.xml`、`Contents/Languages/English/Strings/Names/Noun/*.txt` |
| Worldbuilder preset schema 分析 | `~/repo/pas/analysis/rimworld_mods/worldbuilder/details/01_preset_schema.md` |
| 手寫 preset 範例（赤色黎明） | `~/repo/pas/analysis/rimworld_mods/worldbuilder/others/example_preset_mod/` |

## 文化設計依據（每個決策長在鼠族既有世界觀上）
從 NewRatkinPlus 素材抽出的文化元素 → 對應本 mod 的設計決策：

| # | 鼠族文化元素 | 來源（file:line） | 本 mod 對應決策 |
|---|---|---|---|
| 1 | 名字取自**堅果**（Nut/Peanut/Walnut/Chestnut…）、花、香草、自然 | `Contents/Languages/English/Strings/Names/Noun/RK_Noun_Prefix_Nut.txt`、`RK_Noun_Prefix_Flower.txt` | 派系主題＝**橡實/堅果**；命名器 `RKP_NamerFaction_AcornGuild` 直接複用 Nut/Flower 詞庫檔 |
| 2 | 王國命名自嘲式地用食物入名（"of the Peanut"、"of the Sunflower"） | `1.6/Defs/RulePack/RulePacks_Namers_Factions.xml:93-94` | 商會名沿用同款幽默："The Order of the Round [Nut]"、"The Hundred-Thousand [Nut] Company" |
| 3 | 囤積、倉儲是鼠的種族本能形象；商隊馱獸是**倉鼠王** | `Factions_Misc.xml:139,301`（`Ratkin_KingHamster` carriers） | 商會的執念＝「囤滿十萬顆橡實冬天就不會來」；Trader 群組 carriers 沿用 `Ratkin_KingHamster` |
| 4 | 鼠族愛挖洞、據點藏起來（王國「人類無法發現」、地下巢穴形象） | 繁中描述 `Contents/Languages/ChineseTraditional/DefInjected/FactionDef/Factions_Misc.xml:5` | 聚落命名器用 **-burrow/-hollow/-nest/-den**（洞窟系字尾）；preset 把 `mountainDensity` 調到 1.2 |
| 5 | 鼠族已有流浪商隊傳統（hidden 派系 `RK_Faction_Caravan`，賣 exotic goods） | `Factions_Misc.xml:240-324` | 新派系＝把商隊傳統升格為**可見的、有聚落的商會**（hidden 商隊照舊並存，不衝突） |
| 6 | 王國調性＝和平農耕、非侵略（"非侵略的種族"）、봉建騎士文化重榮譽 | `Factions_Misc.xml:184`、`1.6/Defs/CultureDefs/Cultures.xml:6` | 商會不天生敵對、`canSiege=false`、raid 頻率曲線壓到 0.8；戰鬥群組以傭兵/押運護衛為主，精銳騎士僅低權重客串（護寶隊） |
| 7 | 文化 `RK_Culture_Virtuard`：騎士封建、榮譽至上，人名器走花名+茶名 | `Cultures.xml:5-11`、`RulePacks_Namers_Factions.xml:68-71`（"[Flower]tea" 姓氏） | `allowedCultures` 直接複用 `RK_Culture_Virtuard` → 人名、信仰命名器全套繼承 |
| 8 | 王國自有商隊 TraderKind 系列（原料/食材/布料/衣裝/珍品） | `1.6/Defs/TraderDefs/TraderKinds_Caravan_Ratkin.xml` | `caravanTraderKinds` 全部複用王國五種商隊（商會賣的就是王國產的貨，設定自洽） |
| 9 | 萌系種族、**傻氣可愛**調性（使用者校準：不狡黠、不陰暗） | 全 mod 美術與文案基調 | 描述文案全走「認真出包」：地圖拿反、帳本重抄、貨送到隔壁鎮；派系圖示是一顆長著嚇呆表情的橡實 |
| 10 | 鼠族專屬 xenotype 與外觀（melanin 0~0.3、無鬍鬚） | `Factions_Misc.xml:20,173-178`、`Cultures.xml:38-42` | `xenotypeSet`＝`RK_XenoType_Ratkin`（MayRequire Biotech）、`melaninRange 0~0.3` 照抄 |

## 關鍵檔案
```
About/About.xml                      # 硬相依 NewRatkinPlus；Worldbuilder 只 loadAfter（理由見下）
Defs/FactionDefs/Factions_AcornGuild.xml   # RKP_Faction_AcornGuild
Defs/RulePack/RulePacks_Namers_AcornGuild.xml  # 派系名/聚落名命名器（複用鼠族詞庫）
Textures/Icon/RKP_AcornGuild.png     # 白色橡實剪影（派系色染色）
Languages/ChineseTraditional|ChineseSimplified/DefInjected/FactionDef/
Worldbuilder/AcornWorld/Preset.xml   # 世界預設「橡實之路」＋ Thumbnail.png
tests/healthcheck.py                 # 靜態驗證（XML/引用/貼圖/preset schema）
```

## Worldbuilder 相依決策
**不列 modDependencies，只 loadAfter。** 依據：Worldbuilder 的 preset 掃描發生在它自己那端——
`WorldPresetManager.GetAllPresets` 對每個已載入 mod 的每個 load folder 找 `Worldbuilder/` 子目錄
（worldbuilder `Source/WorldPresetManager.cs:145-158`）。本 mod 的 `Worldbuilder/` 目錄不屬於
任何原版載入路徑（原版只認 Defs/Textures/Languages/…），未裝 Worldbuilder 時完全惰性、零錯誤。
與「赤色黎明」範例不同（該 mod 只有 preset、無自帶內容，故硬相依）；本 mod 的 FactionDef
獨立成立，Worldbuilder 是純加值。

## 完成定義與驗證
- [x] `tests/healthcheck.py` PASS（XML well-formed；40 個外部引用全數在 NewRatkinPlus/Core 中存在；
      詞庫檔、貼圖、preset 欄位白名單、preset 派系交叉檢查）
- [ ] 實機：載入無紅字；新世界生成出現商會；派系名/聚落名走橡實命名器
- [ ] 實機：Worldbuilder 預設清單出現「橡實之路」，用它生成世界＝王國×1＋商會×2＋原版四家
- [ ] 實機：商會商隊來訪可交易（五種 TraderKind）、馱獸為倉鼠王
- [ ] 實機（Biotech）：商會成員 xenotype 全為鼠族
- [ ] 實機：把商會打到敵對後，raid 出現且組成為傭兵/押運系

## 限制備忘
- 部署到遊戲需在 `install/Mods` 建 symlink（遊戲不掃 `~/rimworld_mods`）；本次交付不部署。
- preset 刻意不帶 `saveTerrain`/`saveBases`/`saveIdeologies`（快照類只能遊戲內匯出且衝突面大）。
