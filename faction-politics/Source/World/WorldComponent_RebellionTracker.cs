using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.politics
{
    /// <summary>唯一心跳。FinalizeInit 兼顧新世界與舊檔中途裝 mod（Game.cs:585 讀檔也呼叫）。
    /// 每輪：補發反叛者 → 逐 record 自癒 → 推進度 → 達標觸發分裂。</summary>
    public class WorldComponent_RebellionTracker : WorldComponent
    {
        private List<RebelRecord> records = new List<RebelRecord>();
        /// <summary>本 mod 分裂誕生的派系（含已 defeated），用於上限防膨脹。</summary>
        private List<Faction> spawnedFactions = new List<Faction>();

        public WorldComponent_RebellionTracker(World world) : base(world)
        {
        }

        private static PoliticsSettingsDef Settings =>
            DefDatabase<PoliticsSettingsDef>.AllDefsListForReading[0];

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            EnsureRebels();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % Settings.checkIntervalTicks != 0)
            {
                return;
            }
            EnsureRebels();
            for (int i = records.Count - 1; i >= 0; i--)
            {
                RebelRecord record = records[i];
                if (record.faction == null || record.faction.defeated)
                {
                    records.RemoveAt(i);
                    continue;
                }
                if (!Heal(record))
                {
                    continue;
                }
                record.progress += record.ratePerDay * Settings.checkIntervalTicks / GenDate.TicksPerDay;
                TrySplit(record, i);
            }
        }

        /// <summary>合格且未追蹤的派系補 record。冪等。</summary>
        public void EnsureRebels()
        {
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!Eligible(faction) || HasRecord(faction))
                {
                    continue;
                }
                RebellionProfileDef profile = RebellionProfileResolver.Resolve(faction);
                if (profile != null && CountSettlements(faction) >= profile.minSettlements)
                {
                    RebelRecord record = RebelSpawner.TrySpawnFor(faction, profile);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
            }
        }

        /// <summary>回 true 表示本輪可推進度。死亡→歸零入冷卻/到期重生；在世→world pawn 與駐地自癒。</summary>
        private bool Heal(RebelRecord record)
        {
            RebellionProfileDef profile = RebellionProfileResolver.Resolve(record.faction);
            if (profile == null)
            {
                return false;
            }
            if (record.rebel == null || record.rebel.Dead || record.rebel.Destroyed)
            {
                if (record.respawnAtTick < 0)
                {
                    record.progress = 0f;
                    record.rebel = null;
                    record.respawnAtTick = Find.TickManager.TicksGame
                        + (int)(profile.respawnDelayDays * GenDate.TicksPerDay);
                }
                else if (Find.TickManager.TicksGame >= record.respawnAtTick)
                {
                    RebelSpawner.Respawn(record, profile);
                }
                return false;
            }
            if (!record.rebel.Spawned && !Find.WorldPawns.Contains(record.rebel))
            {
                Find.WorldPawns.PassToWorld(record.rebel, PawnDiscardDecideMode.KeepForever);
            }
            if (record.homeSettlement == null || record.homeSettlement.Destroyed
                || record.homeSettlement.Faction != record.faction)
            {
                record.homeSettlement = RebelSpawner.PickHome(record.faction);
            }
            if (record.homeSettlement != null
                && !record.homeSettlement.previouslyGeneratedInhabitants.Contains(record.rebel))
            {
                record.homeSettlement.previouslyGeneratedInhabitants.Add(record.rebel);
            }
            return true;
        }

        private void TrySplit(RebelRecord record, int index)
        {
            RebellionProfileDef profile = RebellionProfileResolver.Resolve(record.faction);
            if (profile == null || record.progress < profile.threshold)
            {
                return;
            }
            if (spawnedFactions.Count >= Settings.maxDynamicFactions || record.rebel.Spawned)
            {
                record.progress = profile.threshold;   // 凍結等待（上限觸頂或反叛者在地圖上）
                return;
            }
            Faction newFaction = FactionSplitter.Split(record);
            if (newFaction != null)
            {
                spawnedFactions.Add(newFaction);
                records.RemoveAt(index);   // 母派系冷卻後由 EnsureRebels 再養新反叛者
            }
        }

        private static bool Eligible(Faction faction)
        {
            return faction != null && !faction.IsPlayer && !faction.Hidden && !faction.defeated
                && !faction.temporary && faction.def.humanlikeFaction;
        }

        private bool HasRecord(Faction faction)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].faction == faction)
                {
                    return true;
                }
            }
            return false;
        }

        private static int CountSettlements(Faction faction)
        {
            int count = 0;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Faction == faction && !PoliticsBridges.IsSatellite(all[i]))
                {
                    count++;
                }
            }
            return count;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref records, "pas_rebelRecords", LookMode.Deep);
            Scribe_Collections.Look(ref spawnedFactions, "pas_spawnedFactions", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (records == null)
                {
                    records = new List<RebelRecord>();
                }
                if (spawnedFactions == null)
                {
                    spawnedFactions = new List<Faction>();
                }
                records.RemoveAll((RebelRecord r) => r == null || r.faction == null);
                spawnedFactions.RemoveAll((Faction f) => f == null);
            }
        }
    }
}
