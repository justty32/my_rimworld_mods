using System.Reflection;
using HarmonyLib;
using Verse;

namespace RatkinQuestlines
{
    // Harmony 啟動：載入時套用本組件所有 [HarmonyPatch]。
    // 目前只有一個 patch——交貨 B 方案（原生交易界面）用的 TradeDeal.TryExecute postfix（唯讀偵測賣出武器）。
    // 見 ForgeTradeDelivery.cs 與 brainstorm/6b §6.8。
    [StaticConstructorOnStartup]
    public static class RatkinQLHarmony
    {
        static RatkinQLHarmony()
        {
            new Harmony("justty32.ratkinquestlines").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
