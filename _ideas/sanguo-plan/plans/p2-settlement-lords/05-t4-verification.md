# T4 — 驗證：healthcheck 全量＋build 0/0＋E2E checklist

## 靜態驗證（必過才算 done）

```bash
cd ~/repo/my_rimworld_mods/settlement-lords
dotnet build Source/SettlementLords.csproj -c Release   # 0 警告 0 錯誤
python3 tests/healthcheck.py                            # healthcheck OK
```

healthcheck 覆蓋（抄 P1 改造）：
1. 全 XML well-formed；
2. About packageId=`pas.officers.settlements`＋三 hard dep（modDependencies＋loadAfter）；
3. 恰一個 `pas.officers.OfficerRoleDef`、defName=`pas_settlement_Lord`、
   P0 Source 真有 OfficerRoleDef 類（跨倉契約核對）；
4. XML 引用的 `pas.officers.settlements.*` 類存在於 Source/；
5. C# 引用的 `pas_settlement_*` key/defName 都在 XML；
6. 三語 Keyed key 集合一致且非空；
7. 接點齊全（IncrementSettlementGrowth／GetInspectString）＋TryPatch fail-soft；
8. **鐵律 guard**：Source/ 不得出現 `combatAttribute`/`growthAttribute` 字串；
9. 相依 DLL 存在（NamedOfficers.dll／RimWar.dll／0Harmony.dll）。

## 實機 E2E checklist（09 Phase 2 驗證標準；dev mode＋RimWar＋named-officers）

- [ ] **指派**（驗收前置）：開新局推進時間數日 → P0 `Dump officer registry`
      可見多筆 role=pas_settlement_Lord、assignedTo=各 NPC 聚落；
      同城不出現第二領主；玩家聚落/Excluded 派系城永不掛。
- [ ] **賢主加速**（驗收 1a）：dev 用 P0 `Roll attributes` 把某城領主 polity/loyalty 調高
      （或找天生高政務者）→ 連續數輪（每 2500 tick）該城 RimWarPoints 漲幅
      高於同派系無主/庸主城。
- [ ] **庸主衰退**（驗收 1b）：把某城領主 polity/loyalty 調低 → 該城 points 逐輪下降、
      最終停在 100（getter 地板）不再降；decayEnabled 關閉後不再下降。
- [ ] **不污染他城**（驗收 2）：同派系無主城成長節奏不受鄰城賢主/庸主影響（逐城驗證）。
- [ ] **PointDamage 跳過**：對某掛主城發動攻擊使 PointDamage>0 → 該輪本 mod 不加不扣
      （點數變化只來自 RimWar 療傷分支）。
- [ ] **inspect**（驗收 3）：選中掛主城 → inspect 末行見領主名＋政務/忠誠＋治理係數；
      與 RimWar 的點數行、Mod 1（若載入）各自顯示不互蓋。
- [ ] **存讀往返**（鐵則 ④）：存檔→讀檔 → dump 綁定/屬性一致、inspect 仍顯示同名領主、
      成長/衰退行為延續。
- [ ] **易主處置**：dev 強制某掛主城 ConvertSettlement/SetFaction → 兩輪心跳內領主退場
      （record 移除、inspect 行消失）；log 無紅字。
- [ ] **被毀處置**：dev 摧毀某掛主城 → 兩輪心跳內解綁退場、無 unresolved-ref 警告。
- [ ] **中途裝/移除**（鐵則 ②）：無本 mod 舊檔載入不炸；停用本 mod 後讀檔只有 warning。
- [ ] **threading**：RimWar 設定開 threadingEnabled 跑數日 → 無紅字、無 WarnOnce 噴發。

任何一條失敗 → 回對應任務檔修，不得帶傷簽收。
