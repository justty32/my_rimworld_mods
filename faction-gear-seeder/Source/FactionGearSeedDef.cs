using System.Collections.Generic;
using RimWorld;
using Verse;

namespace pas.gear
{
    /// <summary>一張開局派系「裝備表」：對某個 FactionDef，宣告其各 PawnKind 的強制裝備/武器/品質。
    /// 可有多個 Def（例如各派系分檔，或各主題分檔），全部會被套用。
    /// 以 defName 軟參照派系與物品；解析與套用在 GearSeedApplier（每隻 pawn 生成後一次）。
    ///
    /// 這是「開局世界管線」的裝備層，姊妹於 RelationSeedDef（關係層）。
    /// 資料由 yc Faction Editor 打樣後用 tools/transcribe_yc_preset.py 謄成本 Def。</summary>
    public class FactionGearSeedDef : Def
    {
        /// <summary>目標派系的 FactionDef defName。此表只套用到該派系生成的 pawn。</summary>
        public string factionDef;

        /// <summary>各兵種的裝備規格。</summary>
        public List<GearKindEntry> kinds = new List<GearKindEntry>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            if (factionDef.NullOrEmpty())
            {
                yield return "需指定 factionDef（目標派系 defName）";
            }
            if (kinds == null || kinds.Count == 0)
            {
                yield return "kinds 為空：此裝備表不會套用任何東西";
            }
            else
            {
                for (int i = 0; i < kinds.Count; i++)
                {
                    GearKindEntry k = kinds[i];
                    if (k == null || k.kindDef.NullOrEmpty())
                    {
                        yield return "kinds[" + i + "] 需指定 kindDef（PawnKindDef defName）";
                    }
                }
            }
        }
    }

    /// <summary>一個兵種（PawnKindDef）的裝備規格。缺席的兵種/物品在套用時軟略過。</summary>
    public class GearKindEntry
    {
        /// <summary>目標兵種的 PawnKindDef defName。</summary>
        public string kindDef;

        /// <summary>套用前先脫光既有（vanilla 生成的）衣著/武器，只留本表指定的。
        /// 對應 yc 的 forceOnlySelected。預設 true（要「就是這身」的確定性）。</summary>
        public bool forceOnlySelected = true;

        /// <summary>強制脫光、且不穿任何本表衣物（只保留武器）。對應 yc 的 forceNaked。</summary>
        public bool forceNaked = false;

        /// <summary>本兵種所有生成物品的品質。null＝不強制（用 vanilla 隨機）。
        /// 合法值：Awful/Poor/Normal/Good/Excellent/Masterwork/Legendary。</summary>
        public QualityCategory? quality;

        /// <summary>衣物/裝甲的加權選池（＋逐件 alwaysTake 強制件）。引擎依 weight 挑一套不衝突的穿上；
        /// alwaysTake 的無條件優先穿。對應 yc 的 apparel/armors/others 簡單池 ＋ specificApparel。</summary>
        public List<GearItemEntry> apparel = new List<GearItemEntry>();

        /// <summary>武器的加權選池（＋逐件 alwaysTake）。引擎挑 1 把當主武器；有 alwaysTake 則優先。
        /// 對應 yc 的 weapons 簡單池 ＋ specificWeapons。</summary>
        public List<GearItemEntry> weapons = new List<GearItemEntry>();
    }

    /// <summary>一件裝備：thingDef 必填；weight/alwaysTake 控制選取；stuff/quality/color 可選覆寫。</summary>
    public class GearItemEntry
    {
        /// <summary>物品 ThingDef defName（衣物或武器）。</summary>
        public string thingDef;

        /// <summary>加權選池的權重（越大越可能被挑中）。預設 1。</summary>
        public float weight = 1f;

        /// <summary>true＝無條件強制此件（不參與加權挑選；衣物優先穿、武器優先為主武器）。
        /// 對應 yc 的 SpecRequirementEdit（AlwaysTake）。預設 false（一般池成員）。</summary>
        public bool alwaysTake = false;

        /// <summary>材質 ThingDef defName（如 Steel、DevilstrandCloth）。null＝該物品的預設/隨機材質。</summary>
        public string stuff;

        /// <summary>此件的品質覆寫（優先於 kind 層的 quality）。null＝用 kind 層 quality 或 vanilla。</summary>
        public QualityCategory? quality;

        /// <summary>衣物染色（RGBA，0–1）。null＝不染色。</summary>
        public UnityEngine.Color? color;
    }
}
