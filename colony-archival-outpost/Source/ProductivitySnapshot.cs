using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyArchivalOutpost
{
    // 採樣結果：資源每日有號淨流（N1–N5）+ 技能基礎 XP 速率（N7）。封存當下算好、隨 Outpost_Sampled 存檔。
    public class ProductivitySnapshot : IExposable
    {
        public Dictionary<ThingDef, float> dailyRates = new Dictionary<ThingDef, float>();

        // N4：per-pawn 縮放
        public bool perPawnScaling;
        public int basePawnCount = 1;

        // N7：技能採樣
        public Dictionary<SkillDef, float> dailySkillXP = new Dictionary<SkillDef, float>();
        public bool applySkillXP;

        // N6：傷勢採樣——每日平均可癒傷勢 severity 變化量（負=淨治癒，正=淨惡化）
        public float avgHealthDeltaPerDay; // 負值=有益（每天平均治癒 X severity/pawn）
        public bool applyHealthDelta;
        public bool applyHealthDeterioration; // 正值惡化套用開關

        // N6b：非傷勢 hediff severity 每日變化率（正=加重/新增，負=消退）
        public Dictionary<HediffDef, float> dailyHediffDeltas = new Dictionary<HediffDef, float>();
        public bool applyHediffDeltas;

        // N5：電力採樣——封存窗平均淨功率（有號，瓦；正=發電、負=耗電）
        public float avgNetPowerW;
        public bool applyPowerSampling;

        public ProductivitySnapshot() { }

        public ProductivitySnapshot(Dictionary<ThingDef, float> rates)
        {
            dailyRates = rates ?? new Dictionary<ThingDef, float>();
        }

        public bool IsEmpty => (dailyRates == null || dailyRates.Count == 0)
                            && (dailySkillXP == null || dailySkillXP.Count == 0)
                            && (dailyHediffDeltas == null || dailyHediffDeltas.Count == 0)
                            && avgHealthDeltaPerDay == 0f;

        // E1：深複製。新建各字典、複製值；def key 為共享單例直接帶引用。
        // 註冊與建造時各複製一次，杜絕共享可變字典。
        public ProductivitySnapshot Clone()
        {
            var c = new ProductivitySnapshot
            {
                perPawnScaling = perPawnScaling,
                basePawnCount = basePawnCount,
                applySkillXP = applySkillXP,
                avgHealthDeltaPerDay = avgHealthDeltaPerDay,
                applyHealthDelta = applyHealthDelta,
                applyHealthDeterioration = applyHealthDeterioration,
                applyHediffDeltas = applyHediffDeltas,
                avgNetPowerW = avgNetPowerW,
                applyPowerSampling = applyPowerSampling
            };
            c.dailyRates = dailyRates == null
                ? new Dictionary<ThingDef, float>()
                : new Dictionary<ThingDef, float>(dailyRates);
            c.dailySkillXP = dailySkillXP == null
                ? new Dictionary<SkillDef, float>()
                : new Dictionary<SkillDef, float>(dailySkillXP);
            c.dailyHediffDeltas = dailyHediffDeltas == null
                ? new Dictionary<HediffDef, float>()
                : new Dictionary<HediffDef, float>(dailyHediffDeltas);
            return c;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref dailyRates, "dailyRates", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref perPawnScaling, "perPawnScaling", false);
            Scribe_Values.Look(ref basePawnCount, "basePawnCount", 1);
            Scribe_Collections.Look(ref dailySkillXP, "dailySkillXP", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref applySkillXP, "applySkillXP", false);
            Scribe_Values.Look(ref avgHealthDeltaPerDay, "avgHealthDeltaPerDay", 0f);
            Scribe_Values.Look(ref applyHealthDelta, "applyHealthDelta", false);
            Scribe_Values.Look(ref applyHealthDeterioration, "applyHealthDeterioration", false);
            Scribe_Collections.Look(ref dailyHediffDeltas, "dailyHediffDeltas", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref applyHediffDeltas, "applyHediffDeltas", false);
            Scribe_Values.Look(ref avgNetPowerW, "avgNetPowerW", 0f);
            Scribe_Values.Look(ref applyPowerSampling, "applyPowerSampling", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (dailyRates == null) dailyRates = new Dictionary<ThingDef, float>();
                if (dailySkillXP == null) dailySkillXP = new Dictionary<SkillDef, float>();
                if (dailyHediffDeltas == null) dailyHediffDeltas = new Dictionary<HediffDef, float>();
                // 移除提供該 def 的內容 mod 後，Def key 解析為 null → 否則後續用字典時取 null key 出錯。
                dailyRates.RemoveAll(kv => kv.Key == null);
                dailySkillXP.RemoveAll(kv => kv.Key == null);
                dailyHediffDeltas.RemoveAll(kv => kv.Key == null);
            }
        }
    }
}
