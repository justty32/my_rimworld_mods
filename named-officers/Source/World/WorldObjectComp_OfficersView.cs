using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.officers
{
    /// <summary>無狀態 view comp 的 props（G3 決議）。P0 不注入任何 def（零補丁）；
    /// P1/P2 把本 props 注入 RW_Warband/Settlement defs 即得 inspect 顯示。</summary>
    public class WorldObjectCompProperties_Officers : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_Officers()
        {
            compClass = typeof(WorldObjectComp_OfficersView);
        }
    }

    /// <summary>讀 registry 供 inspect；不持有資料、不 scribe（record 是屬性唯一真相）。</summary>
    public class WorldObjectComp_OfficersView : WorldObjectComp
    {
        public override string CompInspectStringExtra()
        {
            IReadOnlyList<OfficerRecord> officers = OfficersApi.GetOfficers(parent);
            if (officers.Count == 0)
            {
                return null;
            }
            List<OfficerRecord> sorted = new List<OfficerRecord>(officers);
            sorted.Sort((a, b) => (a.role?.displayPriority ?? 0).CompareTo(b.role?.displayPriority ?? 0));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(sorted[i].DisplayName).Append(" (")
                    .Append(sorted[i].role?.label ?? "?").Append(")");
            }
            return "pas_officers_InspectLine".Translate(sb.ToString());
        }
    }
}
