using RimWar.Planet;
using Verse;

namespace pas.officers.warband
{
    /// <summary>Postfix WarObject.GetInspectString（RW:14860）。原 override 自建字串、不附
    /// comp inspect extras（T0 核對）→ P0 view comp 注入無效，必須 postfix 附將領行。
    /// Warband 無自身 override → patch 基類即覆蓋。</summary>
    public static class Patch_WarObjectInspectString
    {
        public static void Postfix(WarObject __instance, ref string __result)
        {
            try
            {
                OfficerRecord record = WorldComponent_WarbandGenerals.Get()?.GeneralOf(__instance);
                if (record == null || record.dead)
                {
                    return;
                }
                string line = "pas_warband_InspectGeneral".Translate(
                    record.DisplayName, record.might, record.command);
                __result = string.IsNullOrEmpty(__result) ? line : __result + "\n" + line;
            }
            catch (System.Exception e)
            {
                GeneralsUtility.WarnOnce("inspect", "inspect postfix 異常，省略將領行：" + e);
            }
        }
    }
}
