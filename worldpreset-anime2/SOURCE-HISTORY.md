# 來源沿革

這個 mod 的原始碼原本**只存在於部署側**，違反本工作區「自製 mod 原始碼一律放
`projects/my_rimworld_mods/`」的規則。2026-08-27 收回開發側，建立本目錄為**原始碼權威版本**。

## 時間線

- **~2026-07-17 ~ 2026-07-18**：mod 直接在部署側從零建立與迭代（陣容策展、生成參數、
  27 份 `.rid` 信仰凍結、`CURATION-AUDIT-2026-07-18.md` 稽核與兩個信仰改皮修正）。
  開發側從未有這份原始碼。
- **2026-08-27**：以 `cp -a` 方式將部署側目錄完整複製回開發側本目錄，只複製、未刪除、
  未修改來源。部署側目錄原樣保留，繼續作為**部署副本**（已 symlink 進遊戲 `Mods/`，
  且 `justty32.worldpreset.anime2` 已在 `pack-2-anime` activeMods 清單中啟用）。

## 來源

- 絕對路徑：`/home/lorkhan/notes/projects/modding/rimworld/worldpreset-2-anime/`
- 複製方式：`cp -a` （保留權限/時間戳，未做任何轉換）

## 收回當下的檔案清單與大小（驗證基準）

- 檔案數：**32**（`find <來源> -type f | wc -l`）
- 總位元組數：**3,021,132**（`du -sb <來源>`）
- 複製後在開發側目錄用同樣兩個指令重新量測，數字與上列完全一致；另跑
  `diff -rq <來源> <目標>` 確認零差異（exit code 0）。

檔案結構（32 個檔案，其中 27 個為 `Worldbuilder/Anime2World/CustomIdeos/*.rid`）：

```
About/About.xml
README.md
Worldbuilder/Anime2World/Preset.xml
Worldbuilder/Anime2World/CULTURE-PLAN.md
Worldbuilder/Anime2World/CURATION-AUDIT-2026-07-18.md
Worldbuilder/Anime2World/CustomIdeos/*.rid   (27 個檔案)
```

## 之後的維護方向

**本目錄（開發側）是唯一該被編輯的地方。** 部署側目錄之後只當作「發布出去的副本」，
不應再直接編輯。任何改動的落地流程：

1. 在本目錄改 `Preset.xml` / `CULTURE-PLAN.md` / `CustomIdeos/*.rid` 等。
2. 用 `cp -a` 把整個目錄重新複製到
   `~/notes/projects/modding/rimworld/worldpreset-2-anime/`（覆蓋部署副本）。
3. 部署側的 symlink 與 `pack-2-anime` 啟用狀態不需重做，覆蓋內容即可生效於下次開局。

若未來要驗證兩側是否又飄散（drift），可重跑收回當時用的驗證方式：
`find ... | wc -l` 比檔案數、`du -sb ...` 比總位元組數、`diff -rq ... ...` 比逐檔內容。
