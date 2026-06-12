using RimWorld.Planet;
using Verse;

namespace pas.officers.settlements
{
    /// <summary>聚落↔領主綁定一筆。host=Settlement（穩定 WorldObject，Scribe_References）；
    /// record 只存 id、經 OfficersApi 懶解析（record 本體由 P0 registry 深存，唯一真相）。</summary>
    public class LordBinding : IExposable
    {
        public Settlement host;
        public int recordId;

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && host != null && host.Destroyed)
            {
                host = null;   // 已毀宿主以 null 寫出（防 unresolved-ref 警告）；load 後心跳補退場
            }
            Scribe_References.Look(ref host, "host");
            Scribe_Values.Look(ref recordId, "recordId");
        }
    }
}
