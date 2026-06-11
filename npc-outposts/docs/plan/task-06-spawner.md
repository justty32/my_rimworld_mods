# Task 6: 鋪設（OutpostPlacer + WorldComponent_OutpostSpawner）

**Files:**
- Create: `npc-outposts/Source/World/OutpostPlacer.cs`
- Create: `npc-outposts/Source/World/WorldComponent_OutpostSpawner.cs`

- [ ] **Step 1: OutpostPlacer.cs**

途中放置範式（02 報告 §4.3）：MakeWorldObject → Tile → SetFaction → Add。tile 用 `TileFinder.TryFindPassableTileWithTraversalDistance`（`TileFinder.cs:146`）+ `IsValidTileForNewSettlement`（`:65`）。

```csharp
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.outposts
{
    /// <summary>哨站放置核心。public static 供 FinalizeInit / tick / 第三方共用。</summary>
    public static class OutpostPlacer
    {
        public static NpcOutpost TryPlaceFor(Settlement parent, OutpostProfileDef profile, OutpostTypeDef type = null)
        {
            if (parent == null || parent.Faction == null || profile == null || profile.types.NullOrEmpty())
            {
                return null;
            }
            if (type == null)
            {
                type = profile.types.RandomElementByWeight(e => e.weight).type;
            }
            if (type?.worldObjectDef == null)
            {
                return null;
            }
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(
                    parent.Tile, profile.radius.min, profile.radius.max, out PlanetTile tile,
                    t => TileFinder.IsValidTileForNewSettlement(t)))
            {
                return null;
            }
            NpcOutpost outpost = (NpcOutpost)WorldObjectMaker.MakeWorldObject(type.worldObjectDef);
            outpost.Tile = tile;
            outpost.SetFaction(parent.Faction);
            outpost.Setup(type, parent);
            outpost.Name = "pas_outposts_NameFormat".Translate(parent.Name);
            Find.WorldObjects.Add(outpost);
            return outpost;
        }
    }
}
```

（`Name` setter 以 Task 0 Step 3 為準；若 set 不可用改存自訂欄位 + override Label。）

- [ ] **Step 2: WorldComponent_OutpostSpawner.cs**

單一元件吃三個場景：新世界（`FinalizeInit(false)`，在所有 world gen step 之後——`WorldGenerator.cs:67`）、讀檔（`FinalizeInit(true)`——`Game.cs:585`）、隨時間增生（tick）。中途裝 mod 的舊檔走讀檔路徑自動補鋪。

```csharp
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.outposts
{
    public class WorldComponent_OutpostSpawner : WorldComponent
    {
        private const int CheckInterval = 2500;

        /// <summary>聚落 → 哨站上限（首見時擲定，持久化）。</summary>
        private Dictionary<Settlement, int> caps = new Dictionary<Settlement, int>();
        private List<Settlement> tmpSettlements;
        private List<int> tmpCaps;

        public WorldComponent_OutpostSpawner(World world) : base(world)
        {
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            InitializeNewSettlements();
        }

        /// <summary>給未登記的 NPC 聚落擲上限並鋪初始批（上限的一半上下）。冪等：caps 已含者跳過。</summary>
        public virtual void InitializeNewSettlements()
        {
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];
                if (settlement is NpcOutpost || settlement.Faction == null || settlement.Faction.IsPlayer
                    || caps.ContainsKey(settlement))
                {
                    continue;
                }
                OutpostProfileDef profile = OutpostProfileResolver.Resolve(settlement.Faction);
                if (profile == null)
                {
                    caps[settlement] = 0;
                    continue;
                }
                int cap = profile.countPerSettlement.RandomInRange;
                caps[settlement] = cap;
                int initial = Rand.RangeInclusive(cap / 2, (cap + 1) / 2);
                for (int j = 0; j < initial; j++)
                {
                    OutpostPlacer.TryPlaceFor(settlement, profile);
                }
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % CheckInterval != 0)
            {
                return;
            }
            Dictionary<Settlement, int> counts = CountOutpostsByParent();
            foreach (KeyValuePair<Settlement, int> kv in caps)
            {
                Settlement settlement = kv.Key;
                if (settlement == null || settlement.Destroyed || kv.Value <= 0)
                {
                    continue;
                }
                counts.TryGetValue(settlement, out int existing);
                if (existing >= kv.Value)
                {
                    continue;
                }
                OutpostProfileDef profile = OutpostProfileResolver.Resolve(settlement.Faction);
                if (profile != null && Rand.MTBEventOccurs(profile.spawnMtbDays, GenDate.TicksPerDay, CheckInterval))
                {
                    OutpostPlacer.TryPlaceFor(settlement, profile);
                }
            }
        }

        private static Dictionary<Settlement, int> CountOutpostsByParent()
        {
            Dictionary<Settlement, int> counts = new Dictionary<Settlement, int>();
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (settlements[i] is NpcOutpost outpost && outpost.ParentSettlement != null)
                {
                    counts.TryGetValue(outpost.ParentSettlement, out int n);
                    counts[outpost.ParentSettlement] = n + 1;
                }
            }
            return counts;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref caps, "pas_outpostCaps", LookMode.Reference, LookMode.Value, ref tmpSettlements, ref tmpCaps);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (caps == null)
                {
                    caps = new Dictionary<Settlement, int>();
                }
                caps.RemoveAll(kv => kv.Key == null);   // 被毀聚落的引用讀檔後為 null → 清掉防 null key 紅字
            }
        }
    }
}
```

注意：
- `WorldComponentTick` 內 `OutpostPlacer.TryPlaceFor` 修改的是 `Find.WorldObjects`，不動 `caps` → foreach 安全。
- 遍歷時機：`caps.RemoveAll` 需要 `using Verse;` 的 GenCollection 擴充（已 using）。
- 遊戲中途新建的 NPC 聚落要等下次讀檔（`FinalizeInit`）才登記——O1 接受，記入 session_log。

- [ ] **Step 3: 建置驗證**

```powershell
dotnet build C:\code\mine\my_rimworld_mods\npc-outposts\Source\NpcOutposts.csproj -c Release
```
Expected: 0 警告 0 錯誤。

- [ ] **Step 4: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add npc-outposts/Source
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: 哨站鋪設（Placer + WorldComponent：開局/舊檔/隨時間增生三合一）

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
