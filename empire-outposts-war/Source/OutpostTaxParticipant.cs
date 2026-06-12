using System;
using System.Collections.Generic;
using FactionColonies;
using UnityEngine;
using Verse;

namespace pas.empire.outposts.war
{
    /// <summary>功能 1 產出面：每附庸按同主存活哨站數加白銀稅收。
    /// 走 Empire 契約層 TaxTickRegistry（PostSettlementCreateTax 可 ref silverAmount）。</summary>
    public class OutpostTaxParticipant : ITaxTickParticipant
    {
        public void PreTaxResolution(FactionFC faction) { }
        public void PostTaxResolution(FactionFC faction) { }
        public void PreSettlementCreateTax(WorldSettlementFC settlement) { }

        public void PostSettlementCreateTax(WorldSettlementFC settlement, ref int silverAmount, List<Thing> titheThings)
        {
            try
            {
                OutpostsWarSettings s = OutpostsWarMod.Settings;
                if (s == null || !s.vassalOutpostsEnabled || s.perOutpostSilver <= 0 || settlement == null)
                {
                    return;
                }
                int outposts = OutpostWarUtility.CountSatellites(settlement);
                if (outposts <= 0)
                {
                    return;
                }
                outposts = Mathf.Min(outposts, Mathf.Max(1, s.maxOutpostsCounted));
                silverAmount += outposts * s.perOutpostSilver;
            }
            catch (Exception e)
            {
                OutpostWarUtility.WarnOnce("taxPart", "哨站產出加成異常，本次跳過：" + e);
            }
        }
    }
}
