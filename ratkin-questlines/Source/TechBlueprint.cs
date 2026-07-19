using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // 科技藍圖：使用後解鎖一組研究（每層第一個委託接受時空投給玩家，讓玩家做得出該層委託的武器）。
    // ------------------------------------------------------------------
    // 使用者定案（brainstorm/6b §6.2）：村莊(T1)/城鎮(T2)/王國(T3) 各自「第一個任務」接受瞬間空投「科技藍圖」；
    //   可用道具，玩家自選時機使用 → 完成該層武器所需研究（整層）。T1＝RK_Research_Carpentry + RK_Research_SwordAndShield。
    // 道具透過 CompProperties_Usable 觸發，並掛 CompProperties_UseEffectDestroySelf 一次即毀（同令狀寫法）。
    public class CompProperties_UnlockResearch : CompProperties_UseEffect
    {
        public List<ResearchProjectDef> projects = new List<ResearchProjectDef>();

        public CompProperties_UnlockResearch()
        {
            compClass = typeof(CompUseEffect_UnlockResearch);
        }
    }

    public class CompUseEffect_UnlockResearch : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            List<ResearchProjectDef> projs = ((CompProperties_UnlockResearch)props).projects;
            if (projs == null)
            {
                return;
            }
            int done = 0;
            foreach (ResearchProjectDef proj in projs)
            {
                if (proj == null)
                {
                    continue;
                }
                if (Find.ResearchManager.GetProgress(proj) < proj.baseCost)
                {
                    Find.ResearchManager.FinishProject(proj, false, null, false);
                    done++;
                }
            }
            Messages.Message("RatkinQL_TechBlueprint_Used".Translate(done), MessageTypeDefOf.PositiveEvent, false);
        }
    }

    // 空投科技藍圖（每層只送一次）：接受首個該層任務時觸發；讀 gateBool，未送過才空投＋set true。
    // ------------------------------------------------------------------
    // 仿 QuestNode_SpawnRatkinEnvoy：RunInt 建 QuestPart，inSignal 留空＝吃 quest initiate（接受時）。
    // 因為 CQF 純腳本 context 生不出實體（見 quest-mechanics-reference §223），故自製 C# QuestPart 直接投放。
    public class QuestNode_DropTechBlueprintOnce : QuestNode
    {
        public ThingDef blueprint;
        public string gateBool;                 // 例：RatkinQL_Forge_T1TechGifted
        public int stackCount = 1;
        [NoTranslate]
        public SlateRef<string> inSignal;
        public string letterLabel;
        public string letterText;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_DropTechBlueprintOnce part = QuestGen.quest.AddPart<QuestPart_DropTechBlueprintOnce>();
            part.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate))
                            ?? slate.Get<string>("inSignal", null, false);
            part.blueprint = blueprint;
            part.gateBool = gateBool;
            part.stackCount = stackCount;
            part.letterLabel = letterLabel;
            part.letterText = letterText;
        }

        protected override bool TestRunInt(Slate slate)
        {
            return blueprint != null;
        }
    }

    public class QuestPart_DropTechBlueprintOnce : QuestPart
    {
        public string inSignal;
        public ThingDef blueprint;
        public string gateBool;
        public int stackCount = 1;
        public string letterLabel;
        public string letterText;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag != inSignal)
            {
                return;
            }
            Drop();
        }

        private void Drop()
        {
            if (blueprint == null)
            {
                return;
            }
            GameComponent_Editor ed = GameComponent_Editor.Component;
            if (ed != null && !string.IsNullOrEmpty(gateBool) && ed.GetBool(gateBool))
            {
                return;   // 該層藍圖已送過，不重複
            }
            Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            Thing t = ThingMaker.MakeThing(blueprint);
            t.stackCount = stackCount;
            IntVec3 spot = DropCellFinder.TradeDropSpot(map);
            DropPodUtility.DropThingsNear(spot, map, Gen.YieldSingle(t), 110, false, false, true, true);

            if (ed != null && !string.IsNullOrEmpty(gateBool))
            {
                ed.SetBool(gateBool, true);
            }

            if (!letterLabel.NullOrEmpty() && !letterText.NullOrEmpty())
            {
                Find.LetterStack.ReceiveLetter(letterLabel.Translate(), letterText.Translate(), LetterDefOf.PositiveEvent, new TargetInfo(spot, map));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Defs.Look(ref blueprint, "blueprint");
            Scribe_Values.Look(ref gateBool, "gateBool");
            Scribe_Values.Look(ref stackCount, "stackCount", 1);
            Scribe_Values.Look(ref letterLabel, "letterLabel");
            Scribe_Values.Look(ref letterText, "letterText");
        }
    }
}
