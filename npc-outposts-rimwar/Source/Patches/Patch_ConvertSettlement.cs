using System;
using System.Collections.Generic;
using HarmonyLib;
using pas.outposts;
using RimWar;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.outposts.rimwar
{
    /// <summary>功能 3：WorldUtility.ConvertSettlement（RW:15289，captured 唯一入口 RW:11168）。
    /// Prefix：哨站被佔 → 攔下原版「毀掉重建成 vanilla Settlement」，改為易主（預設）或摧毀。
    /// Postfix：普通聚落易主 → 其衛星哨站跟著易主、spawner cap 字典搬鍵。</summary>
    public static class Patch_ConvertSettlement
    {
        public static bool Prefix(Settlement worldSettlement, RimWarData rwdFrom, RimWarData rwdTo, int points, int pointDamage)
        {
            try
            {
                if (!(worldSettlement is NpcOutpost outpost) || outpost.Destroyed)
                {
                    return true;
                }
                Faction conqueror = rwdTo?.RimWarFaction;
                string outpostLabel = outpost.Label;
                if (OutpostsRimWarMod.Settings.captureToConqueror && conqueror != null)
                {
                    outpost.SetFaction(conqueror);
                    // 重掛母聚落：攻方最近的非哨站聚落（找不到＝null，spawner 計數自然忽略孤兒）。
                    outpost.Setup(outpost.TypeDef, OutpostRimWarUtility.ClosestSettlementOf(conqueror, outpost.Tile));
                    RimWarSettlementComp comp = outpost.GetComponent<RimWarSettlementComp>();
                    if (comp != null)
                    {
                        int basePoints = OutpostRimWarUtility.InitialPointsFor(outpost.TypeDef);
                        comp.RimWarPoints = Mathf.Clamp(points, 100, basePoints * 2);
                        comp.PointDamage = Mathf.Clamp(pointDamage, 0, comp.RimWarPoints - 1);
                        comp.AttackingUnits.Clear();
                    }
                    Find.LetterStack.ReceiveLetter(
                        "pas_outposts_rimwar_LetterCapturedLabel".Translate(),
                        "pas_outposts_rimwar_LetterCapturedText".Translate(outpostLabel, conqueror.NameColored),
                        LetterDefOf.NeutralEvent, outpost);
                }
                else
                {
                    outpost.Destroy();
                    Find.LetterStack.ReceiveLetter(
                        "pas_outposts_rimwar_LetterRazedLabel".Translate(),
                        "pas_outposts_rimwar_LetterRazedText".Translate(
                            outpostLabel, conqueror?.NameColored ?? (TaggedString)"???"),
                        LetterDefOf.NeutralEvent);
                }
                // 哨站不在 WorldSettlements → 原版的 RemoveRWDFaction 存亡判定不適用，直接 skip。
                return false;
            }
            catch (Exception e)
            {
                OutpostRimWarUtility.WarnOnce("convertPrefix", "哨站易主 prefix 異常，放行 RimWar 原版行為：" + e);
                return true;
            }
        }

        public static void Postfix(Settlement worldSettlement, RimWarData rwdFrom, RimWarData rwdTo)
        {
            try
            {
                if (worldSettlement == null || worldSettlement is NpcOutpost)
                {
                    return; // 哨站案例已在 prefix 整段處理
                }
                Faction conqueror = rwdTo?.RimWarFaction;
                if (conqueror == null)
                {
                    return;
                }
                // 原版已在同 tile 毀舊建新；找出新聚落當衛星的新母聚落。
                Settlement newHome = Find.WorldObjects.SettlementAt(worldSettlement.Tile);
                if (newHome != null && (newHome.Faction != conqueror || newHome is NpcOutpost))
                {
                    newHome = null;
                }
                bool any = false;
                List<Settlement> settlements = Find.WorldObjects.Settlements;
                for (int i = 0; i < settlements.Count; i++)
                {
                    if (settlements[i] is NpcOutpost satellite && satellite.ParentSettlement == worldSettlement
                        && !satellite.Destroyed)
                    {
                        satellite.SetFaction(conqueror);
                        satellite.Setup(satellite.TypeDef, newHome);
                        any = true;
                    }
                }
                if (any)
                {
                    MigrateSpawnerCap(worldSettlement, newHome);
                }
            }
            catch (Exception e)
            {
                OutpostRimWarUtility.WarnOnce("convertPostfix", "衛星哨站跟隨易主 postfix 異常，本次跳過：" + e);
            }
        }

        /// <summary>spawner 私有 caps 字典搬鍵（舊聚落→新聚落）。反射 fail-soft：
        /// 失敗只警告——舊鍵已 Destroyed，npc-outposts 存檔前清理會兜底，不會 null-key 紅字。</summary>
        private static void MigrateSpawnerCap(Settlement oldHome, Settlement newHome)
        {
            WorldComponent_OutpostSpawner spawner = Find.World.GetComponent<WorldComponent_OutpostSpawner>();
            Dictionary<Settlement, int> caps =
                AccessTools.Field(typeof(WorldComponent_OutpostSpawner), "caps")?.GetValue(spawner)
                    as Dictionary<Settlement, int>;
            if (caps == null)
            {
                OutpostRimWarUtility.WarnOnce("capsField", "讀不到 WorldComponent_OutpostSpawner.caps，cap 搬鍵降級停用。");
                return;
            }
            if (caps.TryGetValue(oldHome, out int cap))
            {
                caps.Remove(oldHome);
                if (newHome != null && !caps.ContainsKey(newHome))
                {
                    caps[newHome] = cap;
                }
            }
        }
    }
}
