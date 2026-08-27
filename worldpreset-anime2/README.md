# worldpreset-2-anime

給 `pack-2-anime`（510 mod / profile 2-anime）的 **Worldbuilder 世界預設 mod**。
目的：根治「開局隨機亂滾的垃圾文化」——策展派系陣容並給各派系指派固定信仰。

## 是什麼

一個純檔案 mod（不寫 C#），被 Worldbuilder 掃描為一個「世界預設」。新開局時
在 Worldbuilder 的預設選擇頁選「二次元定製世界」即用本檔定義的世界生成。

```
worldpreset-2-anime/
├── About/About.xml
├── Worldbuilder/Anime2World/
│   ├── Preset.xml         ← 世界定義主檔（派系/參數/改造/信仰對映）
│   ├── CULTURE-PLAN.md    ← 10 個文化原型設計＋怎麼補 .rid（讀我）
│   └── CustomIdeos/       ← 放 .rid 信仰檔（目前空，缺檔=該派系退回隨機）
└── README.md
```

## 現況（v1 骨架）

- ✅ 陣容 **30 派系**（7 二次元盟友＋9 鼠族大雜燴＋6 原版錨點＋8 敵對）、生成參數、
  部分派系改名/上色 已定，**可載入**。coverage 0.5。
- ✅ **文化已就位**：27 份 `.rid`（逐派系凍結自使用者 dump 世界）覆蓋全部 30 派系，
  開局 0 個退回隨機。細節與待調味項 → `Worldbuilder/Anime2World/CULTURE-PLAN.md`。
- ⏳ 尚未部署（symlink 進遊戲 + 加進 pack 清單）——避開 `~/notes` 上另一個 agent，待點頭。

改動請直接編 `Preset.xml`（清單都設計成加/刪一行 `<li>` 即可）。
