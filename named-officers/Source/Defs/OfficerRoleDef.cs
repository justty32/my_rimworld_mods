using Verse;

namespace pas.officers
{
    /// <summary>職位（領主/將領/官員…）。消費 mod 以 XML 增訂自己的角色。</summary>
    public class OfficerRoleDef : Def
    {
        /// <summary>inspect 排序（小者在前）。</summary>
        public int displayPriority;

        /// <summary>P2 領主類（每物件至多一名——上限由消費 mod 自管，本層僅標記）。</summary>
        public bool leaderLike;
    }
}
