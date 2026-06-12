# V 全鏈驗證矩陣＋實機 E2E checklist

## 靜態驗證矩陣（每階段完成即跑；全綠才進下一階段）

| 檢查 | 命令 | 門檻 |
|---|---|---|
| P0(S0) 編譯 | `dotnet build named-officers/Source/NamedOfficers.csproj -c Release` | 0 警告 0 錯誤 |
| P0 健檢 | `python3 named-officers/tests/healthcheck.py` | OK（零 Harmony/零相依規則仍綠） |
| P1 零改動重編（A5）/小改重編（B3 後） | `dotnet build warband-generals/...` | 0/0；B3 diff <30 行 |
| P2 同上（A5／B3+B4 後） | `dotnet build settlement-lords/...` | 0/0；diff <30 行 |
| P3 重編（若 ref P0） | `dotnet build city-economy/...` | 0/0 |
| S1 編譯＋健檢 | `dotnet build personnel/...`＋`healthcheck.py` | 0/0＋OK |
| 各 mod healthcheck | 既有腳本 | 全 OK |

## 實機 E2E checklist（dev mode、新殖民地；入口 Debug actions → pas.officers / pas.personnel）

### E2E-A：S0 語意核心（A1–A5）

- [ ] 舊檔遷移：S0 前存檔載入 → dump 全 record status=Serving、不炸、無紅字。
- [ ] 在野態：`Create idle officer at selected` → status=Idle、faction=城派系、
      role=null、homeSettlement=該城、有 nameCached。
- [ ] 職涯轉換保關係：兩官 Offset opinion 互 -100 → 對其一 `Release`→`Employ` 到他城
      → id 不變、opinion 仍 -100、history 兩筆、appointedTick 更新。
- [ ] 升遷：`Promote` → role 換 GenericSenior、id 不變。
- [ ] quota：GenericSenior 設 quotaPerFaction=1 後對同派系第二人 Promote → false、log 無紅字。
- [ ] **主公敗亡不蒸發**：debug defeat 某派系 → 兩心跳內其全部 record 改隸各自居住城
      現任派系、status=Idle、`OfficerOrphaned` listener 印出 oldFaction、record 未刪。
- [ ] 流浪與回歸：毀光某 record 周邊聚落再 orphan → Wandering；建/發現新聚落 →
      兩心跳內轉 Idle 落籍。
- [ ] 橋泛化：在野者 Materialize → 拜訪居住城 → 同名 pawn 在場；離開後心跳
      `world=true forcedKeep=true`。
- [ ] 存讀：每 status 各留一筆 → 存讀 → dump 一致（含 history/appointedTick）。

### E2E-B：S1 玩法鏈（B1–B5）

- [ ] 撒種：新檔兩心跳內各 NPC 派系有在野（≈城數×1.5、≤cap）、全有名、零 pawn 具現。
- [ ] trickle：快進 10 天 → 向 cap 收斂、每心跳 ≤2 新增。
- [ ] **軍滅認領**：P1 將領 warband 被殲 → 將領轉 Idle、落自家最近城、恩怨保留；
      S1 停用重測 → 將領被 Remove（向後相容）。
- [ ] **城毀/易主認領**：毀城 → 太守落鄰城；RimWar 奪城 → 太守隨城改隸新主；
      玩家奪城 → 太守出走 NPC 城（永不入玩家派系）。
- [ ] 招攬：高政務在野＋無主城 → 數心跳內出仕該城（id 不變）；待命者優先回鍋；
      全員 opinion 砸 -100 → 不出山、P2 憑空保底照舊。
- [ ] 徵辟：通訊台 → 選項出現/灰態三案（敵對/冷卻/銀不足）→ 成功：扣銀、
      邊緣入場、同名、record 移除、goodwill -5、letter 可跳轉；冷卻存讀延續。
- [ ] RimWar 共存：其通訊台 4 選項與徵辟選項並存。

### E2E-C：UI（C2–C3）

- [ ] gizmo 顯隱：有料 NPC 城有「人才」、空城/玩家城無。
- [ ] 視窗：分區/欄位/著色/排序/捲動正確；開窗零 pawn 具現；30+ 筆不卡。
- [ ] tooltip：七維全示、履歷 ≤3、關係數值與⚔/♥標記正確；英文 keyed 無缺。
- [ ] 互動（C3）：徵辟鈕三態灰邏輯、成功後清單即時更新、與 B5 共用冷卻。
- [ ] 視窗開著時被毀城/易主（dev）→ 重新整理/關窗不炸（record 失效防護）。

### E2E-D：鐵則（全家族回歸）

- [ ] 中途裝/移除：S0 後舊檔、S1 中途裝（自動補種）、S1 移除後舊檔（僅 warning）。
- [ ] P1/P2/P3 既有 E2E 抽測各一條（將領戰力 postfix、太守治理成長、城防衛）無回歸。
- [ ] 全程 log 無紅字、WarnOnce 不洗版；快進 1 年無異常增長（record 數、world pawn 數
      ——dev count 前後對照）。

## 簽收規則（沿 P0 慣例）

任何一條失敗 → 回對應任務檔修，**不得帶傷簽收**；A5 與 B3 的 diff 行數證據
（`git diff --stat`）存任務 log；全綠後回寫各 mod PROJECT.md 的 E2E 章節。
