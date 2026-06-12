# B3 人才守恆：P1/P2 認領窗口（各 <30 行）＋ S1 認領橋

## 問題（調查 P：三處無情 RemoveOfficer ＝人才守恆洩漏點）

| 洩漏點 | 現碼 | 現行為 |
|---|---|---|
| 軍滅 | `warband-generals/Source/World/WorldComponent_WarbandGenerals.cs` `Heal` 分支3 | warband 覆滅/解散 → `RemoveOfficer`，將領蒸發 |
| 城毀 | `settlement-lords/Source/World/WorldComponent_SettlementLords.cs` `Heal` 分支3 | 城 Destroyed → `RemoveOfficer`，太守蒸發 |
| 城易主 | 同上 `Heal` 分支4 | `LordEvents.RaiseLordLostSettlement` 後 `RemoveOfficer` |

三國志語意：敗軍之將/亡城之守應**轉在野**（隨城改隸/落籍他城），活著就還在棋盤上。

## P1/P2 小改（向後相容：hook=null → 行為與現狀完全一致）

### P1 `warband-generals`（新檔 + 3 行改動，合計 <30 行）

```csharp
// 新檔 Source/GeneralsHooks.cs（~10 行）
namespace pas.officers.warband
{
    /// <summary>軍滅退場攔截：回 true＝有人認領（跳過 RemoveOfficer），false/null＝原行為。</summary>
    public static class GeneralsHooks
    {
        public static System.Func<OfficerRecord, WorldObject, bool> ExitInterceptor;
    }
}

// Heal 分支3 改（WorldComponent_WarbandGenerals.cs）：
if (host == null || (host.Destroyed && !GeneralsUtility.InActiveBattle(host)))
{
    if (GeneralsHooks.ExitInterceptor?.Invoke(record, host) != true)   // ★新增這一行
        OfficersApi.RemoveOfficer(record);
    Unbind(binding);                                                   // 認領與否都解綁
}
```

### P2 `settlement-lords`（hook 加進既有 `LordEvents.cs` + 6 行改動）

```csharp
// LordEvents.cs 增（~6 行）：
public static System.Func<OfficerRecord, Settlement, bool> ExitInterceptor;

// Heal 分支3（城毀）與分支4（易主）各改一處：
//   分支4 順序保持：先 RaiseLordLostSettlement（P4 叛變窗口優先）→ 再問 ExitInterceptor
if (LordEvents.ExitInterceptor?.Invoke(record, host) != true)
    OfficersApi.RemoveOfficer(record);
Unbind(binding);
```

interceptor 例外面：P1/P2 的 Heal 已包 try/catch（心跳 WarnOnce）——S1 端 handler 內仍
自包 try/catch 回 false（雙保險，不讓 S1 的 bug 卡死 P1/P2 清理）。

## S1 認領橋（`Source/Bridges/Bridge_WarbandGenerals.cs` / `Bridge_SettlementLords.cs`）

`PersonnelMod` ctor：`claimEnabled && ModLister.GetActiveModWithIdentifier(...)` → `Init()`。

```
Claim(record, host):                       # 共用核心（PersonnelUtility）
    try:
        home = ResolveClaimHome(record, host)
        if home == null: return false      # 找不到去處 → 放行原 Remove（保守，不造流浪潮）
        OfficersApi.MakeIdle(record, home) # 改隸 home.Faction＋卸職＋落籍（A3 積木）
        OfficerNamer.EnsureNameCached(record)
        節流信件（僅玩家可見派系/曾交手者，見下）
        return true
    catch: WarnOnce; return false
```

### `ResolveClaimHome` 選址（與 A2 ResolveHome 同骨架、語境化）

- **易主（P2 分支4）**：host 本身（活城、新主非玩家非 defeated）→ **人才隨城易主**；
  host 不合格（玩家奪城！）→ 以 host.Tile 為錨找最近合格城（人才出走）。
- **城毀（P2 分支3）／軍滅（P1 分支3）**：以 `host?.Tile`（可得時）為錨，
  優先 `record.faction` 自家最近聚落（敗將歸國），無 → 最近任意合格聚落（流落他鄉，
  faction 隨之改隸），全無 → null 放行。
- 落籍城在野已滿 `maxIdlePerSettlement` → 找次近（至多探 3 城，再不行 null 放行）。

### 亡國信件（OfficerOrphaned 訂閱，同檔順帶）

S1 `WorldComponent_Personnel` 訂 `OfficersApi.OfficerOrphaned`（A3 事件）：
舊主與玩家 goodwill ≠ 0（打過交道）才發一封彙整信（每派系敗亡事件節流成 1 封，
列出轉在野名單），用 `LetterDefOf.NeutralEvent`。MVP 可只 log+letter，不做選項。

## 驗證

1. P1/P2 各自重編 0 警告 0 錯誤；**diff 行數 <30/mod**（git diff --stat 截圖留檔）；
   P1/P2 healthcheck 過。
2. S1 停用時：P1/P2 行為與改動前完全一致（hook=null 路徑；實機毀一個有將 warband →
   record 被 Remove，同現狀）。
3. S1 啟用：dev 毀有將 warband → 兩心跳內將領轉 Idle、落籍自家最近城、id 不變、
   恩怨 dict 保留；毀有主之城 → 太守落籍鄰城；用 RimWar 讓城易主 →
   太守 faction 變新主、homeSettlement=原城（隨城易主）。
4. 玩家奪城：太守**不**落玩家派系（出走最近 NPC 城）。
5. 亡國（debug defeat 派系）：orphan 信件一封、名單正確、log 無紅字。
