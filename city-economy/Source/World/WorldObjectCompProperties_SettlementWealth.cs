using RimWorld;

namespace pas.sanguo.cityeconomy
{
    /// <summary>XML 注入用 props（Patches/CityEconomyComps.xml，鏡像 RimWar 官方注入手法）。</summary>
    public class WorldObjectCompProperties_SettlementWealth : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SettlementWealth()
        {
            compClass = typeof(SettlementWealthComp);
        }
    }

    /// <summary>交易回寫的財富類別（OffsetWealth 用）。</summary>
    public enum WealthKind
    {
        Silver,
        Food,
        Goods,
    }
}
