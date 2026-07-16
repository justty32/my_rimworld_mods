#!/usr/bin/env python3
"""離線靜態健檢：XML well-formed ＋ 內部 defName 交叉引用 ＋ AL class 存在性。

不啟動遊戲。用法：python3 tests/healthcheck.py
"""
import sys
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DECOMPILED = Path.home() / "repo/pas/projects/rimworld_mods/ariandel-library/decompiled/AriandelLibrary.decompiled.cs"

errors = []

# 1. 全部 XML well-formed
xml_files = sorted(ROOT.rglob("*.xml"))
for f in xml_files:
    try:
        ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML 解析失敗 {f.relative_to(ROOT)}: {e}")
print(f"[1] XML well-formed: {len(xml_files)} 檔檢查完畢")

# 2. 內部 defName 交叉引用（本 mod 定義的 def 被本 mod 引用處必須存在）
defined = set()
for f in (ROOT / "1.6/Defs").rglob("*.xml"):
    for m in re.finditer(r"<defName>([^<]+)</defName>", f.read_text(encoding="utf-8")):
        defined.add(m.group(1))
refs = {
    "PAS_AEC_AshenWarcry": "PawnKindDef.abilities",
    "PAS_AEC_WarcryBuff": "AbilityDef GiveHediff",
    "PAS_AEC_EmberHeart": "extraForcedHediffs / RequireHediff",
    "PAS_AEC_AshSoul": "extraForcedTraits / TraitLock",
    "PAS_AEC_Childhood_AshOrphan": "FixedIdentity childhood",
    "PAS_AEC_Adulthood_AshSinger": "FixedIdentity adulthood",
    "PAS_AEC_Tab": "SpecialPawnExtension.tabDef",
    "PAS_AEC_AshSinger": "ShroudOutcome.pawnList",
}
for d, where in refs.items():
    if d not in defined:
        errors.append(f"缺 defName {d}（引用處：{where}）")
print(f"[2] 內部交叉引用: {len(refs)} 項檢查完畢")

# 3. 引用的 AriandelLibrary class 必須存在於反編譯源
al_classes = set()
for f in (ROOT / "1.6/Defs").rglob("*.xml"):
    for m in re.finditer(r'Class="(AriandelLibrary\.[A-Za-z_.]+)"', f.read_text(encoding="utf-8")):
        al_classes.add(m.group(1))
    for m in re.finditer(r"<(?:abilityClass|workerClass)>(AriandelLibrary\.[A-Za-z_.]+)<", f.read_text(encoding="utf-8")):
        al_classes.add(m.group(1))
if DECOMPILED.exists():
    src = DECOMPILED.read_text(encoding="utf-8", errors="replace")
    for c in sorted(al_classes):
        short = c.split(".")[-1]
        if f"class {short}" not in src:
            # Anomaly 子模組不在主 DLL 反編譯內，另行標註
            if c.startswith("AriandelLibrary.Anomaly."):
                print(f"    (跳過 gated 子模組類 {c}，不在主 DLL)")
            else:
                errors.append(f"反編譯源找不到 class {c}")
    print(f"[3] AL class 存在性: {len(al_classes)} 類檢查完畢")
else:
    print("[3] 反編譯源不存在，跳過 class 檢查")

# 4. 貼圖存在
icon = ROOT / "Textures/PAS_AEC/Icon/AshSinger.png"
if not icon.exists():
    errors.append("缺 SCM 頭像 Textures/PAS_AEC/Icon/AshSinger.png")
print("[4] 貼圖存在性檢查完畢")

if errors:
    print("\nFAIL:")
    for e in errors:
        print("  -", e)
    sys.exit(1)
print("\nPASS: 全部靜態檢查通過")
