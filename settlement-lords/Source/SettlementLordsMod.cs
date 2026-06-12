using UnityEngine;
using Verse;

namespace pas.officers.settlements
{
    public class SettlementLordsSettings : ModSettings
    {
        /// <summary>每心跳（2500 tick）無主 NPC 聚落獲派領主的機率（＝上任速度）。
        /// 0＝停用新指派（既有領主照常運作）。</summary>
        public float lordChance = 0.25f;

        /// <summary>治理係數擺幅：屬性 100 → x(1+X)、屬性 0 → x(1-X)（X＝本值）。
        /// 0＝完全停用治理影響（仍指派領主、仍顯示）。</summary>
        public float govAmplitude = 0.5f;

        /// <summary>治理係數 &lt;1 時是否倒扣點數（淨衰退，停在 Rim War 地板 100）。</summary>
        public bool decayEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lordChance, "lordChance", 0.25f);
            Scribe_Values.Look(ref govAmplitude, "govAmplitude", 0.5f);
            Scribe_Values.Look(ref decayEnabled, "decayEnabled", true);
        }
    }

    public class SettlementLordsMod : Mod
    {
        public static SettlementLordsSettings Settings { get; private set; }

        public SettlementLordsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<SettlementLordsSettings>();
        }

        public override string SettingsCategory()
        {
            return "pas_settlement_ModName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("pas_settlement_LordChance".Translate(
                (Settings.lordChance * 100f).ToString("0")), -1f,
                "pas_settlement_LordChanceTip".Translate());
            Settings.lordChance = Mathf.Round(listing.Slider(Settings.lordChance, 0f, 1f) * 20f) / 20f;

            listing.Label("pas_settlement_GovAmplitude".Translate(
                (Settings.govAmplitude * 100f).ToString("0")), -1f,
                "pas_settlement_GovAmplitudeTip".Translate());
            Settings.govAmplitude = Mathf.Round(listing.Slider(Settings.govAmplitude, 0f, 1f) * 20f) / 20f;

            listing.CheckboxLabeled("pas_settlement_DecayEnabled".Translate(),
                ref Settings.decayEnabled, "pas_settlement_DecayEnabledTip".Translate());

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
