using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.officers
{
    /// <summary>B 軌演化：每心跳，各 record 對「同宿主同僚」的 opinion 向 bias 回歸一步。
    /// bias = A 軌 DirectPawnRelation 的 opinionOffset 加總（結拜 +60 / 世仇 -60 / 無關係 0）。
    /// 演化只是回歸骨架——事件式漲跌由消費 mod 經 OfficersApi.OffsetOpinion 寫入脈衝。</summary>
    public static class OpinionEvolver
    {
        public const int OpinionMin = -100;
        public const int OpinionMax = 100;

        internal static void EvolveAll(List<OfficerRecord> records, OfficersSettingsDef settings)
        {
            int drift = settings.opinionDriftPerHeartbeat;
            for (int i = 0; i < records.Count; i++)
            {
                OfficerRecord a = records[i];
                if (a.assignedTo == null || a.dead)
                {
                    continue;   // 跨宿主/待命不演化；既有鍵值保留（舊怨仍在，P1 讀）
                }
                for (int j = 0; j < records.Count; j++)
                {
                    OfficerRecord b = records[j];
                    if (i == j || b.dead || b.assignedTo != a.assignedTo)
                    {
                        continue;   // 只演化同宿主配對（宿主官數 ≤ maxOfficersPerObject，可控）
                    }
                    Evolve(a, b, drift);
                }
            }
        }

        private static void Evolve(OfficerRecord a, OfficerRecord b, int drift)
        {
            int bias = BiasOf(a, b);
            if (!a.opinions.TryGetValue(b.id, out int current))
            {
                a.opinions[b.id] = bias;     // 缺鍵 → 以 bias 初始化
                return;
            }
            if (current < bias)
            {
                current += System.Math.Min(drift, bias - current);
            }
            else if (current > bias)
            {
                current -= System.Math.Min(drift, current - bias);
            }
            a.opinions[b.id] = Clamp(current);
        }

        /// <summary>讀 A 軌當回歸目標。不觸發具現（雙方未具現 → 0）——
        /// 避免心跳路徑悄悄生 world pawn（爆量風險鐵律）。</summary>
        public static int BiasOf(OfficerRecord a, OfficerRecord b)
        {
            if (a?.pawn == null || b?.pawn == null || a.pawn.relations == null)
            {
                return 0;
            }
            int bias = 0;
            List<DirectPawnRelation> direct = a.pawn.relations.DirectRelations;
            for (int i = 0; i < direct.Count; i++)
            {
                if (direct[i].otherPawn == b.pawn)
                {
                    bias += direct[i].def.opinionOffset;
                }
            }
            return Clamp(bias);
        }

        internal static int Clamp(int value)
            => value < OpinionMin ? OpinionMin : (value > OpinionMax ? OpinionMax : value);
    }
}
