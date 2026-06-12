# T2 — 生命週期：綁定 WorldComponent＋心跳指派＋易主/被毀處置

## 產出

- `Source/World/WorldComponent_SettlementLords.cs`（綁定儲存＋heal 心跳，<200 行）
- `Source/World/LordBinding.cs`（IExposable 綁定單元）
- `Source/World/LordAppointer.cs`（指派掃描，拆檔守 200 行線）
- `Source/LordEvents.cs`（P4 消費窗口的 static event）

## LordBinding（IExposable，仿 P1 GeneralBinding）

- `Settlement host`（`Scribe_References`——Settlement 在世界圖、ref 永遠可解析；
  Saving 時 host 已 Destroyed → 以 null 寫出防 unresolved-ref 警告，load 後心跳補退場）
- `int recordId`（record 本體由 P0 registry 深存、唯一真相；本 mod 只存 id 懶解析）

## WorldComponent_SettlementLords

- 心跳 2500 tick、offset **600**（錯開 P0 的 0、P1 的 1200、RimWar update 的 0）。
- `bindings:List<LordBinding>`（scribe Deep）＋ `byHost:Dictionary<Settlement,LordBinding>`
  執行期索引（FinalizeInit/PostLoadInit 重建，不 scribe）。
- `LordOf(Settlement)`：查索引→`OfficersApi.GetById`；record 已被 P0 清 → 順手解綁回 null。
- `BindingsSnapshot()`：回 `LordBinding[]`（growth postfix 在背景執行緒讀，給 snapshot 不給活表）。
- `Bind(host, record)`：先清同 record 舊綁定、再清同 host 既有綁定（防雙主），落表＋索引。

### 心跳兩段

**1) Heal（逐綁定，倒序，例外逐筆隔離 WarnOnce）**
```
record = GetById(recordId)
record == null            → 解綁                    // P0 已清（pawn 死亡 G5 收尾）→ 城變無主，
                                                    // 之後掃描自然補新太守＝繼任
record.dead               → 不動                    // 遺言窗口，P0 下一心跳清 → 走上一分支
host == null || Destroyed → RemoveOfficer＋解綁     // 城亡人去
host.Faction != record.faction（易主）
                          → LordEvents.RaiseLordLostSettlement(record, host)
                            再 RemoveOfficer＋解綁   // 預設政策退場；P4 在事件裡接走可自保留
```
注意順序：本 mod offset 600 在 P0 heal（offset 0）**之後**——易主當輪 P0 已把 record 轉待命
（assignedTo=null）並廣播 OfficerUnassigned，但 record 仍在 registry、`GetById` 仍命中，
本 mod 的易主分支照常觸發（判據用 `host.Faction != record.faction`，不依賴 assignedTo）。

**2) 指派掃描（節流）**
```
lordChance<=0 → return；LordRole 缺 def → WarnOnce 降級 return
foreach Find.WorldObjects.Settlements：
  跳過：Destroyed / Faction null 或 IsPlayer / byHost 已綁 /
        GetComponent<RimWarSettlementComp>()==null /
        GetRimWarDataForFaction(faction)==null 或 behavior Player/Excluded
  Rand.Chance(lordChance) 不中 → 跳過（下輪再試；機率＝上任速度）
  record = OfficersApi.CreateOfficer(s.Faction, s, LordRole)   // G6 上限滿回 null → 安靜跳過
  Bind(s, record)；本心跳建滿 MaxNewLordsPerHeartbeat(5) → break
```

## LordEvents

`public static event Action<OfficerRecord, Settlement> LordLostSettlement`——易主時、
RemoveOfficer **之前**廣播（訂閱者例外逐一隔離，仿 OfficersApi.Raise）。P4 叛變消費。

## 驗收

build 0/0；healthcheck OK；dev 模式下推進時間可見 NPC 聚落陸續掛領主
（`Dump officer registry` 顯示 role=pas_settlement_Lord、assignedTo=該城）。
