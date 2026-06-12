# A3 API 層：Release/Employ/SetRole/MakeIdle/CreateIdle＋三事件＋查詢

**Modify:** `Source/OfficersApi.cs`、`Source/World/WorldComponent_OfficerRegistry.cs`

## 鐵則：禁走 Remove+Create

`Registry.Remove` 會把該 id 從**全網**他人 `opinions` dict 清鍵——跳槽/升遷若走
Remove+Create，新 record 換 id、恩怨歸零、A 軌 pawn 也斷。
**所有職涯轉換一律就地改欄位**；本任務的 API 是唯一合法寫入路徑（沿 P0 門面壟斷原則）。

## 新 API（OfficersApi 增量；全部 null-safe 沿例）

| 成員 | 簽章 | 語意 |
|---|---|---|
| CreateIdleOfficer | `OfficerRecord CreateIdleOfficer(Settlement home)` | 在野人才出生：faction=home.Faction、status=Idle、role/assignedTo=null、homeSettlement=home；home null/玩家城/defeated → null。**不受 maxOfficersPerObject 限**（那是宿主官數上限；在野上限由 S1 settings 管）。屬性照 settings 範圍擲。觸發 `OfficerCreated` |
| ReleaseOfficer | `void ReleaseOfficer(OfficerRecord r)` | 下野：**留派系、留居籍**，卸官職（push history、role=null）、Assign(null)、status=Idle、觸發 `OfficerReleased` |
| EmployOfficer | `bool EmployOfficer(OfficerRecord r, Faction f, WorldObject host, OfficerRoleDef role)` | 仕官/跳槽/復職：quota 檢查（A4）不過 → false；過 → faction=f、Assign(host)、role=role、status=Serving、appointedTick=now、host 是 Settlement → homeSettlement=host（落籍任職地）；觸發 `OfficerEmployed`。**id/opinions 不動＝恩怨保留** |
| SetRole | `bool SetRole(OfficerRecord r, OfficerRoleDef newRole)` | 升遷/降職就地換 def：quota 檢查；push history、role=newRole、appointedTick=now。不觸發事件（同主調職非職涯斷點；要事件由消費 mod 自廣播） |
| MakeIdle | `void MakeIdle(OfficerRecord r, Settlement home)` | **底層積木**（00 決策 2）：faction=home.Faction、homeSettlement=home、push history、role=null、Assign(null)、status=Idle。**不觸發事件**——orphan 流程包 Orphaned、認領橋包語意、ReleaseOfficer 包 Released |
| GetUnaffiliated | `IReadOnlyList<OfficerRecord> GetUnaffiliated(Faction f)` | f 的在野人才（status==Idle && !dead）；永不 null |
| GetByFaction | `IReadOnlyList<OfficerRecord> GetByFaction(Faction f)` | f 的全部 record（含 Serving/Idle）；永不 null |
| GetIdleAt | `IReadOnlyList<OfficerRecord> GetIdleAt(Settlement home)` | 居住於 home 的在野人才（UI 主查詢）；永不 null |

Registry 內部對應 `MakeIdle`/`MakeWandering`/`CreateIdle` internal 方法（Healer A2 直呼
registry 版避免事件遞迴；OfficersApi 版做 null 守衛＋事件）。

### 查詢實作決策：線性掃描，不建 byFaction 索引

record 量級（每城 ≤4 官＋ ≤3 在野 × 數十城 ≈ 數百筆）線性掃 O(n) 在心跳/UI 開窗頻率下
無感；省掉「faction 變更時索引維護」整類 bug 面（Employ/orphan 都改 faction，索引極易漏）。
回傳快照 List（非活表）防呼叫端遍歷時 registry 變動。**若未來 record 破千再回頭加索引**（backlog）。

## 三事件（沿 P0 Raise 隔離範式：訂閱者例外逐一 try/catch）

```csharp
/// 主公敗亡改隸（OrphanFlow 廣播）。oldFaction = 敗亡的舊主。
public static event System.Action<OfficerRecord, Faction> OfficerOrphaned;
/// 仕官/跳槽完成（EmployOfficer 廣播）。
public static event System.Action<OfficerRecord> OfficerEmployed;
/// 下野（ReleaseOfficer 廣播）。
public static event System.Action<OfficerRecord> OfficerReleased;
```

- `RaiseOfficerOrphaned` internal 供 Healer 呼叫（沿 RaiseOfficerDied 形）。
- Orphaned 帶 oldFaction：S1 信件要寫「舊主◯◯敗亡」、未來 P4 叛變鏈要查波及面。
- 事件皆在主執行緒心跳/API 呼叫內同步觸發（與既有三事件同保證）。

## 與既有 API 的相容面

- `CreateOfficer(faction, host, role)` 簽章不變（P1/P2 既有呼叫零改動）；
  內部補 `status=Serving`、`appointedTick=now`、host is Settlement → homeSettlement=host。
- `AssignOfficer` 不變（純調動，不碰 status——待命仍屬 Serving）。
- `RemoveOfficer` 不變（仍是「真正退場」的唯一出口：死亡清理、徵辟離隊）。
- `Materialize`/關係 API 不變。

## 驗證

1. build＋healthcheck 過。
2. dev action 序列（A4 提供）：
   `CreateIdle at selected` → dump：status=Idle、faction=城派系、role=null。
   `Employ first idle to selected`（用 Generic role）→ dump：Serving、appointedTick>0、
   homeSettlement=該城、**id 不變**；對其先 `Offset opinion -100` 再 Employ → opinion 仍在。
   `Release first officer` → Idle、role=null、history+1、faction 不變。
   `SetRole`（A4 加第二測試 role）→ role 換、history+1、id 不變。
3. null-safety probe（A4 擴充）全綠。
4. 存讀檔往返：上述每態各留一筆 → 讀檔 dump 一致。
