using Verse;

namespace pas.sanguo.cityeconomy
{
    /// <summary>extraAttributes 旁路的 Def key（K 段：typed 主幹＋Def-dict 旁路，
    /// 純 string-dict 否決——參與邏輯的屬性需穩定符號）。
    /// 本期不出貨任何實例；P5 領主治理動作層按需以 XML 增訂。</summary>
    public class SettlementAttributeDef : Def
    {
        /// <summary>未寫入時的預設值。</summary>
        public float defaultValue;
    }
}
