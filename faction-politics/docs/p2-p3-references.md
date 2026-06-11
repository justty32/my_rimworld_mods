# P2／P3 設計參照（取自 pas 分析素材）

> 來源：`pas/analysis/rimworld_mods/`（已分析的published mod）。本檔僅作後階段（P2 玩家互動、P3 同盟/合併/通用生命週期）的設計錨點，**不影響 P1**。所有座標為各 mod 反編譯源行號（分析當時記錄）。

## 1. faction-territories — 聚落易主／附庸（P2 玩家互動 + P3 割讓的權威參照）

`jaeger972.factionterritories`（單 DLL 16317 行，重 C#）。其 **Vassalage** 子系統做的正是我們 P2/P3 要碰的「玩家對聚落施加外交動作、聚落在派系間轉手」。

| 元件 | 座標 | 對本案的意義 |
|---|---|---|
| 玩家入口 gizmo | `Settlement_GetGizmos_Vassalise:7742` → `Dialog_Vassalise`/`Dialog_Vassalage` | **P2 安撫/煽動 UI 的範本**：選中聚落長 gizmo 開對話框 |
| 外交貨幣 | `VassalagePointsComponent:8496`（藩屬點數） | **P2 玩家資源模型**：花點數安撫（降反叛進展）/煽動（升） |
| 聚落割讓給他派 | `ExecuteCedeToFactionAtTile:10878` | 與本案 `FactionSplitter` 的 `SetFaction` 倒戈**同類動作**，可比對其轉手細節（是否同樣 in-place） |
| 附庸化 / 轉交毀城 | `ExecuteVassalisationAtTile:10817`、`ExecuteCedeDestroyedSettlementToFaction:10986` | P3 同盟/附庸生命週期參照 |
| 攔截毀城信件 | `InterceptBaseDestroyedLetterPatch:7982` | **分歧點**：它用 Harmony 攔信件提供選項；本案零 Harmony，P2 同等效果須改走 gizmo/alert 而非攔信 |

純 XML 可擴充面極小（`details/extension_points.md`：領土演算法/附庸/入侵全鎖 C#）——提醒：若 P3 要與它互通，多半得 fork，不如自建。

## 2. empire-refactored — 聚落四維狀態機（P2 反叛模型升級參照）

`WorldSettlementFC : Settlement`（`Worldobjects/WorldSettlementFC.cs:19`）用**四維狀態** `unrest`/`loyalty`/`happiness`/`prosperity`（`:78-94`）驅動事件。

- 對本案 `RebellionProfileDef.progress`（單一 float）的啟發：**P2 可把反叛進展拆成 loyalty 主軸**——玩家安撫/善政升 loyalty、苛政/誤傷降 loyalty，loyalty 跌破門檻才開始累積反叛進展。比單一速率更有玩法深度。
- `FCEventDef : Def`（`Defs/FCEventDef.cs:7`）：事件以 def 定義「觸發時間/權重 + 四維狀態 min/max 門檻 + requiredResource/Research/minTechLevel」。**與本案零-Harmony、全 def 化哲學一致**，P2 事件系統可照此 def 化。

## 3. 架構交叉驗證（對 P1 既有決策的背書）

- **empire-refactored 的 9 個 compat 子模組**靠 `LoadFolders.xml` 的 `IfModActive` 條件載入獨立 DLL（`architecture/02_compat_modules.md`）——與本案 `Compat/NpcOutposts` 條件 assembly bridge **完全同款技法**。佐證我們的軟相容架構是業界既有實踐，非自創冒險。
- **warband-warfare 的 League 同盟系統**（`WarbandWarfareQuestline.dll`，`FactionTraitDef`/`PolicyDef`/`PolicyCategoryDef`）：P3「同盟/合併」的 def 體系參照（同盟政策＝資料 XML + workerClass C# 的混合模式）。

## 4. 未解（仍待本機反編譯源）

- Task 0 #3：`FactionGeneratorParms(FactionDef, IdeoGenerationParms, bool hidden)` 三參多載——本批 mod 分析皆為高階剖析，無原始建構子呼叫，無法交叉證實；仍須本機 grep `BackCompatibility.cs:424` / `FactionGenerator.cs:130` 確認。

## 5. 補記 2026-06-11 晚：實裝版到位

- **Empire Refactored 1.3.74 實裝於本機** workshop `3701480464`（`Matathias.Empire`，1.6，**含 Empire.pdb 符號檔**）——P2 設計時可直接 ikdasm/反編譯對照真實 IL 與符號，不再僅靠 pas 高階剖析。
- §4 已解：Task 0 #3 經 Krafs ref 組件 monop 核實（見 session_log 2026-06-11），三參實簽 `(FactionDef, IdeoGenerationParms, Nullable<bool> hidden)`。
- P1 軟相容已落地：`Compat/Empire/Patches/PColonyDisabled.xml` 對 `PColony` 掛 `PoliticsDisabledExtension`（玩家附庸帝國不參與反叛/分裂），`loadFolders IfModActive="Matathias.Empire"` 條件載入。
