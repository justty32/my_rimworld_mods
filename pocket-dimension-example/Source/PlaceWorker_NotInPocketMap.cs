using Verse;

namespace PocketDimensionExample
{
    /// <summary>
    /// 禁止在 pocket map 內再蓋異空間門（避免巢狀）。
    /// 巢狀技術上可行（Deep And Deeper 刻意逐層巢狀），但需要處理
    /// Tile/溫度/回收鏈等額外邊界，example mod 直接擋掉。
    /// </summary>
    public class PlaceWorker_NotInPocketMap : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc,
            Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (map.IsPocketMap)
            {
                return new AcceptanceReport("PDE_CannotBuildInPocketDimension".Translate());
            }
            return AcceptanceReport.WasAccepted;
        }
    }
}
