# 01 — 整合架構：mod 組織、依賴 DAG、建造順序

> 基礎依據：盤點「建議架構」段（investigations:176-178）。本檔定型並補依賴圖。

## 既有地基（已驗證，勿重造）

| mod | packageId | 提供什麼 | 本計畫角色 |
|---|---|---|---|
| `faction-politics` | `pas.politics.community` | `RebelRecord`/`RebelSpawner`（具名 pawn↔城綁定、`PassToWorld(KeepForever)`+`previouslyGeneratedInhabitants` 橋）、`WorldComponent_RebellionTracker`（心跳+自癒+`TrySplit`）、`FactionSplitter`（反叛者升 `newFaction.leader`） | ②的提取來源、支柱⑥承載 |
| `npc-outposts` | `pas.outposts.community` | `NpcOutpost:Settlement`、`OutpostPlacer.TryPlaceFor(…,type=null)`（注入縫，`OutpostPlacer.cs:11/19`）、兩個 public static hook `GrowthRateMultiplier`/`ParentEligibilityOverride`（`WorldComponent_OutpostSpawner.cs:17/22`） | 支柱③地基 |
| `npc-outposts-rimwar`（Mod 1） | — | RimWar 接線範式、fail-soft 框架、`WorldComponent_OutpostWarMomentum`（`GetGrowthMultiplierFor`/`RecordBattle`/`Score`） | D/J 掛載點 |
| `empire-warfare`（Mod 2） | — | 附庸淪陷易主、`WorldComponent_WarfareTracker`、`WarfareLifecycleHooks` | 支柱⑦掛載點 |
| `empire-outposts-war`（Mod 3） | — | 三合一膠水、battle modifier、tax participant 範式 | C#1/H 範式參照 |
| `voe-outpost-enhancement` | `justty32.VOEOutpostEnhancement` | 花銀升級 gizmo+扣費+WorldComponent | 支柱②投資範式 |
| `colony-archival-outpost` | `pas.colonyarchival.outpost` | 封存採樣→抽象產出 | 支柱⑧（E1 進行中） |

> 已驗證：上述 faction-politics / npc-outposts 檔案與行號於 2026-06-12 核對屬實。

## 需新建的 mod（5 個）

1. **`pas.named-officers`（基礎層）** — 從 faction-politics 抽取泛化：具名 pawn + 屬性 comp + 關係 + 懶生成。`02`。
2. **settlement-lords（聚落領主）** — F/G/K/J 的領主端；屬性影響點數成長 postfix。`03`+`06`。
3. **warband-generals（將領）** — E；ResolveCombat 注入。`04`。
4. **city-economy（城池財富/防禦）** — H/K 單一 comp；RimWar comp + Empire participant。`05`。
5. **faction-politics 擴充**（非新 mod，原地擴充）— 叛亂改寫成領主帶城叛變。`07`。

## 依賴 DAG

```
              ┌─────────────────────────────┐
              │  pas.named-officers (基礎層)  │  ← 具名 pawn + 屬性 comp + 關係 + 懶生成
              │  「領主屬性 comp」＝硬前置     │
              └───┬───────┬───────┬─────────┘
       ┌──────────┘       │       └────────────┐
       ▼                  ▼                    ▼
 warband-generals   settlement-lords      faction-politics 擴充
   (E)                (F/G/K/J)              (叛亂×領主)
       │                  │  │                  │
       │                  │  └──► city-economy (H/K)  ◄── Empire registry
       │                  │           │
       │                  └──► npc-outposts hook (J: TypeSelector)
       │                                │
       └────────────► empire-warfare / 戰爭叢集（既有，勿改寫，掛 participant）
```

## 「領主屬性 comp 是硬前置」論證（盤點 + E/F/G/I/J/K 共識）

E（將領戰力，investigations:103/118）、F（領主承載，:118）、G（治理倍率，:129）、
I（關係住此層，:147）、J（讀領主 comp 加權，:154 明寫「硬前置：領主屬性 comp 尚未實作，須先建」）、
K（治理動作讀領主 comp，:160）——**全部讀同一份「具名 pawn + 屬性」**。
故 `named-officers` 是 E/F/G/I/J/K 的共同上游，**必須最先建**，否則五個消費 mod 全無地基。

## 建造順序（拓樸排序，對映 `09` 分期）

```
P0  pas.named-officers (基礎層 + 屬性 comp + 關係雙軌骨架)
P1  warband-generals (E)          ← 最小、最快驗證屬性 comp 走通
P2  settlement-lords (F 承載 + G 點數 postfix)
P3  city-economy (H/K comp + 守城折算)
P4  faction-politics 擴充 (領主帶城叛變)
P5  領主決策層 ILordAction (J/K 對內外動作) + npc-outposts TypeSelector hook
P6  RimWar×Empire 聯動 participant (C#1 起手 → #2/#4)
```

> 每期可獨立編譯、獨立驗證、獨立可玩（`09` 詳列驗證點）。
