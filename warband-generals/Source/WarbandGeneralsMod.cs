using UnityEngine;
using Verse;

namespace pas.officers.warband
{
    public class WarbandGeneralsSettings : ModSettings
    {
        /// <summary>新生成 NPC warband 掛具名將領的機率。0＝停用生成（既有將領照常運作）。</summary>
        public float generalChance = 0.5f;

        /// <summary>將領屬性對會戰傷害的擺幅上限：屬性 100 → +X、屬性 0 → −X（X＝本值）。
        /// 0＝停用戰力加成（仍生將領、仍顯示）。</summary>
        public float bonusMax = 0.3f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref generalChance, "generalChance", 0.5f);
            Scribe_Values.Look(ref bonusMax, "bonusMax", 0.3f);
        }
    }

    public class WarbandGeneralsMod : Mod
    {
        public static WarbandGeneralsSettings Settings { get; private set; }

        public WarbandGeneralsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WarbandGeneralsSettings>();
        }

        public override string SettingsCategory()
        {
            return "pas_warband_ModName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("pas_warband_GeneralChance".Translate(
                (Settings.generalChance * 100f).ToString("0")), -1f,
                "pas_warband_GeneralChanceTip".Translate());
            Settings.generalChance = Mathf.Round(listing.Slider(Settings.generalChance, 0f, 1f) * 20f) / 20f;

            listing.Label("pas_warband_BonusMax".Translate(
                (Settings.bonusMax * 100f).ToString("0")), -1f,
                "pas_warband_BonusMaxTip".Translate());
            Settings.bonusMax = Mathf.Round(listing.Slider(Settings.bonusMax, 0f, 1f) * 20f) / 20f;

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
