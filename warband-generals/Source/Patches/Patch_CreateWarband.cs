using RimWar;
using RimWar.Planet;
using RimWorld;
using Verse;

namespace pas.officers.warband
{
    /// <summary>Postfix WorldUtility.CreateWarband（RW:15467，含 CreateWarObjectOfType 轉呼路徑）。
    /// 傳承優先（戰後重生 warband 接回原將領），否則按機率 roll 新將領。
    /// 鐵律：只掛將領、不碰任何派系級係數。</summary>
    public static class Patch_CreateWarband
    {
        public static void Postfix(Warband __result, RimWarData rwd)
        {
            try
            {
                if (__result == null || __result.Destroyed)
                {
                    return;   // 工廠 NRE 回 null / _launched 即時抵達已銷毀
                }
                WorldComponent_WarbandGenerals comp = WorldComponent_WarbandGenerals.Get();
                if (comp == null)
                {
                    return;
                }
                if (TransferContext.TryConsume(out OfficerRecord transferred))
                {
                    OfficersApi.AssignOfficer(transferred, __result);   // 重建 P0 指派/索引
                    comp.Bind(__result, transferred);                   // 權威綁定搬家
                    return;
                }
                if (rwd == null || rwd.behavior == RimWarBehavior.Player
                    || rwd.behavior == RimWarBehavior.Excluded)
                {
                    return;
                }
                Faction faction = __result.Faction;
                if (faction == null || faction.IsPlayer)
                {
                    return;
                }
                float chance = WarbandGeneralsMod.Settings?.generalChance ?? 0f;
                if (chance <= 0f || !Rand.Chance(chance))
                {
                    return;
                }
                OfficerRoleDef role = GeneralsUtility.GeneralRole;
                if (role == null)
                {
                    GeneralsUtility.WarnOnce("noRole",
                        "找不到角色 def pas_warband_General，將領生成停用。");
                    return;
                }
                OfficerRecord created = OfficersApi.CreateOfficer(faction, __result, role);
                if (created != null)   // null＝G6 上限滿/參數壞 → 安靜跳過
                {
                    comp.Bind(__result, created);
                }
            }
            catch (System.Exception e)
            {
                GeneralsUtility.WarnOnce("createWarband", "將領生成 postfix 異常，本次跳過：" + e);
            }
        }
    }
}
