using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.politics
{
    /// <summary>分裂編排（可行性 02 全序）：hidden 生成同 def 新派系 → 入列 → 反叛者升首領
    /// → 倒戈（駐地必含、排除衛星、母保底留 1）→ 揭示 → goodwill 敵對 → letter。</summary>
    public static class FactionSplitter
    {
        /// <summary>執行分裂；母派系聚落不足（&lt;2）時放棄並回 null（進展保留，下輪再試）。</summary>
        public static Faction Split(RebelRecord record)
        {
            Faction mother = record.faction;
            List<Settlement> owned = OwnedNonSatellite(mother);
            if (owned.Count < 2 || record.rebel == null)
            {
                return null;
            }
            RebellionProfileDef profile = RebellionProfileResolver.Resolve(mother);
            if (profile == null)
            {
                return null;
            }
            FactionGeneratorParms parms = new FactionGeneratorParms(mother.def, default(IdeoGenerationParms), true);
            Faction newFaction = FactionGenerator.NewGeneratedFaction(Find.WorldGrid.Surface, parms);
            Find.FactionManager.Add(newFaction);
            newFaction.leader = record.rebel;
            record.rebel.SetFaction(newFaction);
            List<Settlement> defected = Transfer(owned, record.homeSettlement, mother, newFaction, profile);
            newFaction.hidden = false;
            newFaction.TryAffectGoodwillWith(mother, newFaction.GoodwillToMakeHostile(mother),
                canSendMessage: false, canSendHostilityLetter: false);
            PoliticsBridges.NotifyFactionSplit(mother, newFaction);
            Find.LetterStack.ReceiveLetter("pas_politics_SplitLetterLabel".Translate(newFaction.Name),
                "pas_politics_SplitLetterText".Translate(record.rebel.LabelShortCap, mother.Name,
                    newFaction.Name, defected.Count),
                LetterDefOf.NegativeEvent, defected[0]);
            return newFaction;
        }

        private static List<Settlement> OwnedNonSatellite(Faction faction)
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            List<Settlement> owned = new List<Settlement>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Faction == faction && !PoliticsBridges.IsSatellite(all[i]))
                {
                    owned.Add(all[i]);
                }
            }
            return owned;
        }

        /// <summary>反叛者駐地必倒戈，其餘隨機補足至比例；母派系保底留 1。</summary>
        private static List<Settlement> Transfer(List<Settlement> owned, Settlement home,
            Faction mother, Faction newFaction, RebellionProfileDef profile)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(owned.Count * profile.defectFraction.RandomInRange),
                1, owned.Count - 1);
            List<Settlement> defectors = new List<Settlement>();
            if (home != null && owned.Contains(home))
            {
                defectors.Add(home);
            }
            foreach (Settlement settlement in owned.InRandomOrder())
            {
                if (defectors.Count >= count)
                {
                    break;
                }
                if (!defectors.Contains(settlement))
                {
                    defectors.Add(settlement);
                }
            }
            foreach (Settlement settlement in defectors)
            {
                settlement.SetFaction(newFaction);
                PoliticsBridges.NotifySettlementDefected(settlement, mother, newFaction);
            }
            return defectors;
        }
    }
}
