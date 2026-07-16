# N5 電力採樣 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓玩家在採樣端放「供電節點」量測該電網平均淨功率，封存後在主基地放「電力輸出端 outlet」把該哨站的有號淨功率（正=發電、負=抽電）即時灌進主基地電網。

**Architecture:** 採樣端 `CAO_PowerSamplingNode`（`CompPowerTransmitter`，一圖一個）併入電網；`ColonyArchivalTracker.MapComponentTick` 每 2500 tick 讀 `PowerNet.CurrentEnergyGainRate()` 累加，封存時平均成 `ProductivitySnapshot.avgNetPowerW`（有號）。產出端 `CAO_PowerOutlet`（`CompArchivalPowerOutlet : CompPowerPlant`）每 tick 把連線哨站的 `PowerWatts` 設成 `DesiredPowerOutput`；香草 `PowerNet` 自然結算跨地圖供/耗電，主基地電網本身即緩衝。outlet↔outpost 走雙向 `Scribe_References`（comp 存 `Outpost_Sampled`，outpost 存 outlet 建築 `Thing`）。

**Tech Stack:** RimWorld 1.6 / C# net48 / VOE Outposts / Harmony（沿用既有，無新 patch）。建置 `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`。

---

## 測試節奏說明（本專案無單元測試 runtime）

RimWorld mod 沒有可執行的單元測試框架。本專案的「測試」三層：

1. **`dotnet build Source/ColonyArchivalOutpost.csproj -c Release`** — 必須編譯通過（型別/簽章正確）。
2. **`python3 tests/healthcheck.py`** — 靜態檢查：XML well-formed、Keyed 鍵 EN==zh-Hant 一致、程式碼 `"CAO.x".Translate` 鍵都有定義、About 相依齊全。
3. **實機驗證**（最後一個任務）— 在遊戲內手動跑驗收清單。

因此每個任務的「測試」步驟＝**build 通過 + healthcheck 通過**，最後 commit。Task 11 是純實機驗收清單。

---

## 關鍵 API 事實（已對照 1.6 反編譯確認）

- `CompPower.WattsToWattDaysPerTick = 1.6666667E-05f`（`static readonly`）。
- `PowerNet.CurrentEnergyGainRate()` 回 Wd/tick，**只加總 `PowerOn==true` 的 powerComps 的 `EnergyOutputPerTick`**（不含電池儲量）。淨瓦 = `CurrentEnergyGainRate() / WattsToWattDaysPerTick`，發電正、耗電負。
- `CompPowerTrader.PowerOutput` setter **不夾值**，可為負（負=消耗）。
- `CompPowerPlant.UpdateDesiredPowerOutput()` 設 `PowerOutput = DesiredPowerOutput`（**不夾成 ≥0**）；`PostSpawnSetup` 在 `Props.PowerConsumption < 0f` 時 `PowerOn=true`。→ 把 `basePowerConsumption` 設負即被當「發電機」自動上電，且 `DesiredPowerOutput` 可回有號值。
- `PowerNet.PowerNetTick` 斷電分支：把 `PowerOn && EnergyOutputPerTick<0` 的 comp 隨機關閉；盈餘分支再把 `WantsToBeOn` 的隨機開回。→ outlet 為負（抽電）時，家網缺電會被當家用電器隨機關閉、有電自動恢復＝「家網即緩衝」語意，免費取得。
- `CompProperties_Power.basePowerConsumption` 為 private 欄位，但 Verse XML loader 以反射設值，`<basePowerConsumption>-1</basePowerConsumption>` 有效。
- 香草貼圖 `Things/Building/Power/PowerSwitch`（`Graphic_Single`）確認存在，作無貼圖方框的底圖（`<color>` 改色）。
- `Things/Building/Power/PowerSwitch` 的 `PowerSwitch` def 用 `passability=Standable` + `CompPowerTransmitter`；本計畫節點/outlet 比照，外加 `<building><isEdifice>false</isEdifice>` 允許疊牆任意放。
- `Thing` 與 `WorldObject` 皆 `ILoadReferenceable` → `Scribe_References` 可雙向綁。Comp **不是** `ILoadReferenceable`，故反向引用存 outlet 的**建築 Thing**（再 `TryGetComp` 取 comp）。

---

## File Structure

**新建：**
- `Source/CAODefOf.cs` — `[DefOf]` 持兩個 `ThingDef`（節點、outlet），供程式碼以強型別取得 def。
- `Source/CompArchivalPowerOutlet.cs` — `CompProperties_ArchivalPowerOutlet : CompProperties_Power` + `CompArchivalPowerOutlet : CompPowerPlant`（連線狀態、有號輸出、gizmo、inspect、存讀檔）。
- `Source/PlaceWorker_OnlyOnePerMap.cs` — 一張地圖只允許一個指定 def（含 blueprint/frame）。
- `Defs/ThingDefs/CAO_Power.xml` — 兩個 `ThingDef`（節點、outlet）。

**修改：**
- `Source/ProductivitySnapshot.cs` — `+float avgNetPowerW`、`+bool applyPowerSampling`。
- `Source/ColonyArchivalTracker.cs` — `+double powerAccumW`、`+int powerSampleCount`、`+MapComponentTick()`。
- `Source/ArchivalService.cs` — `ComputeSnapshot` 設 `avgNetPowerW`；`Archive(... , bool applyPower)`；`+static TryGetNodePowerWatts`。
- `Source/Outpost_Sampled.cs` — `+Thing connectedOutlet` ref、`+PowerWatts`、`+SetConnectedOutlet/ConnectedOutlet/NotifyOutletDestroyed`、`Destroy()` override、Scribe。
- `Source/Dialog_ArchivalConfirm.cs` — 電力預覽行 + 計入/無視開關，串進 `Archive`。
- `Source/Dialog_SamplingStatus.cs` — 即時供電節點淨功率行（`+map` 欄位）。
- `Source/ColonyArchivalOutpost.csproj` — `+3` `<Compile Include>`。
- `Languages/{English,ChineseTraditional,ChineseSimplified}/Keyed/CAO.xml` — `CAO.Power.*` 鍵。
- `tests/healthcheck.py` — 檢查 `CAO_Power.xml` 定義兩個 defName。

---

## Task 1: ProductivitySnapshot 加電力欄位

**Files:**
- Modify: `Source/ProductivitySnapshot.cs`

- [ ] **Step 1: 加兩個欄位**

在 N6b 欄位區塊後（`Source/ProductivitySnapshot.cs:27` `applyHediffDeltas` 之後）插入：

```csharp
        // N5：電力採樣——封存窗平均淨功率（有號，瓦；正=發電、負=耗電）
        public float avgNetPowerW;
        public bool applyPowerSampling;
```

- [ ] **Step 2: Clone() 帶上新欄位**

在 `Clone()` 的物件初始化器（`applyHediffDeltas = applyHediffDeltas` 那行）後加一行（注意前一行補逗號）：

```csharp
                applyHediffDeltas = applyHediffDeltas,
                avgNetPowerW = avgNetPowerW,
                applyPowerSampling = applyPowerSampling
```

- [ ] **Step 3: ExposeData() 存兩欄**

在 `ExposeData()` 內 `Scribe_Values.Look(ref applyHediffDeltas, "applyHediffDeltas", false);` 之後加：

```csharp
            Scribe_Values.Look(ref avgNetPowerW, "avgNetPowerW", 0f);
            Scribe_Values.Look(ref applyPowerSampling, "applyPowerSampling", false);
```

> 註：`IsEmpty` 不納入電力——電力是否套用由 `applyPowerSampling` 獨立決定，與「snapshot 是否值得註冊成類型」無關，維持原語意。

- [ ] **Step 4: build**

Run: `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`
Expected: Build succeeded.

- [ ] **Step 5: commit**

```bash
git add Source/ProductivitySnapshot.cs
git commit -m "feat(n5): add avgNetPowerW/applyPowerSampling to ProductivitySnapshot"
```

---

## Task 2: CAODefOf

**Files:**
- Create: `Source/CAODefOf.cs`
- Modify: `Source/ColonyArchivalOutpost.csproj`

> 先建 DefOf（Task 4 的 tracker、Task 5 的 service、Task 9 的 PlaceWorker 都要強型別取得節點 def）。此時 ThingDef 尚未在 XML 定義，DefOf 欄位執行期才解析，編譯不需要 def 存在。

- [ ] **Step 1: 建 CAODefOf.cs**

Create `Source/CAODefOf.cs`：

```csharp
using RimWorld;
using Verse;

namespace ColonyArchivalOutpost
{
    [DefOf]
    public static class CAODefOf
    {
        public static ThingDef CAO_PowerSamplingNode;
        public static ThingDef CAO_PowerOutlet;

        static CAODefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CAODefOf));
        }
    }
}
```

- [ ] **Step 2: csproj 註冊**

在 `Source/ColonyArchivalOutpost.csproj` 的 `<ItemGroup>` 編譯清單內，`<Compile Include="ProductivitySnapshot.cs" />` 之後加：

```xml
    <Compile Include="CAODefOf.cs" />
```

- [ ] **Step 3: build**

Run: `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`
Expected: Build succeeded.（DefOf 欄位無 def 對應只會在執行期 log warning，編譯不報錯。）

- [ ] **Step 4: commit**

```bash
git add Source/CAODefOf.cs Source/ColonyArchivalOutpost.csproj
git commit -m "feat(n5): add CAODefOf for power building defs"
```

---

## Task 3: PlaceWorker_OnlyOnePerMap

**Files:**
- Create: `Source/PlaceWorker_OnlyOnePerMap.cs`
- Modify: `Source/ColonyArchivalOutpost.csproj`

- [ ] **Step 1: 建 PlaceWorker**

Create `Source/PlaceWorker_OnlyOnePerMap.cs`：

```csharp
using Verse;

namespace ColonyArchivalOutpost
{
    // 一張地圖只允許一個指定 def（含已建成、藍圖、施工框）。供電節點用，免去多電網去重。
    public class PlaceWorker_OnlyOnePerMap : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc,
            Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (!(checkingDef is ThingDef tDef)) return true;

            if (HasAny(map, tDef, thingToIgnore)
                || (tDef.blueprintDef != null && HasAny(map, tDef.blueprintDef, thingToIgnore))
                || (tDef.frameDef != null && HasAny(map, tDef.frameDef, thingToIgnore)))
            {
                return new AcceptanceReport("CAO.Power.OnlyOneNode".Translate());
            }
            return true;
        }

        private static bool HasAny(Map map, ThingDef def, Thing ignore)
        {
            foreach (var t in map.listerThings.ThingsOfDef(def))
                if (t != ignore) return true;
            return false;
        }
    }
}
```

- [ ] **Step 2: csproj 註冊**

在編譯清單 `<Compile Include="CAODefOf.cs" />` 之後加：

```xml
    <Compile Include="PlaceWorker_OnlyOnePerMap.cs" />
```

- [ ] **Step 3: build**

Run: `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`
Expected: Build succeeded.（`CAO.Power.OnlyOneNode` 鍵在 Task 8 才加；healthcheck 此時會抓到缺鍵，故本任務暫不跑 healthcheck，留待 Task 8。）

- [ ] **Step 4: commit**

```bash
git add Source/PlaceWorker_OnlyOnePerMap.cs Source/ColonyArchivalOutpost.csproj
git commit -m "feat(n5): add PlaceWorker_OnlyOnePerMap"
```

---

## Task 4: ColonyArchivalTracker 每小時採樣

**Files:**
- Modify: `Source/ColonyArchivalTracker.cs`
- Modify: `Source/ArchivalService.cs`（先加 `TryGetNodePowerWatts` 靜態助手，tracker 與 Dialog 共用，DRY）

- [ ] **Step 1: ArchivalService 加靜態助手**

在 `Source/ArchivalService.cs` 的 `TotalHealableSeverity` 方法之後（`PassionFactor` 之前）插入：

```csharp
        // N5：讀某地圖供電節點所在電網的即時淨功率（瓦，有號）。
        // 回傳 false = 無節點；true 且 watts=採樣值（節點未接網則 watts=0）。
        public static bool TryGetNodePowerWatts(Map map, out float watts)
        {
            watts = 0f;
            if (map == null) return false;
            var node = map.listerThings.ThingsOfDef(CAODefOf.CAO_PowerSamplingNode).FirstOrDefault();
            if (node == null) return false;
            var net = node.TryGetComp<CompPower>()?.PowerNet;
            if (net == null) return true; // 節點已放但未併入任何電網 → 讀 0
            watts = net.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick;
            return true;
        }
```

（`ArchivalService.cs` 已有 `using System.Linq;`、`using RimWorld;`、`using Verse;`，`CompPower`/`PowerNet` 在 `RimWorld` 命名空間，無需新增 using。）

- [ ] **Step 2: tracker 加欄位**

在 `Source/ColonyArchivalTracker.cs` 的 `startHediffSnapshots` 欄位宣告（`:23`）之後加：

```csharp
        // N5：電力採樣累加器
        public double powerAccumW;
        public int powerSampleCount;
```

- [ ] **Step 3: BeginSampling/Reset 歸零**

在 `BeginSampling()` 方法開頭 `isSampling = true;` 之後加：

```csharp
            powerAccumW = 0d;
            powerSampleCount = 0;
```

在 `Reset()` 方法 `startHediffSnapshots = new List<PawnHediffSnapshot>();` 之後加：

```csharp
            powerAccumW = 0d;
            powerSampleCount = 0;
```

- [ ] **Step 4: MapComponentTick 每 2500 tick 採樣**

在 `Reset()` 方法之後、`TotalHealableSeverity` 之前，插入：

```csharp
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!isSampling) return;
            if (Find.TickManager.TicksGame % 2500 != 0) return;
            if (ArchivalService.TryGetNodePowerWatts(map, out float watts))
            {
                powerAccumW += watts;
                powerSampleCount++;
            }
        }
```

> 設計取捨：無節點時 `TryGetNodePowerWatts` 回 false → 不累加、`powerSampleCount` 停在 0 → 封存時無電力資料、確認窗不顯示電力行（符合「沒放節點＝預設不計入」）。節點已放但未接網時回 true、watts=0 → 計入一筆 0（玩家責任：把節點接上電網）。

- [ ] **Step 5: ExposeData 存累加器**

在 `Source/ColonyArchivalTracker.cs` 的 `ExposeData()` 內 `Scribe_Collections.Look(ref startHediffSnapshots, "caoStartHediffSnapshots", LookMode.Deep);` 之後加：

```csharp
            Scribe_Values.Look(ref powerAccumW, "caoPowerAccumW", 0d);
            Scribe_Values.Look(ref powerSampleCount, "caoPowerSampleCount", 0);
```

- [ ] **Step 6: build**

Run: `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`
Expected: Build succeeded.

- [ ] **Step 7: commit**

```bash
git add Source/ColonyArchivalTracker.cs Source/ArchivalService.cs
git commit -m "feat(n5): hourly power sampling in tracker + TryGetNodePowerWatts helper"
```

---

## Task 5: ArchivalService.ComputeSnapshot/Archive 接電力

**Files:**
- Modify: `Source/ArchivalService.cs`

- [ ] **Step 1: ComputeSnapshot 寫入 avgNetPowerW**

在 `Source/ArchivalService.cs` 的 `ComputeSnapshot` 方法 `return snapshot;` 之前插入：

```csharp
            // N5：電力——平均採樣窗淨功率（有號）
            if (tracker.powerSampleCount > 0)
                snapshot.avgNetPowerW = (float)(tracker.powerAccumW / tracker.powerSampleCount);
```

- [ ] **Step 2: Archive 加 applyPower 參數**

把 `Archive(...)` 簽章（`Source/ArchivalService.cs:161-163`）末尾加一個參數：

```csharp
        public static void Archive(Map map, string name = null, string iconPath = null,
            bool perPawn = false, bool applySkillXP = false, bool applyHealthDelta = false,
            bool applyHediffDeltas = false, bool applyHealthDeterioration = false,
            bool applyPower = false)
```

- [ ] **Step 3: Archive 設 applyPowerSampling**

在 `Archive` 內 N6 傷勢惡化開關 block（`if (applyHealthDeterioration && snapshot.avgHealthDeltaPerDay > 0f) snapshot.applyHealthDeterioration = true;`）之後加：

```csharp
            // N5：電力採樣開關（只有採到資料才套用）
            if (applyPower && tracker.powerSampleCount > 0)
                snapshot.applyPowerSampling = true;
```

- [ ] **Step 4: build**

Run: `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`
Expected: Build succeeded.

- [ ] **Step 5: commit**

```bash
git add Source/ArchivalService.cs
git commit -m "feat(n5): compute avgNetPowerW and wire applyPower into Archive"
```

---

## Task 6: Outpost_Sampled 加 PowerWatts 與反向引用

**Files:**
- Modify: `Source/Outpost_Sampled.cs`

> 反向引用存 outlet 的**建築 `Thing`**（`ILoadReferenceable`），不是 comp。`CompArchivalPowerOutlet`（Task 7）再以 `TryGetComp` 取得。

- [ ] **Step 1: 加欄位與唯讀屬性**

在 `Source/Outpost_Sampled.cs` 的 `chosenIconPath` 欄位（`:18`）之後加：

```csharp
        // N5：連線的 outlet 建築（反向引用，供「一哨站一 outlet」去重；跨 WorldObject↔map Thing 走 Scribe_References）
        private Thing connectedOutlet;
        public Thing ConnectedOutlet => connectedOutlet;
        public void SetConnectedOutlet(Thing t) => connectedOutlet = t;

        // N5：有號平均淨功率（瓦）；未套用電力採樣時回 0。outlet 每 tick 讀此值灌進主基地電網。
        public float PowerWatts => snapshot != null && snapshot.applyPowerSampling ? snapshot.avgNetPowerW : 0f;

        // outlet 連線浮選單用：本哨站是否有可輸出的電力資料
        public bool HasPowerSampling => snapshot != null && snapshot.applyPowerSampling;

        // 哨站被毀時由其本身呼叫，通知 outlet comp 清引用（避免 Destroy 互呼遞迴）
        public void NotifyOutletDestroyed() => connectedOutlet = null;
```

- [ ] **Step 2: Destroy override 斷開 outlet**

在 `Source/Outpost_Sampled.cs` 的 `ExposeData()` 方法之前插入：

```csharp
        public override void Destroy()
        {
            if (connectedOutlet != null)
            {
                connectedOutlet.TryGetComp<CompArchivalPowerOutlet>()?.NotifyOutpostDestroyed();
                connectedOutlet = null;
            }
            base.Destroy();
        }
```

- [ ] **Step 3: ExposeData 存反向引用**

在 `Source/Outpost_Sampled.cs` 的 `ExposeData()` 內 `Scribe_Values.Look(ref chosenIconPath, "caoIconPath", null);` 之後加：

```csharp
            Scribe_References.Look(ref connectedOutlet, "caoConnectedOutlet");
```

- [ ] **Step 4: build**

Run: `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`
Expected: **FAIL** — `CompArchivalPowerOutlet` 與 `NotifyOutpostDestroyed` 尚未定義（Task 7 建立）。這是預期的；Task 7 完成後即通過。

> 若採用 subagent-driven，請把 Task 6 與 Task 7 視為一組連續完成再 build/commit。以下 commit 步驟移至 Task 7 末尾一起做。

- [ ] **Step 5:（暫不 commit，待 Task 7）**

---

## Task 7: CompArchivalPowerOutlet

**Files:**
- Create: `Source/CompArchivalPowerOutlet.cs`
- Modify: `Source/ColonyArchivalOutpost.csproj`

- [ ] **Step 1: 建 comp + properties**

Create `Source/CompArchivalPowerOutlet.cs`：

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ColonyArchivalOutpost
{
    public class CompProperties_ArchivalPowerOutlet : CompProperties_Power
    {
        public CompProperties_ArchivalPowerOutlet()
        {
            compClass = typeof(CompArchivalPowerOutlet);
        }
    }

    // 產出端：把連線哨站的有號 PowerWatts 即時灌進主基地電網。
    // 繼承 CompPowerPlant 取得 PowerOn 管理與 UpdateDesiredPowerOutput 鉤子；
    // DesiredPowerOutput 不被夾成 ≥0，故可輸出負值（抽電）。
    // ThingDef 的 basePowerConsumption 設負 → PostSpawnSetup 視為發電機自動上電。
    public class CompArchivalPowerOutlet : CompPowerPlant
    {
        private Outpost_Sampled connectedOutpost;

        protected override float DesiredPowerOutput
        {
            get
            {
                if (connectedOutpost == null || connectedOutpost.Destroyed)
                {
                    connectedOutpost = null;
                    return 0f;
                }
                return connectedOutpost.PowerWatts;
            }
        }

        public void ConnectTo(Outpost_Sampled outpost)
        {
            Disconnect();
            // 一哨站一 outlet：若該哨站已連別的 outlet，先把舊 outlet 斷開
            Thing existing = outpost.ConnectedOutlet;
            if (existing != null && existing != parent)
                existing.TryGetComp<CompArchivalPowerOutlet>()?.Disconnect();

            connectedOutpost = outpost;
            outpost.SetConnectedOutlet(parent);
        }

        public void Disconnect()
        {
            if (connectedOutpost != null)
            {
                if (connectedOutpost.ConnectedOutlet == parent)
                    connectedOutpost.SetConnectedOutlet(null);
                connectedOutpost = null;
            }
        }

        // 由 Outpost_Sampled.Destroy 呼叫：只清本端引用，不回呼（哨站已在自毀流程）
        public void NotifyOutpostDestroyed()
        {
            connectedOutpost = null;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Disconnect();
            base.PostDestroy(mode, previousMap);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref connectedOutpost, "caoConnectedOutpost");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            yield return new Command_Action
            {
                defaultLabel = "CAO.Power.Connect".Translate(),
                defaultDesc = "CAO.Power.Connect.Desc".Translate(),
                icon = TexCommand.GatherSpotActive,
                action = OpenConnectMenu
            };

            if (connectedOutpost != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "CAO.Power.Disconnect".Translate(),
                    icon = TexCommand.GatherSpotActive,
                    action = Disconnect
                };
            }
        }

        private void OpenConnectMenu()
        {
            var opts = new List<FloatMenuOption>();
            foreach (var o in Find.WorldObjects.AllWorldObjects.OfType<Outpost_Sampled>())
            {
                if (!o.HasPowerSampling) continue;
                var outpost = o;
                opts.Add(new FloatMenuOption(outpost.Name, () => ConnectTo(outpost)));
            }
            if (opts.Count == 0)
                opts.Add(new FloatMenuOption("CAO.Power.NoOutposts".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();
            if (connectedOutpost != null && !connectedOutpost.Destroyed)
            {
                sb.AppendLine("CAO.Power.Connected".Translate(connectedOutpost.Name));
                sb.Append("CAO.Power.Output".Translate(PowerOutput.ToString("F0")));
            }
            else
            {
                sb.Append("CAO.Power.NotConnected".Translate());
            }
            string base2 = base.CompInspectStringExtra();
            if (!base2.NullOrEmpty())
                sb.Append("\n" + base2);
            return sb.ToString();
        }
    }
}
```

- [ ] **Step 2: csproj 註冊**

在編譯清單 `<Compile Include="PlaceWorker_OnlyOnePerMap.cs" />` 之後加：

```xml
    <Compile Include="CompArchivalPowerOutlet.cs" />
```

- [ ] **Step 3: build（含 Task 6）**

Run: `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`
Expected: Build succeeded.（Task 6 對 `CompArchivalPowerOutlet` 的引用此時解析。）

- [ ] **Step 4: commit（含 Task 6）**

```bash
git add Source/Outpost_Sampled.cs Source/CompArchivalPowerOutlet.cs Source/ColonyArchivalOutpost.csproj
git commit -m "feat(n5): CompArchivalPowerOutlet + Outpost_Sampled cross-ref and PowerWatts"
```

---

## Task 8: 語系鍵（EN / zh-Hant / zh-Hans）

**Files:**
- Modify: `Languages/English/Keyed/CAO.xml`
- Modify: `Languages/ChineseTraditional/Keyed/CAO.xml`
- Modify: `Languages/ChineseSimplified/Keyed/CAO.xml`

> healthcheck 要求 EN Keyed 與 zh-Hant Keyed 鍵集合完全一致；zh-Hans 不被檢查但使用者重視，一併補齊。三檔加**相同鍵名**、各自語言的值。

- [ ] **Step 1: English**

在 `Languages/English/Keyed/CAO.xml` 的 `</LanguageData>` 之前插入：

```xml
  <!-- N5 power sampling -->
  <CAO.Power.OnlyOneNode>Only one power sampling node is allowed per map.</CAO.Power.OnlyOneNode>
  <CAO.Power.Connect>Connect to outpost</CAO.Power.Connect>
  <CAO.Power.Connect.Desc>Pick an archived outpost whose sampled net power this outlet feeds into (or draws from) the local power grid.</CAO.Power.Connect.Desc>
  <CAO.Power.Disconnect>Disconnect outpost</CAO.Power.Disconnect>
  <CAO.Power.NoOutposts>No archived outpost has power sampling data.</CAO.Power.NoOutposts>
  <CAO.Power.Connected>Connected: {0}</CAO.Power.Connected>
  <CAO.Power.NotConnected>Not connected to any outpost.</CAO.Power.NotConnected>
  <CAO.Power.Output>Net power: {0} W</CAO.Power.Output>
  <CAO.ArchivalConfirm.ApplyPower>Count grid power (avg net {0} W: positive generates, negative consumes)</CAO.ArchivalConfirm.ApplyPower>
  <CAO.SamplingStatus.NodePower>Sampling node net power: {0} W</CAO.SamplingStatus.NodePower>
  <CAO.SamplingStatus.NoNode>No power sampling node placed — power not sampled.</CAO.SamplingStatus.NoNode>
```

- [ ] **Step 2: ChineseTraditional**

在 `Languages/ChineseTraditional/Keyed/CAO.xml` 的 `</LanguageData>` 之前插入：

```xml
  <!-- N5 電力採樣 -->
  <CAO.Power.OnlyOneNode>每張地圖只能放置一個供電節點。</CAO.Power.OnlyOneNode>
  <CAO.Power.Connect>連接封存哨站</CAO.Power.Connect>
  <CAO.Power.Connect.Desc>選擇一個封存哨站，由此輸出端把該哨站採樣到的淨功率灌入（或抽取）本地電網。</CAO.Power.Connect.Desc>
  <CAO.Power.Disconnect>斷開哨站</CAO.Power.Disconnect>
  <CAO.Power.NoOutposts>沒有任何封存哨站具備電力採樣資料。</CAO.Power.NoOutposts>
  <CAO.Power.Connected>已連接：{0}</CAO.Power.Connected>
  <CAO.Power.NotConnected>未連接任何哨站。</CAO.Power.NotConnected>
  <CAO.Power.Output>淨功率：{0} W</CAO.Power.Output>
  <CAO.ArchivalConfirm.ApplyPower>計入電網消耗與產出（平均淨功率 {0} W：正值發電、負值耗電）</CAO.ArchivalConfirm.ApplyPower>
  <CAO.SamplingStatus.NodePower>供電節點淨功率：{0} W</CAO.SamplingStatus.NodePower>
  <CAO.SamplingStatus.NoNode>未放置供電節點——不採集電力。</CAO.SamplingStatus.NoNode>
```

- [ ] **Step 3: ChineseSimplified**

在 `Languages/ChineseSimplified/Keyed/CAO.xml` 的 `</LanguageData>` 之前插入：

```xml
  <!-- N5 电力采样 -->
  <CAO.Power.OnlyOneNode>每张地图只能放置一个供电节点。</CAO.Power.OnlyOneNode>
  <CAO.Power.Connect>连接封存哨站</CAO.Power.Connect>
  <CAO.Power.Connect.Desc>选择一个封存哨站，由此输出端把该哨站采样到的净功率灌入（或抽取）本地电网。</CAO.Power.Connect.Desc>
  <CAO.Power.Disconnect>断开哨站</CAO.Power.Disconnect>
  <CAO.Power.NoOutposts>没有任何封存哨站具备电力采样数据。</CAO.Power.NoOutposts>
  <CAO.Power.Connected>已连接：{0}</CAO.Power.Connected>
  <CAO.Power.NotConnected>未连接任何哨站。</CAO.Power.NotConnected>
  <CAO.Power.Output>净功率：{0} W</CAO.Power.Output>
  <CAO.ArchivalConfirm.ApplyPower>计入电网消耗与产出（平均净功率 {0} W：正值发电、负值耗电）</CAO.ArchivalConfirm.ApplyPower>
  <CAO.SamplingStatus.NodePower>供电节点净功率：{0} W</CAO.SamplingStatus.NodePower>
  <CAO.SamplingStatus.NoNode>未放置供电节点——不采集电力。</CAO.SamplingStatus.NoNode>
```

- [ ] **Step 4: healthcheck**

Run: `python3 tests/healthcheck.py`
Expected: `OK`（Task 3 的 `CAO.Power.OnlyOneNode` 與 Task 7 的 `CAO.Power.*` 鍵此時齊全；EN/zh-Hant 鍵集合一致）。

- [ ] **Step 5: commit**

```bash
git add Languages/English/Keyed/CAO.xml Languages/ChineseTraditional/Keyed/CAO.xml Languages/ChineseSimplified/Keyed/CAO.xml
git commit -m "i18n(n5): add CAO.Power.* keys (en/zh-Hant/zh-Hans)"
```

---

## Task 9: ThingDef XML（節點 + outlet）

**Files:**
- Create: `Defs/ThingDefs/CAO_Power.xml`

- [ ] **Step 1: 建 CAO_Power.xml**

Create `Defs/ThingDefs/CAO_Power.xml`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- 採樣端：供電節點。一圖一個，可疊牆任意放，併入所在電網量測淨功率。 -->
  <ThingDef ParentName="BuildingBase">
    <defName>CAO_PowerSamplingNode</defName>
    <label>power sampling node</label>
    <description>Joins the power grid it overlaps and measures that grid's net power (generation minus consumption) during a productivity sampling period. Place it touching wires or powered buildings. Only one per map. After archival, an archival power outlet replays the sampled net power at your home base.</description>
    <thingClass>Building</thingClass>
    <category>Building</category>
    <graphicData>
      <texPath>Things/Building/Power/PowerSwitch</texPath>
      <graphicClass>Graphic_Single</graphicClass>
      <shaderType>Transparent</shaderType>
      <color>(0.40,0.90,0.55)</color>
    </graphicData>
    <altitudeLayer>Building</altitudeLayer>
    <passability>Standable</passability>
    <leaveResourcesWhenKilled>false</leaveResourcesWhenKilled>
    <building>
      <isEdifice>false</isEdifice>
      <allowWireConnection>true</allowWireConnection>
      <ai_chillDestination>false</ai_chillDestination>
    </building>
    <statBases>
      <MaxHitPoints>80</MaxHitPoints>
      <WorkToBuild>120</WorkToBuild>
      <Flammability>0.5</Flammability>
      <Beauty>-2</Beauty>
    </statBases>
    <costList>
      <Steel>10</Steel>
      <ComponentIndustrial>1</ComponentIndustrial>
    </costList>
    <comps>
      <li Class="CompProperties_Power">
        <compClass>CompPowerTransmitter</compClass>
        <transmitsPower>true</transmitsPower>
      </li>
    </comps>
    <placeWorkers>
      <li>ColonyArchivalOutpost.PlaceWorker_OnlyOnePerMap</li>
    </placeWorkers>
    <rotatable>false</rotatable>
    <selectable>true</selectable>
    <tickerType>Normal</tickerType>
    <designationCategory>Power</designationCategory>
    <researchPrerequisites>
      <li>Electricity</li>
    </researchPrerequisites>
  </ThingDef>

  <!-- 產出端：電力輸出端 outlet。可疊牆任意放，可多個；連封存哨站，把其有號淨功率灌進本地電網。 -->
  <ThingDef ParentName="BuildingBase">
    <defName>CAO_PowerOutlet</defName>
    <label>archival power outlet</label>
    <description>Connect this to an archived outpost that sampled its power grid. Each tick it feeds the outpost's signed net power into the local grid: positive generates power here, negative draws power from here (covering the outpost's deficit). Your home grid is the buffer. Place it touching wires or powered buildings.</description>
    <thingClass>Building</thingClass>
    <category>Building</category>
    <graphicData>
      <texPath>Things/Building/Power/PowerSwitch</texPath>
      <graphicClass>Graphic_Single</graphicClass>
      <shaderType>Transparent</shaderType>
      <color>(0.40,0.62,1.00)</color>
    </graphicData>
    <altitudeLayer>Building</altitudeLayer>
    <passability>Standable</passability>
    <leaveResourcesWhenKilled>false</leaveResourcesWhenKilled>
    <building>
      <isEdifice>false</isEdifice>
      <allowWireConnection>true</allowWireConnection>
      <ai_chillDestination>false</ai_chillDestination>
    </building>
    <statBases>
      <MaxHitPoints>80</MaxHitPoints>
      <WorkToBuild>200</WorkToBuild>
      <Flammability>0.5</Flammability>
      <Beauty>-2</Beauty>
    </statBases>
    <costList>
      <Steel>15</Steel>
      <ComponentIndustrial>2</ComponentIndustrial>
    </costList>
    <comps>
      <li Class="ColonyArchivalOutpost.CompProperties_ArchivalPowerOutlet">
        <transmitsPower>true</transmitsPower>
        <basePowerConsumption>-1</basePowerConsumption>
      </li>
    </comps>
    <rotatable>false</rotatable>
    <selectable>true</selectable>
    <tickerType>Normal</tickerType>
    <designationCategory>Power</designationCategory>
    <researchPrerequisites>
      <li>Electricity</li>
    </researchPrerequisites>
  </ThingDef>

</Defs>
```

- [ ] **Step 2: healthcheck**

Run: `python3 tests/healthcheck.py`
Expected: `OK`（XML well-formed；Task 10 才加 defName 斷言，故此處只驗 well-formed 與既有檢查）。

- [ ] **Step 3: commit**

```bash
git add Defs/ThingDefs/CAO_Power.xml
git commit -m "feat(n5): add CAO_PowerSamplingNode and CAO_PowerOutlet ThingDefs"
```

---

## Task 10: Dialog 接電力（確認窗開關 + 採樣窗即時行）

**Files:**
- Modify: `Source/Dialog_ArchivalConfirm.cs`
- Modify: `Source/Dialog_SamplingStatus.cs`
- Modify: `tests/healthcheck.py`

- [ ] **Step 1: Dialog_ArchivalConfirm 加欄位**

在 `Source/Dialog_ArchivalConfirm.cs` 的 `applyHediffDeltas` 欄位（`:22`）之後加：

```csharp
        private bool applyPowerSampling = true; // N5：預設計入（既然放了節點並採到資料）
        private readonly int powerSampleCount;
        private readonly float avgNetPowerW;
```

- [ ] **Step 2: ctor 讀採樣數與平均值**

在 `Dialog_ArchivalConfirm` 建構子內 `snapshot = ArchivalService.ComputeSnapshot(map, tracker);` 之後加：

```csharp
            powerSampleCount = tracker.powerSampleCount;
            avgNetPowerW = snapshot.avgNetPowerW;
```

- [ ] **Step 3: DoWindowContents 加電力開關行**

在 `Source/Dialog_ArchivalConfirm.cs` 的 N6b hediff 開關 block（`if (snapshot.dailyHediffDeltas?.Count > 0) { ... y += 32f; }`）之後、`// N3：圖標 gallery` 之前插入：

```csharp
            // N5：電力採樣開關（只在放了節點並採到資料時顯示）
            if (powerSampleCount > 0)
            {
                Widgets.CheckboxLabeled(new Rect(x, y, w, 26f),
                    "CAO.ArchivalConfirm.ApplyPower".Translate(avgNetPowerW.ToString("F0")), ref applyPowerSampling);
                y += 32f;
            }
```

- [ ] **Step 4: 確認鈕串入 applyPower**

把 `Source/Dialog_ArchivalConfirm.cs` 確認鈕內的 `ArchivalService.Archive(...)` 呼叫改為（末尾加 `applyPower`）：

```csharp
                ArchivalService.Archive(map, name, chosenIconPath, scalePawnCount, applySkillXP,
                    applyHealthDelta, applyHediffDeltas, applyHealthDeterioration,
                    powerSampleCount > 0 && applyPowerSampling);
```

- [ ] **Step 5: Dialog_SamplingStatus 加 map 欄位**

在 `Source/Dialog_SamplingStatus.cs` 的 `private readonly ProductivitySnapshot snapshot;`（`:9`）之前加：

```csharp
        private readonly Map map;
```

在建構子 `public Dialog_SamplingStatus(Map map)` 內第一行（`var tracker = map.GetComponent<ColonyArchivalTracker>();` 之前）加：

```csharp
            this.map = map;
```

- [ ] **Step 6: Dialog_SamplingStatus 顯示即時供電節點淨功率**

在 `Source/Dialog_SamplingStatus.cs` 的 `DoWindowContents` 內「已歷時」block（`y += 28f;`）之後、預覽 scroll view 之前插入：

```csharp
            // N5：即時供電節點淨功率
            string powerLine = ArchivalService.TryGetNodePowerWatts(map, out float nodeWatts)
                ? "CAO.SamplingStatus.NodePower".Translate(nodeWatts.ToString("F0"))
                : "CAO.SamplingStatus.NoNode".Translate();
            Widgets.Label(new Rect(x, y, w, 24f), powerLine);
            y += 28f;
```

- [ ] **Step 7: healthcheck.py 加 ThingDef defName 斷言**

在 `tests/healthcheck.py` 的「3) 兩語言 Keyed」區塊之前（`# 3)` 那行之前）插入：

```python
# 2b) Power ThingDef defName 齊全
power = ROOT / "Defs/ThingDefs/CAO_Power.xml"
if power.exists():
    proot = ET.parse(power).getroot()
    pnames = {d.findtext("defName") for d in proot.findall("ThingDef")}
    for need in ("CAO_PowerSamplingNode", "CAO_PowerOutlet"):
        if need not in pnames:
            fail(f"CAO_Power.xml 缺 ThingDef defName: {need}")
else:
    fail("缺 Defs/ThingDefs/CAO_Power.xml")

```

- [ ] **Step 8: build + healthcheck**

Run: `dotnet build Source/ColonyArchivalOutpost.csproj -c Release`
Expected: Build succeeded.

Run: `python3 tests/healthcheck.py`
Expected: `OK`

- [ ] **Step 9: commit**

```bash
git add Source/Dialog_ArchivalConfirm.cs Source/Dialog_SamplingStatus.cs tests/healthcheck.py
git commit -m "feat(n5): power toggle in confirm dialog + live node power in status dialog"
```

---

## Task 11: 實機驗證清單（無自動化，遊戲內手動）

**Files:** 無（驗收）

> 部署：把本 repo 同步到遊戲載入路徑（`/home/lorkhan/rimworld_mods/` symlink）。開一局已研究 Electricity 的存檔。

- [ ] **Step 1: 建置與載入無錯**

`dotnet build ... -c Release` 後啟動遊戲，載入 mod，看 dev console 無紅字（DefOf 解析到兩個 ThingDef、無缺貼圖粉紅）。

- [ ] **Step 2: 供電節點放置**

- Power 建造選單出現「power sampling node」。
- 可放在牆上、其他建築上、任意格（`isEdifice=false` 生效）。
- 放第二個被擋，提示「每張地圖只能放置一個供電節點」。
- 放在接觸電網處 → 選取看其 inspect 併入該電網。

- [ ] **Step 3: 採樣量測**

- 對殖民地 gizmo「開始採樣」。
- 開「查看採樣狀況」窗 → 顯示「供電節點淨功率：X W」一行；數值符號正確（白天太陽能盈餘為正、夜間耗電為負）。
- 移除節點 → 該行變「未放置供電節點」。
- 跨遊戲日採樣，確認窗能顯示電力行（`powerSampleCount > 0`）。

- [ ] **Step 4: 封存確認窗**

- 「平均淨功率」勾選行出現，預設勾選（計入）。
- 顯示的平均值合理（白天多→偏正、夜間多→偏負，跨多日趨近真平均）。
- 取消勾選（無視）→ 封存後該哨站 `PowerWatts` 應為 0。

- [ ] **Step 5: outlet 連線與輸出（正功率）**

- 主基地放「archival power outlet」，可疊牆任意放。
- gizmo「連接封存哨站」→ 浮選單列出有電力資料的哨站；選一個。
- inspect 顯示「已連接：<名>」「淨功率：X W」。
- 若哨站平均為正 → 主基地電網總發電量上升（拔掉主基地一台發電機驗證 outlet 真的在供電）。

- [ ] **Step 6: outlet 抽電（負功率）與家網緩衝**

- 連一個平均為負的哨站 → 主基地電網被多抽 X W。
- 故意讓主基地電力吃緊 → outlet 像家用電器一樣在斷電時被關（潮汐式），有電又恢復（驗證「家網即緩衝」、無自製緩衝）。

- [ ] **Step 7: 去重與斷線**

- 同一哨站連到第二個 outlet → 舊 outlet 自動斷（inspect 變「未連接」）。
- 拆掉 outlet → 哨站反向引用清除（再連別的 outlet 正常）。
- 解封/摧毀哨站 → outlet 自動斷、輸出歸 0、無紅字。

- [ ] **Step 8: 存讀檔**

- 採樣中存檔→讀檔：`powerAccumW`/`powerSampleCount` 續存不歸零。
- outlet 連線存檔→讀檔：連線保留（雙向引用重綁）、輸出延續。
- 移除提供某資源的 mod 不影響（電力欄位為純值，無 def key）。

- [ ] **Step 9: 最終 commit（若驗證中有微調）**

```bash
git add -A
git commit -m "fix(n5): adjustments from in-game verification"
```

---

## Self-Review 對照 spec

- §2 供電節點：Task 9（ThingDef、`isEdifice=false`、`CompPowerTransmitter`、Electricity）+ Task 3（一圖一個 PlaceWorker）+ Task 4（每 2500 tick 讀 `CurrentEnergyGainRate()/WattsToWattDaysPerTick` 累加）。✓
- §3 outlet：Task 7（`CompArchivalPowerOutlet : CompPowerPlant`、有號 `DesiredPowerOutput`、連線 gizmo、inspect）+ Task 9（ThingDef、`basePowerConsumption=-1` 上電）。「包括消耗」＝負值抽電由 CompPowerPlant 不夾值達成。✓
- §4 資料模型：Task 1（snapshot 兩欄）+ Task 6（`PowerWatts`/反向引用）+ Task 4（tracker 累加器 + tick）+ Task 5（ComputeSnapshot/Archive）。✓
- §5 UI：Task 10（確認窗計入/無視開關預設計入、僅 `powerSampleCount>0` 顯示；採樣窗即時行「未放置供電節點」提示）。✓
- §6 存讀檔：snapshot/tracker `Scribe_Values`；雙向 `Scribe_References`（Task 6 + Task 7）。✓
- §7 邊界：無節點不採（Task 4）、未接網讀 0（Task 4 助手）、多節點取第一（`FirstOrDefault`）、哨站毀清引用（Task 6/7）、採樣中存檔保累加器（Task 4）。✓
- §8 不確定項：API 換算（已確認）、`PowerOutput` 負值（CompPowerPlant 不夾值，已確認）、`isEdifice=false` 疊牆（Task 11 Step 2 實機驗）。✓
- §9 範圍外：無緩衝/brownout 自製/silver/距離損耗/太陽能加權/多節點去重 —— 計畫皆未引入。✓

**型別一致性檢查：** `PowerWatts`/`HasPowerSampling`/`ConnectedOutlet`/`SetConnectedOutlet`/`NotifyOutletDestroyed`（Outpost_Sampled）與 `ConnectTo`/`Disconnect`/`NotifyOutpostDestroyed`/`DesiredPowerOutput`（comp）跨任務命名一致；`TryGetNodePowerWatts` 簽章 Task 4 定義、Task 10 使用一致；`Archive(..., bool applyPower)` Task 5 定義、Task 10 呼叫一致。✓
