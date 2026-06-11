using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace pas.politics
{
    /// <summary>dev mode 診斷：逐派系列出追蹤/跳過原因＋record 細節，E2E 對帳用。
    /// 入口：Debug actions → pas.politics → Dump rebellion state（輸出到 log）。</summary>
    public static class PoliticsDebugActions
    {
        [DebugAction("pas.politics", "Dump rebellion state", allowedGameStates = AllowedGameStates.Playing)]
        private static void DumpRebellionState()
        {
            WorldComponent_RebellionTracker tracker =
                Find.World.GetComponent<WorldComponent_RebellionTracker>();
            StringBuilder sb = new StringBuilder("[faction-politics] rebellion state dump");
            sb.AppendLine().Append("dynamic factions: ").Append(tracker.SpawnedForDebug.Count);
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                sb.AppendLine().Append("  ").Append(faction.Name).Append(" [")
                    .Append(faction.def.defName).Append("] → ").Append(Status(tracker, faction));
            }
            Log.Message(sb.ToString());
        }

        private static string Status(WorldComponent_RebellionTracker tracker, Faction faction)
        {
            foreach (RebelRecord record in tracker.RecordsForDebug)
            {
                if (record.faction == faction)
                {
                    return Describe(record);
                }
            }
            if (faction.IsPlayer) return "skip: player";
            if (faction.Hidden) return "skip: hidden";
            if (faction.defeated) return "skip: defeated";
            if (faction.temporary) return "skip: temporary";
            if (!faction.def.humanlikeFaction) return "skip: non-humanlike";
            if (RebellionProfileResolver.Disabled(faction)) return "skip: PoliticsDisabledExtension";
            RebellionProfileDef profile = RebellionProfileResolver.Resolve(faction);
            if (profile == null) return "skip: no profile";
            int count = WorldComponent_RebellionTracker.CountSettlements(faction);
            if (count < profile.minSettlements)
            {
                return "skip: settlements " + count + " < " + profile.minSettlements;
            }
            PawnKindDef kind = faction.RandomPawnKind();
            if (kind == null) return "skip: RandomPawnKind null（無 pawnGroupMakers 人形選項）";
            if (!kind.RaceProps.Humanlike) return "skip: member kind non-humanlike";
            return "pending: 合格、待下次心跳補發（或生成例外，見 log 警告）";
        }

        private static string Describe(RebelRecord record)
        {
            if (record.rebel == null || record.rebel.Dead || record.rebel.Destroyed)
            {
                return "tracked: 反叛者死亡/待重生 respawnAtTick=" + record.respawnAtTick
                    + " now=" + Find.TickManager.TicksGame;
            }
            return "tracked: " + record.rebel.LabelShortCap
                + " progress=" + record.progress.ToString("F1")
                + " rate=" + record.ratePerDay.ToString("F1") + "/day"
                + " home=" + (record.homeSettlement?.Name.ToString() ?? "null")
                + " spawned=" + record.rebel.Spawned
                + " world=" + Find.WorldPawns.Contains(record.rebel)
                + " forcedKeep=" + Find.WorldPawns.ForcefullyKeptPawns.Contains(record.rebel)
                + " inhabitantsList=" + (record.homeSettlement != null
                    && record.homeSettlement.previouslyGeneratedInhabitants.Contains(record.rebel));
        }
    }
}
