using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.officers
{
    /// <summary>唯一心跳與權威儲存（泛化 faction-politics WorldComponent_RebellionTracker：
    /// 去掉叛亂語意，留心跳/自癒/存讀骨架）。對外一律走 OfficersApi 門面。</summary>
    public class WorldComponent_OfficerRegistry : WorldComponent
    {
        private List<OfficerRecord> records = new List<OfficerRecord>();
        private int nextId = 1;   // record id 發號器
        /// <summary>執行期索引，讀檔後/變更時重建，不 scribe（Reference-key dict 存讀脆弱）。</summary>
        private Dictionary<WorldObject, List<OfficerRecord>> index =
            new Dictionary<WorldObject, List<OfficerRecord>>();

        public WorldComponent_OfficerRegistry(World world) : base(world) { }

        public static WorldComponent_OfficerRegistry Get()
            => Find.World?.GetComponent<WorldComponent_OfficerRegistry>();

        internal static OfficersSettingsDef Settings =>
            DefDatabase<OfficersSettingsDef>.AllDefsListForReading[0];

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            RebuildIndex();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % Settings.checkIntervalTicks != 0)
            {
                return;
            }
            for (int i = records.Count - 1; i >= 0; i--)
            {
                try
                {
                    OfficerHealer.Heal(this, records[i]);
                }
                catch (System.Exception e)
                {
                    OfficerHealer.WarnOnce(records[i], "heartbeat", e);
                }
            }
            OpinionEvolver.EvolveAll(records, Settings);   // T6：B 軌演化
        }

        // —— 內部 CRUD（OfficersApi 是唯一對外門面）——

        internal OfficerRecord Create(Faction faction, WorldObject host, OfficerRoleDef role)
        {
            if (faction == null || role == null)
            {
                return null;
            }
            if (host != null && For(host).Count >= Settings.maxOfficersPerObject)
            {
                return null;                                  // G6 數量控管
            }
            IntRange range = Settings.initialAttributeRange;
            OfficerRecord record = new OfficerRecord
            {
                id = nextId++,
                faction = faction,
                assignedTo = host,
                role = role,
                might = range.RandomInRange,
                command = range.RandomInRange,
                polity = range.RandomInRange,
                charisma = range.RandomInRange,
                loyalty = range.RandomInRange,
                intellect = range.RandomInRange,
                morale = range.RandomInRange,
            };
            records.Add(record);
            IndexAdd(record);
            return record;
        }

        internal void Assign(OfficerRecord record, WorldObject host)
        {
            IndexRemove(record);
            record.assignedTo = host;
            IndexAdd(record);
        }

        internal void Remove(OfficerRecord record)
        {
            records.Remove(record);
            IndexRemove(record);
            for (int i = 0; i < records.Count; i++)
            {
                records[i].opinions.Remove(record.id);        // 他人 opinions 的鍵清理
            }
        }

        internal List<OfficerRecord> For(WorldObject host)
        {
            if (host != null && index.TryGetValue(host, out List<OfficerRecord> list))
            {
                return list;
            }
            return EmptyList;
        }

        private static readonly List<OfficerRecord> EmptyList = new List<OfficerRecord>();

        internal OfficerRecord ById(int id)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].id == id) return records[i];
            }
            return null;
        }

        internal IReadOnlyList<OfficerRecord> AllForDebug => records;

        private void IndexAdd(OfficerRecord record)
        {
            if (record.assignedTo == null) return;
            if (!index.TryGetValue(record.assignedTo, out List<OfficerRecord> list))
            {
                index[record.assignedTo] = list = new List<OfficerRecord>();
            }
            list.Add(record);
        }

        private void IndexRemove(OfficerRecord record)
        {
            if (record.assignedTo != null && index.TryGetValue(record.assignedTo, out var list))
            {
                list.Remove(record);
                if (list.Count == 0) index.Remove(record.assignedTo);
            }
        }

        private void RebuildIndex()
        {
            index.Clear();
            for (int i = 0; i < records.Count; i++)
            {
                IndexAdd(records[i]);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref records, "pas_officerRecords", LookMode.Deep);
            Scribe_Values.Look(ref nextId, "pas_officerNextId", 1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                records = records ?? new List<OfficerRecord>();
                records.RemoveAll(r => r == null);            // faction null 不清（Heal 分支 5 處理）
                RebuildIndex();
            }
        }
    }
}
