using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace pas.gear
{
    /// <summary>dev 便利：對目前所有地圖上、屬於「有裝備表之派系」的 pawn 立即重套裝備。
    /// 用於肉眼驗證裝備表（免一直重生 raid）。Debug actions → pas.gear。</summary>
    public static class GearSeederDebug
    {
        [DebugAction("pas.gear", "Re-apply gear (spawned pawns)",
            actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ReapplyGearToSpawned()
        {
            HashSet<string> seededFactions = new HashSet<string>();
            foreach (FactionGearSeedDef seed in DefDatabase<FactionGearSeedDef>.AllDefsListForReading)
            {
                if (!seed.factionDef.NullOrEmpty())
                {
                    seededFactions.Add(seed.factionDef);
                }
            }
            if (seededFactions.Count == 0)
            {
                Messages.Message("[gear-seeder] 沒有任何 FactionGearSeedDef。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int touched = 0;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in new List<Pawn>(map.mapPawns.AllPawnsSpawned))
                {
                    Faction f = pawn.Faction;
                    if (f?.def == null || !seededFactions.Contains(f.def.defName))
                    {
                        continue;
                    }
                    GearSeedApplier.TryApply(pawn, f);
                    touched++;
                }
            }
            Messages.Message("[gear-seeder] 重套裝備於 " + touched + " 隻 pawn（"
                + seededFactions.Count + " 個派系）。", MessageTypeDefOf.TaskCompletion, false);
        }
    }
}
