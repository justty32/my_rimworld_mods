using System.Collections.Generic;
using Verse;

namespace ColonyArchivalOutpost
{
    // E1：per-save 自訂哨站類型清單。GameComponent 由 Current.Game.components 自動實例化，
    // 只要類別存在即可；以 Current.Game.GetComponent<GameComponent_ArchivalTypes>() 取用。
    public class GameComponent_ArchivalTypes : GameComponent
    {
        private List<OutpostType> types = new List<OutpostType>();

        public GameComponent_ArchivalTypes(Game game) { }

        public IReadOnlyList<OutpostType> All => types;

        public static GameComponent_ArchivalTypes Current =>
            Verse.Current.Game?.GetComponent<GameComponent_ArchivalTypes>();

        // 同 label 視為覆寫並回報 true（呼叫端據此提示「已覆寫」）。
        public bool Register(OutpostType type)
        {
            if (type == null) return false;
            int idx = types.FindIndex(t => t.label == type.label);
            if (idx >= 0)
            {
                types[idx] = type;
                return true; // overwrote existing
            }
            types.Add(type);
            return false;
        }

        public void Unregister(OutpostType type)
        {
            if (type == null) return;
            types.Remove(type);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref types, "caoTypes", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && types == null)
                types = new List<OutpostType>();
        }
    }
}
