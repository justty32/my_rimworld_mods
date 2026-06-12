using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace pas.officers.warband
{
    /// <summary>warband↔將領綁定一筆。host 走 Scribe_References（warband 在世界圖、或交戰時
    /// 深存於 BattleSite.Units / AttackingUnits，皆可解析）；record 只存 id、經 OfficersApi
    /// 懶解析（record 本體由 P0 registry 深存，唯一真相）。</summary>
    public class GeneralBinding : IExposable
    {
        public WorldObject host;
        public int recordId;

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving && host != null && host.Destroyed
                && !GeneralsUtility.InActiveBattle(host))
            {
                host = null;   // 真消亡宿主以 null 寫出（防 unresolved-ref 警告）；load 後心跳補退場
            }
            Scribe_References.Look(ref host, "host");
            Scribe_Values.Look(ref recordId, "recordId");
        }
    }

    /// <summary>權威綁定儲存＋心跳退場（00-overview 決策 1/3）。
    /// 不依賴 P0 的 assignedTo——warband 交戰即被 RimWar Destroy（深存容器續用實例），
    /// P0 heal 會在此時解除指派；本 comp 的 ref 綁定不斷，戰力 postfix 讀這裡。</summary>
    public class WorldComponent_WarbandGenerals : WorldComponent
    {
        private const int HeartbeatInterval = 2500;
        private const int HeartbeatOffset = 1200;   // 錯開 P0 registry 心跳（%==0）

        private List<GeneralBinding> bindings = new List<GeneralBinding>();

        /// <summary>執行期索引（reference key），讀檔後重建、不 scribe（仿 P0 registry index）。</summary>
        private Dictionary<WorldObject, GeneralBinding> byHost =
            new Dictionary<WorldObject, GeneralBinding>();

        public WorldComponent_WarbandGenerals(World world) : base(world) { }

        public static WorldComponent_WarbandGenerals Get()
            => Find.World?.GetComponent<WorldComponent_WarbandGenerals>();

        /// <summary>host 上的將領 record；無綁定/record 已被 P0 清 → null（順手解綁）。</summary>
        public OfficerRecord GeneralOf(WorldObject host)
        {
            if (host == null || !byHost.TryGetValue(host, out GeneralBinding binding))
            {
                return null;
            }
            OfficerRecord record = OfficersApi.GetById(binding.recordId);
            if (record == null)
            {
                Unbind(binding);   // pawn 死亡 G5 等 P0 已清 record → 綁定失效
            }
            return record;
        }

        /// <summary>建/搬綁定：先清同 record 舊綁定（戰後傳承時舊 host 已毀仍掛表上）、
        /// 再清同 host 既有綁定（防雙將），最後落表＋索引。</summary>
        public void Bind(WorldObject host, OfficerRecord record)
        {
            if (host == null || record == null)
            {
                return;
            }
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                if (bindings[i].recordId == record.id)
                {
                    Unbind(bindings[i]);
                }
            }
            if (byHost.TryGetValue(host, out GeneralBinding existing))
            {
                Unbind(existing);
            }
            GeneralBinding binding = new GeneralBinding { host = host, recordId = record.id };
            bindings.Add(binding);
            byHost[host] = binding;
        }

        private void Unbind(GeneralBinding binding)
        {
            bindings.Remove(binding);
            if (binding.host != null
                && byHost.TryGetValue(binding.host, out GeneralBinding current) && current == binding)
            {
                byHost.Remove(binding.host);
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % HeartbeatInterval != HeartbeatOffset)
            {
                return;
            }
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                GeneralBinding binding = bindings[i];
                try
                {
                    Heal(binding);
                }
                catch (System.Exception e)
                {
                    GeneralsUtility.WarnOnce("heartbeat:" + binding.recordId,
                        "綁定心跳例外，跳過 record " + binding.recordId + "：" + e);
                }
            }
        }

        private void Heal(GeneralBinding binding)
        {
            OfficerRecord record = OfficersApi.GetById(binding.recordId);
            if (record == null)
            {
                Unbind(binding);                      // 1. P0 已清（pawn 死亡 G5 收尾）
                return;
            }
            if (record.dead)
            {
                return;                               // 2. 遺言窗口：P0 下一心跳清 record → 走 1
            }
            WorldObject host = binding.host;
            if (host == null || (host.Destroyed && !GeneralsUtility.InActiveBattle(host)))
            {
                OfficersApi.RemoveOfficer(record);    // 3. 隨軍覆滅/解散 → 將領退場
                Unbind(binding);
            }
        }

        private void RebuildIndex()
        {
            byHost.Clear();
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].host != null)
                {
                    byHost[bindings[i].host] = bindings[i];
                }
            }
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            RebuildIndex();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref bindings, "pas_warbandGeneralBindings", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                bindings = bindings ?? new List<GeneralBinding>();
                bindings.RemoveAll(b => b == null);
                RebuildIndex();
            }
        }
    }
}
