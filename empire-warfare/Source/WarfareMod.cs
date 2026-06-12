using UnityEngine;
using Verse;

namespace pas.empire.warfare
{
    /// <summary>淪陷判定與節奏的全部設定。</summary>
    public class WarfareSettings : ModSettings
    {
        public bool enableVassalFall = true;
        public bool onlyRimWarAttacks = true;
        public float crushingAttackerRemainRatio = 0.5f;
        public int consecutiveFailuresForFall = 0; // 0 = 停用
        public int protectionDays = 15;
        public int vassalHeatDecayPerDay = 0; // 0 = RimWar 原生節奏

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableVassalFall, "enableVassalFall", true);
            Scribe_Values.Look(ref onlyRimWarAttacks, "onlyRimWarAttacks", true);
            Scribe_Values.Look(ref crushingAttackerRemainRatio, "crushingAttackerRemainRatio", 0.5f);
            Scribe_Values.Look(ref consecutiveFailuresForFall, "consecutiveFailuresForFall", 0);
            Scribe_Values.Look(ref protectionDays, "protectionDays", 15);
            Scribe_Values.Look(ref vassalHeatDecayPerDay, "vassalHeatDecayPerDay", 0);
        }
    }

    public class WarfareMod : Mod
    {
        private static WarfareSettings settings;

        /// <summary>未初始化時回傳預設值實例，所有讀取端免 null 判斷。</summary>
        public static WarfareSettings Settings => settings ?? (settings = new WarfareSettings());

        public WarfareMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<WarfareSettings>();
        }

        public override string SettingsCategory() => "pas_warfare_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            WarfareSettings s = Settings;
            Listing_Standard l = new Listing_Standard();
            l.Begin(inRect);

            l.CheckboxLabeled("pas_warfare_SettingEnableFall".Translate(), ref s.enableVassalFall,
                "pas_warfare_SettingEnableFallTip".Translate());
            l.CheckboxLabeled("pas_warfare_SettingOnlyRimWar".Translate(), ref s.onlyRimWarAttacks,
                "pas_warfare_SettingOnlyRimWarTip".Translate());
            l.Gap();

            l.Label("pas_warfare_SettingCrushRatio".Translate(s.crushingAttackerRemainRatio.ToStringPercent()),
                -1f, "pas_warfare_SettingCrushRatioTip".Translate());
            s.crushingAttackerRemainRatio = l.Slider(s.crushingAttackerRemainRatio, 0.1f, 1f);

            string streakLabel = s.consecutiveFailuresForFall == 0
                ? "pas_warfare_Disabled".Translate().ToString()
                : s.consecutiveFailuresForFall.ToString();
            l.Label("pas_warfare_SettingFailStreak".Translate(streakLabel),
                -1f, "pas_warfare_SettingFailStreakTip".Translate());
            s.consecutiveFailuresForFall = (int)l.Slider(s.consecutiveFailuresForFall, 0f, 10f);

            l.Label("pas_warfare_SettingProtectionDays".Translate(s.protectionDays),
                -1f, "pas_warfare_SettingProtectionDaysTip".Translate());
            s.protectionDays = (int)l.Slider(s.protectionDays, 0f, 60f);

            l.Gap();
            l.Label("pas_warfare_SettingHeatDecay".Translate(s.vassalHeatDecayPerDay),
                -1f, "pas_warfare_SettingHeatDecayTip".Translate());
            s.vassalHeatDecayPerDay = (int)l.Slider(s.vassalHeatDecayPerDay, 0f, 50f);

            l.End();
        }
    }
}
