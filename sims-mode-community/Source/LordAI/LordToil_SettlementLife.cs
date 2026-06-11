using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace pas.sims
{
    public class LordToil_SettlementLife : LordToil
    {
        private int lastUpdateHour = -1;

        private LordJob_SettlementLife LifeJob => (LordJob_SettlementLife)lord.LordJob;

        public override void UpdateAllDuties()
        {
            Map map = lord.Map;
            int hour = GenLocalDate.HourOfDay(map);
            lastUpdateHour = hour;
            MapComponent_FacilityRegistry registry = map.GetComponent<MapComponent_FacilityRegistry>();
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                LifeRoleDef role = LifeJob.RoleFor(pawn);
                ScheduleEntry entry = role?.EntryAt(hour);
                if (entry == null || entry.duty == null)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.DefendBase, LifeJob.BaseCenter);
                    continue;
                }
                pawn.mindState.duty = new PawnDuty(entry.duty, PickFocus(entry, i, registry));
            }
        }

        /// <summary>同設施多 pawn 時按 index 取模錯開（兩人不會擠同一張床/工作台）。public virtual 供覆寫。</summary>
        public virtual LocalTargetInfo PickFocus(ScheduleEntry entry, int pawnIndex, MapComponent_FacilityRegistry registry)
        {
            if (entry.focusFacility != null)
            {
                List<Thing> list = registry.Get(entry.focusFacility);
                if (list.Count > 0)
                {
                    return list[pawnIndex % list.Count];
                }
            }
            return LifeJob.BaseCenter;
        }

        public override void LordToilTick()
        {
            if (lord.ticksInToil % 250 == 0 && GenLocalDate.HourOfDay(lord.Map) != lastUpdateHour)
            {
                UpdateAllDuties();
            }
        }
    }
}
