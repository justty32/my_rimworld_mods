using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // F8 善惡值＋分類名聲帳本（morality / reputation ledger）。
    // ------------------------------------------------------------------
    // 目的：讓「玩家在任務線中的選擇」與「完成的任務種類」累積成可查詢的傾向，
    // 據此軟性地開合後續任務（例：做多了會降善惡的暗殺 → 託孤類任務變難觸發）。
    //
    // 為何要 C#：CQF 全域狀態只有 bool（GameComponent_Editor.SetBool/GetBool），數不了「累積」。
    // 所以沿用 F7 側寫器的成功模式——由 C# 持有數值、回寫「階層 bool」給對話端 DialogCondition_Bool 讀。
    //
    // 橋接（對話→C#）：對話選項用 CQFAction_SetGlobalBool 設一個「事件旗標」RatkinQL_Ev_*（一次性）。
    //   本帳本每 IntervalTicks 掃這些事件旗標；只要為 true 就把對應 delta 累加進 karma / rep，
    //   然後把事件旗標重置為 false（消費掉，避免重複累加）。接著回寫階層 bool。
    //
    // 軟性、非硬鎖：清楚的高低 → 確定性階層 bool；邊界個案（善惡值靠近門檻）→ 每 2500 tick 擲骰寫 gate bool，
    //   availability 隨聚居點傾向自然開合＝「用幾率、不要限太死」。
    //
    // GameComponent 由 Game.FillComponents() 自動實例化（同 MapComponent），無需註冊；ExposeData 持久化數值。
    public class GameComponent_RatkinLedger : GameComponent
    {
        private const int IntervalTicks = 500;   // 事件消費＋階層回寫頻率（約 8 秒），成本極低
        private const int SoftRollTicks = 2500;  // 軟性 gate 擲骰頻率（約遊戲內一小時）

        // 鐵匠屋層級門檻（weaponMerchant 名聲）與 per-客戶忠誠獎勵門檻。先拍一組數值，實機調手感。
        private const int ForgeM1 = 40;          // T2 進階解鎖
        private const int ForgeM2 = 120;         // T3 王庭解鎖
        private const int ForgeRewardN = 3;      // 「次次做好」連續達 N 次 → 該客戶專屬神祕獎勵（roster 6a Q3 定案 N=3）

        // 鐵匠屋具名客戶名冊（id → 所屬層；與 brainstorm/6a-forge-client-roster.md 對齊）。
        // 新增客戶＝在此加一列；per-客戶狀態（滿意度/blocked）與回寫 bool 全自動化。
        private static readonly Dictionary<string, int> ForgeClientTier = new Dictionary<string, int>
        {
            { "AcornVillage", 1 },      // T1：橡實村·村長板栗
            { "FluoriteHamlet", 1 },    // T1：螢石塢·村長可可（內容待鋪）
            { "ObsidianTown", 2 },      // T2：黑曜城武備官·懸鈴
            { "AcaciaGuild", 2 },       // T2：金合歡商行·行首金合歡
            { "RedClawBand", 2 },       // T2：赤爪傭兵團·團長鋼鐵之誓
            { "KnightCommission", 2 },  // T2：白銀之護·露娜·朱頂紅（騎士傑作訂製）
            { "CrownProcurer", 3 },     // T3：王國採購官·映山紅（量大薄利、失手掉派系）
            { "FrontierGeneral", 3 },   // T3（選配）：日食·探尋者·邊疆守將（傳奇單件）
        };

        private int karma;                        // 善惡值：正=善，負=惡
        private Dictionary<string, int> rep = new Dictionary<string, int>();  // 分類名聲累積
        private Dictionary<string, int> forgeSat = new Dictionary<string, int>();   // per-客戶滿意度計數（好好完成一次 +1）
        private Dictionary<string, bool> forgeBlocked = new Dictionary<string, bool>(); // per-客戶 blocked（被敲詐後不再上門）

        public GameComponent_RatkinLedger(Game game) { }

        // 事件旗標 → (統計項, delta) 對照表。統計項 "karma" 進 karma，其餘進 rep[stat]。
        // 對話端只要 CQFAction_SetGlobalBool 設下列任一 key=true，本帳本就會累加對應數值。
        // （用 KeyValuePair 陣列而非 ValueTuple——相容較舊的 mcs 編譯器。）
        private static KeyValuePair<string, int> D(string stat, int delta)
        {
            return new KeyValuePair<string, int>(stat, delta);
        }

        private static readonly Dictionary<string, KeyValuePair<string, int>[]> EventTable =
            new Dictionary<string, KeyValuePair<string, int>[]>
            {
                { "RatkinQL_Ev_Charity",     new[] { D("karma", 10), D("merchant", 3) } },     // 慷慨資助/施捨
                { "RatkinQL_Ev_Mercy",       new[] { D("karma", 6) } },                        // 手下留情/寬待
                { "RatkinQL_Ev_ProtectWeak", new[] { D("karma", 8) } },                        // 保護弱者（救難民/擊退追兵）
                { "RatkinQL_Ev_GenericTrade",new[] { D("merchant", 8) } },                     // 一般公平交易
                { "RatkinQL_Ev_FoodDeal",    new[] { D("foodMerchant", 12), D("merchant", 6) } },   // 農產/糧食買賣
                { "RatkinQL_Ev_WeaponDeal",  new[] { D("weaponMerchant", 12), D("merchant", 6) } }, // 軍械買賣
                { "RatkinQL_Ev_MercWork",    new[] { D("mercenary", 12), D("karma", -2) } },   // 受僱武力工作
                { "RatkinQL_Ev_Cruelty",     new[] { D("karma", -8), D("merchant", 4) } },     // 趁人之危/壓價宰客
                { "RatkinQL_Ev_Betray",      new[] { D("karma", -12) } },                      // 背信
                { "RatkinQL_Ev_Assassin",    new[] { D("assassin", 12), D("karma", -8) } },    // 暗殺委託
            };

        public override void GameComponentTick()
        {
            int t = Find.TickManager.TicksGame;
            if (t % IntervalTicks != 0)
            {
                return;
            }
            GameComponent_Editor editor = GameComponent_Editor.Component;
            if (editor == null)
            {
                return;
            }

            // (1) 消費事件旗標 → 累加數值
            foreach (KeyValuePair<string, KeyValuePair<string, int>[]> ev in EventTable)
            {
                if (!editor.GetBool(ev.Key))
                {
                    continue;
                }
                foreach (KeyValuePair<string, int> d in ev.Value)
                {
                    if (d.Key == "karma")
                    {
                        karma = Mathf.Clamp(karma + d.Value, -200, 200);
                    }
                    else
                    {
                        int cur;
                        rep.TryGetValue(d.Key, out cur);
                        rep[d.Key] = Mathf.Clamp(cur + d.Value, 0, 500);
                    }
                }
                editor.SetBool(ev.Key, false);   // 消費掉，避免重複累加
            }

            // (2) 回寫確定性階層 bool（供 DialogCondition_Bool / genrationConditions 讀）
            editor.SetBool("RatkinQL_Karma_Good", karma >= 15);
            editor.SetBool("RatkinQL_Karma_Evil", karma <= -15);
            editor.SetBool("RatkinQL_Karma_NotEvil", karma > -15);
            editor.SetBool("RatkinQL_Rep_Merchant", Rep("merchant") >= 25);
            editor.SetBool("RatkinQL_Rep_WeaponMerchant", Rep("weaponMerchant") >= 25);
            editor.SetBool("RatkinQL_Rep_FoodMerchant", Rep("foodMerchant") >= 25);
            editor.SetBool("RatkinQL_Rep_Mercenary", Rep("mercenary") >= 25);
            editor.SetBool("RatkinQL_Rep_Assassin", Rep("assassin") >= 25);

            // (2b) 鐵匠屋層級制：分級名聲門檻 ＋ per-客戶關係狀態（滿意度/blocked）。
            //   對話端每次交貨用 CQFAction_SetGlobalBool 設 per-客戶事件旗標；本帳本消費它、累加狀態、回寫 gate bool。
            int wm = Rep("weaponMerchant");
            editor.SetBool("RatkinQL_Forge_T1Unlocked", true);            // T1 基層：開局起
            editor.SetBool("RatkinQL_Forge_T2Unlocked", wm >= ForgeM1);   // T2 進階：名聲達 M1
            editor.SetBool("RatkinQL_Forge_T3Unlocked", wm >= ForgeM2);   // T3 王庭：名聲達 M2
            foreach (KeyValuePair<string, int> client in ForgeClientTier)
            {
                string id = client.Key;

                // (a) 消費 per-客戶事件旗標
                string wellDone = "RatkinQL_Ev_ForgeWellDone_" + id;   // 好好完成一次委託
                if (editor.GetBool(wellDone))
                {
                    int s;
                    forgeSat.TryGetValue(id, out s);
                    forgeSat[id] = s + 1;
                    rep["weaponMerchant"] = Mathf.Clamp(Rep("weaponMerchant") + 12, 0, 500); // 完成即提供軍械商名聲
                    editor.SetBool(wellDone, false);
                }
                string block = "RatkinQL_Ev_ForgeBlocked_" + id;       // 被敲詐→此客戶不再上門
                if (editor.GetBool(block))
                {
                    forgeBlocked[id] = true;
                    rep["weaponMerchant"] = Mathf.Clamp(Rep("weaponMerchant") - 10, 0, 500);  // 敲詐降名聲
                    karma = Mathf.Clamp(karma - 4, -200, 200);
                    editor.SetBool(block, false);
                }

                // (b) 回寫 gate bool：所屬層已解鎖 且 未 blocked → 委託信可進池；滿意度達 N → 專屬神祕獎勵就緒
                bool tierOk = TierUnlocked(client.Value, wm);
                bool blocked;
                forgeBlocked.TryGetValue(id, out blocked);
                editor.SetBool("RatkinQL_Forge_" + id + "_Available", tierOk && !blocked);
                int sat;
                forgeSat.TryGetValue(id, out sat);
                editor.SetBool("RatkinQL_Forge_" + id + "_RewardReady", sat >= ForgeRewardN);
            }

            // (3) 軟性 gate：邊界個案用幾率。託孤/收養類任務——善惡值越低越難觸發，
            //     且暗殺名聲越高越壓抑（做多降善惡的暗殺 → 託孤自然難來）。每 2500 tick 擲一次。
            if (t % SoftRollTicks == 0)
            {
                float chance = Mathf.Clamp01((karma + 25f) / 45f);           // karma -25→0%，+20→100%
                chance *= Mathf.Clamp01(1f - Rep("assassin") / 100f);        // 暗殺名聲抑制
                editor.SetBool("RatkinQL_Soft_OrphanOk", Rand.Chance(chance));
            }
        }

        private int Rep(string k)
        {
            int v;
            rep.TryGetValue(k, out v);
            return v;
        }

        // 某層是否已解鎖：T1 開局起、T2 名聲達 M1、T3 達 M2。
        private static bool TierUnlocked(int tier, int weaponMerchantRep)
        {
            if (tier <= 1)
            {
                return true;
            }
            if (tier == 2)
            {
                return weaponMerchantRep >= ForgeM1;
            }
            return weaponMerchantRep >= ForgeM2;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref karma, "RatkinQL_karma", 0);
            Scribe_Collections.Look(ref rep, "RatkinQL_rep", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref forgeSat, "RatkinQL_forgeSat", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref forgeBlocked, "RatkinQL_forgeBlocked", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (rep == null)
                {
                    rep = new Dictionary<string, int>();
                }
                if (forgeSat == null)
                {
                    forgeSat = new Dictionary<string, int>();
                }
                if (forgeBlocked == null)
                {
                    forgeBlocked = new Dictionary<string, bool>();
                }
            }
        }
    }
}
