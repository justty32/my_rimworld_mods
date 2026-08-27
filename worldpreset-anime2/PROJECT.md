# worldpreset-anime2 — 二次元定製世界（Worldbuilder 世界預設）

> RimWorld 1.6。packageId `justty32.worldpreset.anime2`。**純資料 mod，零 C#、零 Harmony。**
> 給 `pack-2-anime`（510 mod / profile 2-anime）用的 Worldbuilder 世界預設：策展派系陣容、
> 生成參數、派系改造，並為各派系指派固定信仰（`.rid`）以根治開局隨機亂數文化。

> ⚠️ **本目錄是原始碼權威版本。** 部署副本在
> `~/notes/projects/modding/rimworld/worldpreset-2-anime/`（那邊 symlink 進遊戲 Mods/，
> 且已在 pack-2-anime activeMods 清單啟用）。**改這邊之後要重新複製過去部署副本才會生效**
> ——部署那份不會自動同步。收回經過見 `SOURCE-HISTORY.md`。

## 目標與範圍

`pack-2-anime` 有約 42 個會生成的 NPC 派系，其中原生只有 1 個手動指定信仰、其餘 41 個開局
隨機滾意識形態——等於亂數 meme 沙拉。本 mod 用 Worldbuilder 的 world preset 機制根治：

1. **策展陣容**：從全部候選派系挑 30 個組成一個有主題、有兩難、有多敵對的世界
   （7 二次元盟友 ＋ 9 鼠族大雜燴 ＋ 6 原版錨點 ＋ 8 敵對）。
2. **固定生成參數**：星球覆蓋率、地形密度、河流/山脈/污染等，配合 30 派系的規模調整。
3. **固定信仰**：透過 `savedIdeoFactionMapping` + `CustomIdeos/*.rid` 給每個派系指派一份
   凍結自真實遊戲 dump、文化正確（CultureDef 對得上種族）的信仰，開局 0 個退回隨機。

## 是什麼／不是什麼

- **是**：一個純檔案 mod，被 Worldbuilder 掃描為「世界預設」候選項。新開局在 Worldbuilder
  的預設選擇頁選「二次元定製世界」即套用本檔定義的世界生成（派系/參數/信仰）。
- **不是**：不寫任何 C#、不掛 Harmony、不新增 FactionDef／IdeoDef（陣容全部複用其他已裝
  mod 提供的 defName）。沒有 Worldbuilder 時本 mod 完全惰性（`Worldbuilder/` 不在原版任何
  載入路徑掃描範圍內）。

## 技術棧

| 層 | 機制 |
|---|---|
| 世界佈景（陣容/顏色/描述/生成參數） | Worldbuilder preset XML（`WorldPresetManager.cs` 原生 Scribe 存檔格式：`<keys>/<values>` 平行清單、`Color = RGBA(...)`） |
| 固定信仰 | Worldbuilder `savedIdeoFactionMapping`（Preset.xml 內）+ `CustomIdeos/<key>.rid`（遊戲匯出的 ideo 快照，直接複製零脆弱風險） |
| 相依 | 硬相依 `ferny.Worldbuilder`（`modDependencies` + `loadAfter`）；沒有它 preset 不會出現在選單，但本 mod 本身不報錯 |

## 關鍵檔案

```
About/About.xml                          # packageId justty32.worldpreset.anime2；硬相依 ferny.Worldbuilder
README.md                                # 原作者現況筆記（v1 骨架時期，內容部分已被下列文件超越）
Worldbuilder/Anime2World/
├── Preset.xml                           # 世界定義主檔：30 派系陣容 + 生成參數 + savedIdeoFactionMapping
├── CULTURE-PLAN.md                      # 信仰對映表（27 個 .rid → 派系，含來源/memes/備註）；改文化前必讀
├── CURATION-AUDIT-2026-07-18.md         # 逐派系 lore 貼合度稽核；2 個「接錯神」已修（SnowRatkin/Guild）、4 個小偏未動
└── CustomIdeos/*.rid                    # 27 份信仰快照（27/30 派系覆蓋；缺檔派系退回隨機，其餘 preset 照常）
```

## 現況（收回時的狀態，2026-08-27）

- 陣容 30 派系、生成參數、派系改名/上色已定，可載入。`planetCoverage 0.5`。
- 文化已就位：27 份 `.rid` 覆蓋全部 30 派系，開局 0 個退回隨機。
- `CURATION-AUDIT-2026-07-18.md` 稽核出 2 個接錯神（SnowRatkin 拜錯到機械族、Guild 被滾成
  AI 邪教），皆已離線改皮修正（風雪之神 / 冒险者盟约），zero precept 殘留、XML 良構。
  另有 4 個口味小偏（Kiiro/RatkinWarlord/ZHP/TravelRatkin）未動，優先度低。
- **未做 in-game 驗證**：worldpreset 的 ideo headless 測不到，真正驗證只能新開局選「二次元
  定製世界」進遊戲逐派系肉眼看。收回當下尚未做這一步。

## 完成定義

- [x] `About.xml`／`Preset.xml` well-formed，defName 全部曾在 pack-2-anime 實測存在。
- [x] 27 個 `.rid` 對映到位，覆蓋 30 派系中的 27 個。
- [x] `CULTURE-PLAN.md` / `CURATION-AUDIT-2026-07-18.md` 記錄完整、可追溯每個信仰的來源與判定。
- [ ] 實機：新開局選「二次元定製世界」，30 派系全部出現、無紅字。
- [ ] 實機：逐派系檢查信仰名稱/precept 顯示正常（尤其 SnowRatkin／Guild 改皮後的風味）。
- [ ] 待處理：4 個口味小偏（優先 ZHP＞TravelRatkin＞RatkinWarlord＞Kiiro）視使用者意願調整。
- [ ] 剩餘 3 個未覆蓋派系（30 − 27）視需要補 `.rid` 或接受退回隨機。

## 改動須知

- 改陣容／生成參數：編 `Preset.xml`，清單設計成加/刪一行 `<li>` 即可。
- 改信仰口味：**不要裸改 `.rid` 裡的 meme**——若該 meme 有專屬 precept（見
  `CURATION-AUDIT-2026-07-18.md` 末節「機制注意」），裸換會殘留舊 precept。安全做法是
  「改皮」（改顯示字串：神祇名、`<description>`、ideo `<name>`、precept `<name>` 等）或整檔
  替換成另一份連貫的 dump ideo。
- 改完 **一定要重新 `cp -a` 整個目錄回部署副本**（`~/notes/projects/modding/rimworld/worldpreset-2-anime/`）
  才會反映到遊戲；部署那邊是 symlink 進 Mods/，但目錄本身不會自動跟開發側同步。
