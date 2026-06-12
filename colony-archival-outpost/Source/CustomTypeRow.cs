using System;
using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ColonyArchivalOutpost
{
    // E1 §3：自繪一列自訂類型（圖標 / label / 速率語意提示 / 建立鈕 / 取消註冊鈕）。
    // 佈局比照 VOE DoOutpostDisplay：左 50px 圖標、右側 label + 內文 + 底部按鈕。
    public static class CustomTypeRow
    {
        public static void Draw(ref Rect inRect, OutpostType type, Caravan creator, Action closeDialog)
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;

            string hint = RateSemanticsHint(type);
            Text.Font = GameFont.Small;
            inRect.height = Text.CalcHeight(hint, inRect.width - 90f) + 60f;

            Rect iconRect = inRect.LeftPartPixels(50f);
            Rect right = inRect.RightPartPixels(inRect.width - 60f);

            Texture2D tex = (!type.iconPath.NullOrEmpty()
                ? ContentFinder<Texture2D>.Get(type.iconPath, false) : null)
                ?? ContentFinder<Texture2D>.Get("WorldObjects/OutpostFarming", false);
            if (tex != null)
            {
                GUI.color = creator.Faction.Color;
                Widgets.DrawTextureFitted(iconRect, tex, 1f);
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Medium;
            string label = type.label.NullOrEmpty()
                ? "CAO.DefaultOutpostName".Translate().ToString() : type.label;
            Widgets.Label(right.TopPartPixels(30f), label.CapitalizeFirst());

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(right.x, right.y + 30f, right.width, right.height - 60f), hint);

            // 底部按鈕列：左「建立」、右「取消註冊」。
            Rect bottom = right.BottomPartPixels(30f);
            Rect createRect = bottom.LeftPartPixels(100f);
            Rect unregRect = new Rect(bottom.x + 110f, bottom.y, 120f, bottom.height);

            Text.Font = font;
            Text.Anchor = anchor;

            DrawCreateButton(createRect, type, creator, closeDialog);
            if (Widgets.ButtonText(unregRect, "CAO.Founding.Unregister".Translate()))
            {
                GameComponent_ArchivalTypes.Current?.Unregister(type);
                CustomTypeMenu.RemoveTransientFor(type);
            }
        }

        private static void DrawCreateButton(Rect rect, OutpostType type, Caravan creator, Action closeDialog)
        {
            List<Pawn> colonists = creator.PawnsListForReading.Where(p => p.IsFreeColonist).ToList();
            bool hasColonist = colonists.Count > 0;

            if (!hasColonist)
            {
                // 商隊無殖民者 → disable，提示空哨站不產出。
                bool prev = GUI.enabled;
                GUI.enabled = false;
                Widgets.ButtonText(rect, "CAO.Founding.Create".Translate());
                GUI.enabled = prev;
                TooltipHandler.TipRegion(rect, "CAO.Founding.NeedColonist".Translate());
                return;
            }

            if (Widgets.ButtonText(rect, "CAO.Founding.Create".Translate()))
            {
                var pawns = creator.PawnsListForReading.ToList();
                OutpostFactory.CreateWithPawns(
                    creator.Tile, creator.Faction, type.snapshot.Clone(),
                    type.label, type.iconPath, pawns);
                closeDialog();
            }
        }

        // 速率語意提示：per-pawn（隨人數縮放）vs 絕對（不論人數套同值），讓玩家建造前知情。
        private static string RateSemanticsHint(OutpostType type)
        {
            return (type.snapshot != null && type.snapshot.perPawnScaling)
                ? "CAO.Founding.RatePerPawn".Translate().ToString()
                : "CAO.Founding.RateAbsolute".Translate().ToString();
        }
    }
}
