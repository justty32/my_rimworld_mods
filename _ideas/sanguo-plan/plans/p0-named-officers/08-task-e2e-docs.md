# T9 — 實機 E2E 驗證 ＋ Languages ＋ 文件（0.5d）

## T9a — Languages（npc-outposts 慣例：English + ChineseTraditional Keyed）

**Create:** `Languages/English/Keyed/NamedOfficers.xml`、
`Languages/ChineseTraditional/Keyed/NamedOfficers.xml`

P0 對玩家可見文字極少：view comp 的 inspect 行（如 `pas_officers_InspectLine`）、
角色 label。所有 `Translate()` key 進 healthcheck 第 5 項防呆。

## T9b — 實機 E2E（對 `09-roadmap.md` Phase 0 驗收逐條簽收）

環境：dev mode、新殖民地、任一 NPC 派系聚落可見。

1. **生 record**（驗收 1）：選中 NPC 聚落 → `Create officer at selected` ×2 →
   `Dump officer registry` 顯示 2 officer、id 遞增、七維在 `initialAttributeRange` 內。
2. **屬性存讀**（驗收 2）：`Roll attributes` 記下數值 → 存檔 → 退主選單 → 讀檔 →
   dump 數值一致。
3. **同一 pawn 請回**（驗收 3）：`Materialize first officer` 記 pawn 名 →
   caravan 拜訪該聚落（或 Sims Mode 若有裝——非依賴，僅驗證途徑）→
   地圖上找到同名 pawn → 離開 → 下一心跳 dump：`world=true forcedKeep=true inhabitantsList=true`。
4. **關係演化**（驗收 4）：`Add sworn brothers` → dev `Advance 1 day` 數次（原版 debug 加速）→
   dump 看 opinion 從 0 向 +60 收斂；`Offset opinion -100` → 再推進 → 確認回歸方向正確；
   存讀檔後曲線延續不重置。
5. **自癒**（鐵則）：`Kill officer pawn` → 兩輪心跳內 dump 顯示 dead→record 消失、
   `OfficerDied` listener log 有印；dev 毀宿主物件 → `OfficerUnassigned` 印出、record 留存。
6. **中途裝/移除**（鐵則）：舊檔（無本 mod 的存檔）載入不炸；
   建好 officer 的檔在停用 mod 後載入只有 warning。
7. 全程 log 無紅字、無 `WarnOnce` 噴發。

任何一條失敗 → 回對應任務檔修，**不得帶傷簽收**（faction-politics E2E 教訓：
redress 清 forced-keep 這類副作用只在實機現形）。

## T9c — 文件（家規：PROJECT.md + session_log.md）

**Create:**
- `named-officers/PROJECT.md`：mod 一句話、對外 API 契約表（`OfficersApi` 全簽章）、
  消費者指南（P1/P2 如何 ref DLL、loadAfter、view comp 注入示例說明——文字非代碼）、
  G1–G6 決議備查。
- `named-officers/session_log.md`：起頭記 P0 實作 session。
- 回寫 `_ideas/sanguo-plan/02-mod-named-officers.md` 增補一行指向本計畫與 G1–G6 決議
  （設計檔是權威，決議要回流——此為**唯一**動到既有檔案處，且僅追加備註）。

## 完成定義（P0 全期 done）

- [ ] T0–T9 全勾、build 0 警告 0 錯誤、healthcheck OK、E2E 七條全簽。
- [ ] `1.6/Assemblies/NamedOfficers.dll` 為資料夾唯一 DLL。
- [ ] PROJECT.md API 契約表與源碼一致（P1 開工的依據）。
- [ ] P1（warband-generals）可開工條件：能 `ref NamedOfficers.dll` 並
      `CreateOfficer(faction, warbandWorldObject, generalRole)` 不需改 P0 任何一行。

## Commit

`feat: named-officers P0 完成（E2E 簽收 + 文件 + languages）`
