# Task 0: 殘餘 API 簽名驗證（grep 反編譯源）

可行性調查已驗證主鏈（見計畫索引表），本 task 補齊實作會直接呼叫、但尚未逐字核對簽名的 8 項。**全部用 Grep 對 `C:\code\mine\pas\projects\rimworld` 查，結果記入 `faction-politics/session_log.md`。**

**Files:**
- Create: `faction-politics/session_log.md`（記錄驗證結果）

- [ ] **Step 1: 驗證 8 項簽名**

| # | 目標 | 查法 | 預期/用途 |
|---|---|---|---|
| 1 | `Faction.GoodwillToMakeHostile(Faction)` 回傳型別與語意 | grep `GoodwillToMakeHostile` in `RimWorld\Faction.cs` -A 10 | int 差值；task-4 餵給 TryAffectGoodwillWith |
| 2 | `Faction.TryAffectGoodwillWith` 完整參數表 | grep `public bool TryAffectGoodwillWith` -A 5 | 確認 `canSendMessage`/`canSendHostilityLetter` 參數名與位置（可行性 02 引用的呼叫形要能編譯） |
| 3 | `FactionGeneratorParms` 建構子 | grep `public FactionGeneratorParms` in `RimWorld\FactionGeneratorParms.cs` -A 8 | `(FactionDef, IdeoGenerationParms, bool? hidden)`（BackCompatibility.cs:424 用例已見 layer 多載） |
| 4 | `PawnGenerationRequest` 最簡建構形 | grep `public PawnGenerationRequest(` in `Verse\PawnGenerationRequest.cs` -A 15 | `(PawnKindDef, Faction, PawnGenerationContext, ...)` 其餘可選；task-3 生成反叛者 |
| 5 | `WorldPawns.PassToWorld` / `Contains` / `RemovePawn` 可見性 | grep 各名稱 in `RimWorld.Planet\WorldPawns.cs` | 全 public（PawnGenerator.cs:218 已見 RemovePawn 用例）；task-3/5 |
| 6 | `WorldPawns` 有無安全棄置 API（`RemoveAndDiscardPawnViaGC` 或同義） | grep `Discard` in `RimWorld.Planet\WorldPawns.cs` | 有→task-4 清自動首領；無→接受殘留（spec §10 已預准） |
| 7 | `FactionDef.basicMemberKind` 欄位存在 | grep `basicMemberKind` in `RimWorld\FactionDef.cs` | task-3 反叛者 kind 來源；缺值 fallback 邏輯已寫 |
| 8 | `GenTypes.GetTypeInAnyAssembly(string)` 存在 + `LetterStack.ReceiveLetter` 參數表 | grep in `Verse\GenTypes.cs` / `RimWorld\LetterStack.cs` | task-6 偵測 Rim War；task-3/4 發信 |

- [ ] **Step 2: 建 session_log.md 記錄結果**

格式照 npc-outposts/session_log.md：日期標題 + `### Task 0: API 殘餘驗證` + 逐項「命中/修正」清單。任何簽名與計畫程式碼不符 → **當場改後續 task 檔的程式碼**並在 log 標注（先改計畫再實作，不留偏差）。

- [ ] **Step 3: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add faction-politics/session_log.md
git -C C:\code\mine\my_rimworld_mods commit -m @'
docs: faction-politics Task 0 API 殘餘驗證記錄

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
