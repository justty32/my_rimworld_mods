using System;
using FactionColonies;
using Verse;

namespace pas.empire.wartimeeconomy
{
    /// <summary>
    /// 入口。對 Empire 全走契約層、零 Harmony：
    ///  - 圍困減產＝WorldObjectComp（IResourceProductionModifier，XML 掛上、無需 Registry）；
    ///  - 戰時加稅＝ITaxTickParticipant 註冊進 TaxTickRegistry。
    /// TaxTickRegistry 會被 Game.ClearCaches（讀檔/開新局）清空，故配
    /// EmpireCacheUtil.RegisterCacheInvalidator 在每次清快取後自動重註冊
    /// （官方 Patch-RW 連自己的 modifier 都漏做這點——見調查 C 結論）。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class WartimeEconomyInit
    {
        public const string InvalidatorKey = "pas.empire.wartimeeconomy";

        private static readonly WartimeTaxParticipant taxParticipant = new WartimeTaxParticipant();

        static WartimeEconomyInit()
        {
            try
            {
                RegisterAll();
                EmpireCacheUtil.RegisterCacheInvalidator(InvalidatorKey, RegisterAll);
                LogUtil.MessageForce("[WartimeEconomy] loaded (siege production cut + wartime tax).");
            }
            catch (Exception e)
            {
                Log.Error("[WartimeEconomy] init failed; wartime economy disabled for this session: " + e);
            }
        }

        // 在 ClearCaches 清空 Registry 後由 invalidator 回呼重跑（CachePatches 在 ClearAll 之後執行 invalidator）。
        private static void RegisterAll()
        {
            TaxTickRegistry.Register(taxParticipant);
        }
    }
}
