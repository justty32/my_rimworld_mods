using System.Collections.Generic;
using RimWorld;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // 可複用自製 CQF 動作：把目標 pawn 改成固定姓名。
    // ------------------------------------------------------------------
    // 用途：實現「具名人物加入殖民地」這類獎勵——例如破產的商人阿蒙求收留，
    //   對話用 CQFAction_Faction 把他收編成殖民者後，再用本動作把他正名為「阿蒙」，
    //   讓玩家得到的不是隨機路人鼠，而是這條故事線裡認識的那個人。
    //
    // 機制：CQFAction_Target.Work() 會先把 targetsText（如 Interviewee）解析成實際 targets，
    //   再呼叫 RealWork。我們在 RealWork 對每個是 Pawn 的目標設 Name = NameTriple(first, nick, last)。
    //
    // XML 用法：
    //   <li Class="RatkinQuestlines.CQFAction_SetName">
    //     <targetsText><li>Interviewee</li></targetsText>
    //     <first>Peirin</first><nick>Peirin</nick><last>Walnut</last><gender>Female</gender>
    //   </li>
    // nick 省略則不設暱稱；last 省略則只有名。三語姓名交由 keyed 之外處理——這裡是直接字面（人名不翻）。
    // gender 選填（"Female"/"Male"）：設定目標性別並刷新外觀——用於「具名角色加入」需固定性別時
    //   （如佩林必須是母鼠）。刷新用 Verse.PawnRenderer.SetAllGraphicsDirty()。
    public class CQFAction_SetName : CQFAction_Target
    {
        public string first;
        public string nick;
        public string last;
        public string gender;

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (KeyValuePair<string, TargetInfo> kv in targets)
            {
                Thing thing = kv.Value.Thing;
                Pawn pawn = thing as Pawn;
                if (pawn == null)
                {
                    continue;
                }
                pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);

                if (!string.IsNullOrEmpty(gender))
                {
                    if (gender == "Female" && pawn.gender != Gender.Female)
                    {
                        pawn.gender = Gender.Female;
                    }
                    else if (gender == "Male" && pawn.gender != Gender.Male)
                    {
                        pawn.gender = Gender.Male;
                    }
                    if (pawn.Drawer != null && pawn.Drawer.renderer != null)
                    {
                        pawn.Drawer.renderer.SetAllGraphicsDirty();
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref first, "RatkinQL_setname_first");
            Scribe_Values.Look(ref nick, "RatkinQL_setname_nick");
            Scribe_Values.Look(ref last, "RatkinQL_setname_last");
            Scribe_Values.Look(ref gender, "RatkinQL_setname_gender");
        }
    }
}
