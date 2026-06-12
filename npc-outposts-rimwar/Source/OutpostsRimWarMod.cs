using UnityEngine;
using Verse;

namespace pas.outposts.rimwar
{
    public class OutpostsRimWarSettings : ModSettings
    {
        /// <summary>每哨站每成長週期貢獻母聚落的點數（×類型係數）。0＝停用功能 1。</summary>
        public float pointsPerOutpost = 4f;

        /// <summary>哨站初始 RimWarPoints（×類型係數）。刻意低於聚落。</summary>
        public int initialOutpostPoints = 400;

        /// <summary>true＝被佔哨站易主給攻方；false＝直接摧毀。</summary>
        public bool captureToConqueror = true;

        /// <summary>戰局動態增減總開關。</summary>
        public bool warMomentumEnabled = true;

        /// <summary>勝方增生速率最大倍率（敗方取倒數）。</summary>
        public float momentumMaxMultiplier = 1.5f;

        /// <summary>連敗派系哨站萎縮。</summary>
        public bool shrinkEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pointsPerOutpost, "pointsPerOutpost", 4f);
            Scribe_Values.Look(ref initialOutpostPoints, "initialOutpostPoints", 400);
            Scribe_Values.Look(ref captureToConqueror, "captureToConqueror", defaultValue: true);
            Scribe_Values.Look(ref warMomentumEnabled, "warMomentumEnabled", defaultValue: true);
            Scribe_Values.Look(ref momentumMaxMultiplier, "momentumMaxMultiplier", 1.5f);
            Scribe_Values.Look(ref shrinkEnabled, "shrinkEnabled", defaultValue: true);
        }
    }

    public class OutpostsRimWarMod : Mod
    {
        public static OutpostsRimWarSettings Settings { get; private set; }

        public OutpostsRimWarMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<OutpostsRimWarSettings>();
        }

        public override string SettingsCategory()
        {
            return "pas_outposts_rimwar_ModName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("pas_outposts_rimwar_PointsPerOutpost".Translate(Settings.pointsPerOutpost.ToString("0.#")));
            Settings.pointsPerOutpost = Mathf.Round(listing.Slider(Settings.pointsPerOutpost, 0f, 20f) * 2f) / 2f;

            listing.Label("pas_outposts_rimwar_InitialPoints".Translate(Settings.initialOutpostPoints));
            Settings.initialOutpostPoints = Mathf.RoundToInt(listing.Slider(Settings.initialOutpostPoints, 100f, 2000f) / 50f) * 50;

            listing.CheckboxLabeled("pas_outposts_rimwar_CaptureToConqueror".Translate(), ref Settings.captureToConqueror,
                "pas_outposts_rimwar_CaptureToConquerorTip".Translate());
            listing.GapLine();

            listing.CheckboxLabeled("pas_outposts_rimwar_MomentumEnabled".Translate(), ref Settings.warMomentumEnabled,
                "pas_outposts_rimwar_MomentumEnabledTip".Translate());
            if (Settings.warMomentumEnabled)
            {
                listing.Label("pas_outposts_rimwar_MomentumMaxMult".Translate(Settings.momentumMaxMultiplier.ToString("0.0")));
                Settings.momentumMaxMultiplier = Mathf.Round(listing.Slider(Settings.momentumMaxMultiplier, 1f, 3f) * 10f) / 10f;
                listing.CheckboxLabeled("pas_outposts_rimwar_ShrinkEnabled".Translate(), ref Settings.shrinkEnabled,
                    "pas_outposts_rimwar_ShrinkEnabledTip".Translate());
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
