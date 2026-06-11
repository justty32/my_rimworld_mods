using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyArchivalOutpost
{
    // N6b：每個 pawn 採樣開始時的非傷勢 hediff severity 快照
    public class PawnHediffSnapshot : IExposable
    {
        public string pawnId;
        public Dictionary<HediffDef, float> hediffSeverities = new Dictionary<HediffDef, float>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Collections.Look(ref hediffSeverities, "hediffSeverities", LookMode.Def, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (hediffSeverities == null) hediffSeverities = new Dictionary<HediffDef, float>();
                // 移除提供該 HediffDef 的內容 mod 後，Def key 解析為 null → 先剔除。
                hediffSeverities.RemoveAll(kv => kv.Key == null);
            }
        }
    }
}
