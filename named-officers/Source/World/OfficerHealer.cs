using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.officers
{
    /// <summary>心跳自癒（泛化 RebellionTracker.Heal，逐分支對照計畫 T4）。
    /// 由 WorldComponent_OfficerRegistry 每心跳逐 record 呼叫。</summary>
    internal static class OfficerHealer
    {
        internal static void Heal(WorldComponent_OfficerRegistry registry, OfficerRecord record)
        {
            if (record.faction == null)                       // 5. faction 滅亡只在 null 時清
            {
                registry.Remove(record);
                return;
            }
            if (record.pawn != null && (record.pawn.Dead || record.pawn.Destroyed))
            {
                if (!record.dead)                             // 1. G5：標記+廣播，留一輪讀遺言
                {
                    record.dead = true;
                    OfficersApi.RaiseOfficerDied(record);
                    return;
                }
                registry.Remove(record);                      // 下一輪心跳清理
                return;
            }
            if (record.pawn != null && !record.pawn.Spawned)  // 2. world pawn 自癒
            {
                if (!Find.WorldPawns.Contains(record.pawn))
                {
                    Find.WorldPawns.PassToWorld(record.pawn, PawnDiscardDecideMode.KeepForever);
                }
                else if (!Find.WorldPawns.ForcefullyKeptPawns.Contains(record.pawn))
                {
                    // 拜訪 redress 副作用會清 forced-keep；補回（冪等）。
                    Find.WorldPawns.ForcefullyKeptPawns.Add(record.pawn);
                }
            }
            if (record.assignedTo != null
                && (record.assignedTo.Destroyed || record.assignedTo.Faction != record.faction))
            {
                registry.Assign(record, null);                // 3. 宿主消失：不自動搬家，待命
                OfficersApi.RaiseOfficerUnassigned(record);
            }
            if (record.assignedTo is Settlement s && record.pawn != null
                && !s.previouslyGeneratedInhabitants.Contains(record.pawn))
            {
                s.previouslyGeneratedInhabitants.Add(record.pawn);   // 4. inhabitants 橋補鋪
            }
        }

        /// <summary>同 record+階段只警告一次（防 log 洪水，仿 RebellionTracker.WarnOnce）。</summary>
        private static readonly HashSet<string> warned = new HashSet<string>();

        internal static void WarnOnce(OfficerRecord record, string stage, System.Exception e)
        {
            string key = stage + ":" + (record?.id.ToString() ?? "null");
            if (warned.Add(key))
            {
                Log.Warning("[named-officers] " + stage + " 例外，跳過職官 "
                    + (record?.DisplayName ?? "?") + "（id=" + (record?.id ?? -1) + "）：" + e);
            }
        }
    }
}
