using UnityEngine;
using Verse;

namespace pas.empire.outposts.war
{
    public class OutpostsWarSettings : ModSettings
    {
        /// <summary>功能 1：附庸是否長衛星哨站。</summary>
        public bool vassalOutpostsEnabled = true;

        /// <summary>功能 1：每存活哨站每稅期給附庸的額外白銀產出。0＝停用產出加成。</summary>
        public int perOutpostSilver = 10;

        /// <summary>功能 2：每存活哨站給防守方 MilitaryForce.militaryLevel 的加值。0＝停用防守加成。</summary>
        public float defenseLevelPerOutpost = 0.35f;

        /// <summary>功能 2：哨站加成的數量上限（避免極端堆疊）。</summary>
        public int maxOutpostsCounted = 8;

        /// <summary>功能 3：哨站隨聚落易主（雙向）。</summary>
        public bool transferOnConquest = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref vassalOutpostsEnabled, "vassalOutpostsEnabled", true);
            Scribe_Values.Look(ref perOutpostSilver, "perOutpostSilver", 10);
            Scribe_Values.Look(ref defenseLevelPerOutpost, "defenseLevelPerOutpost", 0.35f);
            Scribe_Values.Look(ref maxOutpostsCounted, "maxOutpostsCounted", 8);
            Scribe_Values.Look(ref transferOnConquest, "transferOnConquest", true);
        }
    }

    public class OutpostsWarMod : Mod
    {
        public static OutpostsWarSettings Settings { get; private set; }

        public OutpostsWarMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<OutpostsWarSettings>();
        }

        public override string SettingsCategory()
        {
            return "pas_empire_war_ModName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("pas_empire_war_VassalOutposts".Translate(), ref Settings.vassalOutpostsEnabled,
                "pas_empire_war_VassalOutpostsTip".Translate());
            if (Settings.vassalOutpostsEnabled)
            {
                listing.Label("pas_empire_war_PerOutpostSilver".Translate(Settings.perOutpostSilver));
                Settings.perOutpostSilver = Mathf.RoundToInt(listing.Slider(Settings.perOutpostSilver, 0f, 100f) / 5f) * 5;
            }
            listing.GapLine();

            listing.Label("pas_empire_war_DefensePerOutpost".Translate(Settings.defenseLevelPerOutpost.ToString("0.00")));
            Settings.defenseLevelPerOutpost = Mathf.Round(listing.Slider(Settings.defenseLevelPerOutpost, 0f, 2f) * 100f) / 100f;
            listing.Label("pas_empire_war_MaxCounted".Translate(Settings.maxOutpostsCounted));
            Settings.maxOutpostsCounted = Mathf.RoundToInt(listing.Slider(Settings.maxOutpostsCounted, 1f, 20f));
            listing.GapLine();

            listing.CheckboxLabeled("pas_empire_war_TransferOnConquest".Translate(), ref Settings.transferOnConquest,
                "pas_empire_war_TransferOnConquestTip".Translate());

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
