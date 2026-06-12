# B5 玩家通訊台徵辟（N 管線：FactionDialogFor postfix）

**Create:** `Source/Patches/Patch_FactionDialog.cs`、`Source/RecruitService.cs`

## 注入（調查 N 已驗證的管線，RimWar 自己就是同點先例）

```csharp
[HarmonyPatch(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor))]
// postfix(Pawn negotiator, Faction faction, ref DiaNode __result)
```

- 顯示條件：faction 非玩家、`!faction.HostileTo(player)`、未 defeated、
  `GetUnaffiliated(faction).Count > 0`、冷卻已過。
- 選項文字（keyed `pas_personnel_RecruitOption`）**避開** "Request a trade caravan" /
  "Request immediate military aid" 字樣（RimWar 同點 postfix 會刪含該字樣選項，B0 已釘）。
- 冷卻未過/敵對 → 灰色 DiaOption＋原因（vanilla 慣例 `Disable(reason)`）。

## 子選單（DiaNode 清單，MVP 不做世界 targeter）

```
「徵辟人才」→ DiaNode：
  列 GetUnaffiliated(faction) 前 8 名（屬性和降序）：
  「{名}（武{m} 統{c} 政{p} 魅{ch} 忠{l}）｛距離 ~N 格｝ — 費銀 {cost}」
   → 確認節點：支付 → 成功文案；銀不足 → 灰色＋原因
  「返回」→ 回主對話節點（保留 vanilla/RimWar 其他選項可用）
```

## `RecruitService.TryRecruit(record, faction, negotiator)`（流程定案）

```
cost = recruitCostBase + recruitCostPerAttr × 五維和 + loyalty×2   # 忠臣難挖，貴
1. 查銀：negotiator.Map 可達白銀 < cost → false（選項層已灰，雙保險）
2. TradeUtility.LaunchSilver(negotiator.Map, cost)                  # vanilla 通訊台扣銀慣例
3. pawn = OfficersApi.Materialize(record)；null → 退費安全序（先驗後扣：把 2 移到 3 之後）
   → 定案順序：先 Materialize 驗成功 → 再扣銀（失敗零副作用）
4. pawn.SetFaction(Faction.OfPlayer)
5. 入場：CellFinder.TryFindRandomEdgeCellWith(... negotiator.Map) → GenSpawn.Spawn
   （wanderer-join 等效；找不到 edge cell → fallback DropPodUtility.DropThingsNear）
6. OfficersApi.RemoveOfficer(record)        # 單向離開（00 決策 5：殖民者不進 record 系統）
   ※ Remove 清他人 opinions 鍵＝此人退出 NPC 關係網——預期語意（已是玩家的人）
7. faction.TryAffectGoodwillWith(player, -5)  # 挖人傷和氣（小額）
8. 冷卻落筆 WorldComponent_Personnel（per-faction，recruitCooldownDays）
9. Letter（PositiveEvent）：「◯◯應辟而來」＋ pawn 跳轉
```

- **資助出仕**（投資在野者讓其出仕該派系換好感/關係）＝S2 stub：本任務只在
  RecruitService 留 `// S2: SponsorOffice` 註解位，不實作。
- pawn 名：Materialize 已回寫 nameCached（A1/A2）→ 對話清單名＝入場 pawn 名，
  「徵的就是這個人」閉環。
- 七維 → 殖民者映射：MVP **不做**（pawn 技能由 PawnGenerator 自然生成；
  屬性僅是世界層數值）。記 backlog：S2 可按七維調 skills/traits。

## 冷卻儲存

`Dictionary<int,int>`（faction.loadID → 到期 tick），`Scribe_Collections` value/value；
PostLoadInit null 防護。faction 消亡殘鍵無害（查不到即視為無冷卻）。

## 驗證

1. build＋healthcheck；SignatureSpike 編譯期釘 FactionDialogFor 簽章。
2. 實機：通訊台呼叫友好派系 → 有「徵辟人才」選項；清單 ≤8、排序正確、費用顯示正確。
3. 徵辟成功：銀扣對、邊緣入場、pawn 名＝清單名、派系=玩家、letter 跳轉可用、
   `Dump idle by faction` 該員消失、其他 record 的 opinions 鍵已清。
4. 銀不足 → 選項灰；徵後冷卻 → 選項灰＋剩餘天數；存讀檔後冷卻延續。
5. 敵對/defeated 派系 → 無選項；RimWar 啟用時其 4 個選項與我方選項並存無誤刪。
6. Materialize 失敗注入（debug 改 faction kind）→ 不扣銀、log 一條 WarnOnce、對話不炸。
