# 鼠族武器 ThingDef 清單（鐵匠屋軍械委託標的庫，2026-07-19）

> 供鐵匠屋委託挑「玩家能打造、鼠族客戶會來訂」的軍械。來源＝已訂閱鼠族 mod 的 **1.6** 版，
> XML 註解區塊已剝除（`RK_ResumedWeapon.xml` 整檔註解＝0 件；`RK_ExtraWeapon` 12→8、`RK_MoreWeapon` 43→31 真）。
> defName 逐字可用（引用錯字會紅字）。「可否打造」依 `<recipeMaker>`＋繼承 base abstract 判定。

## 總覽

| Mod | 有效武器本體 ThingDef |
|---|---|
| **NewRatkinPlus** (1578693166，硬依賴) | 29 件（近戰16、遠程10、銃槍/鏈劍3；另 2 件無配方） |
| **Ratkin Weapons+** (2779404660) | 24 件武器 ＋ 1 盾（`RK_KiteShield`） |
| **Ratkin Knights+** (3394862242，`RKK_` 前綴) | 14 件武器 ＋ 1 盾（`RKK_MoonShield`）＋ 1 無配方（`RKK_Weapon_RangerBow`） |

**製作檯**：`RK_FueledSmithy`/`RK_ElectricSmithy`＝鼠族鐵匠檯；`TableMachining`＝機工檯；`FabricationBench`＝微加工台/電子檯。NRP 近戰＋新石器遠程、Knights `RKK_MeleeWeapon` 皆落鐵匠檯。

---

## NewRatkinPlus — 近戰（皆 Medieval，鐵匠檯）
`RK_Dagger`(匕首,偷襲暈眩) `RK_OneHanded`(片手劍) `RK_Mace`(錘) `RK_Spear`(矛,穿甲) `RK_LongSword`(長劍,附種植加成) `RK_LightLance`(木騎槍,需盾) `RK_TwoHanded`(雙手劍) `RK_Halberd`(戟,cleave) `RK_HeavyLance`(重騎槍,「衝鋒」技+藝術品質,Craft8)

## NewRatkinPlus — 工具型近戰
`RK_Axe`(斧) `RK_Cleaver`(剁肉刀) `RK_Weapon_Maul`(工作錘) `RK_Pickaxe`(鎬,⚠撞名見下) `RK_Hockey`(鋤,噱頭) `RK_Fork`(草叉,噱頭) `RK_MagicWand`(魔術棒,會爆炸,Industrial,有配方)

## NewRatkinPlus — 遠程
`RK_Crossbow`(十字弩,鐵匠檯Carpentry) `RK_AutoCrossBow`(渦輪連發弩,+RatkinEngineering) `RK_Weapon_Arbalest`(大弩/弩砲,Carpentry+Fortification) `RK_Rifle`(飛鏢步槍,機工檯) `RK_SniperRifle`(飛鏢狙擊,機工檯) `RK_Weapon_SawedOff`(鋸短散彈,機工檯) `RK_Weapon_RatHolicGun`(特殊槍,機工檯) `RK_Weapon_Bolter`(手炮,微加工台) `RK_PrototypePulseRifle`(原型脈衝步槍,微加工台) `RK_Weapon_BFR`(BFR3000,發弩砲彈AP/HE,機工檯)

## NewRatkinPlus — 招牌 / 無配方
`RK_Weapon_Gunlance`(**銃槍**,近戰+砲擊噴火,機工檯,鼠族招牌) ／ ❌`RK_Weapon_ProtoChainSword`、❌`RK_Weapon_ProtoFlameChainSword`(鏈鋸劍,**無配方,派系限定**)

## Ratkin Weapons+ — 近戰
`RK_Rapier`(刺劍,RK_RoyalWeapon) `RK_TwoBladed`(雙刃劍) `RK_Flail`(連枷) `RK_Pickaxe`(鎬,⚠覆蓋NRP) `RK_Bayonet`(刺刀衝鋒) `RK_WonderStick`(奇兵棒,噱頭) `RK_GiantKiller`(巨人殺手,**Spacer**,微加工台) `RK_Detonator`(引爆器,投擲) `RK_Wex`(噴火器,微加工台) ／噱頭：`RK_PaintTray` `RK_Pencil` `RK_Bible` `RK_WoodenSword`(木劍,訓練)

## Ratkin Weapons+ — 遠程
`RK_MachineCrossBow`(機械連弩) `RK_HECrossbow`(破片弩) `RK_AssassinKnife`(刺客飛刀) `RK_AuxiliaryAiming`(輔瞄狙擊) `RK_AssaultRifle`(突擊步槍,微加工台) `RK_LMG`(輕機槍) `RK_AntiTank`(反坦克步槍) `RK_ChargeRifle`(電荷步槍,Spacer,微加工台) `RK_Genocide`(種族滅絕,大殺器,Spacer) `RK_InfernoGrenadeLauncher`(地獄火榴彈,Spacer) `RK_SignalGun`(信號槍,非殺傷噱頭)
盾：`RK_KiteShield`(守衛者盾,apparel)

## Ratkin Knights+（`RKK_`，label 原生簡中，皆鐵匠檯除註明）
`RKK_OriginDragonSword`(初始騎士劍,Craft5) `RKK_DragonSword_Yellow/Red/Blue/Black/Orange`(聖劍五色系列) `RKK_BloodSword`(血之大劍,不可交易) `RKK_BloodTwoBladed`(血之雙刃) `RKK_WarHammerOfLaw`(憲衛騎士錘) `RKK_SilverMoon`(銀月之槍,月光衝刺) `RKK_Scythe`(乾草鐮,低階暴民) `RKK_Torch`(火把,低階暴民) `RKK_GoldZweiHander`(金紋雙手劍,微加工台,對大體型+傷,Craft10/Art4) `RKK_Weapon_MythSword`(秘文劍,**放劍氣**,傳奇貨) 盾`RKK_MoonShield` ／ ❌`RKK_Weapon_RangerBow`(鷹擊弓,**無配方,騎士限定**)

---

## 委託分層建議（defName 可直接寫進委託 XML）

- **T1 低科技現貨（村莊）**：`RK_Crossbow`、`RK_Spear`、`RK_Dagger`、`RK_Axe`、`RK_Cleaver`、`RK_WoodenSword`、`RK_Bayonet`、`RK_Flail`、`RKK_Scythe`、`RKK_Torch`。
- **T2 品質定製（精工近戰/步槍）**：`RK_OneHanded`、`RK_LongSword`、`RK_TwoHanded`、`RK_Mace`、`RK_Halberd`、`RK_LightLance`、`RK_AutoCrossBow`、`RK_Rifle`、`RK_SniperRifle`、`RK_Weapon_SawedOff`、`RK_Rapier`、`RK_TwoBladed`、`RK_MachineCrossBow`、`RK_AuxiliaryAiming`、`RK_AssaultRifle`、`RK_LMG`、`RK_AntiTank`、`RK_HECrossbow`、`RKK_OriginDragonSword`、`RKK_BloodSword`、`RKK_BloodTwoBladed`、`RKK_WarHammerOfLaw`、`RKK_SilverMoon`。
- **T3 王國大單／傳奇（重武器・招牌）**：`RK_Weapon_Gunlance`(銃槍)、`RK_Weapon_BFR`/`RK_Weapon_Arbalest`(弩砲)、`RK_Weapon_Bolter`、`RK_PrototypePulseRifle`、`RK_Weapon_RatHolicGun`、`RK_HeavyLance`、`RK_GiantKiller`、`RK_Genocide`、`RK_ChargeRifle`、`RK_InfernoGrenadeLauncher`、`RK_Wex`、`RKK_Weapon_MythSword`(劍氣)、`RKK_GoldZweiHander`、`RKK_DragonSword_*`(聖劍分色套組大單)。

## ⚠ 坑（設計/實作要避）
- **絕對避開（無配方，玩家造不出→開不了委託）**：`RK_Weapon_ProtoChainSword`、`RK_Weapon_ProtoFlameChainSword`、`RKK_Weapon_RangerBow`。
- **噱頭非軍械（別當正經委託標的）**：`RK_PaintTray`、`RK_Pencil`、`RK_Bible`、`RK_Hockey`、`RK_Fork`、`RK_WonderStick`、`RK_MagicWand`、`RK_SignalGun`。
- **盾＝apparel 非武器本體**：`RK_KiteShield`、`RKK_MoonShield`（若委託納護具再說）。
- **defName 撞名**：`RK_Pickaxe` NRP 與 Weapons+ 各定義一次，載入序後者（Weapons+）生效、不紅字，屬性以 Weapons+ 為準。
- **繁中未翻譯**（遊戲內顯英文 label，不影響功能）：`RK_Mace`/`RK_Spear`/`RK_Halberd`/`RK_Weapon_Arbalest`/`RK_Weapon_SawedOff`/`RK_Weapon_Bolter`/`RK_Weapon_BFR`/`RK_Weapon_RatHolicGun`/`RK_Weapon_Gunlance`/`RK_Pickaxe`/`RK_Weapon_Maul`。
- 遠程武器 `techLevel` 多繼承自 base abstract（表中 `(Neolithic)`/`(Industrial)` 為推定）。
