using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.officers
{
    /// <summary>具現管線：GeneratePawn → PassToWorld(KeepForever) → inhabitants 橋。
    /// 手法照抄 faction-politics RebelSpawner（已實機驗證 1.6 唯一供給者路徑）。
    /// 與 RebelSpawner 差異：不挑駐地（呼叫方指定）、不發 Letter、無 respawn 循環。</summary>
    public static class OfficerSpawner
    {
        /// <summary>按需具現；已具現且在世則直接回。失敗回 null（record 保持輕量態）。</summary>
        public static Pawn Materialize(OfficerRecord record)
        {
            if (record == null || record.dead)
            {
                return null;
            }
            if (record.pawn != null && !record.pawn.Dead && !record.pawn.Destroyed)
            {
                return record.pawn;
            }
            Pawn pawn = GeneratePawn(record.faction);
            if (pawn == null)
            {
                return null;
            }
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            BridgeInhabitants(record, pawn);
            record.pawn = pawn;
            SyncName(record);
            return pawn;
        }

        /// <summary>assignedTo 是 Settlement 才有橋可搭（拜訪 redress 請回同一 pawn）。
        /// warband 等非 Settlement 宿主無此橋，pawn 僅 world pawn（打到地圖時由消費 mod
        /// 自行注入 GeneratePawnGroup——非 P0 範圍）。</summary>
        private static void BridgeInhabitants(OfficerRecord record, Pawn pawn)
        {
            if (record.assignedTo is Settlement s
                && !s.previouslyGeneratedInhabitants.Contains(pawn))
            {
                s.previouslyGeneratedInhabitants.Add(pawn);
            }
        }

        /// <summary>名字策略方案 B（T0 定案）：首次具現後快取 pawn 名進 nameCached，
        /// 此後即使 pawn 亡佚名字仍在；具現前 DisplayName 落到 role label。</summary>
        private static void SyncName(OfficerRecord record)
        {
            if (record.nameCached.NullOrEmpty())
            {
                record.nameCached = record.pawn?.Name?.ToStringShort ?? record.pawn?.LabelShortCap;
            }
        }

        private static Pawn GeneratePawn(Faction faction)
        {
            if (faction == null)
            {
                return null;
            }
            // E2E 實測（faction-politics）：1.6 原版只有玩家派系 def 填 basicMemberKind，
            // NPC 派系全 null。改用 Faction.RandomPawnKind()：彙整 pawnGroupMakers 全部
            // Humanlike 選項隨機，後備 basicMemberKind。
            PawnKindDef kind = faction.RandomPawnKind();
            if (kind == null || !kind.RaceProps.Humanlike)
            {
                return null;
            }
            // 具名引數：PawnGenerationRequest 跨版本欄位多次擴充，位置引數會錯位。
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: kind, faction: faction, context: PawnGenerationContext.NonPlayer);
            return PawnGenerator.GeneratePawn(request);
        }
    }
}
