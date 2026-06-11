using System.Collections.Generic;
using System.Linq;
using pas.sims;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.outposts
{
    /// <summary>NPC 派系哨站。繼承 Settlement 白嫖交易/送禮/擊敗判定；
    /// 拜訪與攻打 override 成小圖流程；ExtraGenStepDefs 注入守軍 trim。
    /// [StaticConstructorOnStartup]：靜態貼圖欄位須主執行緒載入——世界生成在背景執行緒，
    /// 首個 MakeWorldObject 觸發 cctor 會炸（E2E 實測 Quick Test 世界生成紅字）。</summary>
    [StaticConstructorOnStartup]
    public class NpcOutpost : Settlement
    {
        private static readonly Texture2D AttackCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/AttackSettlement");

        private Settlement parentSettlement;
        private OutpostTypeDef typeDef;

        public Settlement ParentSettlement => parentSettlement;

        public OutpostTypeDef TypeDef => typeDef;

        public IntVec3 OutpostMapSize => typeDef?.mapSize ?? new IntVec3(150, 1, 150);

        public override MapGeneratorDef MapGeneratorDef => typeDef?.mapGeneratorDef ?? base.MapGeneratorDef;

        public void Setup(OutpostTypeDef type, Settlement parent)
        {
            typeDef = type;
            parentSettlement = parent;
        }

        /// <summary>所有生圖路徑（拜訪/攻打/任務）都會帶上守軍 trim（GetOrGenerateMapUtility.cs:26 concat）。</summary>
        public override IEnumerable<GenStepWithParams> ExtraGenStepDefs
        {
            get
            {
                foreach (GenStepWithParams step in base.ExtraGenStepDefs)
                {
                    yield return step;
                }
                yield return new GenStepWithParams(OutpostDefOf.pas_outposts_TrimDefenders, default(GenStepParams));
            }
        }

        /// <summary>重組 float menu：原版停格拜訪（交易用）+ 進入小圖（sims-mode ArrivalAction）+ 送禮 + 小圖攻擊。
        /// 不呼叫 base——Settlement 的攻擊選項走全尺寸圖。</summary>
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption option in CaravanArrivalAction_VisitSettlement.GetFloatMenuOptions(caravan, this))
            {
                yield return option;
            }
            foreach (FloatMenuOption option in CaravanArrivalAction_VisitMap.GetFloatMenuOptions(caravan, this, OutpostMapSize))
            {
                yield return option;
            }
            foreach (FloatMenuOption option in CaravanArrivalAction_OfferGifts.GetFloatMenuOptions(caravan, this))
            {
                yield return option;
            }
            foreach (FloatMenuOption option in OutpostAttackUtility.GetFloatMenuOptions(caravan, this))
            {
                yield return option;
            }
        }

        /// <summary>重組 caravan gizmo：交易/送禮照抄 Settlement.cs:313-326，攻擊換小圖流程。</summary>
        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            if (CanTradeNow && CaravanVisitUtility.SettlementVisitedNow(caravan) == this)
            {
                yield return CaravanVisitUtility.TradeCommand(caravan, Faction, TraderKind);
            }
            if ((bool)CaravanArrivalAction_OfferGifts.CanOfferGiftsTo(caravan, this))
            {
                yield return FactionGiftUtility.OfferGiftsCommand(caravan, this);
            }
            if (Attackable)
            {
                yield return new Command_Action
                {
                    icon = AttackCommandTex,
                    defaultLabel = "CommandAttackSettlement".Translate(),
                    defaultDesc = "CommandAttackSettlementDesc".Translate(),
                    action = delegate
                    {
                        OutpostAttackUtility.Attack(caravan, this);
                    }
                };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref parentSettlement, "pas_parentSettlement");
            Scribe_Defs.Look(ref typeDef, "pas_typeDef");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && typeDef == null)
            {
                typeDef = DefDatabase<OutpostTypeDef>.AllDefsListForReading.FirstOrDefault();
            }
        }
    }
}
