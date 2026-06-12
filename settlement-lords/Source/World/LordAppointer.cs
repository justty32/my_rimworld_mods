using System.Collections.Generic;
using RimWar;
using RimWar.Planet;
using RimWorld.Planet;
using Verse;

namespace pas.officers.settlements
{
    /// <summary>心跳指派掃描（00-overview 決策 2）：無主候選城按機率補太守。
    /// 候選＝有 RimWarSettlementComp、Faction 非 null 非玩家、behavior 非 Player/Excluded、
    /// 未綁定。機率＝上任速度（長期收斂為每城有太守）；每心跳節流上限。</summary>
    internal static class LordAppointer
    {
        private const int MaxNewLordsPerHeartbeat = 5;

        internal static void Scan(WorldComponent_SettlementLords lords)
        {
            float chance = SettlementLordsMod.Settings?.lordChance ?? 0f;
            if (chance <= 0f)
            {
                return;
            }
            OfficerRoleDef role = LordsUtility.LordRole;
            if (role == null)
            {
                LordsUtility.WarnOnce("noRole", "找不到角色 def pas_settlement_Lord，領主指派停用。");
                return;
            }
            int created = 0;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count && created < MaxNewLordsPerHeartbeat; i++)
            {
                Settlement s = settlements[i];
                if (s.Destroyed || s.Faction == null || s.Faction.IsPlayer || lords.HasLord(s)
                    || s.GetComponent<RimWarSettlementComp>() == null)
                {
                    continue;
                }
                RimWarData rwd = WorldUtility.GetRimWarDataForFaction(s.Faction);
                if (rwd == null || rwd.behavior == RimWarBehavior.Player
                    || rwd.behavior == RimWarBehavior.Excluded || !Rand.Chance(chance))
                {
                    continue;
                }
                OfficerRecord record = OfficersApi.CreateOfficer(s.Faction, s, role);
                if (record != null)   // null＝G6 上限滿/參數壞 → 安靜跳過
                {
                    lords.Bind(s, record);
                    created++;
                }
            }
        }
    }
}
