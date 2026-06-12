using System;
using FactionColonies;
using FactionColonies.util;
using RimWar.Planet;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.empire.warfare
{
    /// <summary>
    /// 淪陷執行。刻意不走 RimWar 的 WorldUtility.ConvertSettlement——
    /// 該方法的 Destroy 會撞 Empire 的 destroyFlag 守衛、被官方 Patch_ConvertSettlement prefix 短路，
    /// 且失敗方聚落歸零時會 RemoveRWDFaction 清掉整個 PColony。
    /// 改為：Empire 正規退場（ColonyUtil.RemovePlayerSettlement）＋ RimWar 正規建城＋註冊。
    /// </summary>
    public static class VassalFallUtility
    {
        private const float FallenPointsRatio = 0.6f; // 戰損折價
        private const int MinPoints = 500;
        private const int MaxPoints = 100000;

        /// <summary>執行淪陷；成功回傳記錄（供收復偵測），任何前置不成立則回傳 null。</summary>
        public static FallenRecord TryExecuteFall(PlanetTile tile, Faction attacker)
        {
            try
            {
                FactionFC factionFC = FactionCache.FactionComp;
                WorldSettlementFC settlement = factionFC?.ReturnSettlementByLocation(tile);
                if (settlement == null || !factionFC.settlements.Contains(settlement))
                {
                    LogUtil.Warning($"[EmpireWarfare] fall aborted: settlement at tile {tile} no longer exists.");
                    return null;
                }
                if (settlement.HasMap)
                {
                    LogUtil.Warning($"[EmpireWarfare] fall aborted: {settlement.Name} still has an active map.");
                    return null;
                }
                if (attacker == null || attacker.defeated || attacker.IsPlayer
                    || FactionCache.IsPlayerColonyFaction(attacker))
                {
                    LogUtil.Warning($"[EmpireWarfare] fall aborted for {settlement.Name}: attacker faction invalid.");
                    return null;
                }

                string name = settlement.Name;
                int basePoints = settlement.GetComponent<RimWarSettlementComp>()?.RimWarPoints ?? 0;
                int points = Mathf.Clamp((int)(basePoints * FallenPointsRatio), MinPoints, MaxPoints);

                // 1) Empire 正規退場：PreDestruction → PrepareDestroy → InvokeOnSettlementRemoved →
                //    settlements/Bill/事件/快取/軍事全清（聚落視窗「放棄」同款 API）
                ColonyUtil.RemovePlayerSettlement(settlement);

                // 2) 原 tile 建攻方 NPC 聚落（RimWar 包裝，RimCities 相容；def 用原版 Settlement）
                Settlement newHome = RimWar.Planet.SettlementUtility.AddNewHome(tile, attacker, null);
                if (newHome == null)
                {
                    LogUtil.Error($"[EmpireWarfare] AddNewHome failed at tile {tile}; vassal removed without replacement.");
                    return null;
                }

                // 3) 註冊進 RimWarData 並催更新聚落清單
                RimWar.RimWarData rwd = WorldUtility.GetRimWarDataForFaction(attacker);
                if (rwd != null)
                {
                    WorldUtility.CreateRimWarSettlementWithPoints(rwd, newHome, points, false, 0);
                    rwd.rwdNextUpdateTick = Find.TickManager.TicksGame;
                }
                else
                {
                    LogUtil.Warning($"[EmpireWarfare] no RimWarData for {attacker.Name}; "
                        + "settlement created but not point-initialized (PowerTracker will pick it up).");
                }

                // 4) 淪陷信件（紅字）
                Find.LetterStack.ReceiveLetter("pas_warfare_VassalFallenLabel".Translate(),
                    "pas_warfare_VassalFallenDesc".Translate(name, attacker.Name),
                    LetterDefOf.NegativeEvent, new LookTargets(newHome));

                LogUtil.MessageForce($"[EmpireWarfare] vassal {name} (tile {tile}) fell to {attacker.Name}, points={points}.");

                return new FallenRecord
                {
                    tile = tile,
                    settlementName = name,
                    attacker = attacker,
                    tick = Find.TickManager.TicksGame,
                };
            }
            catch (Exception e)
            {
                LogUtil.Error("[EmpireWarfare] TryExecuteFall threw: " + e);
                return null;
            }
        }
    }
}
