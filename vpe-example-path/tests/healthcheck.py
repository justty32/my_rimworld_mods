#!/usr/bin/env python3
"""vpe-example-path 離線健檢：XML well-formed ＋ defName 交叉引用一致性（不啟動遊戲）。"""
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
errors = []

# 1. 所有 XML well-formed
xml_files = sorted(ROOT.rglob("*.xml"))
trees = {}
for f in xml_files:
    try:
        trees[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML 解析失敗 {f.relative_to(ROOT)}: {e}")
print(f"[1] XML well-formed: {len(xml_files) - len(errors)}/{len(xml_files)}")

# 2. 收集本 mod defNames
path_defs, ability_defs, hediff_defs = set(), set(), set()
for f, t in trees.items():
    if f.parts[-3:-1] == ("Defs", "PsycasterPathDefs") or "PsycasterPathDefs" in f.parts:
        path_defs |= {e.text for e in t.getroot().iter("defName")}
    elif "AbilityDefs" in f.parts:
        ability_defs |= {e.text for e in t.getroot().iter("defName")}
    elif "HediffDefs" in f.parts:
        hediff_defs |= {e.text for e in t.getroot().iter("defName")}
print(f"[2] defs: path={sorted(path_defs)} abilities={len(ability_defs)} hediffs={len(hediff_defs)}")

# 3. 能力的 path / prerequisites / hediff 引用一致
VANILLA_OK = {"Anesthetic"}  # 允許引用的外部 hediff 白名單
for f, t in trees.items():
    if "AbilityDefs" not in f.parts:
        continue
    for adef in t.getroot():
        dn = adef.findtext("defName")
        if dn is None:
            continue
        for ext in adef.iter("li"):
            cls = ext.get("Class", "")
            if cls.endswith("AbilityExtension_Psycast"):
                p = ext.findtext("path")
                if p not in path_defs:
                    errors.append(f"{dn}: path '{p}' 不在本 mod path defs")
                lv = ext.findtext("level")
                if lv is None or not lv.isdigit() or not (1 <= int(lv) <= 5):
                    errors.append(f"{dn}: level '{lv}' 不在 1~5")
                for pre in ext.findall("prerequisites/li"):
                    if pre.text not in ability_defs:
                        errors.append(f"{dn}: 前置 '{pre.text}' 不存在")
            if cls.endswith("AbilityExtension_Hediff"):
                h = ext.findtext("hediff")
                if h not in hediff_defs and h not in VANILLA_OK:
                    errors.append(f"{dn}: hediff '{h}' 不存在")
        if adef.findtext("abilityClass") is None:
            errors.append(f"{dn}: 缺 abilityClass（VEF 無預設值，必填）")

# 4. 同一 level 最多 3 個能力（UI abilityTreeXOffsets 硬限制）
from collections import Counter
level_count = Counter()
for f, t in trees.items():
    if "AbilityDefs" not in f.parts:
        continue
    for ext in t.getroot().iter("li"):
        if ext.get("Class", "").endswith("AbilityExtension_Psycast"):
            key = (ext.findtext("path"), ext.findtext("level"))
            level_count[key] += 1
for (p, lv), n in level_count.items():
    if n > 3:
        errors.append(f"path {p} level {lv} 有 {n} 個能力（上限 3）")
print(f"[3][4] 交叉引用＋層寬檢查完成，levels={dict(level_count)}")

# 5. 自訂 hediff 若被帶 durationTime 的能力引用，必須有 HediffCompProperties_Disappears
for f, t in trees.items():
    if "HediffDefs" not in f.parts:
        continue
    for hdef in t.getroot():
        dn = hdef.findtext("defName")
        if dn and not any(li.get("Class") == "HediffCompProperties_Disappears" for li in hdef.iter("li")):
            errors.append(f"hediff {dn}: 無 HediffCompProperties_Disappears，durationTime 將不生效")

if errors:
    print("\n== FAIL ==")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("\n== PASS ==")
