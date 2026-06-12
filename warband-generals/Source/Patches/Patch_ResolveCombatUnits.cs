using RimWar.Planet;
using UnityEngine;

namespace pas.officers.warband
{
    /// <summary>Prefix+Postfix IncidentUtility.ResolveCombat_Units（RW:11271）。
    /// 原方法唯一副作用＝雙方 PointDamage 增量（RW:11331-11332）；prefix 快照、postfix 只對
    /// 本輪 delta 乘將領比值——**絕不讀寫派系級 combat 係數**（01-architecture 鐵律），
    /// 自家將領強 → 對方多掉血、自家少掉血，比值 clamp 0.5~2。</summary>
    public static class Patch_ResolveCombatUnits
    {
        public struct DamageSnapshot
        {
            public int atk;
            public int def;
        }

        public static void Prefix(WarObject attacker, WarObject defender, ref DamageSnapshot __state)
        {
            __state.atk = attacker?.PointDamage ?? 0;
            __state.def = defender?.PointDamage ?? 0;
        }

        public static void Postfix(WarObject attacker, WarObject defender, DamageSnapshot __state)
        {
            try
            {
                if (attacker == null || defender == null)
                {
                    return;
                }
                WorldComponent_WarbandGenerals comp = WorldComponent_WarbandGenerals.Get();
                if (comp == null)
                {
                    return;
                }
                OfficerRecord generalAtk = AliveGeneralOf(comp, attacker);
                OfficerRecord generalDef = AliveGeneralOf(comp, defender);
                if (generalAtk == null && generalDef == null)
                {
                    return;   // 零開銷路徑：無將之戰不動原版結果
                }
                float bonusAtk = GeneralsUtility.CombatBonus(generalAtk)
                    * GeneralsUtility.SafeRelationFactor(generalAtk, generalDef);
                float bonusDef = GeneralsUtility.CombatBonus(generalDef)
                    * GeneralsUtility.SafeRelationFactor(generalDef, generalAtk);
                float ratio = Mathf.Clamp(bonusAtk / bonusDef, 0.5f, 2f);
                if (Mathf.Abs(ratio - 1f) < 0.001f)
                {
                    return;
                }
                int defDelta = defender.PointDamage - __state.def;
                if (defDelta > 0)
                {
                    defender.PointDamage = __state.def + Mathf.RoundToInt(defDelta * ratio);
                }
                int atkDelta = attacker.PointDamage - __state.atk;
                if (atkDelta > 0)
                {
                    attacker.PointDamage = __state.atk
                        + Mathf.Max(0, Mathf.RoundToInt(atkDelta / ratio));
                }
            }
            catch (System.Exception e)
            {
                GeneralsUtility.WarnOnce("combatPostfix", "戰力加成 postfix 異常，本輪保留原版結果：" + e);
            }
        }

        private static OfficerRecord AliveGeneralOf(WorldComponent_WarbandGenerals comp, WarObject unit)
        {
            OfficerRecord record = comp.GeneralOf(unit);
            return (record == null || record.dead) ? null : record;
        }
    }
}
