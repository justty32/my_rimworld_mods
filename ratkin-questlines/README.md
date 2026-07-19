# 鼠族世界任務包 / Ratkin Questlines

用 Custom Quest Framework（CQF）製作的**沉浸式鼠族相遇任務包**。玩家以聚居點管理者的身分，遇見鼠族社會不同角落的來客。目標：讓「有鼠族的世界」感覺真實、活著。

> 目前為**首發垂直切片**（單一任務線），驗證核心管線後將陸續擴充其餘任務線。設計全貌見開發側 `PROJECT.md`（不隨產物發佈）。

## 目前內容

**《陳皮餅乾的老交情》** — 一名路過的鼠族旅人上門兜售旅途乾糧（硬餅乾＋啤酒）。你可以：
- **打聽近況**：聽旅人講講鼠族世界最近的風聲（王國運糧、軍閥抓丁、雜貨鋪商隊…）。
- **講價宰一筆**（需談判者社交 7+，25 銀）：撿便宜，但旅人會記恨。
- **照行情價成交**（40 銀）：一手交錢一手交貨。
- **大方待客**（60 銀）：多付一點、好好送客，換取該鼠族派系的好感。

三種交易一位旅人只成交一次。你的抉擇會被記錄（好客／宰客旗標），供日後任務線呼應。

## 依賴

**硬相依（缺了不能載）：**
- Harmony（`brrainz.harmony`）
- Custom Quest Framework（`HaiLuan.CustomQuestFramework`，Steam 2978572782）
- NewRatkinPlus / Ratkin Race Mod（`Solaris.RatkinRaceMod`，Steam 1578693166）— 提供鼠族派系與物品

**選用增強（有就更好，缺了不報錯，靠 `MayRequire` 保護）：**
- Ratkin Underground+（`RKU.RatkinUnderground`）— 讓游擊隊鼠族訪客也能觸發對話
- Ratkin 邊緣雜貨鋪 / Misc+（`W.ZHP`）— 讓雜貨鋪鼠族訪客也能觸發對話

## 遊戲內怎麼觸發

鼠族中立訪客團到訪時，隊伍裡的**非商隊鼠族**頭上會出現對話圖示（依機率）。右鍵該鼠族 → 開始對話。

> 測試提示：等鼠族中立訪客團自然到訪即可；或用 CQF 遊戲內編輯器把 `RatkinQL_PeddlerManager` 綁到任一 pawn 直接驗對話樹。對話只會綁在鼠族派系訪客身上（`Rakinia`／`RKU_Faction`／`ZHP_Faction`），不會出現在其他種族。
