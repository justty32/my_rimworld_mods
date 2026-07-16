# 佔位貼圖規格說明

此處 PNG 皆為**程式生成的佔位圖**（`tests/gen_placeholder_textures.py`），僅供載入驗證，正式版需重繪。

規格（比照 VIE Dryads 實測，`workshop/content/294100/2720631512/Textures/Things/Pawn/Animal/DryadStonedigger/`，全為 256×256）：

- 格式：PNG，RGBA（帶透明背景）
- 尺寸：256×256（def 中 `drawSize` 1.5，與原版樹精一致）
- 命名（Graphic_Multi）：`<Stem>_north.png`、`<Stem>_south.png`、`<Stem>_east.png`；west 缺省時引擎鏡射 east
- 乾屍圖：`Dessicated_<Stem>_east.png` 一張即可（VIE 同樣只給 east，引擎會回退）
- lifeStages：樹精 race 只有一個 `AnimalAdult` 階段，故每種樹精只需一組貼圖；若 race 定義多個 `lifeStageAges`，`PawnKindDef.lifeStages` 需給等數量貼圖組（依 index 對應）
