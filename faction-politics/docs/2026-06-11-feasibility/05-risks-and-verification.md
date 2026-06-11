# 05 風險分級與實機待驗證清單

## 已從源碼確證、無需實機驗證

- 途中造派系全流程（`02`）、redress 供給鏈全環（`03`）、`IfModActive` 條件載入（`04`）。
- `SetRelationDirect` goodwill 防呆、`HasGoodwill` 的 hidden 條件、`AllFactionsVisible` 即時 filter、`leader`/`hidden` public 可賦值。

## 風險分級

### 高（設計已繞開）
- ~~關係 N² / 真刪派系 / temporary 權衡~~ → 原版量級 + 不真刪 + 上限 5，全數繞開（`01`）。

### 中（實機驗證點）
1. **同 def 多派系的原版反應**：分裂用母派系同一個 FactionDef 造新派系。原版世界生成本就允許同 def 多派系（部落/海盜常見多個），但「途中新增」後的 incident 派系抽選、商隊、任務生成是否正常對待——**實機驗**。
2. **hidden 生成→揭示的完整性**：揭示後派系列表 UI、通訊台、外交頁是否完整顯示；ideo/名字/圖標是否正常——**實機驗**。
3. **`GoodwillToMakeHostile` + `TryAffectGoodwillWith` 的敵對落地**：kind 翻轉、雙向 hostile、無重複信——**實機驗**（API 存在性與簽名於計畫 task-0 先靜態核對）。
4. **redress 實際命中率**：拜訪倒戈前的駐地聚落，反叛者是否如預期出現在場上（守軍生成數量有限，清單唯一條目應必中，但 `WorldPawnSelectionWeight` 權重行為未細查）——**實機驗**。
5. **反叛者 spawn 在地圖上時的世界狀態**：redress 後他暫時不是 world pawn（PawnGenerator.cs:218）；玩家此時觸發分裂（progress 達標）→ `SetFaction` 對在場 pawn 的效果（在場敵我反轉）——**實機驗**，必要時分裂前置條件加「反叛者不在任何地圖上」。

### 低
6. 自動首領殘留 world pawn（≤5 個，`02`）——觀察即可。
7. 分裂 letter 與原版敵對通知的重複度——觀察調整 flag。
8. PlanetLayer 固定 Surface——軌道層派系（Odyssey）不在 P1，遇到非 Surface 母派系直接跳過追蹤。

### 待外部輸入
9. **Rim War `ConvertSettlement` 簽名**：等使用者提供 Rim War DLL/反編譯源後校準反射綁定（`04`）。在那之前 bridge 是防呆 no-op 骨架。
10. Empire Refactor 參考（使用者另案承諾）與本案無直接耦合；若未來「哨站增益母聚落」系統落地，分裂時的數值轉移屬該案範圍。

## E2E 驗證項預告（計畫 task 收錄完整清單）

新檔開局反叛者就位 → 拜訪駐地聚落見到反叛者 → 殺死反叛者進展歸零+冷卻重生 → 放任進展達標 → 分裂 letter + 新派系入列 + 聚落/哨站易主 + 母新敵對 → 存讀檔（record/進展/動態派系全保留）→ 上限觸頂後不再分裂 → 無 Rim War 環境 log 乾淨。
