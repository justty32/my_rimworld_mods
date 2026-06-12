using System.Text;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.officers
{
    /// <summary>dev mode 驗收工具（P0 無玩法，這就是驗收介面）。
    /// 入口：Debug actions → pas.officers。除 Dump 外全程只走 OfficersApi（API 完備性自我檢驗）。</summary>
    public static class OfficerDebugActions
    {
        private static WorldObject Selected => Find.WorldSelector.SingleSelectedObject;

        private static OfficerRecord First(int skip = 0)
        {
            var list = OfficersApi.GetOfficers(Selected);
            return list.Count > skip ? list[skip] : null;
        }

        [DebugAction("pas.officers", "Create officer at selected", allowedGameStates = AllowedGameStates.Playing)]
        private static void CreateOfficer()
        {
            WorldObject host = Selected;
            OfficerRecord r = OfficersApi.CreateOfficer(host?.Faction, host, OfficersDefOf.pas_officers_Generic);
            Log.Message("[pas.officers] CreateOfficer → " + (r == null
                ? "null（未選中物件/無派系/超上限）" : "id=" + r.id + " host=" + host.Label));
        }

        [DebugAction("pas.officers", "Materialize first officer", allowedGameStates = AllowedGameStates.Playing)]
        private static void MaterializeFirst()
        {
            Pawn p = OfficersApi.Materialize(First());
            Log.Message("[pas.officers] Materialize → " + (p == null
                ? "null（無職官/生成失敗）" : p.LabelShortCap + " thingId=" + p.ThingID));
        }

        [DebugAction("pas.officers", "Dump officer registry", allowedGameStates = AllowedGameStates.Playing)]
        private static void Dump()
        {
            var registry = WorldComponent_OfficerRegistry.Get();
            StringBuilder sb = new StringBuilder("[pas.officers] registry dump, records=" + registry.AllForDebug.Count);
            foreach (OfficerRecord r in registry.AllForDebug)
            {
                sb.AppendLine().Append("  id=").Append(r.id)
                    .Append(" name=").Append(r.DisplayName)
                    .Append(" role=").Append(r.role?.defName ?? "null")
                    .Append(" faction=").Append(r.faction?.Name.ToString() ?? "null")
                    .Append(" host=").Append(r.assignedTo?.Label ?? "null")
                    .Append(" dead=").Append(r.dead)
                    .Append(" might=").Append(r.might).Append(" command=").Append(r.command)
                    .Append(" polity=").Append(r.polity).Append(" charisma=").Append(r.charisma)
                    .Append(" loyalty=").Append(r.loyalty).Append(" intellect=").Append(r.intellect)
                    .Append(" morale=").Append(r.morale);
                if (r.pawn != null)
                {
                    sb.Append(" spawned=").Append(r.pawn.Spawned)
                        .Append(" world=").Append(Find.WorldPawns.Contains(r.pawn))
                        .Append(" forcedKeep=").Append(Find.WorldPawns.ForcefullyKeptPawns.Contains(r.pawn))
                        .Append(" inhabitantsList=").Append(r.assignedTo is Settlement s
                            && s.previouslyGeneratedInhabitants.Contains(r.pawn));
                }
                sb.Append(" opinions={");
                foreach (var kv in r.opinions) sb.Append(kv.Key).Append(':').Append(kv.Value).Append(' ');
                sb.Append('}');
            }
            Log.Message(sb.ToString());
        }

        [DebugAction("pas.officers", "Roll attributes (first)", allowedGameStates = AllowedGameStates.Playing)]
        private static void RollAttributes()
        {
            OfficerRecord r = First();
            if (r == null) { Log.Message("[pas.officers] 無職官可擲"); return; }
            foreach (OfficerAttribute attr in System.Enum.GetValues(typeof(OfficerAttribute)))
            {
                OfficersApi.SetAttribute(r, attr, Rand.RangeInclusive(0, 100));
            }
            Log.Message("[pas.officers] rolled id=" + r.id + " might=" + r.might + " command=" + r.command
                + " polity=" + r.polity + " charisma=" + r.charisma + " loyalty=" + r.loyalty
                + " intellect=" + r.intellect + " morale=" + r.morale);
        }

        [DebugAction("pas.officers", "Add sworn brothers (first two)", allowedGameStates = AllowedGameStates.Playing)]
        private static void AddSwornBrothers()
        {
            bool ok = OfficersApi.AddPersistentRelation(First(), First(1), OfficersDefOf.pas_officers_SwornBrother);
            Log.Message("[pas.officers] AddPersistentRelation(SwornBrother) → " + ok);
        }

        [DebugAction("pas.officers", "Offset opinion -100 (first two)", allowedGameStates = AllowedGameStates.Playing)]
        private static void OffsetOpinion()
        {
            OfficerRecord a = First(), b = First(1);
            if (a == null || b == null) { Log.Message("[pas.officers] 需要兩名職官"); return; }
            OfficersApi.OffsetOpinion(a, b, -100);
            OfficersApi.OffsetOpinion(b, a, -100);
            Log.Message("[pas.officers] opinions now " + a.id + "→" + b.id + "=" + OfficersApi.GetOpinion(a, b)
                + ", " + b.id + "→" + a.id + "=" + OfficersApi.GetOpinion(b, a));
        }

        [DebugAction("pas.officers", "Kill first officer pawn", allowedGameStates = AllowedGameStates.Playing)]
        private static void KillFirst()
        {
            OfficerRecord r = First();
            if (r?.pawn == null) { Log.Message("[pas.officers] 無已具現職官可殺"); return; }
            r.pawn.Kill(null);
            Log.Message("[pas.officers] killed id=" + r.id + "；觀察下兩輪心跳：dead 標記→OfficerDied→record 移除（G5）");
        }

        [DebugAction("pas.officers", "Destroy host object test (hint)", allowedGameStates = AllowedGameStates.Playing)]
        private static void DestroyHostHint()
        {
            Log.Message("[pas.officers] 手動用 vanilla debug 毀掉選中世界物件，下一心跳後 Dump："
                + "record 應 host=null 留存、OfficerUnassigned listener 有印（先 Toggle event log）。");
        }

        private static bool listening;

        [DebugAction("pas.officers", "Toggle event log listeners", allowedGameStates = AllowedGameStates.Playing)]
        private static void ToggleListeners()
        {
            if (!listening)
            {
                OfficersApi.OfficerCreated += LogCreated;
                OfficersApi.OfficerDied += LogDied;
                OfficersApi.OfficerUnassigned += LogUnassigned;
            }
            else
            {
                OfficersApi.OfficerCreated -= LogCreated;
                OfficersApi.OfficerDied -= LogDied;
                OfficersApi.OfficerUnassigned -= LogUnassigned;
            }
            listening = !listening;
            Log.Message("[pas.officers] event listeners " + (listening ? "ON" : "OFF"));
        }

        private static void LogCreated(OfficerRecord r) => Log.Message("[pas.officers] event OfficerCreated id=" + r.id);
        private static void LogDied(OfficerRecord r) => Log.Message("[pas.officers] event OfficerDied id=" + r.id);
        private static void LogUnassigned(OfficerRecord r) => Log.Message("[pas.officers] event OfficerUnassigned id=" + r.id);

        [DebugAction("pas.officers", "API null-safety probe", allowedGameStates = AllowedGameStates.Playing)]
        private static void NullProbe()
        {
            int passed = 0, total = 0;
            void Probe(string name, System.Action call)
            {
                total++;
                try { call(); passed++; }
                catch (System.Exception e) { Log.Warning("[pas.officers] null probe FAIL " + name + ": " + e.Message); }
            }
            Probe("GetOfficers", () => OfficersApi.GetOfficers(null));
            Probe("GetOfficer", () => OfficersApi.GetOfficer(null, null));
            Probe("GetById", () => OfficersApi.GetById(-1));
            Probe("CreateOfficer", () => OfficersApi.CreateOfficer(null, null, null));
            Probe("AssignOfficer", () => OfficersApi.AssignOfficer(null, null));
            Probe("RemoveOfficer", () => OfficersApi.RemoveOfficer(null));
            Probe("Materialize", () => OfficersApi.Materialize(null));
            Probe("SetAttribute", () => OfficersApi.SetAttribute(null, OfficerAttribute.Might, 50));
            Probe("GetAttribute", () => OfficersApi.GetAttribute(null, OfficerAttribute.Might));
            Probe("GetOpinion", () => OfficersApi.GetOpinion(null, null));
            Probe("OffsetOpinion", () => OfficersApi.OffsetOpinion(null, null, 1));
            Probe("AddPersistentRelation", () => OfficersApi.AddPersistentRelation(null, null, null));
            Log.Message("[pas.officers] null-safety probe: " + passed + "/" + total + " passed");
        }
    }
}
