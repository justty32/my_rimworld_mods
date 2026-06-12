# B4 NPC 招攬 MVP：P2 `CandidateProvider` hook ＋ S1 willing 公式 provider

## 設計核心（00 決策 3）

**缺位×在野配對「寄生」P2 既有 `LordAppointer.Scan`**——P2 心跳本來就在掃無主城，
S1 不另開配對迴圈（避免兩套 vacancy 真相）。P2 加一個 static hook，S1 註冊三段式 provider：

```
P2 Scan 遇無主合格城 →
  ① CandidateProvider 回「待命職官」（Serving、assignedTo=null、同派系）→ 復職
  ② 否則回「在野人才」（willing 公式選出）→ 出仕
  ③ 否則回 null → P2 照舊 CreateOfficer 憑空保底（lordChance 機率不變）
```

「庸主佔位、能臣在野」由 willing 公式摻 opinion/魅力自然湧現（調查 P）；
升遷/罷免/挖角＝S2（遲滯帶防震盪一併留 S2）。

## P2 小改（<30 行，與 B3 合計仍各 mod <30 行——若超，B3/B4 拆兩個 commit 分開計）

```csharp
// LordAppointer.cs 增 hook（~4 行）：
/// <summary>外部人事供應：回該城太守人選（待命/在野）；null＝由本 mod 憑空保底。</summary>
public static System.Func<Settlement, OfficerRoleDef, OfficerRecord> CandidateProvider;

// Scan 內 CreateOfficer 處改（~8 行）：
OfficerRecord record = CandidateProvider?.Invoke(s, role);
if (record != null)
{
    if (!OfficersApi.EmployOfficer(record, s.Faction, s, role)) record = null;  // quota 等失敗回退
}
if (record == null) record = OfficersApi.CreateOfficer(s.Faction, s, role);     // 原行為保底
if (record != null) { lords.Bind(s, record); created++; }
```

注意：provider 路徑走 `EmployOfficer`（id 不變、恩怨保留、appointedTick 落筆、
homeSettlement 落籍）；保底路徑維持 `CreateOfficer`（行為與現狀位元級一致）。
`Scan` 在 WorldComponentTick 主執行緒（非 RimWar tasker 背景線）→ hook 可安全用 Rand。

## S1 provider（`Bridge_SettlementLords.Init` 註冊；`npcStaffingEnabled` 守衛）

```
Provide(settlement, role):
    try:
        faction = settlement.Faction
        # ① 待命職官（曾任官、宿主沒了）優先回鍋
        standby = GetByFaction(faction) 中 status==Serving && assignedTo==null
                  && !dead && role 相容（MVP：任意）
                  → 取 polity 最高者，直接回
        # ② 在野徵辟：willing 過閾才出山
        best = null
        foreach r in GetUnaffiliated(faction):
            if !Willing(r, faction, settlement): continue
            score = 0.5*polity + 0.3*charisma + 0.2*loyalty
            best = max(score)
        return best          # 可 null → P2 保底
    catch: WarnOnce; return null
```

### willing 公式（MVP 版，調查 P 公式的可算子集）

```
willing = w1·factionStrength + w2·avgOpinion − w4·distance > threshold(50)
  factionStrength：MVP 用「派系聚落數 / 10，clamp 0~10」×5   # 不 ref RimWar 的代理指標
  avgOpinion：r 對該派系 Serving 職官的 GetOpinion 平均（無同僚 → 0）×0.5
  distance：homeSettlement 至 settlement 的 ApproxDistanceInTiles ×0.3（無 home → 0）
  + 基礎分 40 + loyalty×0.2
```

權重做成 PersonnelUtility 常數（非 settings——MVP 不暴露調參面，免測試矩陣爆炸）；
**俸祿/品階項（w3）留 S2 接 P3 city-economy**。不情願（回 false）＝在野者「待價而沽」，
該城落 P2 憑空保底或下輪再試——三國志「三顧」味道自然出現。

## 量級/安全

- provider 每心跳最多被呼 `MaxNewLordsPerHeartbeat(5)` 次 × 線性掃派系 record（數百筆）
  → 負載忽略不計。
- S1 停用/未註冊 → P2 行為與現狀一致（保底路徑）。
- provider 絕不回玩家派系 record（GetByFaction(faction) 天然限定）。

## 驗證

1. P2 重編 0 警告 0 錯誤、healthcheck 過；S1 停用實機 → 指派行為同現狀。
2. S1 啟用：dev 在某城撒 3 個在野（高 polity 一名）→ 拔掉該城太守（debug）→
   數心跳內新太守＝在野中 polity 最高者、status=Serving、id 不變（事前 Offset opinion 驗
   恩怨保留）、homeSettlement=該城。
3. 待命優先：先 Release 一名前太守（待命態）再清空城 → 回鍋的是待命者非在野者。
4. willing 拒絕路徑：把在野者對該派系職官 opinion 全 Offset 到 -100 → 不出山、
   P2 憑空保底照常（log 觀察）。
5. `OfficerEmployed` 事件有廣播（Toggle listeners）。
