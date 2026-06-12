using System.Collections.Generic;
using RimWar;
using RimWar.Planet;
using UnityEngine;
using Verse;

namespace pas.officers.settlements
{
    /// <summary>共用小工具：去重警告、角色 def 懶解析、治理係數公式、成長上限鏡像。</summary>
    public static class LordsUtility
    {
        /// <summary>每輪成長週期、治理係數滿幅偏離（|gov-1|=1）對應的點數擺幅。
        /// 預設幅度 0.5 → 極端 ±15 點/輪（原版每輪 +1..100、低科技常態 +1~5）。</summary>
        public const int GovPointsScale = 30;

        private static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>同 key 只 Warning 一次（防衛式降級不洗版，仿 Mod 1／P1）。</summary>
        public static void WarnOnce(string key, string message)
        {
            if (warned.Add(key))
            {
                Log.Warning("[SettlementLords] " + message);
            }
        }

        private static OfficerRoleDef lordRole;

        /// <summary>領主角色 def（本 mod XML 出貨）；缺 def 回 null，呼叫端 WarnOnce 降級。</summary>
        public static OfficerRoleDef LordRole =>
            lordRole ?? (lordRole = DefDatabase<OfficerRoleDef>.GetNamedSilentFail("pas_settlement_Lord"));

        /// <summary>治理係數：score=0.7×政務+0.3×忠誠，1 + (score-50)/50 × govAmplitude。
        /// 無主/已死/幅度 0 → 1。clamp 0.25~2 防極端設定。</summary>
        public static float GovernanceFactor(OfficerRecord record)
        {
            if (record == null || record.dead)
            {
                return 1f;
            }
            float amplitude = SettlementLordsMod.Settings?.govAmplitude ?? 0f;
            if (amplitude <= 0f)
            {
                return 1f;
            }
            float score = 0.7f * record.polity + 0.3f * record.loyalty;
            return Mathf.Clamp(1f + (score - 50f) / 50f * amplitude, 0.25f, 2f);
        }

        /// <summary>聚落成長上限，鏡像 IncrementSettlementGrowth（RW:17597-17612）：
        /// 基礎 50000、City_Citadel +5000、首都 +5000（Vassal +1000）。
        /// 自帶一份（與 Mod 1 GrowthCapFor 同邏輯）避免硬依賴 Mod 1。</summary>
        public static int GrowthCapFor(RimWarSettlementComp comp, RimWarData rwd)
        {
            int cap = 50000;
            if (comp.parent?.def?.defName == "City_Citadel")
            {
                cap += 5000;
            }
            if (comp.isCapitol)
            {
                cap += (rwd != null && rwd.behavior == RimWarBehavior.Vassal) ? 1000 : 5000;
            }
            return cap;
        }
    }
}
