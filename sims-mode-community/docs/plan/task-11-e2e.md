# Task 11: 實機 E2E 驗證（手動清單）

> 屬於 `../2026-06-11-implementation-plan.md`。

**Files:** 無（遊戲內驗證；結果記入 `session_log.md`）

> 需要 RimWorld 1.6 本體。把整個 `sims-mode-community/` 資料夾複製（或 junction）到 RimWorld `Mods/` 目錄，啟用 mod。
> 用 Dev mode：`Debug actions > Time` 可跳時辰；地圖上選 NPC pawn 可看當前 Job/duty。

- [ ] **Step 1: 載入測試**：開新世界+新殖民地，確認無紅字錯誤（特別是 Def 載入與 patch 應用階段）。
- [ ] **Step 2: 白天訪問**：組商隊 → 訪問鄰近非敵對聚落（外邦/聯盟）→ 白天進場。驗證：pawn 不是全部站在中心；有人在工作台/田前（檢查其 Job 為 `pas_sims_FakeWork`）；衛兵在中心附近。
- [ ] **Step 3: 作息切換**：Dev 快進到 17-22 時 → 多數 pawn 聚到桌子/營火附近社交（冒互動泡泡）；快進到 22 時後 → pawn 走向床、躺床睡覺（驗證 NPC 睡床成立；若全睡地上，記錄並檢查 `IsValidBedFor` 哪條擋住）。
- [ ] **Step 4: 部落 profile**：訪問部落聚落 → 確認分配走 `pas_sims_Profile_Tribal`（無工人角色；可在 Step 2 前於 `GenStep_SettlementLife.Generate` 加 `Log.Message` 印 profile defName，驗完移除）。
- [ ] **Step 5: 翻臉防禦**：訪問中攻擊任一 NPC → 全員切防禦（衝向玩家或集結；duty 變 `DefendBase`）；繼續打到損失 >20% → 轉主動反擊。
- [ ] **Step 6: 攻打不受影響**：直接「攻擊」一個敵對聚落 → 行為與原版一致（守軍直接 DefendBase，無生活行為）。
- [ ] **Step 7: 存讀檔**：訪問中（生活狀態下）存檔 → 讀檔 → pawn 繼續按作息行動、無紅字（驗證 `LordJob_SettlementLife.ExposeData` 的 Dictionary 序列化）。
- [ ] **Step 8: 玩家離場**：商隊離開地圖 → 地圖正常回收、無錯誤。
- [ ] **Step 9: 結果記入 session_log.md（含發現的問題與修復），commit**

```
git add sims-mode-community/session_log.md
git commit -m "test: 實機 E2E 驗證記錄"
```
