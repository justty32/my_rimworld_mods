# T5 — 驗證：healthcheck 全量＋build 0/0＋E2E checklist

## 靜態驗證

```bash
cd ~/repo/my_rimworld_mods/city-economy
dotnet build Source/CityEconomy.csproj -c Release   # 0 警告 0 錯誤
python3 tests/healthcheck.py                        # healthcheck OK
```

healthcheck 全量項（T1 雛形＋T3/T4 後補）：

1. XML well-formed（About/Patches/Languages）。
2. About：packageId `pas.sanguo.cityeconomy`；modDependencies 恰含 harmony+RimWar
   （**officers/settlements 不得在 modDependencies**）；loadAfter 四件齊。
3. Patches XML 引用的 `pas.sanguo.cityeconomy.*` 類存在 Source。
4. C# 引用 `pas_cityecon_*` key 都在 XML；三語 key 集合一致且非空。
5. 接點字串齊：ResolveCombat_Settlement／ResolveBattle_Settlement／RegenerateStock／
   GiveSoldThingToTrader／GiveSoldThingToPlayer／GetInspectString；TryPatch 存在。
6. 鐵律 guard：Source 禁 `combatAttribute`/`growthAttribute`（派系級係數）、
   禁 `ThingSetMakerDefOf`（M 段勿選注入點）；csproj 禁 `NamedOfficers`/
   `SettlementLords`（soft-optional 反射鐵律）。
7. RimWar.dll／0Harmony.dll 存在（本機建置前提）。

## 實機 E2E checklist（09 Phase 3 驗證標準；dev mode、新殖民地）

- [ ] 存量（驗收 1）：任一 NPC 城 inspect 出現「銀/糧/貨｜城防」行；推進數日數值上升；
      大城（高 RimWarPoints）成長量 > 小城。
- [ ] 存讀（鐵則④）：記某城五欄位 → 存檔 → 讀檔 → 數值一致、繼續成長。
- [ ] 劫掠（驗收 2）：dev 觸發/等待 warband 圍城至 sack 信件（"sacked"）→ 該城
      silver/food/goods 掉約 `sackLossRatio`、defensePoints 折半；RimWar 點數搬移照舊。
- [ ] 守城（驗收 3）：兩座點數相近城（一座 defensePoints 高、一座剛播種）受同級
      warband 攻擊 → 高防城存活回合明顯更多；戰後 inspect PointDamage 無負值殘留。
- [ ] 貨架（驗收 4）：dev 把某城 silver 調高/調低（或選富/窮兩城）→ 與商隊交易，
      富城 stock 白銀/貨量約為窮城 2~8 倍（0.25~2 因子差）；
      賣 100 銀貨物給城 → goods +≈100、（城付銀）silver −；買走貨物 → goods −、silver +。
- [ ] 治理 soft-optional（驗收 5）：
      啟用 P0+P2 → 有賢主（高政務）城財富成長 > 無主城（gov>1）；
      **停用 P0+P2 重啟** → mod 照常運作、成長中性、log 無紅字（反射橋降級 WarnOnce 至多一條）。
- [ ] 中途裝/移除（鐵則）：無本 mod 舊檔載入不炸（comp 自動補掛、首輪播種）；
      停用本 mod 後讀檔只有無害 warning。
- [ ] 設定開關：growthRate=0 停成長、sackLossRatio=0 停劫掠搬資源、
      defenseAmplitude=0 停守城加成、traderEconomyEnabled=false 回原版貨架——皆即時生效。
- [ ] 全程 log 無紅字、WarnOnce 無噴發。

任何一條失敗 → 回對應任務檔修，不得帶傷簽收。不 git commit、不部署。
