# 三國志化總體設計計畫 — 願景與檔案索引（2026-06-12）

> **純設計/計畫，非實作、非程式碼。** 之後另有「實作計畫」階段。
> 唯一真相來源：`../2026-06-12-rimwar-empire-investigations.md`（調查 A–K + 13 mod 盤點）、
> `../2026-06-12-empire-rimwar-war-cluster.md`（戰爭叢集三 mod 構想）。
> 本計畫**大量交叉引用**調查段落代號（A–K）與原碼座標（`RW:行`/`VOE:行`/檔案:行），不重抄。

## 檔案索引

| 檔案 | 一句話 |
|---|---|
| `00-vision.md` | 願景、八大支柱、檔案索引、調查↔支柱對照（本檔）。 |
| `01-architecture.md` | mod 組織架構、依賴 DAG、建造順序、`named-officers` 硬前置論證。 |
| `02-mod-named-officers.md` | 共用基礎層：具名 pawn+屬性 comp+懶生成（E/F 共消費）。 |
| `03-mod-settlement-lords.md` | 聚落領主/官員系統（F/G）+ 點數影響 postfix。 |
| `04-mod-warband-generals.md` | warband 將領戰力（E）+ ResolveCombat 注入。 |
| `05-mod-city-economy.md` | 城池財富/防禦單一 comp（H/K）+ Empire registry participant。 |
| `06-relations-and-lord-actions.md` | 關係雙軌（I）+ 領主決策層 `ILordAction`（J/K）。 |
| `07-mod-faction-politics-rebellion.md` | faction-politics 擴充：叛亂改寫成領主帶城叛變（F×叛亂）。 |
| `08-rimwar-empire-linkage.md` | RimWar×Empire 聯動（C 八機會選序）+ 進行中片段定位。 |
| `09-roadmap.md` | 分期路線圖（地基→消費層→聯動），每期可獨立驗證。 |

## 願景一句話

在裝有 Rim War + Empire Refactored 的 RimWorld 上，配合既有自家 mod 叢集，
把**抽象點數戰爭**升級成**有具名人物**的三國志式大戰略：
名將率軍、太守治城、城池興衰、哨站擴張、叛亂分裂、勢力消長。

## 八大支柱 → 調查 / mod 對照

| 支柱 | 核心調查 | 主要 mod（消費/承載） |
|---|---|---|
| ① 具名職官（領主+將領共用基礎） | E、F、I | **新建** `pas.named-officers` |
| ② 城池發展經濟 | H、K | **新建** city-economy（RimWar comp + Empire participant） |
| ③ 哨站擴張 | D、J | `npc-outposts`（hook）+ `npc-outposts-rimwar`（權重） |
| ④ warband 將領戰力 | E | **新建** warband-generals |
| ⑤ 聚落點數受治理影響 | F、G、K | **新建** settlement-lords（postfix 逐城補乘/扣） |
| ⑥ 叛亂分裂 | F、I | `faction-politics` 擴充（領主帶城叛變） |
| ⑦ RimWar×Empire 聯動 | C | 既有 `empire-warfare` 上長新 participant |
| ⑧ 封存哨站 | （盤點）、B | `colony-archival-outpost`（E1 進行中）+ B 影子 Settlement |

## 設計鐵律（貫穿全計畫）

- **不動派系級係數**：`growthAttribute`/`combatAttribute`（`RW:1171/1173`）共享，動之污染全派系；
  領主/將領倍率**一律逐城/逐 warband 局部補乘**（G 的根本論點）。
- **複用勿重造**：faction-politics 的具名 pawn 管線、npc-outposts 的兩 hook、戰爭叢集三 mod
  皆已驗證可用（見 `01`）；新功能**掛載其上、勿改寫**。
- **Empire 端優先免 Harmony**：走 `FCInterfaces` + Registry；注意 `ClearCaches` 陷阱（C 通用陷阱）。
- **每檔 < 200 行**（使用者硬規定）。
