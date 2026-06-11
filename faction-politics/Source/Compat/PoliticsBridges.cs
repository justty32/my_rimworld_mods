using System;
using RimWorld;
using RimWorld.Planet;

namespace pas.politics
{
    /// <summary>bridge 註冊點。npc-outposts bridge（條件 assembly）與 Rim War bridge（反射）在
    /// StaticConstructorOnStartup 期掛入；無 bridge 時全部 no-op。</summary>
    public static class PoliticsBridges
    {
        /// <summary>衛星聚落（如 npc-outposts 的哨站）判定——不被抽為倒戈對象、不計入聚落數、不當反叛者駐地。
        /// 注意：本檔禁止出現衛星型別的類名字串（健檢第 7 檢把關軟相容不變式）。</summary>
        public static Func<Settlement, bool> IsSatelliteResolver;

        /// <summary>(倒戈聚落, 母派系, 新派系)：每筆倒戈後觸發（哨站跟隨、Rim War 同步）。</summary>
        public static Action<Settlement, Faction, Faction> SettlementDefected;

        /// <summary>(母派系, 新派系)：分裂完成後觸發。</summary>
        public static Action<Faction, Faction> FactionSplit;

        public static bool IsSatellite(Settlement settlement)
        {
            return IsSatelliteResolver != null && IsSatelliteResolver(settlement);
        }

        public static void NotifySettlementDefected(Settlement settlement, Faction mother, Faction newFaction)
        {
            SettlementDefected?.Invoke(settlement, mother, newFaction);
        }

        public static void NotifyFactionSplit(Faction mother, Faction newFaction)
        {
            FactionSplit?.Invoke(mother, newFaction);
        }
    }
}
