using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // 信件觸發「具名客戶線」的地基（鐵匠屋／傭兵團共用）。
    // ------------------------------------------------------------------
    // 為什麼要這個：舊機制把對話隨機掛在隨機鼠族訪客頭上，連 dev 都刷不出、正常玩很可能整局遇不到、也測不動。
    // 改成信件制：QuestScriptDef 由說書人提供＝任務欄看得到、可接受/拒絕的「任務信」；玩家接受後才召帶對話的訪客。
    //
    // 機制（仿 QuestNode_DoCQFActions 的信號掛法 ＋ IncidentWorker_VisitorGroup 的生訪客邏輯）：
    //   RunInt（任務生成時）建一個 QuestPart_SpawnRatkinEnvoy，inSignal 留空＝吃 quest initiate 信號（＝玩家接受時）。
    //   接受時 → 生一小隊鼠族訪客（PawnGroupMakerUtility，同原版 VisitorGroup）＋做到訪 Lord（LordJob_VisitColony，訪畢自行離開）
    //   ＋用已證實的 GameComponent_Editor.Component.AddDialog(帶頭訪客, dm) 把該客戶的對話掛上去。
    //   之後玩家與帶頭訪客對話＝走現有 DialogTree 機器（委託/交貨/善惡旗標，全照舊）。
    //
    // ✅ 已坐實：自製 C# 子類能被反射載入＋runtime 執行（F7 側寫器/CompUseEffect_RatkinAid）；AddDialog(Thing,DialogManagerDef) 存在。
    public class QuestNode_SpawnRatkinEnvoy : QuestNode
    {
        public DialogManagerDef dialogManager;              // 掛到帶頭訪客的客戶對話
        [NoTranslate]
        public SlateRef<string> inSignal;                   // 留空＝吃 quest initiate（玩家接受時）
        public string factionDef = "Rakinia";               // 訪客所屬鼠族派系（和平王國，恆存在）
        public FloatRange points = new FloatRange(50f, 70f); // 訪客團規模點數（小隊）
        public string letterLabel;                          // 選填：到訪信件標題（Keyed key）
        public string letterText;                           // 選填：到訪信件內文（Keyed key，可用 {PAWN_...}）

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_SpawnRatkinEnvoy part = QuestGen.quest.AddPart<QuestPart_SpawnRatkinEnvoy>();
            part.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate))
                            ?? slate.Get<string>("inSignal", null, false);
            part.dialogManager = dialogManager;
            part.factionDefName = factionDef;
            part.points = points.RandomInRange;
            part.letterLabel = letterLabel;
            part.letterText = letterText;
        }

        protected override bool TestRunInt(Slate slate)
        {
            return dialogManager != null;
        }
    }

    public class QuestPart_SpawnRatkinEnvoy : QuestPart
    {
        public string inSignal;
        public DialogManagerDef dialogManager;
        public string factionDefName = "Rakinia";
        public float points = 60f;
        public string letterLabel;
        public string letterText;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag != inSignal)
            {
                return;
            }
            SpawnEnvoy();
        }

        private void SpawnEnvoy()
        {
            Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null || dialogManager == null)
            {
                return;
            }

            FactionDef fdef = DefDatabase<FactionDef>.GetNamedSilentFail(factionDefName);
            Faction faction = fdef != null ? Find.FactionManager.FirstFactionOfDef(fdef) : null;
            if (faction == null || faction.HostileTo(Faction.OfPlayer))
            {
                return;
            }

            IntVec3 entry;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out entry, map, CellFinder.EdgeRoadChance_Neutral))
            {
                return;
            }

            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = faction,
                points = points,
                spawnCenter = entry,
            };

            List<Pawn> pawns = PawnGroupMakerUtility.GeneratePawns(
                IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Peaceful, parms, true),
                false).ToList();

            // 保底：某派系沒有 Peaceful pawnGroupMaker 時，至少生一名基本成員當信使，避免觸發鏈斷掉。
            if (pawns.Count == 0 && faction.def.basicMemberKind != null)
            {
                pawns.Add(PawnGenerator.GeneratePawn(faction.def.basicMemberKind, faction));
            }
            if (pawns.Count == 0)
            {
                return;
            }

            foreach (Pawn p in pawns)
            {
                // 清掉與殖民者/彼此的社交關係：訪客團 GeneratePawns 預設會 roll 關係，
                //   臨時 pawn 帶殖民者關係→存讀檔黃字（not deep-saved / relation with null）。訪客是過客，關係無意義。
                if (p.relations != null)
                {
                    p.relations.ClearAllRelations();
                }
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(entry, map, 5);
                GenSpawn.Spawn(p, loc, map);
            }

            IntVec3 chill;
            RCellFinder.TryFindRandomSpotJustOutsideColony(pawns[0], out chill);
            // 到訪停留拉長（預設 ~1 天太短，玩家研究+打造+交貨來不及）：停留 15 天，交貨成功會由 SendEnvoyAway 提前遣離。
            LordMaker.MakeNewLord(faction, new LordJob_VisitColony(faction, chill, 900000), map, pawns);

            Pawn leader = pawns.Find(x => faction.leader == x) ?? pawns[0];
            GameComponent_Editor.Component.AddDialog(leader, dialogManager);

            if (!letterLabel.NullOrEmpty() && !letterText.NullOrEmpty())
            {
                Find.LetterStack.ReceiveLetter(
                    letterLabel.Translate(),
                    letterText.Translate(leader.Named("PAWN")),
                    LetterDefOf.PositiveEvent,
                    leader);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Defs.Look(ref dialogManager, "dialogManager");
            Scribe_Values.Look(ref factionDefName, "factionDefName", "Rakinia");
            Scribe_Values.Look(ref points, "points", 60f);
            Scribe_Values.Look(ref letterLabel, "letterLabel");
            Scribe_Values.Look(ref letterText, "letterText");
        }
    }

    // 守衛節點：某個 CQF 全域 bool 為指定值時，任務才被說書人提供。
    // ------------------------------------------------------------------
    // 用法：塞進 QuestScriptDef 的 root Sequence 最前面。TestRunInt 回 false ⇒ 說書人不會挑這條劇本。
    //   鐵匠屋據點身份 gate（RatkinQL_State_Crafter）、分級解鎖（RatkinQL_Forge_T2Unlocked…）、
    //   per-客戶未 blocked（RatkinQL_Forge_<id>_Available）都靠它。多個條件＝疊多個守衛節點。
    public class QuestNode_RequireGlobalBool : QuestNode
    {
        public string key;
        public bool value = true;

        protected override void RunInt()
        {
        }

        protected override bool TestRunInt(Slate slate)
        {
            if (string.IsNullOrEmpty(key))
            {
                return true;
            }
            GameComponent_Editor ed = GameComponent_Editor.Component;
            return ed != null && ed.GetBool(key) == value;
        }
    }
}
