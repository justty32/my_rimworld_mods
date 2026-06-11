using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace pas.outposts
{
    /// <summary>哨站地圖生成後，按 OutpostTypeDef.defenderPointsFactor 比例削減守軍人數。
    /// 僅對 NpcOutpost 地圖生效（經 ExtraGenStepDefs 注入，其他地圖不會跑到）。
    /// order 9990：跑在 sims-mode GenStep_SettlementLife（9999）之前，角色分配看到削減後人口。</summary>
    public class GenStep_TrimDefenders : GenStep
    {
        public override int SeedPart => 612873451;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!(map.Parent is NpcOutpost outpost) || outpost.Faction == null)
            {
                return;
            }
            float factor = outpost.TypeDef?.defenderPointsFactor ?? 1f;
            if (factor >= 1f)
            {
                return;
            }
            List<Pawn> defenders = map.mapPawns.SpawnedPawnsInFaction(outpost.Faction)
                .Where(p => p.RaceProps.Humanlike).ToList();
            int keep = Mathf.Max(1, Mathf.CeilToInt(defenders.Count * factor));
            int removeCount = defenders.Count - keep;
            for (int i = 0; i < removeCount; i++)
            {
                Pawn victim = defenders.RandomElement();
                defenders.Remove(victim);
                victim.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
