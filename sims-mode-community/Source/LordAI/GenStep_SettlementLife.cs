using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace pas.sims
{
    /// <summary>聚落地圖生成尾端：非敵對派系 → 把守軍從 LordJob_DefendBase 移到生活 Lord。</summary>
    public class GenStep_SettlementLife : GenStep
    {
        public override int SeedPart => 745619032;

        public override void Generate(Map map, GenStepParams parms)
        {
            Faction faction = map.ParentFaction;
            if (faction == null || faction == Faction.OfPlayer || faction.HostileTo(Faction.OfPlayer))
            {
                return;     // 攻打/玩家地圖：保持原版行為
            }
            List<Pawn> pawns = CollectDefendBasePawns(map, faction);
            if (pawns.Count == 0)
            {
                return;
            }
            MapComponent_FacilityRegistry registry = map.GetComponent<MapComponent_FacilityRegistry>();
            registry.RebuildAll();
            LifeProfileDef profile = ProfileResolver.Resolve(faction);
            if (profile == null)
            {
                return;
            }
            Dictionary<Pawn, LifeRoleDef> assignments = profile.Worker.Assign(pawns, profile, map, registry);
            if (assignments.Count == 0)
            {
                return;     // 無可用角色（如地圖無任何設施）：保持原版
            }
            IntVec3 center = ComputeCenter(pawns);
            for (int i = 0; i < pawns.Count; i++)
            {
                pawns[i].GetLord()?.Notify_PawnLost(pawns[i], PawnLostCondition.ForcedToJoinOtherLord);
            }
            LordMaker.MakeNewLord(faction, new LordJob_SettlementLife(faction, center, assignments), map, pawns);
        }

        /// <summary>只接管掛在原版 LordJob_DefendBase 上的人形 pawn（讓位給其他已改聚落行為的 mod）。</summary>
        public virtual List<Pawn> CollectDefendBasePawns(Map map, Faction faction)
        {
            var result = new List<Pawn>();
            List<Pawn> spawned = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < spawned.Count; i++)
            {
                Pawn p = spawned[i];
                if (p.RaceProps.Humanlike && p.GetLord()?.LordJob is LordJob_DefendBase)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        public static IntVec3 ComputeCenter(List<Pawn> pawns)
        {
            IntVec3 sum = IntVec3.Zero;
            for (int i = 0; i < pawns.Count; i++)
            {
                sum += pawns[i].Position;
            }
            return new IntVec3(sum.x / pawns.Count, 0, sum.z / pawns.Count);
        }
    }
}
