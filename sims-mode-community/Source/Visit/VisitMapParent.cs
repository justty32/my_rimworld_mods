using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.sims
{
    /// <summary>拜訪圖的影子宿主（E2E 實測根因）：原版 Settlement.TickInterval 無條件跑 CheckDefeated，
    /// 而 IsDefeated 只認「對玩家構成主動威脅」的人形——友方/中立圖上的住民永不構成威脅，
    /// 聚落只要掛著拜訪圖就必被判攻陷。生圖後把 map.info.parent 換成本物件
    /// （CheckDefeated 摧毀流程自己也這樣重指 parent，公開欄位、受認可的操作），
    /// Settlement.Map 回到 null → CheckDefeated 永遠早退，聚落安全。
    /// 重逢 redress 不受影響：PawnGenerator 按 tile 用 WorldObjectAt&lt;Settlement&gt; 讀寫
    /// previouslyGeneratedInhabitants，與 map parent 無關（IL 核對）。</summary>
    public class VisitMapParent : MapParent
    {
        private Settlement settlement;

        /// <summary>給「被拜訪的聚落/據點本體」在世界地圖上轉發影子的重組遠行隊 gizmo——
        /// 影子隱形不可選取，玩家入口在聚落圖示上（E2E 回饋）。</summary>
        public static IEnumerable<Gizmo> ReformGizmosFor(MapParent owner)
        {
            if (owner == null || !owner.Spawned || owner.HasMap)
            {
                yield break;   // 聚落自己掛圖（被進攻中）時原版 FormCaravanComp 已有 gizmo
            }
            VisitMapParent visit = Find.WorldObjects.WorldObjectAt<VisitMapParent>(owner.Tile);
            if (visit == null || visit.settlement != owner || !visit.HasMap)
            {
                yield break;
            }
            FormCaravanComp form = visit.GetComponent<FormCaravanComp>();
            if (form == null)
            {
                yield break;
            }
            foreach (Gizmo gizmo in form.GetGizmos())
            {
                yield return gizmo;
            }
        }

        public Settlement VisitedSettlement
        {
            get => settlement;
            set => settlement = value;
        }

        public override string Label => settlement != null ? settlement.Label : base.Label;

        // 與聚落同格：本物件不自行繪製，避免圖示疊影/z-fighting。
        public override void Print(LayerSubMesh subMesh)
        {
        }

        public override void Draw()
        {
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            // 條件抄 Settlement.ShouldRemoveMapNow；差別只在圖收掉時本物件一起移除，聚落本體留在世界。
            alsoRemoveWorldObject = true;
            Map map = Map;
            return map != null
                && !map.AnyBuildingBlockingMapRemoval
                && !map.IsPlayerHome
                && !map.mapPawns.AnyPawnBlockingMapRemoval
                && !TransporterUtility.IncomingTransporterPreventingMapRemoval(map);
        }

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!HasMap && !Destroyed)
            {
                Destroy();   // 防呆：地圖被其他途徑移除時自清，不留隱形殘骸
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
        }
    }
}
