using RimWorld.Planet;
using Verse;

namespace pas.sanguo.cityeconomy
{
    /// <summary>Postfix Settlement.GetInspectString。RimWar 自身（RW:5977→6570）與 P2
    /// settlement-lords 也 postfix 此方法 → 多 postfix 疊加安全，各 append 自己段（H 定案：
    /// 獨立 postfix、勿改 RimWar 的）。未播種（玩家城/非 RimWar 城）不顯示。</summary>
    public static class Patch_SettlementInspectString
    {
        public static void Postfix(Settlement __instance, ref string __result)
        {
            try
            {
                SettlementWealthComp comp = __instance?.GetComponent<SettlementWealthComp>();
                if (comp == null || !comp.initialized)
                {
                    return;
                }
                string line = "pas_cityecon_InspectLine".Translate(
                    comp.silver, comp.food, comp.goods, comp.defenseLevel, comp.DefenseBonus);
                __result = string.IsNullOrEmpty(__result) ? line : __result + "\n" + line;
            }
            catch (System.Exception e)
            {
                EconomyUtility.WarnOnce("inspect", "inspect postfix 異常，省略財富行：" + e);
            }
        }
    }
}
