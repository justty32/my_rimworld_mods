using System.Collections.Generic;
using Verse;

namespace pas.relations
{
    /// <summary>一張開局派系關係表。可有多個 Def（例如各主題分檔），全部會被套用。
    /// 以 defName 軟參照派系；解析與套用在 WorldComponent_RelationSeeder.Apply（開局一次）。</summary>
    public class RelationSeedDef : Def
    {
        public List<RelationSeedEntry> relations = new List<RelationSeedEntry>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            for (int i = 0; i < relations.Count; i++)
            {
                RelationSeedEntry r = relations[i];
                if (r.a.NullOrEmpty() || r.b.NullOrEmpty())
                {
                    yield return "relations[" + i + "] 需同時有 a 與 b（派系 defName）";
                }
                else if (r.a == r.b)
                {
                    yield return "relations[" + i + "] a 與 b 不可相同：" + r.a;
                }
                if (r.goodwill < -100 || r.goodwill > 100)
                {
                    yield return "relations[" + i + "] goodwill 需在 [-100,100]：" + r.goodwill;
                }
            }
        }
    }

    /// <summary>一對派系的目標善意值。缺席的派系（未裝該 mod / 未生成）在套用時自動略過。</summary>
    public class RelationSeedEntry
    {
        /// <summary>派系 A 的 FactionDef defName。</summary>
        public string a;
        /// <summary>派系 B 的 FactionDef defName。</summary>
        public string b;
        /// <summary>目標善意 [-100,100]。≤-75 敵對、≥75 結盟、其間中立（vanilla 閾值）。預設 0＝中立。</summary>
        public int goodwill;
    }
}
