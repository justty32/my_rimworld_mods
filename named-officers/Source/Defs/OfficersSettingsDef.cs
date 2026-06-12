using System.Collections.Generic;
using Verse;

namespace pas.officers
{
    /// <summary>全域設定，恰好 1 個實例（healthcheck 把關）。仿 faction-politics PoliticsSettingsDef。</summary>
    public class OfficersSettingsDef : Def
    {
        /// <summary>心跳節流（02 設計指定 2500-tick 範式）。</summary>
        public int checkIntervalTicks = 2500;

        /// <summary>每世界物件職官上限（G6 數量控管）。</summary>
        public int maxOfficersPerObject = 4;

        /// <summary>B 軌 opinion 每心跳向 bias 回歸步長。</summary>
        public int opinionDriftPerHeartbeat = 1;

        /// <summary>record 建立時七維屬性擲定範圍。</summary>
        public IntRange initialAttributeRange = new IntRange(20, 80);

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (checkIntervalTicks <= 0)
            {
                yield return "checkIntervalTicks must be > 0";
            }
            if (maxOfficersPerObject < 1)
            {
                yield return "maxOfficersPerObject must be >= 1";
            }
        }
    }
}
