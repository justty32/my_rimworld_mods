using System;
using FactionColonies;
using RimWorld.Planet;
using UnityEngine;

namespace pas.empire.outposts.war
{
    /// <summary>功能 1（附庸防守加成）＋功能 2（前哨緩衝層）的單一戰力修正器。
    /// 走 Empire 契約層 BattleModifierRegistry，免對 Empire 動 Harmony。
    /// 只對防守方（!isAttacker）加 militaryLevel；無哨站＝零變化。</summary>
    public class OutpostBattleModifier : IBattleModifier
    {
        public void ModifyForce(MilitaryForce force, bool isAttacker)
        {
            try
            {
                if (isAttacker || force == null)
                {
                    return; // 只強化防守方
                }
                OutpostsWarSettings s = OutpostsWarMod.Settings;
                if (s == null || s.defenseLevelPerOutpost <= 0f)
                {
                    return;
                }

                int outposts = 0;
                if (force.homeSettlement != null)
                {
                    // 案例 A：附庸（或任何有 homeSettlement 的聚落）防守 → 數其同主哨站（功能 1/2 附庸側）。
                    outposts = OutpostWarUtility.CountSatellites(force.homeSettlement);
                }
                else if (force.homeFaction != null && CaptureContext.Active
                    && force.homeFaction == CaptureContext.TargetFaction)
                {
                    // 案例 B：玩家 Capture NPC 聚落，敵方 force 無 homeSettlement →
                    // 用 Capture 上下文定位目標聚落，數其哨站加敵防（功能 2 玩家側削防）。
                    Settlement target = CaptureContext.TargetSettlement;
                    if (target != null && !target.Destroyed)
                    {
                        outposts = OutpostWarUtility.CountSatellites(target);
                    }
                }

                if (outposts <= 0)
                {
                    return;
                }
                outposts = Mathf.Min(outposts, Mathf.Max(1, s.maxOutpostsCounted));
                force.militaryLevel += outposts * s.defenseLevelPerOutpost;
            }
            catch (Exception e)
            {
                OutpostWarUtility.WarnOnce("battleMod", "哨站防守加成異常，本次跳過：" + e);
            }
        }
    }
}
