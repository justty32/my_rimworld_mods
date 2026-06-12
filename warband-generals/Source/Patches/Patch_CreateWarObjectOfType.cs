using RimWar.Planet;

namespace pas.officers.warband
{
    /// <summary>戰後傳承的 ambient context（單槽）：RimWar 戰鬥結算以舊 warband 為樣板呼
    /// CreateWarObjectOfType → 內部同步轉呼 CreateWarband 造「新物件」（T0 核對、無巢狀）。
    /// prefix 抓舊將領 → Patch_CreateWarband postfix 消費；postfix 清殘留
    /// （非 Warband 分支 / 工廠失敗回 null 時防 context 漏到下一次呼叫）。</summary>
    internal static class TransferContext
    {
        private static OfficerRecord pending;

        internal static void Set(OfficerRecord record) => pending = record;

        internal static void Clear() => pending = null;

        internal static bool TryConsume(out OfficerRecord record)
        {
            record = pending;
            pending = null;
            return record != null;
        }
    }

    /// <summary>Prefix+Postfix WorldUtility.CreateWarObjectOfType（RW:15358）。</summary>
    public static class Patch_CreateWarObjectOfType
    {
        public static void Prefix(WarObject warObject)
        {
            try
            {
                TransferContext.Clear();
                if (!(warObject is Warband))
                {
                    return;
                }
                OfficerRecord record = WorldComponent_WarbandGenerals.Get()?.GeneralOf(warObject);
                if (record != null && !record.dead)
                {
                    TransferContext.Set(record);
                }
            }
            catch (System.Exception e)
            {
                GeneralsUtility.WarnOnce("transferPrefix", "傳承 prefix 異常，本次不傳承：" + e);
            }
        }

        public static void Postfix()
        {
            TransferContext.Clear();
        }
    }
}
