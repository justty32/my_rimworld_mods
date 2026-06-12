using System;
using RimWorld.Planet;
using Verse;

namespace pas.officers.settlements
{
    /// <summary>P4（領主帶城叛變）消費窗口。易主時於 RemoveOfficer **之前**廣播——
    /// 訂閱者要保留領主就在事件裡自行接走（再指派/複製 record）。</summary>
    public static class LordEvents
    {
        /// <summary>城易主（host.Faction != record.faction），預設政策＝廣播後領主退場。
        /// 參數：領主 record（尚未移除）、易主後的聚落。</summary>
        public static event Action<OfficerRecord, Settlement> LordLostSettlement;

        internal static void RaiseLordLostSettlement(OfficerRecord record, Settlement settlement)
        {
            Action<OfficerRecord, Settlement> handlers = LordLostSettlement;
            if (handlers == null)
            {
                return;
            }
            foreach (Delegate d in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<OfficerRecord, Settlement>)d)(record, settlement);
                }
                catch (Exception e)
                {
                    Log.Warning("[SettlementLords] LordLostSettlement 訂閱者例外（已隔離）：" + e);
                }
            }
        }
    }
}
