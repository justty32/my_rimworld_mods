using System;
using System.Collections.Generic;
using RimWar.Planet;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.officers.warband
{
    /// <summary>共用小工具：去重警告、角色 def 懶解析、戰力加成公式、戰鬥容器查詢、關係 hook。</summary>
    public static class GeneralsUtility
    {
        private static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>同 key 只 Warning 一次（防衛式降級不洗版，仿 Mod 1）。</summary>
        public static void WarnOnce(string key, string message)
        {
            if (warned.Add(key))
            {
                Log.Warning("[WarbandGenerals] " + message);
            }
        }

        private static OfficerRoleDef generalRole;

        /// <summary>將領角色 def（本 mod XML 出貨）；缺 def 回 null，呼叫端 WarnOnce 降級。</summary>
        public static OfficerRoleDef GeneralRole =>
            generalRole ?? (generalRole = DefDatabase<OfficerRoleDef>.GetNamedSilentFail("pas_warband_General"));

        /// <summary>將領戰力加成：score=(武力+統率)/2，1 + (score-50)/50 × bonusMax。
        /// 無將/停用 → 1。下限 clamp 防除零（bonusMax=1 且 score=0 時為 0）。</summary>
        public static float CombatBonus(OfficerRecord record)
        {
            if (record == null || record.dead)
            {
                return 1f;
            }
            float max = WarbandGeneralsMod.Settings?.bonusMax ?? 0f;
            if (max <= 0f)
            {
                return 1f;
            }
            float score = (record.might + record.command) / 2f;
            return Mathf.Clamp(1f + (score - 50f) / 50f * max, 0.05f, 5f);
        }

        /// <summary>I 段預留 hook（兩將關係影響戰力）：(self, enemy) → 額外乘到 self 側 bonus。
        /// 預設 null＝1；P4/關係 mod 註冊。</summary>
        public static Func<OfficerRecord, OfficerRecord, float> RelationFactor;

        /// <summary>包一層防衛的 RelationFactor 取值：null/例外/NaN/非正 → 1，clamp 0.5~2。</summary>
        public static float SafeRelationFactor(OfficerRecord self, OfficerRecord enemy)
        {
            Func<OfficerRecord, OfficerRecord, float> hook = RelationFactor;
            if (hook == null || self == null)
            {
                return 1f;
            }
            try
            {
                float factor = hook(self, enemy);
                if (float.IsNaN(factor) || factor <= 0f)
                {
                    return 1f;
                }
                return Mathf.Clamp(factor, 0.5f, 2f);
            }
            catch (Exception e)
            {
                WarnOnce("relationFactor", "RelationFactor hook 例外（已忽略、視為 1）：" + e);
                return 1f;
            }
        }

        /// <summary>host 雖 Destroyed 但仍深存於戰鬥容器（BattleSite.Units /
        /// RimWarSettlementComp.AttackingUnits）＝交戰中，將領不得退場。低頻呼叫（心跳/存檔）。</summary>
        public static bool InActiveBattle(WorldObject host)
        {
            if (!(host is WarObject unit))
            {
                return false;
            }
            try
            {
                List<WorldObject> objects = Find.WorldObjects.AllWorldObjects;
                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i] is RimWarSite site && site.Units.Contains(unit))
                    {
                        return true;
                    }
                }
                List<Settlement> settlements = Find.WorldObjects.Settlements;
                for (int i = 0; i < settlements.Count; i++)
                {
                    RimWarSettlementComp comp = settlements[i].GetComponent<RimWarSettlementComp>();
                    if (comp?.AttackingUnits != null && comp.AttackingUnits.Contains(unit))
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                WarnOnce("inActiveBattle", "戰鬥容器查詢例外（視為不在戰鬥）：" + e);
            }
            return false;
        }
    }
}
