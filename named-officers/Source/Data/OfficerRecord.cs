using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.officers
{
    /// <summary>一名具名職官（權威資料；pawn 只是懶生成的具現）。
    /// 泛化自 faction-politics RebelRecord（單一反叛者 → 通用職官）。</summary>
    public class OfficerRecord : IExposable
    {
        // —— 身份 ——

        /// <summary>registry 發號；關係 dict 的 key（跨具現穩定，pawn 換體不斷鍵）。</summary>
        public int id;
        public Faction faction;

        /// <summary>宿主世界物件（Settlement / warband / …，基類涵蓋）。null = 待命。</summary>
        public WorldObject assignedTo;
        public OfficerRoleDef role;

        /// <summary>懶生成；可 null（平時輕量 record，按需具現）。</summary>
        public Pawn pawn;

        /// <summary>pawn==null 時的顯示名（T0 決議方案 B：首次具現後快取）。</summary>
        public string nameCached;

        // —— 屬性（G2：七維全建、MVP 啟用五維，智力/士氣預留）——
        public int might;        // 武力（P1 將領讀）
        public int command;      // 統率（P1 將領讀）
        public int polity;       // 政務（P2 領主讀）
        public int charisma;     // 魅力（P2 領主讀）
        public int loyalty;      // 忠誠（P2/P4 讀）
        public int intellect;    // 智力（預留）
        public int morale;       // 士氣（預留）

        // —— 關係 B 軌（vanilla opinion 對 world pawn 凍結 → 自存自演化）——

        /// <summary>key = 對方 OfficerRecord.id（非 pawnId——pawn 可死後換體，record id 不變）。
        /// 非對稱：甲對乙 ≠ 乙對甲，各存各的。</summary>
        public Dictionary<int, int> opinions = new Dictionary<int, int>();

        // —— 生命週期 ——

        /// <summary>G5：死亡標記。標記後留一輪心跳給消費 mod 讀遺言，下一輪移除。</summary>
        public bool dead;

        public string DisplayName => pawn?.LabelShortCap ?? nameCached ?? role?.label ?? "officer";

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref assignedTo, "assignedTo");
            Scribe_Defs.Look(ref role, "role");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref nameCached, "nameCached");
            Scribe_Values.Look(ref might, "might");
            Scribe_Values.Look(ref command, "command");
            Scribe_Values.Look(ref polity, "polity");
            Scribe_Values.Look(ref charisma, "charisma");
            Scribe_Values.Look(ref loyalty, "loyalty");
            Scribe_Values.Look(ref intellect, "intellect");
            Scribe_Values.Look(ref morale, "morale");
            Scribe_Collections.Look(ref opinions, "opinions", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref dead, "dead");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && opinions == null)
            {
                opinions = new Dictionary<int, int>();
            }
        }
    }
}
