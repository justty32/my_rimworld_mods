using System;
using HarmonyLib;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ColonyArchivalOutpost
{
    // E1 §3：把已註冊自訂類型注入 VOE 建站選單，並隱藏 base def 空項。
    // 機制＝ctor 抽換 OutpostsMod.Outposts（加暫態 def）、Close 還原、DoOutpostDisplay 自繪自訂列。
    [HarmonyPatch(typeof(Dialog_CreateCamp))]
    public static class Dialog_CreateCamp_Patch
    {
        // ctor prefix：在 VOE ctor 迭代 OutpostsMod.Outposts 前注入暫態 def。
        [HarmonyPatch(MethodType.Constructor, new[] { typeof(Caravan) })]
        [HarmonyPrefix]
        public static void CtorPrefix()
        {
            CustomTypeMenu.Install();
        }

        // PostClose postfix：還原靜態清單、清暫態狀態。
        // Dialog_CreateCamp 未 override PostClose → 目標解析到 Window.PostClose（對所有 Window 生效），
        // 故以 __instance 型別過濾，只在我們的對話框關閉時還原。
        [HarmonyPatch(typeof(Window), nameof(Window.PostClose))]
        [HarmonyPostfix]
        public static void PostClosePostfix(Window __instance)
        {
            if (__instance is Dialog_CreateCamp)
                CustomTypeMenu.Restore();
        }

        // DoOutpostDisplay prefix：暫態 def → 自繪整列、return false 跳過 VOE 原繪製。
        [HarmonyPatch("DoOutpostDisplay")]
        [HarmonyPrefix]
        public static bool DoOutpostDisplayPrefix(ref Rect inRect, WorldObjectDef outpostDef, object __instance)
        {
            if (!CustomTypeMenu.IsTransient(outpostDef))
                return true; // 非我們的 def → 走 VOE 原繪製

            var type = CustomTypeMenu.transientMap[outpostDef];
            var creator = Traverse.Create(__instance).Field("creator").GetValue<Caravan>();
            CustomTypeRow.Draw(ref inRect, type, creator,
                () => ((Window)__instance).Close(true));
            return false;
        }
    }
}
