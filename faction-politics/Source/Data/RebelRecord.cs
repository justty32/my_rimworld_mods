using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.politics
{
    /// <summary>一個派系的反叛追蹤（權威資料；previouslyGeneratedInhabitants 只是請回場的橋）。</summary>
    public class RebelRecord : IExposable
    {
        public Faction faction;
        public Pawn rebel;
        public Settlement homeSettlement;
        public float progress;
        /// <summary>生成時自 profile.progressPerDay 擲定，每反叛者步調不同。</summary>
        public float ratePerDay;
        /// <summary>-1 = 反叛者在世；>=0 = 死亡冷卻，到期重生。</summary>
        public int respawnAtTick = -1;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref rebel, "rebel");
            Scribe_References.Look(ref homeSettlement, "homeSettlement");
            Scribe_Values.Look(ref progress, "progress");
            Scribe_Values.Look(ref ratePerDay, "ratePerDay");
            Scribe_Values.Look(ref respawnAtTick, "respawnAtTick", -1);
        }
    }
}
