# Task 10: 實機 E2E（待 RimWorld 1.6 環境）

需要 RimWorld 1.6 本體手動驗證。結果記 `session_log.md`，commit 訊息 `test: faction-politics 實機 E2E 驗證記錄`。

## 前置

mod 順序：sims-mode-community → npc-outposts → faction-politics（與 loadAfter 一致）。開發模式開啟看 log。

## 清單（13 項）

1. **新世界補發**：新檔開局後 dev log 無紅字；數個 NPC 派系收到「反叛者蠢動」信（NeutralEvent，每合格派系一封）。
2. **舊檔補發**：拿 npc-outposts E2E 的舊檔載入 → FinalizeInit 自動補發，不需新開檔。
3. **拜訪找得到**：商隊拜訪某反叛者駐地聚落（sims-mode 真訪問或攻打）→ 反叛者本人出現在場上（名字對 record）；離場再訪 → 還是同一人（redress 重逢）。
4. **鎮壓循環**：殺死反叛者 → 進展歸零；快轉 `respawnDelayDays` 後該派系出現新反叛者（無信，靜默重生）。
5. **分裂全套**：dev 快轉（或 dev 工具改 progress）至閾值 → 分裂信（NegativeEvent）；派系列表多一個同 def 新派系（名字/圖標/ideo 正常＝可行性風險 2）；反叛者是新派系首領（外交頁確認）；倒戈聚落數符合 defectFraction；母新關係 Hostile 且無重複敵對信（風險 3）。
6. **哨站跟隨**：倒戈聚落的衛星哨站（npc-outposts）同步易主（大地圖圖標換色）。
7. **保底**：反覆分裂後母派系始終 ≥1 聚落。
8. **上限觸頂**：dev 觸發分裂至 `maxDynamicFactions` → 之後進展凍結在閾值、不再分裂、log 不刷。
9. **存讀檔**：分裂後存檔→讀檔 → records/spawnedFactions/進展/反叛者引用完整；中途存讀（反叛者被 redress 在場時存檔）不炸。
10. **同 def 多派系原版反應**（風險 1）：分裂後快轉數天，觀察 incident/商隊/任務對新派系的對待是否正常。
11. **缺姊妹 mod**：只開 faction-politics（無 sims-mode/npc-outposts）→ 載入乾淨、Compat 資料夾未載入、衛星判定恆 false 不炸。
12. **Rim War 簽名 dump**（若使用者屆時已裝）：log 出現 `ConvertSettlement 候選簽名` → 抄進 session_log 作校準素材。
13. **redress 在場分裂凍結**（風險 5）：反叛者在玩家地圖上時 progress 達標 → 分裂延後至離場，無敵我反轉異常。

## 已接受行為（觀察並記錄，不修）

- 自動首領殘留 world pawn（≤ 上限個數）。
- defeated 動態派系不回收（原版語意）。
- Rim War 戰力資料在校準前滯後（bridge no-op）。
