# 08 — RimWar × Empire 聯動（C）+ 進行中/獨立片段定位

> 對應調查：C（:56-75 八機會）。war-cluster 三 mod（既有）+ 進行中片段（D/E1/B/C#1）。

## A. C 八機會 — 選序（第一梯隊先做）

> Empire 端全免 Harmony 的最乾淨機會：#1/#2/#4/#7/#8；RimWar 側唯一需 patch=#3（C，:75）。
> **通用陷阱**：Empire Registry `ClearCaches` 讀檔 ClearAll → 必 `EmpireCacheUtil.RegisterCacheInvalidator`（`CachePatches.cs:21`）。

| # | 機會 | 接點（免 Harmony 除非註明） | 梯隊 | 本計畫排序 |
|---|---|---|---|---|
| 1 | **戰時加稅/被圍困減產** | 減產 `IResourceProductionModifier`（`ResourceFC.cs:338/...`）、戰時稅 `ITaxTickParticipant.PostSettlementCreateTax`（`WorldSettlementFC.cs:1917`，`ref`）；訊號讀 `RimWarSettlementComp.AttackingUnits/nextCombatTick RW:9088`、`RimWarData.IsAtWar RW:1462` | 一 | **P6 起手**（最高比值、零 patch） |
| 2 | 附庸繁榮回饋 RimWar 點數 | `ITaxTickParticipant.PostTaxResolution`（`FactionFC.cs:1696`）→ `RimWarPoints` setter（尊重上限 50000，`RW:17597`；勿與 `Patch_RimWarPoints` 雙算） | 一 | P6 次 |
| 3 | RimWar 全事件流→Empire FCEvent | RimWar postfix `RW_LetterMaker.Archive_RWLetter RW:7851`（六類 def `RW:1772-1782`）+ `FactionFC.AddEvent FactionFC.cs:1736`；需節流去重 | 一 | P6（唯一需 RimWar patch） |
| 4 | 帝國威望/軍力受勝負回饋 | `IThreatScalingContributor`（`ThreatScalingUtil.cs:83`）；讀 `RimWarData.TotalFactionPoints RW:1510` 或 postfix `ResolveBattle_Settlement RW:11086`；自存滑動視窗 | 二 | 後續 |
| 5 | 帝國成為 RimWar 外交一方 | `RimWarFactionUtility.DeclareWarOn/DeclareAllianceWith RW:468/526`（public static，免 patch）；Empire 端缺外交入口需自建 | 二 | 後續（風險中高，:68） |
| 6 | Empire squad 介入 NPC vs NPC | `IAutoDefender`（`FCInterfaces.cs:280`）+ `SimulateBattleFc.FightBattle`（public）；工程大 | 二 | 延後 |
| 7 | 勢力範圍→附庸擴張/被攻頻率 | `IRaidWeightProvider`（`FactionFC.cs:709`）+`ISettlementFoundingValidator`（`CreateColonyWindowFC.cs:130`） | 三 | 延後 |
| 8 | 未用 `ILifecycleParticipant` 事件對接 | `LifecycleParticipantBase` 子類覆寫（`ResearchPatches.cs:24` 等）；`OnBattleResolved` 已被 empire-warfare 用 | 三 | 延後 |

**聯動歸屬**：新 participant 掛在既有 `empire-warfare`（Mod 2）之上或獨立 compat mod，**勿改寫戰爭叢集**（`01` 鐵律）。

## B. 進行中/獨立片段定位（任務點 4）

| 片段 | 狀態 | 與大計畫關係 | 先後 |
|---|---|---|---|
| **E1 自訂哨站類型** | 實作中 | 支柱⑧封存哨站；`colony-archival-outpost` 採樣→抽象產出。屬③/⑧地基擴充，獨立於領主層。 | 與 P0–P2 並行（不阻塞） |
| **D settler→哨站變體** | 實作中 | 支柱③節奏控制；prefix `WorldUtility.CreateSettlement RW:15248` 按機率改建 NpcOutpost（歸 Mod 1，`settlerToOutpostChance`，D:85-89）。是 `06` 對外動作的**底層管道**。 | 須在 P5 決策層之前/同時就緒（決策層呼此接點） |
| **B 影子 Settlement**（warband 打哨站） | 構想（推薦路 B） | 支柱③×④閉環：讓 warband 能攻防哨站。影子真 Settlement 過型別過濾、prefix `ConvertSettlement` 改對 Outpost（B:46-52）。獨立於領主/將領層。 | 可獨立排程；建議 P3 後（與 city-economy 守城折算協調） |
| **C#1 戰時加稅** | 構想（C 第一梯隊起手） | 即上表 #1；P6 聯動起手。 | P6 |

## C. 片段間先後總結

- **D 是 `06` 對外動作的管道前置**：決策層「建哪種 outpost」最終呼 D 的 `CreateSettlement` prefix + npc-outposts `TypeSelector`。故 D 應在 P5 前完成基礎版。
- **E1 / B 與領主主線正交**，可並行或穿插，不阻塞 named-officers→領主→經濟主鏈。
- **C#1 屬聯動層（P6）**，建在地基與消費層成熟之後。
