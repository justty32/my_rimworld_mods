using Verse;

namespace ColonyArchivalOutpost
{
    // E1：玩家定義、可重複建造的哨站類型。
    // 存名稱（label）+ 世界圖標（iconPath）+ ProductivitySnapshot 原樣複本。
    public class OutpostType : IExposable
    {
        public string label;
        public string iconPath;
        public ProductivitySnapshot snapshot = new ProductivitySnapshot();

        public OutpostType() { }

        public OutpostType(string label, string iconPath, ProductivitySnapshot snapshot)
        {
            this.label = label;
            this.iconPath = iconPath;
            this.snapshot = snapshot ?? new ProductivitySnapshot();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref iconPath, "iconPath");
            Scribe_Deep.Look(ref snapshot, "snapshot");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && snapshot == null)
                snapshot = new ProductivitySnapshot();
        }
    }
}
