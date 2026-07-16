# city-economy session log

- 2026-06-12 T0-T5 代碼全落地（12 個 .cs、Patches、三語 Keyed、csproj 引 RimWar/0Harmony），commit a936110，build 0/0、healthcheck OK；T5 實機驗證未跑。
- 2026-07-10 重驗 build 0/0＋healthcheck OK、SignatureSpike 確認 RimWar workshop 未斷簽章；同步落後的部署 dll 到 ~/rimworld_mods；把 pas.sanguo.cityeconomy 加入 Proton ModsConfig activeMods（插在 torann.rimwar 後，備份 ModsConfig.xml.bak-cityeconomy）。下一步＝實機跑 T5 checklist（_ideas/sanguo-plan/plans/p3-city-economy/06-t5-verification.md），首輪不開 P0/P2，驗 soft-optional 中性分支＋log 乾淨。
