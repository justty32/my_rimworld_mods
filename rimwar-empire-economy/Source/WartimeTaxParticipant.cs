using System;
using System.Collections.Generic;
using FactionColonies;
using UnityEngine;
using Verse;

namespace pas.empire.wartimeeconomy
{
    /// <summary>
    /// 戰時加稅（Empire 端零 Harmony，走 TaxTickRegistry 的 ITaxTickParticipant）。
    /// 每座聚落造稅完成後（WorldSettlementFC.cs:1917 的 PostSettlementCreateTax），
    /// 若該聚落所屬派系在 RimWar 處於戰爭狀態，就按設定比例上調白銀稅額（ref 改值）。
    /// </summary>
    public class WartimeTaxParticipant : ITaxTickParticipant
    {
        public void PreTaxResolution(FactionFC faction) { }
        public void PostTaxResolution(FactionFC faction) { }
        public void PreSettlementCreateTax(WorldSettlementFC settlement) { }

        public void PostSettlementCreateTax(WorldSettlementFC settlement, ref int silverAmount, List<Thing> titheThings)
        {
            try
            {
                WartimeEconomySettings s = WartimeEconomyMod.Settings;
                if (!s.enableWartimeTax || silverAmount <= 0) return;
                if (!RimWarSignals.FactionAtWar(settlement)) return;

                int surcharge = Mathf.RoundToInt(silverAmount * s.wartimeTaxSurcharge);
                silverAmount += Mathf.Max(0, surcharge);
            }
            catch (Exception e)
            {
                LogUtil.Warning("[WartimeEconomy] wartime tax surcharge failed: " + e);
            }
        }
    }
}
