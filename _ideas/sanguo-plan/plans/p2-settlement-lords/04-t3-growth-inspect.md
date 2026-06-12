# T3 — 治理影響 postfix＋inspect 顯示

## 產出

- `Source/Patches/Patch_IncrementSettlementGrowth.cs`
- `Source/Patches/Patch_SettlementInspectString.cs`
- `LordsUtility.GovernanceFactor`／`GrowthCapFor` 填實
- HarmonyInit 接上兩個 TryPatch

## GovernanceFactor（LordsUtility）

```
record null 或 dead 或 govAmplitude<=0 → 1f
score = 0.7×polity + 0.3×loyalty                       // 0..100
gov   = Clamp(1 + (score-50)/50 × govAmplitude, 0.25f, 2f)
```

## GrowthCapFor（自帶鏡像，抄 Mod 1 邏輯、勿 ref）

基礎 50000；`comp.parent?.def?.defName=="City_Citadel"` +5000；
`comp.isCapitol` → Vassal +1000、否則 +5000（鏡像 RW:17597-17612）。

## Patch_IncrementSettlementGrowth（postfix，無參數）

與 Mod 1 同方法疊加、互不知情；**可能在背景執行緒跑**（RW:17062 threadingEnabled）：
不用 Rand、不發 letter/Message、綁定走 snapshot、整體 try/catch WarnOnce。

```
comp=WorldComponent_SettlementLords.Get()；Settings.govAmplitude<=0 → return（停用）
foreach binding in comp.BindingsSnapshot():
  host as Settlement；null/Destroyed → skip（heal 收）
  record=GetById(recordId)；null/dead → skip
  rwsc=host.GetComponent<RimWarSettlementComp>()；null → skip
  rwsc.PointDamage > 0 → skip                       // 鏡像 RW:17616 療傷分支：當輪不成長
  rwd=GetRimWarDataForFaction(host.Faction)；null/Player/Excluded → skip
  delta = RoundToInt((GovernanceFactor(record)-1) × GovPointsScale(30))
  delta>0：cap=GrowthCapFor(rwsc,rwd)；points<cap 才 RimWarPoints=Min(points+delta, cap)
  delta<0 且 decayEnabled：RimWarPoints = Max(0, points+delta)
                                                    // setter Max(0,)、getter 地板 100 → 停在 100
  delta==0：不動（gov≈1 中庸之主）
```

鐵律：不讀寫任何 `RimWarData` 派系級係數（源碼字串級 guard）；只動單城 points。

## Patch_SettlementInspectString（postfix）

```
Postfix(Settlement __instance, ref string __result)
  record = Get()?.LordOf(__instance)；null/dead → return
  line = "pas_settlement_InspectLord".Translate(DisplayName, polity, loyalty,
          GovernanceFactor(record).ToString("0.00"))
  __result 空 ? line : __result + "\n" + line       // 與 RimWar(RW:6570) 各 append 自己段
  try/catch WarnOnce（GUI 執行緒，異常省略領主行）
```

## HarmonyInit 接線

| 目標 | 型式 |
|---|---|
| `WorldComponent_PowerTracker.IncrementSettlementGrowth` | postfix |
| `Settlement(RimWorld.Planet).GetInspectString` | postfix |

兩者皆 TryPatch fail-soft：target null → WarnOnce「RimWar 版本不符？對應功能降級」。

## 驗收

build 0/0；healthcheck OK（接點清單＋鐵律 guard 通過）。
dev 實機：政務高領主城 points 漲速 > 鄰城；政務低領主城 points 漸減至 100 停；
選中該城 inspect 末行見「Lord: 〇〇 (Polity x, Loyalty y, governance z)」。
