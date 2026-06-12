# B2 在野撒種：開局 seed ＋ trickle

**Modify:** `Source/World/WorldComponent_Personnel.cs`
**Create:** `Source/World/TalentSeeder.cs`

## 設計

世界上要先「有」在野人才，招攬/徵辟/UI 才有料。兩條供給線共用 `TalentSeeder`：

### 1. 開局撒種（一次性，懶觸發）

- **觸發**：心跳首輪發現 `seeded==false`（WorldComponent scribe 旗標）→ 執行後置 true。
  懶觸發而非 `FinalizeInit`：避開世界生成期派系/聚落未就緒的時序坑（faction-politics 同款
  心跳-自癒哲學）；**舊檔中途裝 mod 也自動補種**（同一路徑，零特判）。
- **合格聚落**：`Find.WorldObjects.Settlements` 中 Faction 非 null、非玩家、非 defeated。
  **不要求 RimWarSettlementComp**——S1 不 ref RimWar（在野層是通用人事，非戰爭功能）。
- **量**：每合格聚落擲 `Poisson-ish` 簡化版：`floor(seedDensity) + (Rand < frac ? 1 : 0)`，
  clamp 到 `maxIdlePerSettlement`；逐城呼 `OfficersApi.CreateIdleOfficer(settlement)`。
- **預擲名**：建後立即 `OfficerNamer.EnsureNameCached(r)`（UI 前置；無 pawn、便宜）。
- **節流**：單輪上限 200 城（超大世界分多心跳續種，`seedCursor` int scribe）。

### 2. trickle（持續湧現，「人才輩出」）

每心跳（2500-tick，offset 1800）：

```
foreach 合格聚落（同上條件）:
    if GetIdleAt(s).Count >= maxIdlePerSettlement: continue
    if Rand.Chance(trickleChancePerHeartbeat):           # 預設 0.02 ≈ 每城每 ~2 天一擲
        CreateIdleOfficer(s) + EnsureNameCached
        created++; if created >= MaxTricklePerHeartbeat(2): break   # 全域節流
```

- trickle 由 `trickleChancePerHeartbeat=0` 完全停用（settings）。
- **不發信件**：NPC 城湧現人才對玩家是背景噪音；玩家可從 UI/通訊台發現。
  （備選：友好派系出大才（屬性和 >350）發一封低調 letter——記 backlog，MVP 不做。）

## 量級估算（防爆量鐵律核對）

40 城 × (1.5 種子 + trickle 收斂至 cap 3) ≈ 60–120 筆在野 record。
全部**零 pawn**（純 record＋預擲名字串）；具現只發生在玩家拜訪（vanilla redress）、
徵辟（B5）、A 軌建立（既有 G4）。registry 總量數百筆，線性掃描決策（A3）成立。

## 與 P0 的責任切線

- 「在野人才存在」＝S1 玩法（P0 G6 鐵則：本層不自動鋪官——CreateIdleOfficer 只是 API，
  撒種迴圈住 S1）。
- 在野者七維由 P0 settings `initialAttributeRange(20~80)` 擲；S1 MVP 不調分佈
  （「名士出寒門」之類的加權留 S2）。

## 驗證

1. build＋healthcheck 過。
2. 實機新檔：兩心跳內 `Dump idle by faction` 顯示各 NPC 派系有在野人才、
   數量 ≈ 城數×seedDensity、全部有 nameCached、無 pawn 具現
   （dump 的 spawned/world 欄全空）。
3. 中途裝 mod（舊檔）：同樣補種成功。
4. trickle：開 dev 快進 ~10 天 → 在野數向 cap 收斂、不超 `maxIdlePerSettlement`、
   每心跳新增 ≤2。
5. 存讀檔：seeded 旗標／在野 record 往返不丟、不重複撒種。
6. 玩家拜訪某在野者居住城 → 地圖出現同名 pawn（P0 橋＋A2 泛化的整合驗證）。
