using System.Collections.Generic;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ColonyArchivalOutpost
{
    // E1 §3 機制：暫時抽換 OutpostsMod.Outposts + 自繪自訂列。
    // 此處持「對話框開啟期間」的暫態狀態：原清單參考、暫態 def → OutpostType 映射。
    public static class CustomTypeMenu
    {
        // 原 OutpostsMod.Outposts 清單參考（Close 時還原）。
        public static List<WorldObjectDef> originalList;
        // 暫態 def → 來源類型；僅供選單渲染，永不落存檔。
        public static readonly Dictionary<WorldObjectDef, OutpostType> transientMap =
            new Dictionary<WorldObjectDef, OutpostType>();

        public static bool IsTransient(WorldObjectDef def) =>
            def != null && transientMap.ContainsKey(def);

        // 依當前已註冊類型，建「對話框用清單」並抽換 OutpostsMod.Outposts。
        // ＝原清單移除 base def（pas_archival_Outpost）＋每類型一個暫態 WorldObjectDef。
        public static void Install()
        {
            if (originalList == null)
                originalList = OutpostsMod.Outposts;
            transientMap.Clear();

            var dialogList = new List<WorldObjectDef>();
            foreach (var def in originalList)
                if (def.defName != "pas_archival_Outpost")
                    dialogList.Add(def);

            var comp = GameComponent_ArchivalTypes.Current;
            if (comp != null)
            {
                int i = 0;
                foreach (var type in comp.All)
                {
                    var tdef = MakeTransientDef(type, i++);
                    transientMap[tdef] = type;
                    dialogList.Add(tdef);
                }
            }
            OutpostsMod.Outposts = dialogList;
        }

        // 還原 OutpostsMod.Outposts、清掉暫態狀態。ctor↔Close 成對呼叫。
        public static void Restore()
        {
            if (originalList != null)
                OutpostsMod.Outposts = originalList;
            originalList = null;
            transientMap.Clear();
        }

        // 取消註冊一個類型後即時重整對話框清單（移除其暫態 def）。
        public static void RemoveTransientFor(OutpostType type)
        {
            WorldObjectDef toRemove = null;
            foreach (var kv in transientMap)
                if (kv.Value == type) { toRemove = kv.Key; break; }
            if (toRemove == null) return;
            transientMap.Remove(toRemove);
            OutpostsMod.Outposts?.Remove(toRemove);
        }

        // 建一個暫態 WorldObjectDef（不註冊進 DefDatabase）。
        // worldObjectClass = Outpost_Sampled 讓 VOE ctor 的 CanSpawnOnWith 反射不致 NRE。
        private static WorldObjectDef MakeTransientDef(OutpostType type, int idx)
        {
            return new WorldObjectDef
            {
                defName = "CAO_TransientType_" + idx,
                label = type.label.NullOrEmpty() ? "CAO.DefaultOutpostName".Translate().ToString() : type.label,
                description = "",
                worldObjectClass = typeof(Outpost_Sampled),
                expandingIconTexture = type.iconPath.NullOrEmpty()
                    ? "WorldObjects/OutpostFarming" : type.iconPath
            };
        }
    }
}
