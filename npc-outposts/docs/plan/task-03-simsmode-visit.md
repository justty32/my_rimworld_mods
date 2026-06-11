# Task 3: sims-mode 交付——「真訪問」（ArrivalAction + comp + patch）

> **校準保留**：使用者將提供參考 mod。本 task 做**最小版**；介面凍結為 `CaravanArrivalAction_VisitMap(MapParent, IntVec3?)` 與 `GetFloatMenuOptions(caravan, mapParent, size)`——參考 mod 到位後只准動 `Arrived` 內部（letter、善後、進場細節），不動介面，npc-outposts 不受影響。

**Files:**（全部在 sims-mode-community）
- Create: `sims-mode-community/Source/Visit/CaravanArrivalAction_VisitMap.cs`
- Create: `sims-mode-community/Source/Visit/WorldObjectComp_VisitMap.cs`
- Create: `sims-mode-community/Patches/Settlement_VisitMap.xml`
- Create: `sims-mode-community/Languages/English/Keyed/SimsModeCommunity.xml`
- Create: `sims-mode-community/Languages/ChineseTraditional/Keyed/SimsModeCommunity.xml`

- [ ] **Step 1: CaravanArrivalAction_VisitMap.cs**

照 `CaravanArrivalAction_VisitSettlement.cs` 骨架，`Arrived` 改生圖進場（範式：`SettlementUtility.cs:44-59` 攻擊路徑，去掉關係懲罰與徵召）：

```csharp
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.sims
{
    /// <summary>拜訪＝實際生成地圖進場（原版拜訪只停格不生圖）。size Invalid = 世界初始尺寸。</summary>
    public class CaravanArrivalAction_VisitMap : CaravanArrivalAction
    {
        private MapParent mapParent;
        private IntVec3 mapSize = IntVec3.Invalid;

        public override string Label => "pas_sims_EnterSettlement".Translate(mapParent.Label);

        public override string ReportString => "CaravanVisiting".Translate(mapParent.Label);

        public CaravanArrivalAction_VisitMap()
        {
        }

        public CaravanArrivalAction_VisitMap(MapParent mapParent, IntVec3? mapSize = null)
        {
            this.mapParent = mapParent;
            this.mapSize = mapSize ?? IntVec3.Invalid;
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
            if (!report)
            {
                return report;
            }
            if (mapParent != null && mapParent.Tile != destinationTile)
            {
                return false;
            }
            return CanVisit(caravan, mapParent);
        }

        public override void Arrived(Caravan caravan)
        {
            bool newMap = !mapParent.HasMap;
            IntVec3 size = mapSize.IsValid ? mapSize : Find.World.info.initialMapSize;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(mapParent.Tile, size, null);
            if (map == null)
            {
                return;
            }
            if (newMap)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
            }
            if (caravan.IsPlayerControlled)
            {
                Find.LetterStack.ReceiveLetter(
                    "LetterLabelCaravanEnteredMap".Translate(mapParent),
                    "LetterCaravanEnteredMap".Translate(caravan.Label, mapParent).CapitalizeFirst(),
                    LetterDefOf.NeutralEvent, caravan.PawnsListForReading);
            }
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: false);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_Values.Look(ref mapSize, "mapSize", IntVec3.Invalid);
        }

        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, MapParent mapParent)
        {
            if (mapParent == null || !mapParent.Spawned)
            {
                return false;
            }
            if (mapParent is Settlement settlement)
            {
                return settlement.Visitable;
            }
            return mapParent.Faction != null && mapParent.Faction != Faction.OfPlayer
                && !mapParent.Faction.HostileTo(Faction.OfPlayer);
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, MapParent mapParent, IntVec3? mapSize = null)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(
                () => CanVisit(caravan, mapParent),
                () => new CaravanArrivalAction_VisitMap(mapParent, mapSize),
                "pas_sims_EnterSettlement".Translate(mapParent.Label),
                caravan, mapParent.Tile, mapParent);
        }
    }
}
```

（`Notify_GeneratedPotentiallyHostileMap`：拜訪對象非敵對，但圖上可能有原版野獸/機械殘骸，保守保留——若 Task 0 Step 1 或參考 mod 顯示不該呼叫，移除即可。）

- [ ] **Step 2: WorldObjectComp_VisitMap.cs**

```csharp
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.sims
{
    public class WorldObjectCompProperties_VisitMap : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_VisitMap()
        {
            compClass = typeof(WorldObjectComp_VisitMap);
        }
    }

    /// <summary>由 PatchOperation 掛上原版 Settlement WorldObjectDef，給 float menu 注入「進入」。</summary>
    public class WorldObjectComp_VisitMap : WorldObjectComp
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            if (parent is MapParent mapParent)
            {
                foreach (FloatMenuOption option in CaravanArrivalAction_VisitMap.GetFloatMenuOptions(caravan, mapParent))
                {
                    yield return option;
                }
            }
        }
    }
}
```

（`compClass` 欄位名以 Task 0 Step 6 結果為準。）

- [ ] **Step 3: Patches/Settlement_VisitMap.xml**

vanilla Settlement def 帶 TimedDetectionRaids comp（`SettlementDefeatUtility.cs:29` 用到），`comps` 節點應存在；仍用 conditional 防禦：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/WorldObjectDef[defName="Settlement"]/comps</xpath>
    <match Class="PatchOperationAdd">
      <xpath>Defs/WorldObjectDef[defName="Settlement"]/comps</xpath>
      <value>
        <li Class="pas.sims.WorldObjectCompProperties_VisitMap" />
      </value>
    </match>
    <nomatch Class="PatchOperationAdd">
      <xpath>Defs/WorldObjectDef[defName="Settlement"]</xpath>
      <value>
        <comps>
          <li Class="pas.sims.WorldObjectCompProperties_VisitMap" />
        </comps>
      </value>
    </nomatch>
  </Operation>
</Patch>
```

- [ ] **Step 4: Languages 兩份 Keyed**

`Languages/English/Keyed/SimsModeCommunity.xml`：
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <pas_sims_EnterSettlement>Enter {0}</pas_sims_EnterSettlement>
</LanguageData>
```

`Languages/ChineseTraditional/Keyed/SimsModeCommunity.xml`：
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <pas_sims_EnterSettlement>進入{0}</pas_sims_EnterSettlement>
</LanguageData>
```

- [ ] **Step 5: 建置驗證（雙 mod）**

```powershell
dotnet build C:\code\mine\my_rimworld_mods\sims-mode-community\Source\SimsModeCommunity.csproj -c Release
dotnet build C:\code\mine\my_rimworld_mods\npc-outposts\Source\NpcOutposts.csproj -c Release
python C:\code\mine\my_rimworld_mods\sims-mode-community\tests\healthcheck.py
```
Expected: build 0 警告 0 錯誤；sims-mode healthcheck OK（新 patch 檔 XML well-formed 會被掃到）。

- [ ] **Step 6: Commit（到 sims-mode，獨立訊息）**

```powershell
git -C C:\code\mine\my_rimworld_mods add sims-mode-community/Source/Visit sims-mode-community/Patches/Settlement_VisitMap.xml sims-mode-community/Languages
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat(sims-mode): 真訪問——拜訪聚落生成地圖進場（ArrivalAction + comp patch）

原版 1.6 拜訪不生圖（CaravanArrivalAction_VisitSettlement.Arrived 只發信），
活聚落缺正門入口；本 commit 補上。最小版，待參考 mod 校準 Arrived 細節。

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
