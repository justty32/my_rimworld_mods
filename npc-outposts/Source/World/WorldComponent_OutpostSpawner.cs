using System;
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

        /// <summary>擴充接點：橋接 mod（如 npc-outposts-rimwar）可註冊「派系 → 增生速率倍率」。
        /// null 或回傳 ≤0 視為 1（零行為變化）；倍率作用於 spawnMtbDays（>1 加速、<1 減速）。</summary>
        public static Func<Faction, float> GrowthRateMultiplier;

        /// <summary>擴充接點：橋接 mod（如 empire-outposts-war）可覆寫「某聚落是否為合格哨站母體」。
        /// 回傳 true＝強制納入、false＝強制排除、null＝不表態（沿用本體預設判定）。
        /// hook 為 null 或拋例外＝視為不表態（零行為變化）。</summary>
        public static Func<Settlement, bool?> ParentEligibilityOverride;

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
                if (!IsEligibleParent(settlement) || caps.ContainsKey(settlement))
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
                if (profile != null && Rand.MTBEventOccurs(profile.spawnMtbDays / GrowthMultiplierFor(settlement.Faction), GenDate.TicksPerDay, CheckInterval))
                {
                    OutpostPlacer.TryPlaceFor(settlement, profile);
                }
            }
        }

        /// <summary>合格母體判定：本體預設＝非哨站、有派系、非玩家；
        /// ParentEligibilityOverride 回 true/false 可強制納入/排除，null/異常＝沿用預設。</summary>
        private static bool IsEligibleParent(Settlement settlement)
        {
            if (settlement is NpcOutpost || settlement.Faction == null)
            {
                return false;
            }
            bool defaultEligible = !settlement.Faction.IsPlayer;
            Func<Settlement, bool?> hook = ParentEligibilityOverride;
            if (hook == null)
            {
                return defaultEligible;
            }
            try
            {
                bool? verdict = hook(settlement);
                return verdict ?? defaultEligible;
            }
            catch
            {
                return defaultEligible;
            }
        }

        /// <summary>hook 取倍率；未註冊/異常/非正值一律 1（行為與無 hook 完全相同）。</summary>
        private static float GrowthMultiplierFor(Faction faction)
        {
            Func<Faction, float> hook = GrowthRateMultiplier;
            if (hook == null)
            {
                return 1f;
            }
            float mult;
            try
            {
                mult = hook(faction);
            }
            catch
            {
                return 1f;
            }
            return mult > 0f ? mult : 1f;
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
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // 治本：已毀聚落寫進存檔會在讀檔 BuildDictionary 期噴「Null key」紅字
                //（早於 PostLoadInit，事後清理擋不住）→ 存檔前就剔除。
                caps.RemoveAll(kv => kv.Key == null || kv.Key.Destroyed);
            }
            Scribe_Collections.Look(ref caps, "pas_outpostCaps", LookMode.Reference, LookMode.Value, ref tmpSettlements, ref tmpCaps);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (caps == null)
                {
                    caps = new Dictionary<Settlement, int>();
                }
                caps.RemoveAll(kv => kv.Key == null);   // 舊檔已含 null 引用的後備清理
            }
        }
    }
}
