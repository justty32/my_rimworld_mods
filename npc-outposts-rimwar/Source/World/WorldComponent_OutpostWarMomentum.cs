using System;
using System.Collections.Generic;
using pas.outposts;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.outposts.rimwar
{
    /// <summary>單筆戰績：派系於某 tick 的勝負（+1/−1）。</summary>
    public class WarScoreEntry : IExposable
    {
        public Faction faction;
        public int tick;
        public int delta;

        public WarScoreEntry()
        {
        }

        public WarScoreEntry(Faction faction, int tick, int delta)
        {
            this.faction = faction;
            this.tick = tick;
            this.delta = delta;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref tick, "tick");
            Scribe_Values.Look(ref delta, "delta");
        }
    }

    /// <summary>戰局狀態機：收 ResolveBattle_Settlement 勝負訊號（30 天滑窗、線性衰減）；
    /// 對 npc-outposts 提供增生倍率；連敗派系哨站萎縮；
    /// 兼任哨站 comp 點數初始化（哨站不在 IsValidSettlement 白名單，RimWar 不會替它初始化）。</summary>
    public class WorldComponent_OutpostWarMomentum : WorldComponent
    {
        private const int CheckInterval = 2500;
        private const int ShrinkInterval = 60000;
        private const int WindowTicks = 30 * 60000;
        private const int MaxEntries = 400;
        private const float MaxAbsScore = 5f;
        private const int ShrinkScoreThreshold = -4;
        private const float ShrinkChancePerDay = 0.2f;

        private List<WarScoreEntry> entries = new List<WarScoreEntry>();
        /// <summary>已初始化 comp 點數的哨站 WorldObject.ID（持久化防重複）。</summary>
        private HashSet<int> initializedOutpostIds = new HashSet<int>();
        private List<int> tmpIds;

        public WorldComponent_OutpostWarMomentum(World world) : base(world)
        {
        }

        /// <summary>npc-outposts GrowthRateMultiplier hook 的註冊目標（static：hook 註冊先於 world 存在）。</summary>
        public static float GetGrowthMultiplierFor(Faction faction)
        {
            try
            {
                if (faction == null || OutpostsRimWarMod.Settings == null || !OutpostsRimWarMod.Settings.warMomentumEnabled)
                {
                    return 1f;
                }
                return Find.World?.GetComponent<WorldComponent_OutpostWarMomentum>()?.GrowthMultiplier(faction) ?? 1f;
            }
            catch (Exception e)
            {
                OutpostRimWarUtility.WarnOnce("growthMult", "增生倍率計算異常，退回 1：" + e);
                return 1f;
            }
        }

        public void RecordBattle(Faction winner, Faction loser)
        {
            int now = Find.TickManager.TicksGame;
            if (winner != null)
            {
                entries.Add(new WarScoreEntry(winner, now, 1));
            }
            if (loser != null)
            {
                entries.Add(new WarScoreEntry(loser, now, -1));
            }
            Prune(now);
        }

        /// <summary>滑窗加權戰績，夾 ±5。</summary>
        public float Score(Faction faction)
        {
            int now = Find.TickManager.TicksGame;
            float score = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                WarScoreEntry e = entries[i];
                if (e.faction != faction)
                {
                    continue;
                }
                float age = now - e.tick;
                if (age < WindowTicks)
                {
                    score += e.delta * (1f - age / WindowTicks);
                }
            }
            return Mathf.Clamp(score, -MaxAbsScore, MaxAbsScore);
        }

        /// <summary>勝者線性升到 momentumMaxMultiplier；敗者取對稱倒數。</summary>
        public float GrowthMultiplier(Faction faction)
        {
            float score = Score(faction);
            float max = Mathf.Max(1f, OutpostsRimWarMod.Settings.momentumMaxMultiplier);
            float scaled = 1f + Mathf.Abs(score) / MaxAbsScore * (max - 1f);
            return score >= 0f ? scaled : 1f / scaled;
        }

        private void Prune(int now)
        {
            entries.RemoveAll(e => e.faction == null || now - e.tick >= WindowTicks);
            if (entries.Count > MaxEntries)
            {
                entries.RemoveRange(0, entries.Count - MaxEntries);
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            int ticks = Find.TickManager.TicksGame;
            if (ticks % CheckInterval == 0)
            {
                try
                {
                    InitializeOutpostPoints();
                }
                catch (Exception e)
                {
                    OutpostRimWarUtility.WarnOnce("initPoints", "哨站點數初始化異常，跳過本輪：" + e);
                }
            }
            if (ticks % ShrinkInterval == 0)
            {
                try
                {
                    Prune(ticks);
                    ShrinkLosingFactions();
                }
                catch (Exception e)
                {
                    OutpostRimWarUtility.WarnOnce("shrink", "哨站萎縮檢查異常，跳過本輪：" + e);
                }
            }
        }

        /// <summary>給尚未初始化的哨站寫入初始 RimWarPoints（功能 2 的點數面）。
        /// comp 缺失＝XML 注入失敗 → 警告一次後不再嘗試該哨站。</summary>
        private void InitializeOutpostPoints()
        {
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (!(settlements[i] is NpcOutpost outpost) || outpost.Destroyed
                    || initializedOutpostIds.Contains(outpost.ID))
                {
                    continue;
                }
                RimWarSettlementComp comp = outpost.GetComponent<RimWarSettlementComp>();
                if (comp == null)
                {
                    OutpostRimWarUtility.WarnOnce("noComp",
                        "哨站缺 RimWarSettlementComp（comp 注入 patch 未生效？），點數初始化降級停用。");
                    return;
                }
                comp.RimWarPoints = Mathf.RoundToInt(
                    OutpostRimWarUtility.InitialPointsFor(outpost.TypeDef) * Rand.Range(0.8f, 1.2f));
                initializedOutpostIds.Add(outpost.ID);
            }
        }

        /// <summary>連敗派系（score ≤ −4）每天 20% 機率荒廢一座哨站。跳過有玩家地圖者。</summary>
        private void ShrinkLosingFactions()
        {
            if (!OutpostsRimWarMod.Settings.warMomentumEnabled || !OutpostsRimWarMod.Settings.shrinkEnabled)
            {
                return;
            }
            List<Faction> losers = null;
            for (int i = 0; i < entries.Count; i++)
            {
                Faction f = entries[i].faction;
                if (f != null && !f.IsPlayer && (losers == null || !losers.Contains(f))
                    && Score(f) <= ShrinkScoreThreshold)
                {
                    (losers ??= new List<Faction>()).Add(f);
                }
            }
            if (losers == null)
            {
                return;
            }
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < losers.Count; i++)
            {
                if (!Rand.Chance(ShrinkChancePerDay))
                {
                    continue;
                }
                List<NpcOutpost> candidates = null;
                for (int j = 0; j < settlements.Count; j++)
                {
                    if (settlements[j] is NpcOutpost op && op.Faction == losers[i] && !op.Destroyed && !op.HasMap)
                    {
                        (candidates ??= new List<NpcOutpost>()).Add(op);
                    }
                }
                if (candidates == null)
                {
                    continue;
                }
                NpcOutpost doomed = candidates.RandomElement();
                string label = doomed.Label;
                doomed.Destroy();
                Find.LetterStack.ReceiveLetter(
                    "pas_outposts_rimwar_LetterShrinkLabel".Translate(),
                    "pas_outposts_rimwar_LetterShrinkText".Translate(label, losers[i].NameColored),
                    LetterDefOf.NeutralEvent);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // 治本（同 npc-outposts caps 慣例）：存檔前剔除失效項，避免讀檔期 null 引用紅字。
                entries.RemoveAll(e => e == null || e.faction == null);
                tmpIds = CollectLiveOutpostIds();
            }
            Scribe_Collections.Look(ref entries, "pas_warScoreEntries", LookMode.Deep);
            Scribe_Collections.Look(ref tmpIds, "pas_initializedOutpostIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                entries = entries ?? new List<WarScoreEntry>();
                entries.RemoveAll(e => e == null || e.faction == null);
                initializedOutpostIds = tmpIds != null ? new HashSet<int>(tmpIds) : new HashSet<int>();
                tmpIds = null;
            }
        }

        /// <summary>只持久化仍存活哨站的 ID（已毀者出存檔即清）。</summary>
        private List<int> CollectLiveOutpostIds()
        {
            List<int> live = new List<int>();
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (settlements[i] is NpcOutpost op && initializedOutpostIds.Contains(op.ID))
                {
                    live.Add(op.ID);
                }
            }
            return live;
        }
    }
}
