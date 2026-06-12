using Verse;

namespace pas.outposts.rimwar
{
    /// <summary>掛在 OutpostTypeDef 上的平衡係數（Patches XML 注入，免改 npc-outposts 本體）。
    /// 同時縮放「每週期貢獻」與「初始 RimWarPoints」。</summary>
    public class RimWarOutpostExtension : DefModExtension
    {
        public float pointsFactor = 1f;
    }
}
