using RimWorld.Planet;
using Verse;

namespace pas.officers.settlements
{
    /// <summary>Postfix Settlement.GetInspectString。RimWar 自己也 postfix 此方法附點數行
    /// （RW:5977→6570）、Mod 1 未碰 → 多 postfix 疊加安全，各 append 自己段。</summary>
    public static class Patch_SettlementInspectString
    {
        public static void Postfix(Settlement __instance, ref string __result)
        {
            try
            {
                OfficerRecord record = WorldComponent_SettlementLords.Get()?.LordOf(__instance);
                if (record == null || record.dead)
                {
                    return;
                }
                string line = "pas_settlement_InspectLord".Translate(
                    record.DisplayName, record.polity, record.loyalty,
                    LordsUtility.GovernanceFactor(record).ToString("0.00"));
                __result = string.IsNullOrEmpty(__result) ? line : __result + "\n" + line;
            }
            catch (System.Exception e)
            {
                LordsUtility.WarnOnce("inspect", "inspect postfix 異常，省略領主行：" + e);
            }
        }
    }
}
