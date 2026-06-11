using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.outposts
{
    /// <summary>單一元件吃三場景：新世界（FinalizeInit(false)，在所有 world gen step 之後——WorldGenerator.cs:67）、
    /// 讀檔（FinalizeInit(true)——Game.cs:585；中途裝 mod 的舊檔自動補鋪）、隨時間增生（tick MTB）。</summary>
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
            for (int i = settlements.Count - 1; i >= 0; i--)
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
