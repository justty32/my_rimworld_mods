# T5 — `OfficerSpawner` 懶生成（0.5d）

**Create:** `Source/World/OfficerSpawner.cs`
（泛化 `RebelSpawner.cs`：拆掉 profile/letter/PickHome 叛亂語意，留純管線）

## 與 RebelSpawner 的差異對照

| RebelSpawner | OfficerSpawner | 理由 |
|---|---|---|
| `TrySpawnFor` 立即生 pawn | **建 record 不生 pawn**（Create 在 registry）；`Materialize` 才生 | 02 核心：平時輕量 record，按需具現（world pawn 爆量風險對策） |
| `PickHome` 挑駐地 | 呼叫方指定 `assignedTo` | 駐地選擇是玩法，住消費 mod |
| 發 Letter | 不發 | 基礎層零 UI 騷擾；事件走 T7 hook |
| respawn 循環 | 無（G5 死亡→事件→清理） | 繼任屬玩法 |

## 核心簽章

```csharp
namespace pas.officers
{
    /// <summary>具現管線：GeneratePawn → PassToWorld(KeepForever) → inhabitants 橋。
    /// 手法照抄 faction-politics RebelSpawner（已實機驗證 1.6 唯一供給者路徑）。</summary>
    public static class OfficerSpawner
    {
        /// <summary>按需具現；已具現且在世則直接回。失敗回 null（record 保持輕量態）。</summary>
        public static Pawn Materialize(OfficerRecord record)
        {
            if (record.pawn != null && !record.pawn.Dead && !record.pawn.Destroyed)
                return record.pawn;
            Pawn pawn = GeneratePawn(record.faction);            // 照抄 RebelSpawner.cs:71-84
            if (pawn == null) return null;
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            BridgeInhabitants(record, pawn);                     // 見下
            SyncName(record, pawn);                              // 見下
            record.pawn = pawn;
            return pawn;
        }

        /// <summary>assignedTo 是 Settlement 才有橋可搭（拜訪 redress 請回同一 pawn）。</summary>
        private static void BridgeInhabitants(OfficerRecord record, Pawn pawn)
        {
            if (record.assignedTo is Settlement s
                && !s.previouslyGeneratedInhabitants.Contains(pawn))
                s.previouslyGeneratedInhabitants.Add(pawn);
            // warband 等非 Settlement 宿主：無此橋，pawn 僅 world pawn（P1 打到地圖時
            // 由消費 mod 自行注入 GeneratePawnGroup——非 P0 範圍）。
        }
    }
}
```

## GeneratePawn（逐行沿用 `RebelSpawner.cs:71-84`，含兩條實戰教訓）

- `faction.RandomPawnKind()` 而非 `basicMemberKind`（1.6 NPC 派系全 null——E2E 實測教訓，
  原註解照搬進新碼）；非 Humanlike 回 null。
- `PawnGenerationRequest` **具名引數**（跨版本欄位錯位教訓）：
  `new PawnGenerationRequest(kind: kind, faction: faction, context: PawnGenerationContext.NonPlayer)`。

## 名字策略 `SyncName`（G4 相鄰問題；依 T0 驗證結果二選一）

- **方案 A（T0 證實無 pawn 可產人名）**：record 建立時即產 `nameCached`；
  具現時 `pawn.Name = NameTriple/NameSingle(nameCached)` 強制同名（身份穩定，玩家視角同一人）。
- **方案 B（fallback）**：record 建立時 `nameCached=null`、顯示 role label；
  首次具現後快取 `pawn.Name.ToStringShort` 進 `nameCached`，此後即使 pawn 亡佚名字仍在。
- 兩案皆滿足驗收「name fallback」；計畫預設 B（零新 API 風險），A 留 T0 升級。

## 屬性擲定

record 建立時（registry.Create）以 `Settings.initialAttributeRange.RandomInRange` 擲七維
（仿 `ratePerDay` 在生成時自 profile 擲定的手法，`RebelSpawner.cs:29`）。
消費 mod 可在 Create 後改寫（名將高武力等玩法分布是它們的事）。

## 驗證步驟

1. build 過。
2. dev action（T8）對選中聚落 `Create officer` → `Materialize` → log 印 pawn 名。
3. `Find.WorldPawns.Contains(pawn)` 與 `ForcefullyKeptPawns.Contains(pawn)` 皆 true（dump 對帳）。
4. **同一 pawn 請回**：用 Sims Mode 或 caravan 拜訪該聚落 → 地圖上出現同名 pawn；
   離開後心跳自癒把 forced-keep 補回（`RebellionTracker.cs:131-137` 已知的 redress 副作用）。
5. 存讀檔 → `record.pawn` ref 不丟、再 Materialize 直接回同一 pawn（不重生）。
