# Task 4: NpcOutpost 世界物件 + 攻擊流程 + WorldObjectDef XML + Languages

**Files:**
- Create: `npc-outposts/Source/World/NpcOutpost.cs`
- Create: `npc-outposts/Source/World/OutpostAttackUtility.cs`
- Create: `npc-outposts/Defs/WorldObjectDefs/Outposts.xml`
- Create: `npc-outposts/Languages/English/Keyed/NpcOutposts.xml`
- Create: `npc-outposts/Languages/ChineseTraditional/Keyed/NpcOutposts.xml`

- [ ] **Step 1: NpcOutpost.cs**

```csharp
using System.Collections.Generic;
using System.Linq;
using pas.sims;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace pas.outposts
{
    /// <summary>NPC 派系哨站。繼承 Settlement 白嫖交易/送禮/擊敗判定；
    /// 拜訪與攻打 override 成小圖流程；ExtraGenStepDefs 注入守軍 trim。</summary>
    public class NpcOutpost : Settlement
    {
        private static readonly Texture2D AttackCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/AttackSettlement");

        private Settlement parentSettlement;
        private OutpostTypeDef typeDef;

        public Settlement ParentSettlement => parentSettlement;

        public OutpostTypeDef TypeDef => typeDef;

        public IntVec3 OutpostMapSize => typeDef?.mapSize ?? new IntVec3(150, 1, 150);

        public override MapGeneratorDef MapGeneratorDef => typeDef?.mapGeneratorDef ?? base.MapGeneratorDef;

        public void Setup(OutpostTypeDef type, Settlement parent)
        {
            typeDef = type;
            parentSettlement = parent;
        }

        /// <summary>所有生圖路徑（拜訪/攻打/任務）都會帶上守軍 trim（GetOrGenerateMapUtility.cs:26 concat）。</summary>
        public override IEnumerable<GenStepWithParams> ExtraGenStepDefs
        {
            get
            {
                foreach (GenStepWithParams step in base.ExtraGenStepDefs)
                {
                    yield return step;
                }
                yield return new GenStepWithParams(OutpostDefOf.pas_outposts_TrimDefenders, default(GenStepParams));
            }
        }

        /// <summary>重組 float menu：原版停格拜訪（交易用）+ 進入小圖（sims-mode ArrivalAction）+ 送禮 + 小圖攻擊。
        /// 不呼叫 base——Settlement 的攻擊選項走全尺寸圖。</summary>
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption option in CaravanArrivalAction_VisitSettlement.GetFloatMenuOptions(caravan, this))
            {
                yield return option;
            }
            foreach (FloatMenuOption option in CaravanArrivalAction_VisitMap.GetFloatMenuOptions(caravan, this, OutpostMapSize))
            {
                yield return option;
            }
            foreach (FloatMenuOption option in CaravanArrivalAction_OfferGifts.GetFloatMenuOptions(caravan, this))
            {
                yield return option;
            }
            foreach (FloatMenuOption option in OutpostAttackUtility.GetFloatMenuOptions(caravan, this))
            {
                yield return option;
            }
        }

        /// <summary>重組 caravan gizmo：交易/送禮照抄 Settlement.cs:313-326，攻擊換小圖流程。</summary>
        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            if (CanTradeNow && CaravanVisitUtility.SettlementVisitedNow(caravan) == this)
            {
                yield return CaravanVisitUtility.TradeCommand(caravan, Faction, TraderKind);
            }
            if ((bool)CaravanArrivalAction_OfferGifts.CanOfferGiftsTo(caravan, this))
            {
                yield return FactionGiftUtility.OfferGiftsCommand(caravan, this);
            }
            if (Attackable)
            {
                yield return new Command_Action
                {
                    icon = AttackCommandTex,
                    defaultLabel = "CommandAttackSettlement".Translate(),
                    defaultDesc = "CommandAttackSettlementDesc".Translate(),
                    action = delegate
                    {
                        OutpostAttackUtility.Attack(caravan, this);
                    }
                };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref parentSettlement, "pas_parentSettlement");
            Scribe_Defs.Look(ref typeDef, "pas_typeDef");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && typeDef == null)
            {
                typeDef = DefDatabase<OutpostTypeDef>.AllDefsListForReading.FirstOrDefault();
            }
        }
    }
}
```

注意：
- 不 override `Visitable`/`Attackable`/`CanTradeNow`/`TickInterval`——交易、擊敗判定（`Settlement.cs:196`）全繼承。
- `parentSettlement` 母聚落被毀後 `Scribe_References` 自然回 null（孤站保留，spec §4）。
- 跳過 base.GetFloatMenuOptions / base.GetCaravanGizmos 會丟失 comp 注入選項——本 mod 的 WorldObjectDef 不掛 comp（sims-mode 的 visit comp 只 patch 原版 `Settlement` def），無實際損失。

- [ ] **Step 2: OutpostAttackUtility.cs**

攻擊流程照 `SettlementUtility.cs:29-59`，只差 size 參數；ArrivalAction 鏡像 Task 0 Step 1 查到的 `CaravanArrivalAction_AttackSettlement`：

```csharp
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace pas.outposts
{
    public static class OutpostAttackUtility
    {
        public static void Attack(Caravan caravan, NpcOutpost outpost)
        {
            if (!outpost.HasMap)
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    AttackNow(caravan, outpost);
                }, "GeneratingMapForNewEncounter", doAsynchronously: false, null);
            }
            else
            {
                AttackNow(caravan, outpost);
            }
        }

        private static void AttackNow(Caravan caravan, NpcOutpost outpost)
        {
            bool newMap = !outpost.HasMap;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(outpost.Tile, outpost.OutpostMapSize, null);
            TaggedString letterLabel = "LetterLabelCaravanEnteredEnemyBase".Translate();
            TaggedString letterText = "LetterCaravanEnteredEnemyBase".Translate(caravan.Label, outpost.Label.ApplyTag(TagType.Settlement, outpost.Faction.GetUniqueLoadID())).CapitalizeFirst();
            SettlementUtility.AffectRelationsOnAttacked(outpost, ref letterText);
            if (newMap)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(map.mapPawns.AllPawns, ref letterLabel, ref letterText, "LetterRelatedPawnsSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), informEvenIfSeenBefore: true);
            }
            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NeutralEvent, caravan.PawnsListForReading, outpost.Faction);
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
            Find.GoodwillSituationManager.RecalculateAll(canSendHostilityChangedLetter: true);
        }

        public static FloatMenuAcceptanceReport CanAttack(Caravan caravan, NpcOutpost outpost)
        {
            if (outpost == null || !outpost.Spawned || !outpost.Attackable)
            {
                return false;
            }
            if (outpost.EnterCooldownBlocksEntering())
            {
                return FloatMenuAcceptanceReport.WithFailMessage("MessageEnterCooldownBlocksEntering".Translate(outpost.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
            }
            return true;
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, NpcOutpost outpost)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(
                () => CanAttack(caravan, outpost),
                () => new CaravanArrivalAction_AttackOutpost(outpost),
                "AttackSettlement".Translate(outpost.Label),
                caravan, outpost.Tile, outpost);
        }
    }

    public class CaravanArrivalAction_AttackOutpost : CaravanArrivalAction
    {
        private NpcOutpost outpost;

        public override string Label => "AttackSettlement".Translate(outpost.Label);

        public override string ReportString => "CaravanAttacking".Translate(outpost.Label);

        public CaravanArrivalAction_AttackOutpost()
        {
        }

        public CaravanArrivalAction_AttackOutpost(NpcOutpost outpost)
        {
            this.outpost = outpost;
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
            if (!report)
            {
                return report;
            }
            if (outpost != null && outpost.Tile != destinationTile)
            {
                return false;
            }
            return OutpostAttackUtility.CanAttack(caravan, outpost);
        }

        public override void Arrived(Caravan caravan)
        {
            OutpostAttackUtility.Attack(caravan, outpost);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref outpost, "outpost");
        }
    }
}
```

（`"CaravanAttacking"`/`"AttackSettlement"` 翻譯 key 與 `EnterCooldownBlocksEntering` 以 Task 0 Step 1 查到的原版 AttackSettlement ArrivalAction 為準，照原版用的 key 改。）

- [ ] **Step 3: Defs/WorldObjectDefs/Outposts.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <WorldObjectDef>
    <defName>pas_outposts_Outpost</defName>
    <label>outpost</label>
    <description>A small satellite outpost serving a nearby settlement.</description>
    <worldObjectClass>pas.outposts.NpcOutpost</worldObjectClass>
    <texture>World/WorldObjects/DefaultSettlement</texture>
    <expandingIcon>true</expandingIcon>
    <expandingIconTexture>World/WorldObjects/Expanding/Settlement</expandingIconTexture>
    <expandingIconPriority>0.3</expandingIconPriority>
    <canHaveFaction>true</canHaveFaction>
    <selectable>true</selectable>
    <neverMultiSelect>true</neverMultiSelect>
    <allowCaravanIncidentsWhichGenerateMap>true</allowCaravanIncidentsWhichGenerateMap>
  </WorldObjectDef>
</Defs>
```

（貼圖路徑以 Task 0 Step 8 為準；錯了 E2E 粉紅方塊當場改。`expandingIconPriority` 壓低讓聚落圖標優先。）

- [ ] **Step 4: Languages**

`Languages/English/Keyed/NpcOutposts.xml`：
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <pas_outposts_NameFormat>{0} Outpost</pas_outposts_NameFormat>
</LanguageData>
```

`Languages/ChineseTraditional/Keyed/NpcOutposts.xml`：
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <pas_outposts_NameFormat>{0}哨站</pas_outposts_NameFormat>
</LanguageData>
```

- [ ] **Step 5: 建置驗證（雙 mod）**

```powershell
dotnet build C:\code\mine\my_rimworld_mods\sims-mode-community\Source\SimsModeCommunity.csproj -c Release
dotnet build C:\code\mine\my_rimworld_mods\npc-outposts\Source\NpcOutposts.csproj -c Release
```
Expected: 0 警告 0 錯誤（NpcOutpost 引用 `pas.sims.CaravanArrivalAction_VisitMap` 跨 assembly 編譯通過＝相依鏈驗證）。

- [ ] **Step 6: Commit**

```powershell
git -C C:\code\mine\my_rimworld_mods add npc-outposts/Source npc-outposts/Defs npc-outposts/Languages
git -C C:\code\mine\my_rimworld_mods commit -m @'
feat: NpcOutpost 世界物件（小圖拜訪/攻打、交易繼承、trim 注入）

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>
'@
```
