# Pocket Dimension Example（異空間門範例 mod）

> RimWorld 1.6。packageId `pas.pocketdimension.example`。**零 Harmony、零 DLC 相依**。
> 對應可行性報告：`~/repo/pas/analysis/rimworld_mods/_mod_ideas/05_pocket_dimension.md`。

## 目標

示範「可建造的異空間門 → 一張小型持久 pocket map → pawn 自由進出」的**最小 C# 集合**，
完整站在原版 1.5+ pocket map 機制（`MapPortal` / `PocketMapExit` / `PocketMapUtility`）之上。

## 玩家體驗

1. 建築選單（雜項）建造「dimension door」（3x3，鋼 120 + 零件 4）。
2. 點門 →「Enter dimension door...」gizmo（原版 `Dialog_EnterPortal`，可批量選人＋裝載物資），或右鍵單人進入。
3. 第一次有人走進去時才生成 50x50 異空間地圖（恆溫 21°C、全圖岩頂、金屬地板房間、中央出口）。
4. 地圖持久存在：可劃區、蓋房、種菇（有燈的話）、存檔讀檔全支援。
5. 拆除門 → 裡面所有 pawn 與物品被 Skip 傳送回門口，異空間回收。

## 檔案結構

| 檔案 | 內容 |
|---|---|
| `1.6/Defs/ThingDefs_Buildings/PDE_Buildings.xml` | 門（`PDE_DimensionDoor`，自訂 thingClass）＋出口（`PDE_DimensionExit`，**原版 `PocketMapExit` thingClass，零 C#**） |
| `1.6/Defs/MapGeneration/PDE_MapGenerator.xml` | `MapGeneratorDef PDE_PocketDimension`（isUnderground＋pocketMapProperties）＋單一 GenStepDef |
| `Source/Building_PocketDimensionDoor.cs` | `MapPortal` 子類：**唯一職責＝Destroy 時疏散＋回收 pocket map** |
| `Source/GenStep_PocketRoom.cs` | 鋪地、圍岩牆、中央生出口、設 PlayerStartSpot；參數全由 XML 餵 |
| `Source/PlaceWorker_NotInPocketMap.cs` | 禁止在異空間內再蓋門（防巢狀） |

## 最小 C# 集合（就這三個類）

原版免費提供的（完全不用寫）：
- lazy 生成：`MapPortal.GetOtherMap()` 首次進入呼叫 `PocketMapUtility.GeneratePocketMap`（`RimWorld/MapPortal.cs:247-254,324-334`）
- 入口/出口互綁：`PocketMapExit.SpawnSetup` 讀 `PocketMapUtility.currentlyGeneratingPortal`（`RimWorld/PocketMapExit.cs:27-34`）
- 進出 UI 與 Job：`Dialog_EnterPortal`、`FloatMenuOptionProvider_EnterMapPortal`、`JobDriver_EnterPortal`（DeSpawn+GenSpawn，`RimWorld/JobDriver_EnterPortal.cs:57-58`）、`HaulToPortal` 搬運、`ITab_ContentsMapPortal`
- 存檔：`MapPortal.ExposeData` 的 `Scribe_References(pocketMap/exit)`（`RimWorld/MapPortal.cs:120-121`）；世界層 `Find.World.pocketMaps`
- 邊界：pocket map 不吃襲擊（`PocketMapParent` 無 incident target tags）、威脅點轉宿主圖（`RimWorld/StorytellerUtility.cs:133-135`）、恆溫（`Verse/MapTemperature.cs:33`）、敗逃 AI 自動走 portal 離開（`Verse.AI/JobGiver_ExitMap.cs:45-53`）、宿主地圖被棄時連動回收（`Verse/Game.cs:772-778`）

必須自己寫的（原版缺口）：
1. **回收**：原版 `MapPortal` 被摧毀不回收 pocket map（地圖漏掉、pawn 困死）→ `Building_PocketDimensionDoor.Destroy` 用 `SkipUtility.SkipTo` 疏散再 `DestroyPocketMap`（仿 Anomaly 迷宮 `RimWorld/LabyrinthMapComponent.cs:70-98`）。
2. **出口生成**：`MapPortalProperties.exitDef` 是死欄位（原版程式碼無人讀），出口必須由 GenStep spawn（原版走 `GenStep_PlaceCaveExit` 硬編碼 `CaveExit`）→ 自寫 `GenStep_PocketRoom` 順便造房間。
3. **防巢狀**：PlaceWorker 擋掉在 pocket map 內建門。

## 建置

```bash
cd pocket-dimension-example
dotnet build Source/PocketDimensionExample.csproj -c Release
# 產物：1.6/Assemblies/PocketDimensionExample.dll
```

Managed 路徑自動偵測 `RimWorldLinux_Data`，可用環境變數 `RimWorldManaged` 覆寫。
2026-07-16 於 Linux（net48 / dotnet build）編譯通過，0 warning 0 error。

## 已知限制／留待實機驗證

- **未實機跑過**（依工作限制不啟動遊戲）。留驗：門建成→首次進入生圖、出口互綁、批量裝載、拆門疏散、存讀檔。
- 出口貼圖借用原版 `Things/Building/Misc/CaveExit/CaveExit`（垂降繩），門也用同一張——正式化時應自繪。
- 異空間全圖 `RoofRockThick`（isUnderground 自動鋪）：房間内是「厚岩頂」，不可種太陽燈作物；照明要自己拉電＋燈。
- 電力不跨圖：門內外電網獨立（跨圖供電參考 RV 的 `CompLinkedFueld` 直接引用充電手法）。
- 商隊/太空船場景未處理：門是固定建築，不會像 RV 一樣跟著載具移動（RV 需要 `CompTickRare` 同步 `Parent.Tile`）。
