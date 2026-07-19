using RimWorld;
using Verse;

namespace RatkinQuestlines
{
    // 獎勵道具「王國調兵符」的使用效果：召喚鼠族王國友軍前來馳援。
    // ------------------------------------------------------------------
    // 武器商任務線的招牌獎勵。使用後，一隊鼠族王國（Rakinia）武裝從地圖邊緣走入，
    // 以「立即協防」姿態幫玩家作戰——即原版「請求盟友軍事支援」所走的同一機制：
    //   IncidentDefOf.RaidFriendly + faction=Rakinia + raidStrategy=ImmediateAttackFriendly
    //   + raidArrivalMode=EdgeWalkIn → 內部產生 LordJob_AssistColony。
    //
    // 道具透過 CompProperties_Usable 觸發（showUseGizmo），並掛 CompProperties_UseEffectDestroySelf 一次即毀。
    //
    // ⚠ 待實機：本效果的觸發鏈（RaidFriendly.Worker.TryExecute + friendly 策略）沿用原版節點，
    //   但 CQF/道具實機互動未在 2-anime modlist 內跑過，列為最高 runtime 風險項（見 REVIEW）。
    //   v1 為即時到達（信物＝求援煙火）；「一兩天後才到」列為後續精修。
    public class CompProperties_RatkinAid : CompProperties_UseEffect
    {
        public float points = 800f;   // 友軍戰力點數；越高來的越多/越強

        public CompProperties_RatkinAid()
        {
            compClass = typeof(CompUseEffect_RatkinAid);
        }
    }

    public class CompUseEffect_RatkinAid : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            Map map = (usedBy != null ? usedBy.MapHeld : null) ?? Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            FactionDef ratDef = DefDatabase<FactionDef>.GetNamedSilentFail("Rakinia");
            Faction rat = ratDef != null ? Find.FactionManager.FirstFactionOfDef(ratDef) : null;
            if (rat == null || rat.HostileTo(Faction.OfPlayer))
            {
                Messages.Message("RatkinQL_Aid_NoFaction".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            float pts = ((CompProperties_RatkinAid)props).points;
            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = rat,
                points = pts,
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
                raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly,
            };
            IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
        }
    }
}
