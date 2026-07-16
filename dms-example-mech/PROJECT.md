# dms-example-mech — DMS/FFF 生態新機械體示範 mod

## 目標

以「可載入的最小實例」示範在 Dead Man's Switch (DMS) / Fortified Features Framework (FFF)
生態下新增內容的四條純 XML 路徑，數值刻意與既有機體區隔（超輕偵察位）：

| 內容 | defName | 示範的機制 |
|---|---|---|
| 紅隼 Kestrel（超輕偵察機兵） | `PASEX_Mech_Kestrel` | 新機械體最小集合（race ThingDef + PawnKindDef + 孵化配方），特色機制＝光學迷彩（FFF `CompProperties_Camouflage`，Royalty/Anomaly 條件式） |
| 機兵偵察卡賓槍 | `PASEX_ScoutCarbine` | 機兵武器 + `MechWeaponExtension` 白名單 tag 對接 |
| 光學感測套件 | `PASEX_OpticSensorSuite`（物品+hediff 同名一對） | 機兵插件（modification）：`CompProperties_AddHediffOnTarget` → hediff statOffsets/statFactors，可拆卸 |
| 偵察感測莢艙 | `PASEX_Module_ReconSensorPod` / `PASEX_Apparel_ReconSensorPod` | Mobile Dragoon（Exosuit Framework）部件：物品+衣服雙 def、`CompProperties_ExosuitModule` 佔 `MountLeft` 槽（條件載入） |

對應調查報告（機制原理、欄位語意表、file:line 引用）：
- `~/repo/pas/analysis/rimworld_mods/dead-mans-switch/tutorial/02_define_new_mech.md`（新機兵／武器／插件）
- `~/repo/pas/analysis/rimworld_mods/mobile-dragoon/tutorial/02_dragoon_module_and_weapon.md`（龍騎兵部件／武器）

## 相依鏈

- **硬相依**（About.xml `modDependencies`）：Biotech、`AOBA.Framework`（FFF）、`Aoba.DeadManSwitch.Core`（DMS Core）。
  - DMS Core 1.6 自身硬相依 Biotech 與 FFF，故孵化配方等 Biotech 內容可直接放主 `Defs/`。
  - Harmony 不需直接宣告（由 FFF/DMS 的相依鏈帶入）。
- **軟相依**（LoadFolders 條件資料夾 `MOD/MobileDragoon/`）：`Aoba.DeadManSwitch.MobileDragoon`
  啟用時才載入龍騎兵部件（其母 def `DMS_ModuleItemMountLeft` 等由 MD 提供）。
- `loadAfter`：AOBA.Framework → DMS Core → MobileDragoon。

## 決策：單一 mod 而非拆成 dms-example-dragoon

倉庫其他 mod 為單一功能小 mod，但本 mod 是「教學示例集」，四個示例共用同一相依底座
（FFF+DMS Core），僅龍騎兵部件多一層 MD 相依——用 DMS 生態自己的慣例
（LoadFolders `IfModActive` 條件資料夾，同 DMS Core / MD 的 `MOD/` 目錄做法）處理即可，
拆成兩個 mod 只會複製 About/PROJECT/Languages 樣板。

## 貼圖狀態（全部佔位，待補）

刻意複用相依 mod 的既有 texPath，不附任何貼圖檔，保證零素材即可載入：

| def | 佔位 texPath | 來源 | 正式版該做的事 |
|---|---|---|---|
| PawnKind 紅隼 | `Things/Automatroid/falcon` | DMS Core | 自繪 `kestrel_east/north/south.png`（512×512，PNG，`Graphic_Multi` 三向；west 可省略自動鏡射） |
| 偵察卡賓槍 | `Things/Weapons/DMS_SemiAutoRifle` | DMS Core | 自繪單張 `Graphic_Single` |
| 感測套件物品 | `Things/Resource/ReinforcedFrame` | DMS Core | 自繪單張 |
| 感測莢艙 | `Things/Dragoon/SmokeLauncher/apparel_south`（物品）＋ `Things/Dragoon/SmokeLauncher/apparel`（穿戴，四向） | MobileDragoon | 自繪 `apparel_east/north/south/west.png` 並重調 drawData offsets |

## 驗證狀態

- ✅ 全部 XML 以 `python xml.etree.ElementTree` 驗證 well-formed（見 tests/validate_xml.py）。
- ✅ 所有引用的母 def／comp 類別／StatDef／BodyPartDef／research 均已回源碼確認存在
  （出處見各 Defs 檔內註解）。
- ⏳ **未實機測試**（依任務限制不啟動遊戲、不部署）。留待實機驗證清單：
  1. 紅隼可在 `DMS_MechGestatorSmall` 孵化、頻寬 1、可被命令裝備卡賓槍。
  2. 光學迷彩 comp 在 Royalty/Anomaly 環境下的 gizmo 與偵測行為。
  3. 感測套件右鍵安裝目標選取只認機械體、部位占用邏輯（一感測器一件）、拆卸返還物品。
  4. 龍騎兵莢艙在維修龍門架 ITab 中可裝入 MountLeft、穿戴貼圖 offsets 是否需要重調。
  5. PawnKind 出現於 DMS 派系突襲時武器生成（weaponMoney 範圍 vs 卡賓槍市值）。
