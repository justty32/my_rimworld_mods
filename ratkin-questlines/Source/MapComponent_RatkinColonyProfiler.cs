using System.Collections.Generic;
using RimWorld;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // F7 據點側寫器（colony profiler）。
    // 定期讀殖民地「可觀測狀態」，把推導出的「身份」寫回 CQF 全域 bool 庫
    // （GameComponent_Editor.Component.SetBool），供對話端 DialogCondition_Bool 讀，篩訪客/委託。
    //
    // 採 VOE 的身份邏輯：身份＝功能。主導技能→類型；財富/人口→量級（規模進程軸）。
    // 只感測殖民地狀態＋寫自己的 RatkinQL_State_* 旗標——獨立、不碰模擬經營框架（解耦約束）。
    // 對話端另有玩家「玩出來」的 RatkinQL_Ident_*（Supplier/Sanctuary/Mercenary），兩者並存互補。
    //
    // MapComponent 由遊戲對每張地圖自動實例化，無需註冊。
    public class MapComponent_RatkinColonyProfiler : MapComponent
    {
        private const int IntervalTicks = 2500; // 約遊戲內一小時評估一次，成本極低

        public MapComponent_RatkinColonyProfiler(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % IntervalTicks != 0)
            {
                return;
            }
            if (!map.IsPlayerHome)
            {
                return;
            }
            GameComponent_Editor editor = GameComponent_Editor.Component;
            if (editor == null)
            {
                return;
            }

            float wealth = map.wealthWatcher != null ? map.wealthWatcher.WealthTotal : 0f;
            int pop = map.mapPawns.FreeColonistsSpawnedCount;

            // --- 量級（規模進程軸：前哨→村莊→城鎮→城市；成熟門檻）---
            editor.SetBool("RatkinQL_State_Established", wealth >= 30000f || pop >= 5);
            editor.SetBool("RatkinQL_State_Hamlet", pop < 5);
            editor.SetBool("RatkinQL_State_Town", pop >= 5 && pop < 12);
            editor.SetBool("RatkinQL_State_City", pop >= 12);
            editor.SetBool("RatkinQL_State_Wealthy", wealth >= 150000f);

            // --- 類型：主導技能（VOE：身份＝派駐 pawn 技能總和）---
            Dictionary<SkillDef, int> totals = new Dictionary<SkillDef, int>();
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p.skills == null)
                {
                    continue;
                }
                List<SkillRecord> skills = p.skills.skills;
                for (int j = 0; j < skills.Count; j++)
                {
                    SkillRecord s = skills[j];
                    int cur;
                    totals.TryGetValue(s.def, out cur);
                    totals[s.def] = cur + s.Level;
                }
            }

            SkillDef dom = null;
            int best = -1;
            foreach (KeyValuePair<SkillDef, int> kv in totals)
            {
                if (kv.Value > best)
                {
                    best = kv.Value;
                    dom = kv.Key;
                }
            }

            editor.SetBool("RatkinQL_State_Farming", dom == SkillDefOf.Plants);
            editor.SetBool("RatkinQL_State_Crafter", dom == SkillDefOf.Crafting);
            editor.SetBool("RatkinQL_State_Trading", dom == SkillDefOf.Social);
            editor.SetBool("RatkinQL_State_Martial", dom == SkillDefOf.Shooting || dom == SkillDefOf.Melee);
            editor.SetBool("RatkinQL_State_Clinic", dom == SkillDefOf.Medicine);
            editor.SetBool("RatkinQL_State_Academy", dom == SkillDefOf.Intellectual);
            editor.SetBool("RatkinQL_State_Kitchen", dom == SkillDefOf.Cooking);

            // --- 「能打鐵」＝有鐵匠檯（功能性建物判斷，不看技能）---
            //   使用者定調：不用 pawn 工藝技能判斷鐵匠身份。「身份＝功能」＝你有沒有鍛造的傢伙什（鐵匠檯/機工檯）。
            //   有爐子 → 能接鐵匠屋委託（技能高低影響的是做得出多好的貨，不是能不能接單）。
            editor.SetBool("RatkinQL_CanForge", HasForgeBench());
        }

        // 殖民地是否有鍛造用工作檯（鼠族／原版鐵匠檯、機工檯）。用建物＝功能性判斷，不受 pawn 外出 caravan 影響。
        private static readonly string[] ForgeBenchDefs =
        {
            "RK_FueledSmithy", "RK_ElectricSmithy",         // 鼠族鐵匠檯（NewRatkinPlus）
            "FueledSmithy", "ElectricSmithy",               // 原版鐵匠檯
            "TableMachining",                               // 原版機工檯（槍械）
        };

        private bool HasForgeBench()
        {
            for (int i = 0; i < ForgeBenchDefs.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ForgeBenchDefs[i]);
                if (def != null && map.listerBuildings.ColonistsHaveBuilding(def))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
