using RimWorld;
using Verse;

namespace pas.officers
{
    /// <summary>A 軌包裝：持久關係（結拜/世仇）= vanilla DirectPawnRelation。
    /// 對 world pawn 完全可用（AddDirectRelation 無 Spawned 檢查）、隨 pawn 自動存檔。
    /// 命名避開 vanilla RelationsUtility。</summary>
    public static class RelationsUtility_Officers
    {
        /// <summary>建立持久關係；兩端未具現則先 Materialize（G4 決議）。失敗回 false。</summary>
        public static bool AddPersistent(OfficerRecord a, OfficerRecord b, PawnRelationDef def)
        {
            if (a == null || b == null || def == null || a == b)
            {
                return false;
            }
            Pawn pa = OfficerSpawner.Materialize(a);
            Pawn pb = OfficerSpawner.Materialize(b);
            if (pa == null || pb == null || pa == pb)
            {
                return false;
            }
            if (!pa.relations.DirectRelationExists(def, pb))
            {
                pa.relations.AddDirectRelation(def, pb);   // reflexive：vanilla 自動補對向
            }
            return true;
        }

        /// <summary>查持久關係；不觸發具現（任一端未具現 → false）。</summary>
        public static bool HasPersistent(OfficerRecord a, OfficerRecord b, PawnRelationDef def)
        {
            if (a?.pawn == null || b?.pawn == null || def == null)
            {
                return false;
            }
            return a.pawn.relations.DirectRelationExists(def, b.pawn);
        }
    }
}
