using System;
using System.Collections.Generic;
using pas.outposts;
using RimWar;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.outposts.rimwar
{
    /// <summary>功能 5：降低 RimWar 建聚落頻率、改以哨站擴張。
    /// Prefix WorldUtility.CreateSettlement（RW:15248，settler 抵達落地呼叫）：按 settlerToOutpostChance
    /// 擲骰命中 → 取 settler 母聚落、解析派系 OutpostProfile、改建 NpcOutpost、return false skip 原版建城。
    /// 未命中／非 settler／母聚落缺失／profile null／放置失敗／例外 → return true 放行原版（fail-soft）。
    /// 本片段不含領主選型（調查 J 的 TypeSelector），type 由 profile 權重隨機。</summary>
    public static class Patch_CreateSettlement
    {
        public static bool Prefix(WarObject warObject, List<WorldObject> objectsHere,
            RimWarData rwd, PlanetTile tile, Faction faction)
        {
            try
            {
                float chance = OutpostsRimWarMod.Settings.settlerToOutpostChance;
                if (chance <= 0f || !(warObject is Settler settler))
                {
                    return true; // 機率 0 或非 settler → 原版行為
                }
                if (Rand.Value > chance)
                {
                    return true; // 未命中 → 原版建城
                }
                Settlement parent = settler.ParentSettlement;
                if (parent == null || parent.Destroyed || faction == null)
                {
                    return true; // 沒母聚落可掛 → 放行原版
                }
                OutpostProfileDef profile = OutpostProfileResolver.Resolve(faction);
                if (profile == null)
                {
                    return true; // 派系停用哨站 → 放行原版建城
                }
                NpcOutpost outpost = OutpostPlacer.TryPlaceFor(parent, profile);
                if (outpost == null)
                {
                    return true; // 找不到合適 tile／放置失敗 → 放行原版
                }
                Find.LetterStack.ReceiveLetter(
                    "pas_outposts_rimwar_LetterOutpostFoundedLabel".Translate(),
                    "pas_outposts_rimwar_LetterOutpostFoundedText".Translate(
                        outpost.Label, faction.NameColored, parent.Name),
                    LetterDefOf.NeutralEvent, outpost);
                return false; // 已改建哨站 → skip 原版建聚落
            }
            catch (Exception e)
            {
                OutpostRimWarUtility.WarnOnce("createSettlementPrefix",
                    "settler→哨站 prefix 異常，放行 RimWar 原版建聚落：" + e);
                return true;
            }
        }
    }
}
