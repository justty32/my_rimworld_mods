using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace pas.officers.settlements
{
    /// <summary>權威綁定儲存＋心跳（00-overview 決策 1/2/3）：heal（record 消失/城毀/易主）
    /// → 指派掃描（無主城按機率補太守）。不依賴 P0 的 assignedTo——P0 heal 在易主當輪
    /// 會搶先解除指派，本 comp 以 host.Faction != record.faction 自行判定並走 G5 政策。</summary>
    public class WorldComponent_SettlementLords : WorldComponent
    {
        private const int HeartbeatInterval = 2500;
        private const int HeartbeatOffset = 600;   // 錯開 P0(0)/P1(1200)/RimWar update(0)

        private List<LordBinding> bindings = new List<LordBinding>();

        /// <summary>執行期索引（reference key），讀檔後重建、不 scribe（仿 P0/P1）。</summary>
        private Dictionary<Settlement, LordBinding> byHost = new Dictionary<Settlement, LordBinding>();

        public WorldComponent_SettlementLords(World world) : base(world) { }

        public static WorldComponent_SettlementLords Get()
            => Find.World?.GetComponent<WorldComponent_SettlementLords>();

        /// <summary>host 的領主 record；無綁定/record 已被 P0 清 → null（順手解綁）。</summary>
        public OfficerRecord LordOf(Settlement host)
        {
            if (host == null || !byHost.TryGetValue(host, out LordBinding binding))
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

        /// <summary>成長 postfix 可能在 RimWar tasker 背景執行緒跑（RW:17062）→
        /// 給陣列快照不給活表；快照本身的競態由呼叫端 try/catch 收。</summary>
        public LordBinding[] BindingsSnapshot() => bindings.ToArray();

        /// <summary>建綁定：先清同 record 舊綁定、再清同 host 既有綁定（防雙主）。</summary>
        public void Bind(Settlement host, OfficerRecord record)
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
            if (byHost.TryGetValue(host, out LordBinding existing))
            {
                Unbind(existing);
            }
            LordBinding binding = new LordBinding { host = host, recordId = record.id };
            bindings.Add(binding);
            byHost[host] = binding;
        }

        private void Unbind(LordBinding binding)
        {
            bindings.Remove(binding);
            if (binding.host != null
                && byHost.TryGetValue(binding.host, out LordBinding current) && current == binding)
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
                LordBinding binding = bindings[i];
                try
                {
                    Heal(binding);
                }
                catch (System.Exception e)
                {
                    LordsUtility.WarnOnce("heartbeat:" + binding.recordId,
                        "綁定心跳例外，跳過 record " + binding.recordId + "：" + e);
                }
            }
            try
            {
                LordAppointer.Scan(this);
            }
            catch (System.Exception e)
            {
                LordsUtility.WarnOnce("appointScan", "指派掃描例外，本輪跳過：" + e);
            }
        }

        /// <summary>指派掃描用：該城是否已有綁定（不解析 record，輕量）。</summary>
        public bool HasLord(Settlement host) => host != null && byHost.ContainsKey(host);

        private void Heal(LordBinding binding)
        {
            OfficerRecord record = OfficersApi.GetById(binding.recordId);
            if (record == null)
            {
                Unbind(binding);                      // 1. P0 已清（pawn 死亡 G5 收尾）→ 城變無主
                return;
            }
            if (record.dead)
            {
                return;                               // 2. 遺言窗口：P0 下一心跳清 record → 走 1
            }
            Settlement host = binding.host;
            if (host == null || host.Destroyed)
            {
                OfficersApi.RemoveOfficer(record);    // 3. 城亡人去
                Unbind(binding);
                return;
            }
            if (host.Faction != record.faction)
            {
                // 4. 易主：先廣播（P4 叛變消費窗口）再退場（G5 預設政策）
                LordEvents.RaiseLordLostSettlement(record, host);
                OfficersApi.RemoveOfficer(record);
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
            Scribe_Collections.Look(ref bindings, "pas_settlementLordBindings", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                bindings = bindings ?? new List<LordBinding>();
                bindings.RemoveAll(b => b == null);
                RebuildIndex();
            }
        }
    }
}
