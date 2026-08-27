# B0 spike ＋ B1 personnel mod 骨架

## B0 — S1 spike（0.25d，唯讀）

1. **`FactionDialogMaker.FactionDialogFor(Pawn negotiator, Faction faction)`**
   （`~/repo/projects/rimworld/RimWorld/FactionDialogMaker.cs:16`，public static，回 DiaNode）：
   確認 postfix 簽章 `(Pawn negotiator, Faction faction, ref DiaNode __result)` 可行、
   DiaNode.options 可直接 Add。
2. **RimWar 共存**：RimWar 同點 postfix（`RW:5875-5908`）插 4 選項並**刪除**含
   "Request a trade caravan"/"Request immediate military aid" 字樣的 vanilla 選項——
   確認我方選項文字避開該兩字樣即不被誤刪；patch 順序（兩個 postfix 互不依賴）無影響。
3. **走入式入場**：確認 `CellFinder.TryFindRandomEdgeCellWith`＋`GenSpawn.Spawn`＋
   `pawn.SetFaction(Faction.OfPlayer)` 的 wanderer-join 等效路徑（vanilla
   `IncidentWorker_WandererJoin` 抄法）；以及 `TradeUtility.LaunchSilver(map, amount)` 簽章。
4. **soft-dep 橋接前例**：grep 自家 mod 群是否已有 ModLister 守衛＋橋接類隔離前例可抄
   （empire-warfare / rimwar-empire-economy 對 Empire 的處理）；無前例則用標準式
   `ModLister.GetActiveModWithIdentifier("pas.officers.settlements") != null` →
   只在 true 時 JIT 到 Bridge 類。
5. `SignatureSpike.cs` 慣例（仿 P1/P2）：對 FactionDialogFor／P1 P2 hook 欄位寫
   編譯期簽章釘子，patch 目標變動在編譯期就爆。

**驗證**：結論回填本檔修正框。

## B1 — mod 骨架（0.5d）

**Create:** `my_rimworld_mods/personnel/` 全套（仿 P1/P2 骨架逐檔對照）：

```
personnel/
  About/About.xml            # packageId pas.personnel.community；
                             # modDependencies: brrainz.harmony, pas.officers.community
                             # loadAfter: + pas.officers.warband, pas.officers.settlements（soft）
  Source/Personnel.csproj    # net48；ref Krafs.Rimworld.Ref + Lib.Harmony(Private=false)
                             #  + NamedOfficers.dll(HintPath ../named-officers, Private=false)
                             #  + WarbandGenerals.dll / SettlementLords.dll（Private=false，soft 用）
  Source/PersonnelMod.cs     # ModSettings（下表）
  Source/HarmonyInit.cs      # TryPatch fail-soft（逐 patch try/catch + WarnOnce，仿 P1/P2）
  Source/SignatureSpike.cs   # B0 釘子
  Source/PersonnelUtility.cs # WarnOnce／settings 快取／willing 公式（B4 填肉）
  Source/World/WorldComponent_Personnel.cs   # 心跳殼：2500-tick offset 1800；本任務先空轉
  Defs/PersonnelDefs/Settings or 純 ModSettings（決策：走 ModSettings，玩家可調——仿 P2）
  Languages/{English,ChineseTraditional,ChineseSimplified}/Keyed/Personnel.xml
  tests/healthcheck.py       # 抄 P1/P2 健檢：XML well-formed、packageId、defName 前綴、
                             #  csproj 引用白名單（Harmony/NamedOfficers/P1/P2 dll 許可）
  PROJECT.md                 # 契約/驗證/E2E 殼
```

### ModSettings（B2–B5 消費；預設值為建議起點）

| 欄位 | 預設 | 用途 |
|---|---|---|
| `seedDensity` | 1.5 | 開局撒種：每合格聚落期望在野人數（B2） |
| `maxIdlePerSettlement` | 3 | 單城在野上限（B2 撒種＋認領落籍都看） |
| `trickleChancePerHeartbeat` | 0.02 | 每心跳每合格聚落新人才湧現機率（B2） |
| `recruitCostBase` / `recruitCostPerAttr` | 300 / 5 | 徵辟銀價公式（B5） |
| `recruitCooldownDays` | 15 | 每派系徵辟冷卻（B5） |
| `npcStaffingEnabled` | true | CandidateProvider 註冊開關（B4） |
| `claimEnabled` | true | 人才守恆認領開關（B3） |

### soft-dep 結構（00 決策；B3/B4 落地）

```
Source/Bridges/Bridge_SettlementLords.cs   # 唯一 touch P2 型別的檔：註冊 ExitInterceptor、
Source/Bridges/Bridge_WarbandGenerals.cs   #   CandidateProvider；由 PersonnelMod ctor 經
                                           #   ModLister 守衛呼叫 static Init()
```

P1/P2 未啟用 → Bridge 類永不 JIT → 無 TypeLoadException；S1 退化為
「撒種＋orphan 信件＋通訊台徵辟＋UI」仍完整可玩（fail-soft 鐵律）。

### 心跳殼

```csharp
// WorldComponent_Personnel.WorldComponentTick：
if (Find.TickManager.TicksGame % 2500 != 1800) return;   // 00 決策 7 錯峰
// B2: SeedTick()；B3: 無（事件驅動）；信件節流 state 也存這裡（Scribe）
```

`ExposeData`：徵辟冷卻 `Dictionary<Faction,int>`（Scribe_Collections Reference+Value——
仿家族慣例改存 `factionLoadId→tick` 的 value dict 更穩，B5 細節）＋開局撒種完成旗標。

## 驗證（B1）

1. `dotnet build Source/Personnel.csproj -c Release` 0 警告 0 錯誤（含 P1/P2 ref 解析）。
2. `python3 tests/healthcheck.py` 通過。
3. 實機空轉：新存檔＋舊存檔載入不炸；P1/P2 停用時載入不炸（soft-dep 驗證）；
   mod 列表移除本 mod 後舊檔可讀（僅 warning）。
4. Harmony log（`HarmonyInit` WarnOnce 路徑）乾淨。
