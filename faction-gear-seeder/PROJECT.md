# Faction Gear Seeder（開局派系裝備播種器）

> RimWorld 1.6。packageId `pas.gear.community`。**破例引 Harmony**（單一 postfix）、硬相依 `brrainz.harmony`。
> 「開局世界管線」的**第三層＝裝備層**，姊妹於 `faction-relation-seeder`（關係層，零 Harmony）。
> 緣起：`舞臺三件套` 的 yc's Faction Editor 能在遊戲內把派系裝備調得很好，但那是**執行期直改 Def
> 的重 Harmony 工具、無公開 API、preset 綁在它自己的 ModSettings**——無法被外部 mod 程式化消費、
> 不能隨管線發佈。本 mod 把「yc 打樣出來的裝備」謄成**可發佈、資料驅動、生成時自動套用**的純資料 Def。
> 分析佐證：`analysis/rimworld_mods/yc-faction-editor/analysis.md`（§2.2 GeneratePawn postfix、
> §2.7 資料模型、§4.B 借鏡清單）。

## 為什麼一定要 Harmony（破例的理由）

vanilla 純資料只能表達粗粒度的 `PawnKindDef.apparelRequired/weaponTags`，**無法**還原 yc 那種
「指定某兵種穿這件卓越品質鋼製防彈衣＋這把突擊步槍」的**確定性強制 loadout**。使用者裁示要
「完整裝備保真」，唯一路就是在生成時介入——一個 `PawnGenerator.GeneratePawn` postfix。
這是叢集裡**刻意的唯一破例**（見 [[faction-politics-content-gap]] 哲學：零/低 Harmony 為常態）。

## 目標

用 XML 宣告「某 FactionDef → 各 PawnKind 的強制裝備/武器/品質」（`FactionGearSeedDef`），
該派系**每次生成的 humanlike pawn**（raid/caravan/守軍/補員）都套上這身裝備。
之後不再干預，任世界演化。**執行期不依賴 yc**，yc 只當桌上打樣 GUI。

**本 mod ＝純引擎（數據源/執行層分離）**：只提供 `FactionGearSeedDef` 型別 + 套用 postfix，
**不自帶任何裝備資料**。實際裝備表由消費端內容 mod 提供（由 yc preset 謄回，見 `tools/`）。

## 管線位置

| 層 | 引擎 | 資料 |
|---|---|---|
| 佈景（陣容/名/色/生成參數） | Worldbuilder | Preset.xml |
| 開局關係矩陣 | `pas.relations.community`（零 Harmony） | RelationSeedDef |
| **派系裝備（本 mod）** | **`pas.gear.community`（單 postfix）** | **FactionGearSeedDef** |

## 謄回工作流（yc → 管線）

1. 遊戲內用 **yc's Faction Editor** 把某派系某兵種的裝備/武器/品質調好 → **Save Preset**（如 `aa`）。
2. 跑 `tools/transcribe_yc_preset.py --config <yc ModSettings xml> --preset aa --out MyGear.xml`
   （或 `--save <存檔.rws>` 讀存檔內 GameComponent；或 `--live` 讀即時全域）。
   → 把 yc 的 `factionGearData` 謄成一份 `FactionGearSeedDef`（純資料）。
3. 把 `MyGear.xml` 放進消費端內容 mod 的 `1.6/Defs/`，硬相依本引擎。發佈物**不需玩家裝 yc**。

**謄回對照**（yc Scribe 欄位 → 本 Def）：`factionDefName`→`factionDef`；`kindDefName`→`kindDef`；
`forceOnlySelected/forceNaked/itemQuality`→同名；簡單池 `weapons/apparel/armors/others`（`GearItem`，只 thingDef）
＋進階 `specificWeapons/specificApparel`（`SpecRequirementEdit`，帶 material/quality/color）→ `weapons/apparel`。

## 機制（單一 Harmony postfix）

- **補丁面**：`PawnGenerator.GeneratePawn(PawnGenerationRequest)` 的 **Postfix**（唯一目標）。
  `[ThreadStatic]` 防遞迴旗標；`Current.ProgramState == Playing` 守衛（鏡像 yc，避開世界/地圖生成期脆弱）；
  faction 取 `request.Faction ?? __result.Faction`（生成期 `__result.Faction` 可能未指派）。
- **套用**（`GearSeedApplier`）：查表命中派系＋兵種 →（forceNaked/forceOnlySelected 時）脫既有衣著、
  清主武器 → 依序 `ThingMaker.MakeThing(def, stuff)` 生成 → `SetQuality`／`CompColorable.SetColor` →
  `pawn.apparel.Wear(...)` / `pawn.equipment.AddEquipment(...)`。**只用 vanilla API，不改任何 Def。**
- **軟略過**：缺席派系/兵種/物品、身體部位不符（機械族/異種穿不了）→ 靜默跳過，永不報錯。
- **逐件例外隔離**：壞一件不拖垮整隻，整隻例外不拖垮生成。

## 檔案結構

| 檔案 | 內容 |
|---|---|
| `About/About.xml` | packageId `pas.gear.community`；硬相依 `brrainz.harmony`；loadAfter harmony/worldbuilder |
| `loadFolders.xml` | `/` + `1.6` |
| `1.6/Assemblies/FactionGearSeeder.dll` | 建置產物（只此一 DLL，不隨附 Harmony/RimWorld） |
| `Source/FactionGearSeedDef.cs` | `FactionGearSeedDef : Def` + `GearKindEntry` + `GearItemEntry` + ConfigErrors |
| `Source/GearSeedApplier.cs` | Harmony 引導 + GeneratePawn postfix + 純套用邏輯 |
| `Source/Dev/GearSeederDebug.cs` | dev「對已生成 pawn 重套裝備」DebugAction（肉眼驗證用） |
| `tools/transcribe_yc_preset.py` | 謄回工具：yc preset/存檔 → FactionGearSeedDef XML |
| `tests/healthcheck.py` | 靜態健檢（XML/交叉引用/**反向 Harmony 不變式**/建置產物/轉換器煙霧測試） |

## 建置

```
cd Source && dotnet build -c Release      # → ../1.6/Assemblies/FactionGearSeeder.dll
python3 tests/healthcheck.py              # 靜態健檢（含轉換器煙霧測試）
```

## 狀態

- ✅ 編譯（net48 / Krafs.Rimworld.Ref 1.6.* + Lib.Harmony 2.3.3 compile-only，零警告）。
- ✅ healthcheck OK（含轉換器合成樣本煙霧測試綠）。
- ✅ **實機 E2E 綠**（2026-07-18，部署側經 inbox 回執）：以真實 yc preset `asaf`（派系 PirateWaster / 20 兵種）
  謄成 `Gear_PirateWaster` 示範，打包 FactionGearSeeder-0.1.0 + GearSeedDemo-0.1.0 上機。
  - 第 1 關（headless）：48s 進主選單、`[gear-seeder] Harmony 補丁就緒`、NRE 0、零真紅字。
  - 第 3 關（桌面實機）：對 PirateWaster `Re-apply gear` 命中 N＞0、**全程零 `[gear-seeder]` 例外**、
    不刷紅字；非 PirateWaster（普通 Pirate）正確跳過不誤套。使用者目視 loadout 連貫、非隨機。
  - 這是首個 E2E-verified cut（發佈標準見 [[publish-bar-realmachine-e2e]]）。
- ⏳ 可選加嚴（非阻塞）：`Mercenary_GunnerTox` 的「品質 Poor＋背包 Weapon_GrenadeTox＋主武器一把槍」
  三細節部署側只做到目視、未逐項硬簽收。要正式加嚴可回丟一封測試委託給部署側附期望值。

## v0.1.0 範圍與已知限制

- **只做裝備核心**：apparel（衣/甲/其他）＋weapons（主武器）＋quality＋stuff＋color。
- **暫不做**（yc 有、本引擎 v0.1.0 未實作）：forcedTraits/Skills/Genes/**Appearance（外觀髮型體型）**、
  inventory、budget/pool 預算策略、biocode、CE ammo、xenotype/age。轉換器會忽略這些欄位。
  → 若之後要補「外觀層」，在 `GearKindEntry` 加欄位 + `GearSeedApplier` 加套用段即可。
- **pawnGroup 陣容**不在本 mod：那是「哪些兵種上場」而非「穿什麼」，屬**純資料** `FactionDef.pawnGroupMakers`
  的 PatchOperation，零 Harmony，應另路處理（可寫成 transcriber 的第二種輸出）。
- **生效範圍**：只影響 Playing 期「之後新生成」的 pawn；地圖上既有 pawn 不變（與 yc 同）。
  dev 動作可對既有 pawn 手動重套以便驗證。
