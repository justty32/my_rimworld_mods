# A2 Healer 改造：主公敗亡改隸不刪＋inhabitants 橋泛化＋流浪自癒

**Modify:** `Source/World/OfficerHealer.cs`、`Source/World/OfficerSpawner.cs`（SyncName 雙向化，見 s0-01）

## 現況與反特性

`OfficerHealer.Heal` 現行分支5（檔內第一個 if）：`record.faction == null → registry.Remove`
＝「主公敗亡→武將蒸發」。且 faction 是 Scribe_References，FactionManager 永不移除派系
→ 此分支實務上幾乎打不中（只防壞檔），**真正的敗亡訊號是 `Faction.defeated`，現碼完全沒看**。

## 新分支結構（重排後，逐分支對照）

```
Heal(registry, record):
  1. pawn 死亡        → 標記 dead → OfficerDied → 下輪移除          （不變，但移到最前：
                         死亡清理優先於改隸——人死了不用再安排出路）
  2. 主公敗亡 orphan  → record.faction == null || record.faction.defeated
                         → OrphanFlow(record)（改隸不刪，見下）       （取代舊分支5）
  3. Wandering 自癒   → status==Wandering → TryRehome(record)         （新增）
  4. world pawn 自癒  → PassToWorld/forced-keep 補回                   （不變）
  5. 宿主消失/易主    → Assign(null) + OfficerUnassigned               （不變；P1/P2 自有
                         綁定不依賴此分支，認領窗口在 P1/P2 自己的 Heal，見 s1-03）
  6. inhabitants 橋   → 泛化：assignedTo-Settlement「與」homeSettlement 都補鋪（擴）
  7. 不變式自癒       → status==Idle && (assignedTo!=null || role!=null)
                         → 修正 status=Serving + WarnOnce（容忍消費 mod 直寫，不炸）（新增）
```

## OrphanFlow（核心語意：人才隨城易主、主公敗亡不蒸發）

```csharp
private static void OrphanFlow(WorldComponent_OfficerRegistry registry, OfficerRecord record)
{
    Faction oldFaction = record.faction;
    Settlement home = ResolveHome(record);          // 見下
    if (home != null)
    {
        registry.MakeIdle(record, home);            // A3 積木：改隸 home.Faction＋卸職＋落籍＋status=Idle
    }
    else
    {
        registry.MakeWandering(record);             // 卸職＋status=Wandering；faction 保留舊值（00 語意框）
    }
    OfficersApi.RaiseOfficerOrphaned(record, oldFaction);   // 認領/信件消費窗口（S1 訂閱）
}
```

### `ResolveHome` 選址順序

1. `record.homeSettlement` 活著（非 Destroyed、Faction 非 null 非玩家、Faction 未 defeated）→ 用它。
2. `record.assignedTo is Settlement s` 且 s 滿足同條件 → 落籍任職地（s 已易主＝隨城改隸）。
3. 以 `homeSettlement?.Tile ?? assignedTo?.Tile` 為錨，掃 `Find.WorldObjects.Settlements`
   取 `ApproxDistanceInTiles` 最近的合格聚落（**排除玩家派系、defeated 派系**）。
4. 無錨點或全圖無合格聚落 → null（→ Wandering）。

### `TryRehome`（Wandering 自癒，每心跳一次，便宜）

無錨點流浪者：取全圖第一個合格聚落（決定論、免 Rand——心跳可能高頻呼叫）；
找到 → `MakeIdle(record, home)`＋廣播 `OfficerOrphaned(record, record.faction)`？
**不**——re-home 屬恢復非孤兒化，**靜默轉 Idle 即可**（事件語意保持乾淨）。

### 鐵則

- **永不指派玩家派系**（00 決策 5：殖民者不進 record 系統）。
- OrphanFlow 在心跳內逐 record 執行，**不得具現 pawn**（爆量鐵律——MakeIdle 純 record 操作）。
- defeated 判定每心跳都跑：派系「復活」（某些 mod 會 un-defeat）下一輪不再觸發即可，
  已轉在野者**不自動復職**（履歷已斷，合理）。

## inhabitants 橋泛化（分支6）

現碼只橋 `assignedTo is Settlement`；改為兩處都鋪：

```csharp
BridgeTo(record.assignedTo as Settlement, record.pawn);
BridgeTo(record.homeSettlement, record.pawn);          // 在野者：玩家拜訪居住城遇得到同一人
```

`OfficerSpawner.BridgeInhabitants`（Materialize 路徑）同步泛化。
注意：兩處可能是同一 Settlement（Employ 落籍任職地）→ `Contains` 冪等防重。

## 與既有消費者的互動（不破壞面分析）

- **P1**（warband 將領）：將領的 warband 宿主在派系敗亡時多半已被 RimWar Destroy →
  P1 自己的 Heal 分支3 先走 RemoveOfficer（這正是 s1-03 要堵的洩漏點）；
  若 record 倖存到本 orphan 流程，P1 `GeneralOf` 查 `GetById` 仍命中（id 不變），無害。
- **P2**（聚落領主）：城易主走 P0 分支5（不變）＋ P2 自己的 Heal 分支4
  （LordLostSettlement→Remove）；本任務不動該路徑，認領在 s1-03。
- **舊檔**：defeated 派系遺留的 Serving record（舊版被刪過的不會回來）下一心跳起改隸——
  行為改變屬預期特性，列入 E2E。

## 驗證

1. build 0 警告 0 錯誤；healthcheck 通過。
2. dev（A4 新增 debug action 配合）：`Orphan-simulate first officer`（強制設其 faction.defeated
   或直接呼 OrphanFlow）→ dump：faction 改為居住城派系、status=Idle、role=null、
   serviceHistory 多一筆、record 未刪、`OfficerOrphaned` listener 有印。
3. 毀掉 homeSettlement 後再 orphan → 改落最近合格聚落；全圖無合格聚落（測試世界）→ Wandering；
   之後手動建聚落 → 兩心跳內 TryRehome 轉 Idle。
4. 在野者具現 → 拜訪其居住城 → 同名 pawn 在場（inhabitants 橋泛化走通）。
5. 全程 log 無紅字；WarnOnce 不洗版。
