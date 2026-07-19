using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // 彈性貨品需求（具名客戶定製用）：不指定 defName，只要求「數量區間 ＋ 品質門檻 ＋ 交付總價值門檻」。
    // ------------------------------------------------------------------
    // 為什麼：把「交固定 defName ×N」升級成「N~M 把武器、品質≥minQ、總值≥門檻」→ 玩家有打造自由、鼓勵做好料。
    // 價值門檻可寫絕對值(minTotalValue)，或用基準式(refThing 用 refStuff 在 refQuality 的 MarketValue × refCount)，
    //   作者只要寫「以 4 把鋼鐵良好長劍為基準」，無需硬算銀數。分級後果＝同一節點放多個不同門檻的交貨選項(band)。
    // 見 brainstorm/6b-forge-design-increment.md §6.5/§6.5.1/§6.6。
    public static class WeaponValueUtil
    {
        private static readonly Dictionary<string, float> baselineCache = new Dictionary<string, float>();

        // 參考武器單件市值（用 refStuff、refQuality 造一件臨時物讀 MarketValue，靜態快取避免每 tick 重算）。
        public static float BaselineUnitValue(ThingDef def, ThingDef stuff, QualityCategory q)
        {
            if (def == null)
            {
                return 0f;
            }
            // 參考武器可上材質但作者沒填 refStuff → 補預設材質，否則 ThingMaker.MakeThing 會噴紅字（madeFromStuff but stuff=null）。
            if (def.MadeFromStuff && stuff == null)
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            string key = def.defName + "|" + (stuff != null ? stuff.defName : "") + "|" + ((int)q);
            float cached;
            if (baselineCache.TryGetValue(key, out cached))
            {
                return cached;
            }
            Thing t = ThingMaker.MakeThing(def, stuff);
            CompQuality cq = t.TryGetComp<CompQuality>();
            if (cq != null)
            {
                cq.SetQuality(q, ArtGenerationContext.Colony);
            }
            float v = t.MarketValue;   // 未 spawn，讀值即可；不 Destroy（未註冊物件交由 GC）
            baselineCache[key] = v;
            return v;
        }

        public static bool PassFilter(ThingDef def, string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return def.IsWeapon;
            }
            if (filter == "Melee")
            {
                return def.IsMeleeWeapon;
            }
            if (filter == "Ranged")
            {
                return def.IsRangedWeapon;
            }
            return def.IsWeapon;
        }

        // 玩家地圖上「可交付」的合格武器（品質達標、符合過濾），依市值由高到低。
        // 用 ThingRequestGroup.Weapon 索引群組（快）；只含放在地圖上的（倉庫/地面）＝現貨，pawn 已裝備的不算。
        public static List<Thing> QualifyingWeapons(Map map, QualityCategory minQuality, string filter)
        {
            List<Thing> result = new List<Thing>();
            if (map == null)
            {
                return result;
            }
            List<Thing> pool = map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon);
            for (int i = 0; i < pool.Count; i++)
            {
                Thing t = pool[i];
                if (t == null || t.MapHeld == null || t.def == null)
                {
                    continue;
                }
                if (!PassFilter(t.def, filter))
                {
                    continue;
                }
                if (minQuality > QualityCategory.Awful)
                {
                    CompQuality cq = t.TryGetComp<CompQuality>();
                    if (cq == null || (int)cq.Quality < (int)minQuality)
                    {
                        continue;
                    }
                }
                result.Add(t);
            }
            result.Sort((a, b) => b.MarketValue.CompareTo(a.MarketValue));
            return result;
        }
    }

    // 閂交貨選項：地圖上要有 count.min~ 件、品質≥minQuality 的武器，且其中最值錢的至多 count.max 件市值加總 ≥ 門檻。
    public class DialogCondition_WeaponValue : DialogCondition
    {
        public IntRange count = new IntRange(1, 1);
        public QualityCategory minQuality = QualityCategory.Awful;
        public string weaponFilter;              // "Melee"／"Ranged"／留空＝不限
        public float minTotalValue = -1f;        // 絕對價值門檻；<0 ＝改用基準式
        public ThingDef refThing;                // 基準武器（如 MeleeWeapon_LongSword 或 RK_LongSword）
        public ThingDef refStuff;                // 基準材質（如 Steel）
        public QualityCategory refQuality = QualityCategory.Good;
        public int refCount = 4;

        public float Threshold()
        {
            if (minTotalValue >= 0f)
            {
                return minTotalValue;
            }
            if (refThing != null)
            {
                return refCount * WeaponValueUtil.BaselineUnitValue(refThing, refStuff, refQuality);
            }
            return 0f;
        }

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            Map map = QualityDeliveryUtil.MapFrom(targets);
            List<Thing> q = WeaponValueUtil.QualifyingWeapons(map, minQuality, weaponFilter);
            if (q.Count >= count.min)
            {
                int take = Math.Min(count.max, q.Count);
                float sum = 0f;
                for (int i = 0; i < take; i++)
                {
                    sum += q[i].MarketValue;
                }
                if (sum >= Threshold())
                {
                    reason = null;
                    return true;
                }
            }
            reason = Translator.Translate(failReason);
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count", new IntRange(1, 1));
            Scribe_Values.Look(ref minQuality, "minQuality", QualityCategory.Awful);
            Scribe_Values.Look(ref weaponFilter, "weaponFilter");
            Scribe_Values.Look(ref minTotalValue, "minTotalValue", -1f);
            Scribe_Defs.Look(ref refThing, "refThing");
            Scribe_Defs.Look(ref refStuff, "refStuff");
            Scribe_Values.Look(ref refQuality, "refQuality", QualityCategory.Good);
            Scribe_Values.Look(ref refCount, "refCount", 4);
        }
    }

    // 交貨扣料（簡易自動路線）：消費 count.min 件、且從價值最低的達標武器扣起
    //   ——不超交（只取最少件）、保留玩家好料。★需「按價值精算交多少」＝走 C 挑選視窗（Dialog_ForgeDelivery，玩家自選）。
    //   本 action 目前未接線，留作日後簡單自動交貨用；若接線，注意它不讀 value 門檻，可能低於 condition 驗的總值。
    public class CQFAction_ConsumeWeaponsByValue : CQFAction_Target
    {
        public IntRange count = new IntRange(1, 1);
        public QualityCategory minQuality = QualityCategory.Awful;
        public string weaponFilter;

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Map map = QualityDeliveryUtil.MapFrom(targets);
            if (map == null)
            {
                return;
            }
            List<Thing> q = WeaponValueUtil.QualifyingWeapons(map, minQuality, weaponFilter);  // 依市值由高到低
            int take = Math.Min(count.min, q.Count);
            for (int i = 0; i < take; i++)
            {
                q[q.Count - 1 - i].Destroy(DestroyMode.Vanish);   // 從最低價值端扣起
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count", new IntRange(1, 1));
            Scribe_Values.Look(ref minQuality, "minQuality", QualityCategory.Awful);
            Scribe_Values.Look(ref weaponFilter, "weaponFilter");
        }
    }
}
