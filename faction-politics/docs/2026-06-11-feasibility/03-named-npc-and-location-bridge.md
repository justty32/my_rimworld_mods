# 03 具名反叛者 NPC：生成、保存、定位橋、自癒

## 生成與長期保存（照原版首領那套）

- `PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, …))` → `Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever)`。
- KeepForever 進 `pawnsForcefullyKeptAsWorldPawns`（WorldPawns.cs:226-228），`WorldPawnGC.GetCriticalPawnReason` 對 ForceKept 回非空（WorldPawnGC.cs:212-214，已驗證）→ **永不被 GC 回收**。
- kind 選擇：`faction.def.basicMemberKind`（缺則 fallback 派系任一 humanlike kind）。redress 過濾只看 **race + faction**（見下），kind 不必精準對齊聚落守軍 kind。

## 定位橋：`previouslyGeneratedInhabitants`（含重要新發現）

「反叛者駐在某聚落、玩家拜訪時找得到」的原版表達：

1. 生成後 `homeSettlement.previouslyGeneratedInhabitants.Add(rebel)`（`public List<Pawn>`，Settlement.cs:14；`LookMode.Reference` 隨存檔走，:165）。
2. 聚落地圖生成時的居民請求鏈（**本調查逐環驗證**）：
   - `SymbolResolver_Settlement.cs:59`：`pawnGroupMakerParams.inhabitants = true`
   - `PawnGroupKindWorker_Normal.cs:67-71`：request 帶 `tile = parms.tile`、`inhabitants = parms.inhabitants`
   - `PawnGenerator.GenerateOrRedressPawnInternal`（PawnGenerator.cs:210-220）：`request.Inhabitant && request.Tile.Valid` → 查該 tile 的 Settlement → **從 `previouslyGeneratedInhabitants` 優先 redress**（請回同一個 pawn），並 `Find.WorldPawns.RemovePawn(result)`。
3. redress 候選過濾 `IsValidCandidateToRedress`（:369-）：**race 相符（:371）+ faction 相符（:375）**＋活著、不流血、kind 的 skills/forcedTraits 約束（一般 humanlike kind 不設）→ 反叛者幾乎必然合格。

### 🔴 新發現：原版從不自動填這份清單（PawnGenerator.cs:236 死碼）

```csharp
if (request.Inhabitant && !request.Tile.Valid)   // ← 負向條件
{
    Find.WorldObjects.WorldObjectAt<Settlement>(request.Tile)?.previouslyGeneratedInhabitants.Add(result);
}
```

tile 無效時 `WorldObjectAt(tile)` 必然查無 → 整段死碼（疑原版 1.6 bug，條件應為 `request.Tile.Valid`）。含義：

- **正面**：清單是空的，我們 Add 的反叛者是唯一條目 → 拜訪時 redress 必中他（不會被原版自動記錄的雜魚稀釋）。
- **注意**：「上次見過的守軍下次再見」這個原版敘事在 1.6 實際不運作；本 mod 不依賴它。

## 生命週期自癒（每輪 tick 維護）

redress 與地圖收場會擾動反叛者狀態，WorldComponent 每輪自癒：

| 擾動 | 來源 | 自癒 |
|---|---|---|
| redress 時被移出 world pawns | PawnGenerator.cs:218 `RemovePawn` | 若 `!rebel.Spawned && !Find.WorldPawns.Contains(rebel)` → 重新 `PassToWorld(KeepForever)` |
| 地圖關閉時清單被剪 | `Settlement.Notify_MyMapRemoved`（Settlement.cs:202-209）剪掉「非 world pawn 或已毀」條目 | 若 `!homeSettlement.previouslyGeneratedInhabitants.Contains(rebel)` → 重 Add |
| 反叛者死亡/被毀 | 玩家擊殺（=鎮壓玩法）、事件波及 | record 進展歸零、入冷卻（`respawnDelayDays`），到期生成新反叛者 |
| 駐地聚落易主/被毀 | 戰爭、其他分裂 | 重挑該派系現存聚落為新駐地；無聚落則暫停追蹤 |

## 進展推進

- 單一 `WorldComponent`，`TicksGame % 2500 == 0` 批次推進：`progress += ratePerDay × 2500/60000`（`ratePerDay` 生成時從 profile 的 FloatRange 擲定，存進 record——每個反叛者步調不同）。
- 不掛 `Pawn.Tick`（mothball pawn 走合併的 `TickMothballed`，間隔不可控——報告 §5.2 結論正確，照用 WorldComponent 集中排程）。
- 權威資料自管（record：faction/rebel/homeSettlement/progress/ratePerDay/respawnAtTick），`previouslyGeneratedInhabitants` 只當「請回場上」的橋——與報告 §5.4 建議一致。
