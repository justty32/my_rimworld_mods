using UnityEngine;
using Verse;

namespace pas.sanguo.cityeconomy
{
    public class CityEconomySettings : ModSettings
    {
        /// <summary>財富/城防每輪成長率乘數。0＝停用成長（存量/劫掠/守城照常運作）。</summary>
        public float growthRate = 1f;

        /// <summary>城破被劫時 silver/food/goods 損失比例。0＝停用真實資源劫掠
        /// （RimWar 原點數搬移不受影響）。</summary>
        public float sackLossRatio = 0.45f;

        /// <summary>守城加成幅度：DefenseBonus = 有效城防點數 × 本值。0＝停用守城折算。</summary>
        public float defenseAmplitude = 1f;

        /// <summary>貨架縮放＋交易回寫總開關。關＝回復原版貿易行為。</summary>
        public bool traderEconomyEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref growthRate, "growthRate", 1f);
            Scribe_Values.Look(ref sackLossRatio, "sackLossRatio", 0.45f);
            Scribe_Values.Look(ref defenseAmplitude, "defenseAmplitude", 1f);
            Scribe_Values.Look(ref traderEconomyEnabled, "traderEconomyEnabled", true);
        }
    }

    public class CityEconomyMod : Mod
    {
        public static CityEconomySettings Settings { get; private set; }

        public CityEconomyMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CityEconomySettings>();
        }

        public override string SettingsCategory()
        {
            return "pas_cityecon_ModName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("pas_cityecon_GrowthRate".Translate(
                Settings.growthRate.ToString("0.00")), -1f,
                "pas_cityecon_GrowthRateTip".Translate());
            Settings.growthRate = Mathf.Round(listing.Slider(Settings.growthRate, 0f, 3f) * 20f) / 20f;

            listing.Label("pas_cityecon_SackLossRatio".Translate(
                (Settings.sackLossRatio * 100f).ToString("0")), -1f,
                "pas_cityecon_SackLossRatioTip".Translate());
            Settings.sackLossRatio = Mathf.Round(listing.Slider(Settings.sackLossRatio, 0f, 1f) * 20f) / 20f;

            listing.Label("pas_cityecon_DefenseAmplitude".Translate(
                Settings.defenseAmplitude.ToString("0.00")), -1f,
                "pas_cityecon_DefenseAmplitudeTip".Translate());
            Settings.defenseAmplitude = Mathf.Round(listing.Slider(Settings.defenseAmplitude, 0f, 2f) * 20f) / 20f;

            listing.CheckboxLabeled("pas_cityecon_TraderEconomy".Translate(),
                ref Settings.traderEconomyEnabled, "pas_cityecon_TraderEconomyTip".Translate());

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
