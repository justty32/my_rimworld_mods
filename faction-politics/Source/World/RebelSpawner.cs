using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.politics
{
    /// <summary>反叛者生成：world pawn KeepForever + 記入駐地 previouslyGeneratedInhabitants
    /// （原版 1.6 從不自動填這清單——PawnGenerator.cs:236 死碼——我們是唯一供給者）。</summary>
    public static class RebelSpawner
    {
        /// <summary>給派系生成首個反叛者並建 record；失敗（無聚落/無 kind）回 null。</summary>
        public static RebelRecord TrySpawnFor(Faction faction, RebellionProfileDef profile)
        {
            Settlement home = PickHome(faction);
            Pawn rebel = ((home != null) ? GeneratePawn(faction) : null);
            if (rebel == null)
            {
                return null;
            }
            Find.WorldPawns.PassToWorld(rebel, PawnDiscardDecideMode.KeepForever);
            home.previouslyGeneratedInhabitants.Add(rebel);
            RebelRecord record = new RebelRecord
            {
                faction = faction,
                rebel = rebel,
                homeSettlement = home,
                progress = 0f,
                ratePerDay = profile.progressPerDay.RandomInRange,
                respawnAtTick = -1
            };
            Find.LetterStack.ReceiveLetter("pas_politics_RebelEmergedLabel".Translate(rebel.LabelShortCap),
                "pas_politics_RebelEmergedText".Translate(rebel.LabelShortCap, faction.Name, home.Name),
                LetterDefOf.NeutralEvent, home);
            return record;
        }

        /// <summary>冷卻到期重生（沿用 record；不發信，鎮壓循環不刷信箱）。</summary>
        public static void Respawn(RebelRecord record, RebellionProfileDef profile)
        {
            Settlement home = PickHome(record.faction);
            Pawn rebel = ((home != null) ? GeneratePawn(record.faction) : null);
            if (rebel == null)
            {
                return;
            }
            Find.WorldPawns.PassToWorld(rebel, PawnDiscardDecideMode.KeepForever);
            home.previouslyGeneratedInhabitants.Add(rebel);
            record.rebel = rebel;
            record.homeSettlement = home;
            record.progress = 0f;
            record.ratePerDay = profile.progressPerDay.RandomInRange;
            record.respawnAtTick = -1;
        }

        /// <summary>挑駐地：該派系非衛星聚落隨機一個；無則 null。</summary>
        public static Settlement PickHome(Faction faction)
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            List<Settlement> candidates = new List<Settlement>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Faction == faction && !PoliticsBridges.IsSatellite(all[i]))
                {
                    candidates.Add(all[i]);
                }
            }
            return candidates.TryRandomElement(out Settlement result) ? result : null;
        }

        private static Pawn GeneratePawn(Faction faction)
        {
            // E2E 實測：1.6 原版只有玩家派系 def 填 basicMemberKind，NPC 派系全 null（Task 0 #7 盲點）。
            // 改用 Faction.RandomPawnKind()：彙整 pawnGroupMakers 全部 Humanlike 選項隨機，後備 basicMemberKind。
            PawnKindDef kind = faction.RandomPawnKind();
            if (kind == null || !kind.RaceProps.Humanlike)
            {
                return null;
            }
            // 具名引數：PawnGenerationRequest 跨版本欄位多次擴充，位置引數會錯位（rimworld-mod-guide 第 11 章）。
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: kind, faction: faction, context: PawnGenerationContext.NonPlayer);
            return PawnGenerator.GeneratePawn(request);
        }
    }
}
