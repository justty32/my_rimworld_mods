using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace pas.gear
{
    /// <summary>Harmony 引導：啟動時對 PawnGenerator.GeneratePawn 掛一個 Postfix。
    /// 這是本 mod「破例引 Harmony」的唯一補丁——完整裝備保真無純資料替代路。</summary>
    [StaticConstructorOnStartup]
    public static class GearSeederBootstrap
    {
        static GearSeederBootstrap()
        {
            try
            {
                Harmony h = new Harmony("pas.gear.community");
                h.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[gear-seeder] Harmony 補丁就緒（PawnGenerator.GeneratePawn postfix）。");
            }
            catch (Exception e)
            {
                Log.Error("[gear-seeder] Harmony 引導失敗，裝備層停用：" + e);
            }
        }
    }

    /// <summary>對每隻生成的 pawn，若其派系＋兵種在某張 FactionGearSeedDef 裡有規格，套用強制裝備。
    /// 只讀 vanilla API 生成/穿戴，不改任何 Def。逐步例外隔離，壞一件不拖垮整隻。</summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
        new Type[] { typeof(PawnGenerationRequest) })]
    public static class Patch_GeneratePawn_Gear
    {
        // 防遞迴：本補丁生成裝備不會回呼 GeneratePawn，但保險起見仍加旗標。
        [ThreadStatic] private static bool applying;

        [HarmonyPostfix]
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            // 只在遊戲中套用（鏡像 yc）：避開世界/地圖生成期的脆弱狀態——
            // 那時派系/玩家尚未就緒，且生成順序敏感（relation-seeder 亦因此延後）。
            if (applying || __result == null || Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            // 生成期 __result.Faction 可能尚未指派到 Thing 上；優先取 request.Faction（yc 同）。
            Faction faction = request.Faction ?? __result.Faction;
            if (faction == null)
            {
                return;
            }
            try
            {
                applying = true;
                GearSeedApplier.TryApply(__result, faction);
            }
            catch (Exception e)
            {
                Log.Warning("[gear-seeder] 套用裝備於 " + (__result.kindDef?.defName ?? "?") + " 例外，跳過：" + e.Message);
            }
            finally
            {
                applying = false;
            }
        }
    }

    /// <summary>純套用邏輯（可獨立測試/呼叫）。查表→脫既有→生成穿戴。</summary>
    public static class GearSeedApplier
    {
        public static void TryApply(Pawn pawn, Faction faction)
        {
            if (pawn == null || pawn.def == null || !pawn.RaceProps.Humanlike)
            {
                return;
            }
            if (faction?.def == null || pawn.kindDef == null)
            {
                return;
            }
            string factionDefName = faction.def.defName;
            string kindDefName = pawn.kindDef.defName;

            // Def 數量極小（每派系一張），直接線性掃描——鏡像 relation-seeder 的簡潔風格。
            foreach (FactionGearSeedDef seed in DefDatabase<FactionGearSeedDef>.AllDefsListForReading)
            {
                if (seed.factionDef != factionDefName || seed.kinds == null)
                {
                    continue;
                }
                foreach (GearKindEntry kind in seed.kinds)
                {
                    if (kind == null || kind.kindDef != kindDefName)
                    {
                        continue;
                    }
                    ApplyKind(pawn, kind);
                }
            }
        }

        private static void ApplyKind(Pawn pawn, GearKindEntry kind)
        {
            // ── 武器：主武器＝從非 alwaysTake 池加權挑 1；alwaysTake 武器（手雷/副武器）進背包 ──
            if (pawn.equipment != null && !kind.weapons.NullOrEmpty())
            {
                GearItemEntry primary;
                List<GearItemEntry> extras;
                ChooseWeapons(kind.weapons, out primary, out extras);
                // 只在真的挑到主武器時才清換——避免 forceOnlySelected 把 pawn 弄成空手。
                if (primary != null)
                {
                    if (kind.forceOnlySelected)
                    {
                        pawn.equipment.DestroyAllEquipment();
                    }
                    TryEquipWeapon(pawn, primary, kind.quality);
                }
                foreach (GearItemEntry ex in extras)
                {
                    TryAddToInventory(pawn, ex, kind.quality);
                }
            }

            // ── 衣物：forceNaked 只脫不穿；否則脫既有→穿 alwaysTake→加權挑一套不衝突的 ──
            if (pawn.apparel == null)
            {
                return;
            }
            if (kind.forceNaked)
            {
                StripApparel(pawn);
                return;
            }
            List<GearItemEntry> apparelPool = kind.apparel ?? new List<GearItemEntry>();
            if (apparelPool.Count == 0)
            {
                return;   // 沒指定衣物 → 保留 vanilla 生成的（即使 forceOnlySelected，避免全裸）
            }
            if (kind.forceOnlySelected)
            {
                StripApparel(pawn);
            }
            // alwaysTake 先穿（優先佔層）
            foreach (GearItemEntry a in apparelPool)
            {
                if (a != null && a.alwaysTake)
                {
                    TryWearApparel(pawn, a, kind.quality);
                }
            }
            // 其餘為加權池：依權重隨機順序逐件嘗試，與已穿不衝突才穿（挑出一套連貫穿搭）。
            List<GearItemEntry> rest = new List<GearItemEntry>();
            foreach (GearItemEntry a in apparelPool)
            {
                if (a != null && !a.alwaysTake && !a.thingDef.NullOrEmpty())
                {
                    rest.Add(a);
                }
            }
            while (rest.Count > 0)
            {
                GearItemEntry pick = rest.RandomElementByWeight(e => Mathf.Max(0.0001f, e.weight));
                rest.Remove(pick);
                TryWearApparel(pawn, pick, kind.quality, respectLayers: true);
            }
        }

        /// <summary>武器選取：主武器＝從非 alwaysTake 池加權挑 1（池空則退回第一件 alwaysTake）；
        /// 其餘 alwaysTake（手雷/副武器）為 extras 進背包。primary 可能為 null（無可解析武器）。</summary>
        private static void ChooseWeapons(List<GearItemEntry> weapons,
            out GearItemEntry primary, out List<GearItemEntry> extras)
        {
            primary = null;
            extras = new List<GearItemEntry>();
            List<GearItemEntry> pool = new List<GearItemEntry>();
            List<GearItemEntry> forced = new List<GearItemEntry>();
            foreach (GearItemEntry w in weapons)
            {
                if (w == null || ResolveThing(w.thingDef)?.IsWeapon != true)
                {
                    continue;
                }
                (w.alwaysTake ? forced : pool).Add(w);
            }
            if (pool.Count > 0)
            {
                primary = pool.RandomElementByWeight(e => Mathf.Max(0.0001f, e.weight));
                extras = forced;   // 全部 alwaysTake 當副武器/手雷進背包
            }
            else if (forced.Count > 0)
            {
                primary = forced[0];           // 沒有池武器 → 第一件強制武器當主武器
                extras = forced.GetRange(1, forced.Count - 1);
            }
        }

        private static void StripApparel(Pawn pawn)
        {
            List<Apparel> worn = new List<Apparel>(pawn.apparel.WornApparel);
            foreach (Apparel app in worn)
            {
                try
                {
                    pawn.apparel.Remove(app);
                    if (!app.Destroyed)
                    {
                        app.Destroy(DestroyMode.Vanish);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("[gear-seeder] 脫除 " + app.def.defName + " 失敗：" + e.Message);
                }
            }
        }

        private static void TryWearApparel(Pawn pawn, GearItemEntry entry, QualityCategory? kindQuality,
            bool respectLayers = false)
        {
            ThingDef def = ResolveThing(entry.thingDef);
            if (def == null || !def.IsApparel)
            {
                return;
            }
            if (!ApparelUtility.HasPartsToWear(pawn, def) || !def.apparel.PawnCanWear(pawn))
            {
                return;   // 身體部位不符（如機械族/異種）→ 軟略過
            }
            // 加權池挑選：若與已穿的衝突（同層/覆蓋重疊）就跳過，讓一套穿搭連貫、不互相頂替。
            // alwaysTake（respectLayers=false）不檢查——它有優先權，用 Wear 的 dropReplaced 頂掉衝突件。
            if (respectLayers)
            {
                foreach (Apparel worn in pawn.apparel.WornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(def, worn.def, pawn.RaceProps.body))
                    {
                        return;
                    }
                }
            }
            Thing made = MakeItem(def, entry, kindQuality);
            Apparel apparel = made as Apparel;
            if (apparel == null)
            {
                made?.Destroy(DestroyMode.Vanish);
                return;
            }
            try
            {
                pawn.apparel.Wear(apparel, dropReplacedApparel: true, locked: false);
            }
            catch (Exception e)
            {
                Log.Warning("[gear-seeder] 穿戴 " + def.defName + " 失敗：" + e.Message);
                if (!apparel.Destroyed)
                {
                    apparel.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void TryEquipWeapon(Pawn pawn, GearItemEntry entry, QualityCategory? kindQuality)
        {
            ThingDef def = ResolveThing(entry.thingDef);
            if (def == null || !def.IsWeapon)
            {
                return;
            }
            Thing made = MakeItem(def, entry, kindQuality);
            ThingWithComps twc = made as ThingWithComps;
            if (twc == null)
            {
                made?.Destroy(DestroyMode.Vanish);
                return;
            }
            try
            {
                pawn.equipment.AddEquipment(twc);
            }
            catch (Exception e)
            {
                Log.Warning("[gear-seeder] 裝備武器 " + def.defName + " 失敗：" + e.Message);
                if (!twc.Destroyed)
                {
                    twc.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void TryAddToInventory(Pawn pawn, GearItemEntry entry, QualityCategory? kindQuality)
        {
            if (pawn.inventory == null)
            {
                return;
            }
            ThingDef def = ResolveThing(entry.thingDef);
            if (def == null)
            {
                return;
            }
            Thing made = MakeItem(def, entry, kindQuality);
            if (made == null)
            {
                return;
            }
            try
            {
                if (!pawn.inventory.innerContainer.TryAdd(made))
                {
                    made.Destroy(DestroyMode.Vanish);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[gear-seeder] 放入背包 " + def.defName + " 失敗：" + e.Message);
                if (!made.Destroyed)
                {
                    made.Destroy(DestroyMode.Vanish);
                }
            }
        }

        /// <summary>依 thingDef＋stuff 生成物品，套 quality（件層＞kind 層）與 color。</summary>
        private static Thing MakeItem(ThingDef def, GearItemEntry entry, QualityCategory? kindQuality)
        {
            ThingDef stuff = null;
            if (def.MadeFromStuff)
            {
                stuff = ResolveThing(entry.stuff);
                if (stuff == null || !stuff.IsStuff || !stuff.stuffProps.CanMake(def))
                {
                    stuff = GenStuff.DefaultStuffFor(def);
                }
            }
            Thing thing = ThingMaker.MakeThing(def, stuff);

            QualityCategory? q = entry.quality ?? kindQuality;
            if (q.HasValue)
            {
                CompQuality cq = thing.TryGetComp<CompQuality>();
                cq?.SetQuality(q.Value, ArtGenerationContext.Outsider);
            }

            if (entry.color.HasValue)
            {
                CompColorable cc = thing.TryGetComp<CompColorable>();
                cc?.SetColor(entry.color.Value);
            }
            return thing;
        }

        private static ThingDef ResolveThing(string defName)
        {
            if (defName.NullOrEmpty())
            {
                return null;
            }
            return DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        }
    }
}
