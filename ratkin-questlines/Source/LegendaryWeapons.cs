using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using QuestEditor_Library;

namespace RatkinQuestlines
{
    // §6.9 名匠傳奇武器：交出頂級武器 → 客戶為它命名 → 入名冊 → 之後聽到它的傳說。
    // ------------------------------------------------------------------
    // 設計權威：brainstorm/6b-forge-design-increment.md §6.9（使用者定案）。
    // 三段機制：
    //   1. 頂級門檻交貨＝沿用既有 DialogCondition_WeaponValue / Dialog_ForgeDelivery，只是在最高 band 上
    //      掛 <nameLegendary>true</nameLegendary>（見 ForgeDelivery.cs），不新增交貨路徑。
    //   2. 命名＋登記＝本檔。交貨視窗消費武器**之前**先把最值錢的那把快照下來（def/stuff/quality），
    //      生一個鼠族風格的名字，寫進 GameComponent_LegendaryWeapons 的名冊並持久化。
    //   3. 衍生任務＝本檔的 QuestNode_LegendaryWeaponTale。說書人挑中 QuestScript_LegendaryWeaponTale 時，
    //      從名冊隨機抽一把已命名武器，發一封「聽聞你鑄的『XX』……」的傳聞信，並回饋軍械商名聲。
    //
    // 為什麼名字要走 Keyed 而不是硬寫中文：本包是三語包（healthcheck 會驗三語 key 齊）。
    //   命名素材（靈獸／天象／事功前綴、器名、組合式）全部放 Keyed，用 '|' 分隔成池，
    //   英文語系就能給出英文風格的名字，不會讓英文玩家看到一串中文。

    // 名冊裡的一筆：一把被客戶命名、從此有自己傳說的武器。
    public class LegendaryWeaponRecord : IExposable
    {
        public string name;             // 生成的名字（已在地化，命名當下決定，之後不再變）
        public string defName;          // 武器 ThingDef
        public string stuffDefName;     // 材質（可能為空：不吃材質的武器）
        public QualityCategory quality;
        public int birthTick;           // 誕生日（交貨當下的 TicksGame）
        public string clientKey;        // 哪位客戶帶走的（ForgeClientTier 的 id，例 CrownProcurer）
        public int talesTold;           // 已經衍生過幾次傳聞（用來壓低重複抽中同一把的機率）

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref stuffDefName, "stuffDefName");
            Scribe_Values.Look(ref quality, "quality", QualityCategory.Normal);
            Scribe_Values.Look(ref birthTick, "birthTick", 0);
            Scribe_Values.Look(ref clientKey, "clientKey");
            Scribe_Values.Look(ref talesTold, "talesTold", 0);
        }

        // 武器本身的顯示名（在地化 label）。def 不在（那個 mod 被移除了）時退回 defName，不噴紅字。
        public string WeaponLabel
        {
            get
            {
                ThingDef d = string.IsNullOrEmpty(defName) ? null : DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (d == null)
                {
                    return defName ?? "?";
                }
                ThingDef s = string.IsNullOrEmpty(stuffDefName) ? null : DefDatabase<ThingDef>.GetNamedSilentFail(stuffDefName);
                return s != null ? GenLabel.ThingLabel(d, s, 1) : d.label;
            }
        }
    }

    // 鼠族風格的傳奇武器命名。素材全部由 Keyed 提供，'|' 分隔。
    public static class LegendaryWeaponNamer
    {
        // 器類：決定用哪個「器名」池。用 defName 關鍵字判，比翻 verbClass 穩，也不依賴任何前置 mod。
        private enum Kind { Blade, Spear, Axe, Hammer, Bow, Gun, Generic }

        private static readonly string[] BladeWords = { "Sword", "Blade", "Dagger", "Rapier", "Knife", "Scythe", "Bayonet" };
        private static readonly string[] SpearWords = { "Spear", "Lance", "Halberd", "Fork", "Gunlance" };
        private static readonly string[] AxeWords = { "Axe", "Cleaver", "Pickaxe" };
        private static readonly string[] HammerWords = { "Mace", "Hammer", "Maul", "Flail" };
        private static readonly string[] BowWords = { "Crossbow", "Bow", "Arbalest" };

        private static bool Has(string s, string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (s.IndexOf(words[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static Kind KindOf(ThingDef def)
        {
            if (def == null)
            {
                return Kind.Generic;
            }
            string n = def.defName ?? "";
            // 銃槍這種近戰＋砲擊的混種，關鍵字順序決定歸類：先看槍矛再看刀刃。
            if (Has(n, SpearWords))
            {
                return Kind.Spear;
            }
            if (Has(n, BowWords))
            {
                return Kind.Bow;
            }
            if (Has(n, AxeWords))
            {
                return Kind.Axe;
            }
            if (Has(n, HammerWords))
            {
                return Kind.Hammer;
            }
            if (Has(n, BladeWords))
            {
                return Kind.Blade;
            }
            return def.IsRangedWeapon ? Kind.Gun : Kind.Generic;
        }

        private static string NounKey(Kind k)
        {
            switch (k)
            {
                case Kind.Blade: return "RatkinQL_Legendary_NounBlade";
                case Kind.Spear: return "RatkinQL_Legendary_NounSpear";
                case Kind.Axe: return "RatkinQL_Legendary_NounAxe";
                case Kind.Hammer: return "RatkinQL_Legendary_NounHammer";
                case Kind.Bow: return "RatkinQL_Legendary_NounBow";
                case Kind.Gun: return "RatkinQL_Legendary_NounGun";
                default: return "RatkinQL_Legendary_NounGeneric";
            }
        }

        // 品質決定前綴的氣派：傳奇＝靈獸賦名、傑作＝天象、其餘＝事功／志節。
        private static string PrefixKey(QualityCategory q)
        {
            if (q >= QualityCategory.Legendary)
            {
                return "RatkinQL_Legendary_PoolBeast";
            }
            if (q >= QualityCategory.Masterwork)
            {
                return "RatkinQL_Legendary_PoolSky";
            }
            return "RatkinQL_Legendary_PoolDeed";
        }

        // 讀一個 '|' 分隔的 Keyed 池。key 不存在時 Translate 會回傳 key 本身，這裡當成單一元素用，不炸。
        private static string[] Pool(string key)
        {
            string raw = key.Translate();
            if (string.IsNullOrEmpty(raw))
            {
                return new[] { key };
            }
            string[] parts = raw.Split('|');
            List<string> clean = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length > 0)
                {
                    clean.Add(p);
                }
            }
            return clean.Count > 0 ? clean.ToArray() : new[] { raw };
        }

        public static string Generate(ThingDef def, QualityCategory quality)
        {
            string[] prefixes = Pool(PrefixKey(quality));
            string[] nouns = Pool(NounKey(KindOf(def)));
            string[] patterns = Pool("RatkinQL_Legendary_Patterns");

            string prefix = prefixes[Rand.Range(0, prefixes.Length)];
            string noun = nouns[Rand.Range(0, nouns.Length)];
            string pattern = patterns[Rand.Range(0, patterns.Length)];

            // pattern 用 %P=前綴、%N=器名。作者若只寫 %P 就是「獨名」（例：不悔）。
            //   ⚠ 刻意不用 {0}/{1}：那是 RimWorld Translate 自己的佔位符語法，
            //     混用會讓翻譯系統嘗試解析並記警告。這裡純字串替換，不碰它的 formatter。
            string result = pattern.Replace("%P", prefix).Replace("%N", noun);
            return result.Length > 0 ? result : prefix + noun;
        }
    }

    // 傳奇武器名冊（持久化）。GameComponent 由 Game.FillComponents() 自動實例化，無需註冊。
    public class GameComponent_LegendaryWeapons : GameComponent
    {
        private const int IntervalTicks = 500;   // 回寫 gate bool 的頻率，與 F8 帳本同節奏
        private const int MaxRegistry = 64;      // 上限：超過就汰換最舊且已被說過的，避免存檔無限膨脹

        private List<LegendaryWeaponRecord> registry = new List<LegendaryWeaponRecord>();

        public GameComponent_LegendaryWeapons(Game game) { }

        public static GameComponent_LegendaryWeapons Component
        {
            get
            {
                Game g = Current.Game;
                return g == null ? null : g.GetComponent<GameComponent_LegendaryWeapons>();
            }
        }

        public List<LegendaryWeaponRecord> Registry
        {
            get { return registry; }
        }

        // 交貨當下呼叫：把這把武器命名並入冊。回傳新紀錄（呼叫端拿去發信）；t 為 null 或非武器則回 null。
        // ⚠ 必須在 Destroy 之前呼叫——Destroy 後 comps 讀不到品質。
        public LegendaryWeaponRecord Register(Thing t, string clientKey)
        {
            if (t == null || t.def == null)
            {
                return null;
            }
            QualityCategory q = QualityCategory.Normal;
            CompQuality cq = t.TryGetComp<CompQuality>();
            if (cq != null)
            {
                q = cq.Quality;
            }
            LegendaryWeaponRecord rec = new LegendaryWeaponRecord();
            rec.defName = t.def.defName;
            rec.stuffDefName = t.Stuff != null ? t.Stuff.defName : null;
            rec.quality = q;
            rec.birthTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            rec.clientKey = clientKey;
            rec.name = LegendaryWeaponNamer.Generate(t.def, q);
            registry.Add(rec);
            Trim();
            return rec;
        }

        // 說書人衍生任務用：挑一把已命名武器。已經被說過的傳說權重降低，讓新鑄的比較容易被提起。
        public LegendaryWeaponRecord PickForTale()
        {
            if (registry.Count == 0)
            {
                return null;
            }
            float total = 0f;
            for (int i = 0; i < registry.Count; i++)
            {
                total += Weight(registry[i]);
            }
            if (total <= 0f)
            {
                return registry[Rand.Range(0, registry.Count)];
            }
            float roll = Rand.Range(0f, total);
            for (int i = 0; i < registry.Count; i++)
            {
                roll -= Weight(registry[i]);
                if (roll <= 0f)
                {
                    return registry[i];
                }
            }
            return registry[registry.Count - 1];
        }

        private static float Weight(LegendaryWeaponRecord r)
        {
            return 1f / (1f + r.talesTold);
        }

        private void Trim()
        {
            while (registry.Count > MaxRegistry)
            {
                int drop = 0;
                for (int i = 1; i < registry.Count; i++)
                {
                    // 先汰換已經被說過、且最早誕生的
                    if (registry[i].talesTold > registry[drop].talesTold
                        || (registry[i].talesTold == registry[drop].talesTold && registry[i].birthTick < registry[drop].birthTick))
                    {
                        drop = i;
                    }
                }
                registry.RemoveAt(drop);
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null || Find.TickManager.TicksGame % IntervalTicks != 0)
            {
                return;
            }
            GameComponent_Editor editor = GameComponent_Editor.Component;
            if (editor == null)
            {
                return;
            }
            // 傳聞任務的 gate：名冊非空才讓 QuestScript_LegendaryWeaponTale 進說書人的池。
            editor.SetBool("RatkinQL_HasLegendaryWeapon", registry.Count > 0);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref registry, "RatkinQL_legendaryWeapons", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && registry == null)
            {
                registry = new List<LegendaryWeaponRecord>();
            }
        }
    }

    // 守衛＋抽籤節點：名冊非空時，抽一把已命名武器出來，掛一個發傳聞信的 QuestPart。
    // ------------------------------------------------------------------
    // 為什麼名字走「QuestPart 發信」而不是塞進 questDescriptionRules：本包既有的任務描述全是靜態字串，
    //   沒有用過 slate 變數代換。若代換不成功，玩家會看到字面的 [legendaryName]。
    //   沿用 QuestNode_DropTechBlueprintOnce 已驗證的作法（自己發 Letter、參數用 Translate 代入）最穩。
    public class QuestNode_LegendaryWeaponTale : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> inSignal;
        public string letterLabel = "RatkinQL_Legendary_TaleLabel";
        public string taleKeys = "RatkinQL_Legendary_Tale1|RatkinQL_Legendary_Tale2|RatkinQL_Legendary_Tale3|RatkinQL_Legendary_Tale4";
        public string evFlag = "RatkinQL_Ev_WeaponDeal";   // 傳說傳開＝軍械商名聲回饋

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_LegendaryWeaponTale part = QuestGen.quest.AddPart<QuestPart_LegendaryWeaponTale>();
            part.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate))
                            ?? slate.Get<string>("inSignal", null, false);
            part.letterLabel = letterLabel;
            part.taleKeys = taleKeys;
            part.evFlag = evFlag;
        }

        // 名冊空的時候整條劇本不進池——說書人就不會挑到它。
        protected override bool TestRunInt(Slate slate)
        {
            GameComponent_LegendaryWeapons comp = GameComponent_LegendaryWeapons.Component;
            return comp != null && comp.Registry.Count > 0;
        }
    }

    public class QuestPart_LegendaryWeaponTale : QuestPart
    {
        public string inSignal;
        public string letterLabel;
        public string taleKeys;
        public string evFlag;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag != inSignal)
            {
                return;
            }
            Tell();
        }

        private void Tell()
        {
            GameComponent_LegendaryWeapons comp = GameComponent_LegendaryWeapons.Component;
            if (comp == null)
            {
                return;
            }
            LegendaryWeaponRecord rec = comp.PickForTale();
            if (rec == null)
            {
                return;   // 抽不到就靜默結束，不發空信
            }
            rec.talesTold++;

            string[] keys = (taleKeys ?? "").Split('|');
            string key = keys.Length > 0 ? keys[Rand.Range(0, keys.Length)].Trim() : null;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            // {0}=武器名、{1}=武器種類 label、{2}=品質、{3}=誕生至今的天數
            int days = Find.TickManager != null
                ? Math.Max(0, (Find.TickManager.TicksGame - rec.birthTick) / GenDate.TicksPerDay)
                : 0;
            string text = key.Translate(rec.name, rec.WeaponLabel, rec.quality.GetLabel(), days);
            Find.LetterStack.ReceiveLetter(
                (letterLabel ?? "RatkinQL_Legendary_TaleLabel").Translate(rec.name),
                text,
                LetterDefOf.PositiveEvent);

            GameComponent_Editor ed = GameComponent_Editor.Component;
            if (ed != null && !string.IsNullOrEmpty(evFlag))
            {
                ed.SetBool(evFlag, true);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref letterLabel, "letterLabel");
            Scribe_Values.Look(ref taleKeys, "taleKeys");
            Scribe_Values.Look(ref evFlag, "evFlag");
        }
    }
}
