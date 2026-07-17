using LudeonTK;
using Verse;

namespace pas.relations
{
    /// <summary>dev mode：手動重新套用關係表（E2E 對帳/調參用）。
    /// 入口：Debug actions → pas.relations → Re-apply relation seeds。</summary>
    public static class RelationSeederDebug
    {
        [DebugAction("pas.relations", "Re-apply relation seeds",
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ReapplySeeds()
        {
            Find.World?.GetComponent<WorldComponent_RelationSeeder>()?.Apply();
        }
    }
}
