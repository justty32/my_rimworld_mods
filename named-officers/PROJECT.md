# named-officers（具名職官層）

**一句話**：把 faction-politics 的「單一反叛者」管線泛化為通用具名職官基礎層——
record + 註冊表 + 懶生成 + 七維屬性 + 關係雙軌 + 對外 API。
**本身零玩法、零 Harmony、零硬相依**（不 ref RimWar/Empire/sims-mode），純 RimWorld 1.6 API。
消費者：P1 warband-generals、P2 settlement-lords、P4 faction-politics（三國志 mod 家族）。

- packageId：`pas.officers.community`（G1 決議）
- Assembly / RootNamespace：`NamedOfficers` / `pas.officers`
- defName/key 前綴：`pas_officers_`
- 計畫出處：`_ideas/sanguo-plan/plans/p0-named-officers/`（設計：`_ideas/sanguo-plan/02-mod-named-officers.md`）

## 對外 API 契約（`OfficersApi`，唯一入口；P1 開工依據）

所有方法 null-safe：registry/參數 null → 回 null/空/false。回 `OfficerRecord` 本體不回 DTO，
消費 mod hard-ref `1.6/Assemblies/NamedOfficers.dll` 直讀欄位；**寫入一律走 API**
（保 index 與 opinion 鍵一致）。

| 成員 | 簽章 | 說明 |
|---|---|---|
| GetOfficers | `IReadOnlyList<OfficerRecord> GetOfficers(WorldObject host)` | host 上全部職官；永不 null |
| GetOfficer | `OfficerRecord GetOfficer(WorldObject host, OfficerRoleDef role)` | 首個符合角色 |
| GetById | `OfficerRecord GetById(int id)` | record id 查詢 |
| CreateOfficer | `OfficerRecord CreateOfficer(Faction faction, WorldObject host, OfficerRoleDef role)` | 建 record 不具現；超 `maxOfficersPerObject`(4)/參數壞 → null；host 可 null＝待命 |
| AssignOfficer | `void AssignOfficer(OfficerRecord r, WorldObject newHost)` | 調動；newHost 可 null |
| RemoveOfficer | `void RemoveOfficer(OfficerRecord r)` | 含他人 opinion 鍵清理 |
| Materialize | `Pawn Materialize(OfficerRecord r)` | 按需具現（KeepForever + inhabitants 橋）；失敗 null，record 保持輕量態 |
| SetAttribute | `void SetAttribute(OfficerRecord r, OfficerAttribute attr, int value)` | clamp 0-100 |
| GetAttribute | `int GetAttribute(OfficerRecord r, OfficerAttribute attr)` | enum：Might/Command/Polity/Charisma/Loyalty/Intellect/Morale |
| GetOpinion | `int GetOpinion(OfficerRecord a, OfficerRecord b)` | B 軌 a→b（非對稱）；缺鍵 → A 軌 bias |
| OffsetOpinion | `void OffsetOpinion(OfficerRecord a, OfficerRecord b, int delta)` | 事件脈衝；心跳回歸 bias；clamp ±100 |
| AddPersistentRelation | `bool AddPersistentRelation(OfficerRecord a, OfficerRecord b, PawnRelationDef def)` | A 軌；未具現端按需具現（G4） |
| OfficerCreated | `event Action<OfficerRecord>` | record 建立後（pawn 未具現） |
| OfficerDied | `event Action<OfficerRecord>` | pawn 死亡、record 標記 dead；廣播後下一輪心跳清理（G5 遺言窗口） |
| OfficerUnassigned | `event Action<OfficerRecord>` | 宿主 null/Destroyed/易主，record 轉待命留存 |

`OfficerRecord` 公開欄位：`id, faction, assignedTo, role, pawn, nameCached, might, command,
polity, charisma, loyalty, intellect, morale, opinions(Dictionary<int,int>, key=對方 record id), dead`；
`DisplayName` = pawn 名 → nameCached → role label fallback。

出貨 Def：`pas_officers_Generic`（OfficerRoleDef，測試用）、`pas_officers_Settings`
（OfficersSettingsDef：checkIntervalTicks=2500 / maxOfficersPerObject=4 /
opinionDriftPerHeartbeat=1 / initialAttributeRange=20~80）、
`pas_officers_SwornBrother`(+60) / `pas_officers_BloodFeud`(-60)（PawnRelationDef，reflexive）。

## 消費者指南（P1/P2/P4）

1. csproj 加 `<Reference Include="NamedOfficers">`，HintPath 指
   `../../named-officers/1.6/Assemblies/NamedOfficers.dll`，`Private=false`。
2. About.xml 加 `loadAfter: pas.officers.community`（modDependencies 視需要）。
3. 自訂角色：XML 增訂 `pas.officers.OfficerRoleDef`（如 lord/general），呼
   `CreateOfficer(faction, hostWorldObject, yourRoleDef)`。**本層不自動鋪官**（G6）——
   消費 mod 在自己的心跳裡補官。
4. inspect 顯示：把 `pas.officers.WorldObjectCompProperties_Officers` 注入目標
   WorldObjectDef 的 `comps`（XML patch 或消費 mod 自己的補丁手段；本層不注入任何 def，G3）。
   comp 無狀態、不 scribe，只讀 registry。
5. 玩法數值（GovernanceFactor、戰力公式、事件式 opinion 漲跌）住消費 mod；
   本層只保證屬性可讀可寫、關係骨架演化（B 軌每 2500-tick 向 A 軌 bias 回歸 1 步）。
6. 死亡：訂 `OfficerDied` 在遺言窗口（一個心跳）內做繼任/復仇；之後 record 自動移除。

## 計畫 G1–G6 決議備查

| # | 決議 |
|---|---|
| G1 | packageId = `pas.officers.community`（家規 `pas.X.community`） |
| G2 | 七維 typed int 全建，MVP 啟用五維（武力/統率/政務/魅力/忠誠），智力/士氣預留 |
| G3 | record 為屬性唯一真相；P0 只出貨無狀態 view comp 型別，注入由消費 mod 自做 |
| G4 | A 軌需真 Pawn → AddPersistentRelation 對未具現職官按需具現後再建 |
| G5 | 死亡 = record 標記 + OfficerDied 廣播，下一心跳移除；繼任邏輯留給消費 mod |
| G6 | maxOfficersPerObject 預設 4；只 dev/API 觸發生成，P0 不自動鋪官 |

另：B 軌 dict key 用 **record id**（非 pawnId——pawn 懶生成/死亡換體不斷鍵）；
名字策略採方案 B（建 record 時顯示 role label，首次具現快取 `pawn.Name` 進 `nameCached`）。

## 驗證

```bash
dotnet build Source/NamedOfficers.csproj -c Release   # 0 警告 0 錯誤
python3 tests/healthcheck.py                          # healthcheck OK
```

## 實機 E2E checklist（T9b——**尚未實機簽收**，照 09-roadmap Phase 0）

環境：dev mode、新殖民地、任一 NPC 派系聚落可見；debug 入口 `Debug actions → pas.officers`。

- [ ] 生 record（驗收 1）：選中 NPC 聚落 → `Create officer at selected` ×2 →
      `Dump officer registry` 顯示 2 officer、id 遞增、七維在 20~80 內。
- [ ] 屬性存讀（驗收 2）：`Roll attributes` 記值 → 存檔 → 讀檔 → dump 數值一致。
- [ ] 同一 pawn 請回（驗收 3）：`Materialize first officer` 記 pawn 名 → caravan 拜訪該聚落 →
      地圖同名 pawn → 離開 → 下一心跳 dump：`world=true forcedKeep=true inhabitantsList=true`。
- [ ] 關係演化（驗收 4）：`Add sworn brothers` → 推進時間 → opinion 從 0 向 +60 收斂；
      `Offset opinion -100` → 回歸方向正確；存讀檔曲線延續。
- [ ] 自癒（鐵則）：`Kill first officer pawn` → 兩輪心跳內 dead→OfficerDied→record 移除
      （先 `Toggle event log listeners`）；毀宿主 → OfficerUnassigned 印出、record 留存。
- [ ] 中途裝/移除（鐵則）：無本 mod 舊檔載入不炸；停用 mod 後載有 officer 的檔只有 warning。
- [ ] 全程 log 無紅字、無 WarnOnce 噴發。

任何一條失敗 → 回對應任務檔修，不得帶傷簽收。
