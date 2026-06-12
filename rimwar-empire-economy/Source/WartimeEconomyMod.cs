using UnityEngine;
using Verse;

namespace pas.empire.wartimeeconomy
{
    /// <summary>戰時加稅／圍困減產的全部設定。</summary>
    public class WartimeEconomySettings : ModSettings
    {
        public bool enableSiegeProductionCut = true;
        public bool enableWartimeTax = true;
        // 圍困時保留的產量比例（0.4 = 被圍困聚落只生產 40%）。
        public float siegeProductionMultiplier = 0.4f;
        // 戰時每座聚落稅收加成（0.25 = +25% 白銀）。
        public float wartimeTaxSurcharge = 0.25f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableSiegeProductionCut, "enableSiegeProductionCut", true);
            Scribe_Values.Look(ref enableWartimeTax, "enableWartimeTax", true);
            Scribe_Values.Look(ref siegeProductionMultiplier, "siegeProductionMultiplier", 0.4f);
            Scribe_Values.Look(ref wartimeTaxSurcharge, "wartimeTaxSurcharge", 0.25f);
        }
    }

    public class WartimeEconomyMod : Mod
    {
        private static WartimeEconomySettings settings;

        /// <summary>未初始化時回傳預設值實例，所有讀取端免 null 判斷。</summary>
        public static WartimeEconomySettings Settings => settings ?? (settings = new WartimeEconomySettings());

        public WartimeEconomyMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<WartimeEconomySettings>();
        }

        public override string SettingsCategory() => "pas_wte_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            WartimeEconomySettings s = Settings;
            Listing_Standard l = new Listing_Standard();
            l.Begin(inRect);

            l.CheckboxLabeled("pas_wte_SettingEnableSiege".Translate(), ref s.enableSiegeProductionCut,
                "pas_wte_SettingEnableSiegeTip".Translate());
            l.Label("pas_wte_SettingSiegeMult".Translate(s.siegeProductionMultiplier.ToStringPercent()),
                -1f, "pas_wte_SettingSiegeMultTip".Translate());
            s.siegeProductionMultiplier = l.Slider(s.siegeProductionMultiplier, 0f, 1f);

            l.Gap();

            l.CheckboxLabeled("pas_wte_SettingEnableTax".Translate(), ref s.enableWartimeTax,
                "pas_wte_SettingEnableTaxTip".Translate());
            l.Label("pas_wte_SettingTaxSurcharge".Translate(s.wartimeTaxSurcharge.ToStringPercent()),
                -1f, "pas_wte_SettingTaxSurchargeTip".Translate());
            s.wartimeTaxSurcharge = l.Slider(s.wartimeTaxSurcharge, 0f, 2f);

            l.End();
        }
    }
}
