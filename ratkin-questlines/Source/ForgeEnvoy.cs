using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // 交貨成功後遣離訪客：移除該訪客的對話（互動選項消失、不能再交）＋讓整團走出地圖。
    // ------------------------------------------------------------------
    // 使用者定調：交易/交貨完成後鼠訪客就該離去。三條交貨路徑（板栗同步對話 / C 挑選視窗 / B 原生交易）成功時都呼叫本 util。
    // 反之「暫時不交貨/詳情」等 decline 不呼叫、也把選項的 removeDialogAfterSelect 設 false → 對話持續存在，玩家可回頭再交。
    public static class ForgeEnvoyUtil
    {
        public static void SendEnvoyAway(Thing envoyThing)
        {
            if (envoyThing == null)
            {
                return;
            }
            GameComponent_Editor ed = GameComponent_Editor.Component;
            if (ed != null)
            {
                ed.RemoveDialog(envoyThing);   // 對話掛在帶頭訪客身上，移除＝互動選項消失
            }

            Pawn pawn = envoyThing as Pawn;
            if (pawn == null || pawn.Map == null)
            {
                return;
            }
            Lord lord = pawn.GetLord();
            // 防重入：已在離場中（ExitMapBest lord）就 no-op——否則重複呼叫（連點敲詐/交貨回呼重入）
            //   會 LordMaker.MakeNewLord 第二次，撞「pawn already a member of lord」紅字。
            if (lord != null && lord.LordJob is LordJob_ExitMapBest)
            {
                return;
            }
            List<Pawn> group = (lord != null && lord.ownedPawns != null && lord.ownedPawns.Count > 0)
                ? new List<Pawn>(lord.ownedPawns)
                : new List<Pawn> { pawn };
            // 換成「盡快走出地圖」的 Lord（原本是 LordJob_VisitColony 停留 15 天）
            LordMaker.MakeNewLord(pawn.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Walk), pawn.Map, group);
        }
    }

    // 對話端 action：交貨成功後遣離訪客（Interviewee＝帶頭訪客）。板栗這類「同步對話交貨」的 deliver 選項掛此。
    // （C 視窗 / B 原生交易 由 C# 在成功回呼裡直接呼 ForgeEnvoyUtil.SendEnvoyAway，不經本 action。）
    public class CQFAction_SendEnvoyAway : CQFAction_Target
    {
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (targets == null)
            {
                return;
            }
            foreach (KeyValuePair<string, TargetInfo> kv in targets)
            {
                if (kv.Value.HasThing)
                {
                    ForgeEnvoyUtil.SendEnvoyAway(kv.Value.Thing);
                }
            }
        }
    }
}
