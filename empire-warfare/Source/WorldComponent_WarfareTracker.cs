using System;
using System.Collections.Generic;
using FactionColonies;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.empire.warfare
{
    /// <summary>RimWar 攻擊標記（tile → 攻方/時間）。</summary>
    public class AttackMarker : IExposable
    {
        public Faction attacker;
        public int tick;

        public void ExposeData()
        {
            Scribe_References.Look(ref attacker, "attacker");
            Scribe_Values.Look(ref tick, "tick");
        }
    }

    /// <summary>已判定、待下一 tick 執行的淪陷（與 LifecycleRegistry 迭代解耦）。
    /// tile 用 PlanetTile 保留 layer 資訊（Orbital 聚落）。</summary>
    public class PendingFall : IExposable
    {
        public PlanetTile tile = PlanetTile.Invalid;
        public Faction attacker;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tile, "tile", PlanetTile.Invalid);
            Scribe_References.Look(ref attacker, "attacker");
        }
    }

    /// <summary>淪陷記錄，供「收復失土」偵測。</summary>
    public class FallenRecord : IExposable
    {
        public int tile;
        public string settlementName;
        public Faction attacker;
        public int tick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tile, "tile");
            Scribe_Values.Look(ref settlementName, "settlementName");
            Scribe_References.Look(ref attacker, "attacker");
            Scribe_Values.Look(ref tick, "tick");
        }
    }

    /// <summary>
    /// 戰況狀態機：攻擊標記、失敗連擊、附庸建立時間（保護期）、待執行淪陷、淪陷記錄，
    /// 以及每日 vassalHeat 衰減（暴露 RimWar 的附庸熱度閘節奏）。
    /// </summary>
    public class WorldComponent_WarfareTracker : WorldComponent
    {
        private const int MarkerExpiryTicks = 5 * GenDate.TicksPerDay; // 預告 1 天 + 手動戰餘裕

        private Dictionary<int, AttackMarker> attackMarkers = new Dictionary<int, AttackMarker>();
        private Dictionary<int, int> failStreaks = new Dictionary<int, int>();
        private Dictionary<int, int> creationTicks = new Dictionary<int, int>();
        private List<PendingFall> pendingFalls = new List<PendingFall>();
        private List<FallenRecord> fallenRecords = new List<FallenRecord>();
        private int modStartTick = -1;
        private bool degradeWarned;

        // Scribe_Collections 字典暫存
        private List<int> tmpKeysA, tmpKeysB, tmpKeysC;
        private List<AttackMarker> tmpMarkers;
        private List<int> tmpValsB, tmpValsC;

        public WorldComponent_WarfareTracker(World world) : base(world) { }

        public static WorldComponent_WarfareTracker Current =>
            Find.World?.GetComponent<WorldComponent_WarfareTracker>();

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            if (modStartTick < 0) modStartTick = Find.TickManager.TicksGame;
        }

        // ── 來自 patch / lifecycle 的通知 ──

        public void RecordRimWarAttack(int tile, Faction attacker)
        {
            attackMarkers[tile] = new AttackMarker { attacker = attacker, tick = Find.TickManager.TicksGame };
        }

        public void NotifySettlementCreated(WorldSettlementFC settlement)
        {
            int tile = settlement.Tile;
            creationTicks[tile] = Find.TickManager.TicksGame;

            // 收復閉環：tile 曾淪陷且現在重新建立玩家附庸（Capture 勝利路徑）
            FallenRecord record = fallenRecords.Find(r => r.tile == tile);
            if (record != null)
            {
                fallenRecords.Remove(record);
                Find.LetterStack.ReceiveLetter("pas_warfare_VassalRecapturedLabel".Translate(),
                    "pas_warfare_VassalRecapturedDesc".Translate(
                        record.settlementName ?? settlement.Name, record.attacker?.Name ?? "?"),
                    LetterDefOf.PositiveEvent, new LookTargets(settlement));
            }
        }

        public void NotifySettlementRemoved(WorldSettlementFC settlement)
        {
            int tile = settlement.Tile;
            creationTicks.Remove(tile);
            failStreaks.Remove(tile);
            attackMarkers.Remove(tile);
        }

        public void NotifyDefenseWon(WorldSettlementFC settlement)
        {
            int tile = settlement.Tile;
            failStreaks.Remove(tile);
            attackMarkers.Remove(tile);
        }

        public void NotifyDefenseLost(WorldSettlementFC settlement, BattleResult result)
        {
            int tile = settlement.Tile;
            int now = Find.TickManager.TicksGame;
            WarfareSettings s = WarfareMod.Settings;

            int streak = (failStreaks.TryGetValue(tile, out int prev) ? prev : 0) + 1;
            failStreaks[tile] = streak;

            AttackMarker marker = attackMarkers.TryGetValue(tile, out AttackMarker m) ? m : null;
            bool markerFresh = marker != null && now - marker.tick <= MarkerExpiryTicks;
            attackMarkers.Remove(tile); // 一次戰鬥消耗一次標記

            if (!s.enableVassalFall) return;

            // 來源過濾：預設只認 RimWar 行軍部隊的攻擊；標記 patch 降級時放行並警告一次
            if (s.onlyRimWarAttacks && !markerFresh)
            {
                if (WarfareInit.AttackMarkerPatchActive) return;
                if (!degradeWarned)
                {
                    degradeWarned = true;
                    LogUtil.Warning("[EmpireWarfare] attack marker unavailable; cannot verify attack source. "
                        + "Falls will apply to any failed defense.");
                }
            }

            // 淪陷條件：徹底潰敗（result==null 的手動戰敗/守軍全滅視同），或連續失敗 N 次
            bool crushing = IsCrushingDefeat(result, s.crushingAttackerRemainRatio);
            bool byStreak = s.consecutiveFailuresForFall > 0 && streak >= s.consecutiveFailuresForFall;
            if (!crushing && !byStreak) return;

            // 保護期：開局/舊檔加入本 mod/新附庸
            int knownCreation = creationTicks.TryGetValue(tile, out int created) ? created : modStartTick;
            if (s.protectionDays > 0 && now - knownCreation < s.protectionDays * GenDate.TicksPerDay)
            {
                Messages.Message("pas_warfare_VassalProtectedMsg".Translate(settlement.Name),
                    new LookTargets(settlement), MessageTypeDefOf.NeutralEvent);
                return;
            }

            Faction attacker = markerFresh ? marker.attacker : settlement.MilitaryComp?.attackerForce?.homeFaction;
            if (!IsValidConqueror(attacker))
            {
                LogUtil.Warning($"[EmpireWarfare] fall aborted for {settlement.Name}: no valid attacker faction.");
                return;
            }

            pendingFalls.Add(new PendingFall { tile = settlement.Tile, attacker = attacker });
        }

        private static bool IsCrushingDefeat(BattleResult result, float attackerRemainRatio)
        {
            if (result == null) return true; // 手動地圖戰敗：守軍全滅
            if (result.winner != BattleWinner.Attacker) return false; // Error 等異常不淪陷
            if (result.attackerInitialForce <= 0) return false;
            return result.attackerRemainingForce / result.attackerInitialForce >= attackerRemainRatio;
        }

        private static bool IsValidConqueror(Faction faction)
        {
            return faction != null
                && !faction.IsPlayer
                && !faction.defeated
                && !FactionCache.IsPlayerColonyFaction(faction);
        }

        // ── tick：執行待辦淪陷 + 每日維護 ──

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (pendingFalls.Count > 0)
            {
                List<PendingFall> batch = pendingFalls;
                pendingFalls = new List<PendingFall>();
                foreach (PendingFall fall in batch)
                {
                    FallenRecord record = VassalFallUtility.TryExecuteFall(fall.tile, fall.attacker);
                    if (record != null) fallenRecords.Add(record);
                }
            }

            if (Find.TickManager.TicksGame % GenDate.TicksPerDay == 137) DailyMaintenance();
        }

        private void DailyMaintenance()
        {
            int now = Find.TickManager.TicksGame;

            // 過期標記清理
            List<int> stale = null;
            foreach (KeyValuePair<int, AttackMarker> kv in attackMarkers)
            {
                if (now - kv.Value.tick > MarkerExpiryTicks)
                    (stale ?? (stale = new List<int>())).Add(kv.Key);
            }
            if (stale != null) foreach (int tile in stale) attackMarkers.Remove(tile);

            // vassalHeat 衰減（>0 時附庸更常被 RimWar 選為目標）
            int decay = WarfareMod.Settings.vassalHeatDecayPerDay;
            if (decay <= 0) return;
            try
            {
                FactionFC faction = FactionCache.FactionComp;
                if (faction?.settlements == null) return;
                foreach (WorldSettlementFC settlement in faction.settlements)
                {
                    RimWarSettlementComp comp = settlement?.GetComponent<RimWarSettlementComp>();
                    if (comp != null && comp.vassalHeat > 0)
                        comp.vassalHeat = Math.Max(0, comp.vassalHeat - decay);
                }
            }
            catch (Exception e)
            {
                LogUtil.Warning("[EmpireWarfare] vassalHeat decay failed: " + e.Message);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref modStartTick, "modStartTick", -1);
            Scribe_Collections.Look(ref attackMarkers, "attackMarkers", LookMode.Value, LookMode.Deep,
                ref tmpKeysA, ref tmpMarkers);
            Scribe_Collections.Look(ref failStreaks, "failStreaks", LookMode.Value, LookMode.Value,
                ref tmpKeysB, ref tmpValsB);
            Scribe_Collections.Look(ref creationTicks, "creationTicks", LookMode.Value, LookMode.Value,
                ref tmpKeysC, ref tmpValsC);
            Scribe_Collections.Look(ref pendingFalls, "pendingFalls", LookMode.Deep);
            Scribe_Collections.Look(ref fallenRecords, "fallenRecords", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (attackMarkers == null) attackMarkers = new Dictionary<int, AttackMarker>();
                if (failStreaks == null) failStreaks = new Dictionary<int, int>();
                if (creationTicks == null) creationTicks = new Dictionary<int, int>();
                if (pendingFalls == null) pendingFalls = new List<PendingFall>();
                if (fallenRecords == null) fallenRecords = new List<FallenRecord>();
                pendingFalls.RemoveAll(f => f == null);
                fallenRecords.RemoveAll(r => r == null);
            }
        }
    }
}
