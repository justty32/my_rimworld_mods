using System;
using System.Collections.Generic;
using FactionColonies;
using pas.outposts;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.empire.outposts.war
{
    /// <summary>功能 3：哨站隨聚落易主（雙向，純走 Empire LifecycleRegistry）。
    /// 與 Mod1 的 RimWar ConvertSettlement 路徑正交 → 不雙觸發。
    /// - OnSettlementCreated：玩家 Capture 奪下 NPC 聚落 → 來源哨站改派玩家附庸。
    /// - OnSettlementRemoved：附庸淪陷（Mod2 同 tile 建攻方聚落）→ 哨站改派攻方。</summary>
    public class OutpostTransferHooks : LifecycleParticipantBase
    {
        public override void OnSettlementCreated(WorldSettlementFC settlement)
        {
            try
            {
                if (!Enabled() || settlement == null)
                {
                    return;
                }
                // 僅在 Capture 上下文活躍（＝這是奪城新建的附庸）時認領；自建/淪陷重建不適用。
                if (!CaptureContext.Active)
                {
                    return;
                }
                Faction sourceFaction = CaptureContext.TargetFaction;
                Settlement source = CaptureContext.TargetSettlement; // 已被 Capture Destroy，引用仍可比對
                if (sourceFaction == null)
                {
                    return;
                }
                Faction newOwner = settlement.Faction; // PColony
                int moved = ReassignOutposts(
                    op => (source != null && op.ParentSettlement == source)
                          || (op.Faction == sourceFaction && NearTile(op, settlement.Tile)),
                    newOwner, settlement);
                if (moved > 0)
                {
                    OutpostWarUtility.MigrateSpawnerCap(source, settlement);
                    Find.LetterStack.ReceiveLetter(
                        "pas_empire_war_OutpostsSeizedLabel".Translate(),
                        "pas_empire_war_OutpostsSeizedText".Translate(moved, settlement.Name),
                        LetterDefOf.PositiveEvent, new LookTargets(settlement));
                }
            }
            catch (Exception e)
            {
                OutpostWarUtility.WarnOnce("createHook", "奪城哨站認領異常，本次跳過：" + e);
            }
        }

        public override void OnSettlementRemoved(WorldSettlementFC settlement)
        {
            try
            {
                if (!Enabled() || settlement == null)
                {
                    return;
                }
                // 附庸淪陷：Mod2 已在同 tile AddNewHome 建攻方 NPC 聚落（同 tick）。
                // 找該新聚落＝哨站新主；找不到（非淪陷的一般移除，如玩家放棄）則摧毀孤兒哨站。
                Settlement newHome = SettlementAtTileExcluding(settlement.Tile, settlement);
                List<NpcOutpost> orphans = CollectOutpostsOf(settlement);
                if (orphans.Count == 0)
                {
                    return;
                }
                if (newHome != null && newHome.Faction != null && !newHome.Faction.IsPlayer
                    && !FactionCache.IsPlayerColonyFaction(newHome.Faction))
                {
                    Faction conqueror = newHome.Faction;
                    foreach (NpcOutpost op in orphans)
                    {
                        op.SetFaction(conqueror);
                        op.Setup(op.TypeDef, newHome);
                    }
                    OutpostWarUtility.MigrateSpawnerCap(settlement, newHome);
                    Find.LetterStack.ReceiveLetter(
                        "pas_empire_war_OutpostsLostLabel".Translate(),
                        "pas_empire_war_OutpostsLostText".Translate(orphans.Count, conqueror.NameColored),
                        LetterDefOf.NegativeEvent, new LookTargets(newHome));
                }
                else
                {
                    // 無接收者：哨站失去母聚落 → 摧毀（避免孤兒哨站殘留玩家派系）。
                    foreach (NpcOutpost op in orphans)
                    {
                        op.Destroy();
                    }
                }
            }
            catch (Exception e)
            {
                OutpostWarUtility.WarnOnce("removeHook", "附庸淪陷哨站易主異常，本次跳過：" + e);
            }
        }

        private static bool Enabled()
        {
            return OutpostsWarMod.Settings != null && OutpostsWarMod.Settings.transferOnConquest;
        }

        private static bool NearTile(NpcOutpost op, PlanetTile tile)
        {
            return tile.Valid && Find.WorldGrid.ApproxDistanceInTiles(tile, op.Tile) <= 6f;
        }

        private static List<NpcOutpost> CollectOutpostsOf(Settlement parent)
        {
            List<NpcOutpost> list = new List<NpcOutpost>();
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is NpcOutpost op && !op.Destroyed && op.ParentSettlement == parent)
                {
                    list.Add(op);
                }
            }
            return list;
        }

        private static int ReassignOutposts(Predicate<NpcOutpost> match, Faction newOwner, Settlement newParent)
        {
            int moved = 0;
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is NpcOutpost op && !op.Destroyed && match(op))
                {
                    op.SetFaction(newOwner);
                    op.Setup(op.TypeDef, newParent);
                    moved++;
                }
            }
            return moved;
        }

        private static Settlement SettlementAtTileExcluding(PlanetTile tile, Settlement exclude)
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                Settlement s = all[i];
                if (s != exclude && !(s is NpcOutpost) && !s.Destroyed && s.Tile == tile)
                {
                    return s;
                }
            }
            return null;
        }
    }
}
