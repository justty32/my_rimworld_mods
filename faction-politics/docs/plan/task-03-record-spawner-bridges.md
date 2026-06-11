# Task 3: RebelRecord + PoliticsBridges + RebelSpawner + 翻譯 keys

**Files:**
- Create: `faction-politics/Source/Data/RebelRecord.cs`
- Create: `faction-politics/Source/Compat/PoliticsBridges.cs`
- Create: `faction-politics/Source/World/RebelSpawner.cs`
- Create: `faction-politics/Languages/English/Keyed/FactionPolitics.xml`
- Create: `faction-politics/Languages/ChineseTraditional/Keyed/FactionPolitics.xml`

- [ ] **Step 1: Source/Data/RebelRecord.cs**

```csharp
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.politics
{
    /// <summary>一個派系的反叛追蹤（權威資料；previouslyGeneratedInhabitants 只是請回場的橋）。</summary>
    public class RebelRecord : IExposable
    {
        public Faction faction;
        public Pawn rebel;
        public Settlement homeSettlement;
        public float progress;
        /// <summary>生成時自 profile.progressPerDay 擲定，每反叛者步調不同。</summary>
        public float ratePerDay;
        /// <summary>-1 = 反叛者在世；>=0 = 死亡冷卻，到期重生。</summary>
        public int respawnAtTick = -1;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref rebel, "rebel");
            Scribe_References.Look(ref homeSettlement, "homeSettlement");
            Scribe_Values.Look(ref progress, "progress");
            Scribe_Values.Look(ref ratePerDay, "ratePerDay");
            Scribe_Values.Look(ref respawnAtTick, "respawnAtTick", -1);
        }
    }
}
```

- [ ] **Step 2: Source/Compat/PoliticsBridges.cs**（軟相容 hook 點；主 DLL 不認識任何第三方型別）

```csharp
using System;
using RimWorld;
using RimWorld.Planet;

namespace pas.politics
{
    /// <summary>bridge 註冊點。npc-outposts bridge（條件 assembly）與 Rim War bridge（反射）在
    /// StaticConstructorOnStartup 期掛入；無 bridge 時全部 no-op。</summary>
    public static class PoliticsBridges
    {
        /// <summary>衛星聚落（如 npc-outposts 的哨站）判定——不被抽為倒戈對象、不計入聚落數、不當反叛者駐地。
        /// 注意：本檔禁止出現衛星型別的類名字串（健檢第 7 檢把關軟相容不變式）。</summary>
        public static Func<Settlement, bool> IsSatelliteResolver;

        /// <summary>(倒戈聚落, 母派系, 新派系)：每筆倒戈後觸發（哨站跟隨、Rim War 同步）。</summary>
        public static Action<Settlement, Faction, Faction> SettlementDefected;

        /// <summary>(母派系, 新派系)：分裂完成後觸發。</summary>
        public static Action<Faction, Faction> FactionSplit;

        public static bool IsSatellite(Settlement settlement)
        {
            return IsSatelliteResolver != null && IsSatelliteResolver(settlement);
        }

        public static void NotifySettlementDefected(Settlement settlement, Faction mother, Faction newFaction)
        {
            SettlementDefected?.Invoke(settlement, mother, newFaction);
        }

        public static void NotifyFactionSplit(Faction mother, Faction newFaction)
        {
            FactionSplit?.Invoke(mother, newFaction);
        }
    }
}
```

- [ ] **Step 3: Source/World/RebelSpawner.cs**

```csharp
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.politics
{
    /// <summary>反叛者生成：world pawn KeepForever + 記入駐地 previouslyGeneratedInhabitants
    /// （原版 1.6 從不自動填這清單——PawnGenerator.cs:236 死碼——我們是唯一供給者）。</summary>
    public static class RebelSpawner
    {
        /// <summary>給派系生成首個反叛者並建 record；失敗（無聚落/無 kind）回 null。</summary>
        public static RebelRecord TrySpawnFor(Faction faction, RebellionProfileDef profile)
        {
            Settlement home = PickHome(faction);
            Pawn rebel = ((home != null) ? GeneratePawn(faction) : null);
            if (rebel == null)
            {
                return null;
            }
            Find.WorldPawns.PassToWorld(rebel, PawnDiscardDecideMode.KeepForever);
            home.previouslyGeneratedInhabitants.Add(rebel);
            RebelRecord record = new RebelRecord
            {
                faction = faction,
                rebel = rebel,
                homeSettlement = home,
                progress = 0f,
                ratePerDay = profile.progressPerDay.RandomInRange,
                respawnAtTick = -1
            };
            Find.LetterStack.ReceiveLetter("pas_politics_RebelEmergedLabel".Translate(rebel.LabelShortCap),
                "pas_politics_RebelEmergedText".Translate(rebel.LabelShortCap, faction.Name, home.Name),
                LetterDefOf.NeutralEvent, home);
            return record;
        }

        /// <summary>冷卻到期重生（沿用 record；不發信，鎮壓循環不刷信箱）。</summary>
        public static void Respawn(RebelRecord record, RebellionProfileDef profile)
        {
            Settlement home = PickHome(record.faction);
            Pawn rebel = ((home != null) ? GeneratePawn(record.faction) : null);
            if (rebel == null)
            {
                return;
            }
            Find.WorldPawns.PassToWorld(rebel, PawnDiscardDecideMode.KeepForever);
            home.previouslyGeneratedInhabitants.Add(rebel);
            record.rebel = rebel;
            record.homeSettlement = home;
            record.progress = 0f;
            record.ratePerDay = profile.progressPerDay.RandomInRange;
            record.respawnAtTick = -1;
        }

        /// <summary>挑駐地：該派系非衛星聚落隨機一個；無則 null。</summary>
        public static Settlement PickHome(Faction faction)
        {
            List<Settlement> all = Find.WorldObjects.Settlements;
            List<Settlement> candidates = new List<Settlement>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Faction == faction && !PoliticsBridges.IsSatellite(all[i]))
                {
                    candidates.Add(all[i]);
                }
            }
            return candidates.TryRandomElement(out Settlement result) ? result : null;
        }

        private static Pawn GeneratePawn(Faction faction)
        {
            PawnKindDef kind = faction.def.basicMemberKind;
            if (kind == null || !kind.RaceProps.Humanlike)
            {
                return null;
            }
            PawnGenerationRequest request = new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer);
            return PawnGenerator.GeneratePawn(request);
        }
    }
}
```

（`PawnGenerationRequest` 建構形以 task-0 #4 驗證結果為準，不符就地修。）

- [ ] **Step 4: Languages/English/Keyed/FactionPolitics.xml**（四個 key 一次到位，task-4 的分裂信共用）

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <pas_politics_RebelEmergedLabel>Rebel stirs: {0}</pas_politics_RebelEmergedLabel>
  <pas_politics_RebelEmergedText>{0} of {1} has begun plotting rebellion from {2}. Their influence will grow with time — unless someone removes them.</pas_politics_RebelEmergedText>
  <pas_politics_SplitLetterLabel>Faction split: {0}</pas_politics_SplitLetterLabel>
  <pas_politics_SplitLetterText>{0}, a rebel within {1}, has declared independence. The new faction {2} seized {3} settlement(s) and is hostile to its former rulers.</pas_politics_SplitLetterText>
</LanguageData>
```

- [ ] **Step 5: Languages/ChineseTraditional/Keyed/FactionPolitics.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <pas_politics_RebelEmergedLabel>反叛者蠢動：{0}</pas_politics_RebelEmergedLabel>
  <pas_politics_RebelEmergedText>{1}的{0}開始在{2}密謀反叛。其影響力將隨時間增長——除非有人除掉他。</pas_politics_RebelEmergedText>
  <pas_politics_SplitLetterLabel>派系分裂：{0}</pas_politics_SplitLetterLabel>
  <pas_politics_SplitLetterText>{1}內的反叛者{0}宣布獨立。新派系{2}奪取了 {3} 座聚落，並與舊主敵對。</pas_politics_SplitLetterText>
</LanguageData>
```

- [ ] **Step 6: 建置驗證**

Run: `dotnet build C:\code\mine\my_rimworld_mods\faction-politics\Source\FactionPolitics.csproj`
Expected: 0 Warning(s) 0 Error(s)

- [ ] **Step 7: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add faction-politics/Source faction-politics/Languages faction-politics/1.6
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: faction-politics 反叛者生成 + bridge hook 點 + 翻譯

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
