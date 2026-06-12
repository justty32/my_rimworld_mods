using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ColonyArchivalOutpost
{
    public class CompProperties_ArchivalPowerOutlet : CompProperties_Power
    {
        public CompProperties_ArchivalPowerOutlet()
        {
            compClass = typeof(CompArchivalPowerOutlet);
        }
    }

    // 產出端：把連線哨站的有號 PowerWatts 即時灌進主基地電網。
    // 繼承 CompPowerPlant 取得 PowerOn 管理與 UpdateDesiredPowerOutput 鉤子；
    // DesiredPowerOutput 不被夾成 ≥0，故可輸出負值（抽電）。
    // ThingDef 的 basePowerConsumption 設負 → PostSpawnSetup 視為發電機自動上電。
    public class CompArchivalPowerOutlet : CompPowerPlant
    {
        private Outpost_Sampled connectedOutpost;

        protected override float DesiredPowerOutput
        {
            get
            {
                if (connectedOutpost == null || connectedOutpost.Destroyed)
                {
                    connectedOutpost = null;
                    return 0f;
                }
                return connectedOutpost.PowerWatts;
            }
        }

        public void ConnectTo(Outpost_Sampled outpost)
        {
            Disconnect();
            // 一哨站一 outlet：若該哨站已連別的 outlet，先把舊 outlet 斷開
            Thing existing = outpost.ConnectedOutlet;
            if (existing != null && existing != parent)
                existing.TryGetComp<CompArchivalPowerOutlet>()?.Disconnect();

            connectedOutpost = outpost;
            outpost.SetConnectedOutlet(parent);
        }

        public void Disconnect()
        {
            if (connectedOutpost != null)
            {
                if (connectedOutpost.ConnectedOutlet == parent)
                    connectedOutpost.SetConnectedOutlet(null);
                connectedOutpost = null;
            }
        }

        // 由 Outpost_Sampled.Destroy 呼叫：只清本端引用，不回呼（哨站已在自毀流程）
        public void NotifyOutpostDestroyed()
        {
            connectedOutpost = null;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Disconnect();
            base.PostDestroy(mode, previousMap);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref connectedOutpost, "caoConnectedOutpost");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            yield return new Command_Action
            {
                defaultLabel = "CAO.Power.Connect".Translate(),
                defaultDesc = "CAO.Power.Connect.Desc".Translate(),
                icon = TexCommand.GatherSpotActive,
                action = OpenConnectMenu
            };

            if (connectedOutpost != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "CAO.Power.Disconnect".Translate(),
                    icon = TexCommand.GatherSpotActive,
                    action = Disconnect
                };
            }
        }

        private void OpenConnectMenu()
        {
            var opts = new List<FloatMenuOption>();
            foreach (var o in Find.WorldObjects.AllWorldObjects.OfType<Outpost_Sampled>())
            {
                if (!o.HasPowerSampling) continue;
                var outpost = o;
                opts.Add(new FloatMenuOption(outpost.Name, () => ConnectTo(outpost)));
            }
            if (opts.Count == 0)
                opts.Add(new FloatMenuOption("CAO.Power.NoOutposts".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();
            if (connectedOutpost != null && !connectedOutpost.Destroyed)
            {
                sb.AppendLine("CAO.Power.Connected".Translate(connectedOutpost.Name));
                sb.Append("CAO.Power.Output".Translate(PowerOutput.ToString("F0")));
            }
            else
            {
                sb.Append("CAO.Power.NotConnected".Translate());
            }
            string base2 = base.CompInspectStringExtra();
            if (!base2.NullOrEmpty())
                sb.Append("\n" + base2);
            return sb.ToString();
        }
    }
}
