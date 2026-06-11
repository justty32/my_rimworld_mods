using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace pas.sims
{
    public class ScheduleEntry
    {
        public int from;                       // 起始時辰（含），0-23
        public int to;                         // 結束時辰（不含）；from > to 表跨夜（如 22-7）
        public DutyDef duty;
        public FacilityTagDef focusFacility;   // 可空：空 → fallback 聚落中心

        public bool Contains(int hour)
        {
            if (from <= to)
            {
                return hour >= from && hour < to;
            }
            return hour >= from || hour < to;
        }
    }

    public class LifeRoleDef : Def
    {
        public FacilityTagDef requiredFacility;            // 地圖上沒有此設施 → 不分配此角色
        public List<PawnKindDef> fixedRoleForPawnKinds;    // 這些 pawnKind 一律此角色
        public List<ScheduleEntry> schedule = new List<ScheduleEntry>();

        public ScheduleEntry EntryAt(int hour)
        {
            for (int i = 0; i < schedule.Count; i++)
            {
                if (schedule[i].Contains(hour))
                {
                    return schedule[i];
                }
            }
            return null;
        }
    }
}
