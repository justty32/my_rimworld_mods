# T4 — 驗證：healthcheck 全量＋E2E checklist

## healthcheck.py 全量（Mod 1 風格，靜態、無遊戲環境）

1. XML wellformed：Defs/Languages/About。
2. About：packageId==`pas.officers.warband`；modDependencies 含
   brrainz.harmony / Torann.RimWar / pas.officers.community；loadAfter 同三者。
3. Role def：恰好 1 個 `pas.officers.OfficerRoleDef`、defName==`pas_warband_General`；
   類 `OfficerRoleDef` 存在於 named-officers Source（跨倉核對，仿 Mod 1 第 7 項）。
4. XML 引用的 `pas.officers.warband.*` 類存在於 Source/。
5. C# 引用的 `pas_warband_*` key/defName 都在 XML。
6. 三語 key 集合一致：English == ChineseTraditional == ChineseSimplified。
7. Harmony 接點字串存在於 Source/：CreateWarband / CreateWarObjectOfType /
   ResolveCombat_Units / GetInspectString；且 HarmonyInit 含 TryPatch fail-soft。
8. **鐵律 guard**：Source/ 不得出現 `combatAttribute` / `growthAttribute`。
9. dep DLL 存在：`named-officers/1.6/Assemblies/NamedOfficers.dll`、
   Steam RimWar.dll（v1.6 資料夾）、0Harmony.dll。

## build 驗證

```bash
dotnet build Source/WarbandGenerals.csproj -c Release   # 0 警告 0 錯誤
python3 tests/healthcheck.py                            # healthcheck OK
```

## 實機 E2E checklist（對映 09 Phase 1 驗證標準；dev mode＋RimWar 世界）

- [ ] **生成**：開新檔（RimWar 啟用），加速等 NPC warband 出現（或 RimWar dev 工具催生）
      → 約半數（generalChance=0.5）warband select 後 inspect 末行
      「General: 某名（Might X, Command Y）」。log 零紅字。
- [ ] **戰力（驗收 1）**：dev 對兩支敵對 warband 將領分別 SetAttribute（武/統 100 vs 0；
      P0 debug `Roll attributes` 或 SetAttribute 入口），導引互撞成 BattleSite
      → 數輪後高屬性側 PointDamage 顯著較低/勝率明顯偏高（RimWar dev 點數顯示）。
- [ ] **無將基線**：generalChance 調 0 → 新 warband 無將領行、戰鬥行為與原版一致
      （postfix 零開銷早退）。
- [ ] **存讀（驗收 2）**：有將 warband 行進中存檔→讀檔 → inspect 同名同屬性；
      P0 debug `Dump officer registry` record 仍在、assignedTo 指向同 warband。
- [ ] **交戰中存讀**：兩軍纏鬥成 BattleSite 時存讀 → 戰鬥繼續、戰後傳承/退場正常、零
      unresolved-ref 警告。
- [ ] **傳承**：將領 warband 戰勝（對 warband 或聚落）→ 重生的新 warband inspect 仍同一將領
      （DisplayName/屬性一致）；registry 無重複 record。
- [ ] **退場**：將領 warband 覆滅 → 兩輪心跳內 record 自 registry 消失（P0 dump 驗證）、
      無殘留綁定；warband 抵達自家聚落解散（增援）→ 同樣退場。
- [ ] **鐵律（不污染派系）**：戰鬥前後該派系其他 warband/聚落點數成長不受影響
      （faction combatAttribute 未動——只能由代碼審計＋healthcheck guard 保證，實機抽查）。
- [ ] **中途裝/移除**：無本 mod 舊檔載入不炸；停用本 mod 後讀有將領檔 → 只有
      named-officers 側 record 殘留（P0 心跳自癒），無紅字。
- [ ] **降級**：暫改 HarmonyInit 目標名（模擬 RimWar 改版）→ 啟動只 WarnOnce 一次、
      其餘功能照常（手動測一次後復原）。

任何一條失敗 → 回對應任務檔修，不得帶傷簽收。**不 git commit、不部署。**
