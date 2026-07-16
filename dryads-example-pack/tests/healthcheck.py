#!/usr/bin/env python3
"""dryads-example-pack 離線健檢（不啟動遊戲）：
1. 全部 XML well-formed
2. GauranlenTreeModeDef.pawnKindDef → PawnKindDef 存在
3. PawnKindDef.race → ThingDef 存在
4. lifeStages texPath → 對應 _east.png 實體檔存在
"""
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
errors = []

xml_files = sorted(ROOT.rglob("*.xml"))
trees = {}
for f in xml_files:
    try:
        trees[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML 解析失敗 {f}: {e}")

thing_defs, kind_defs, mode_defs = {}, {}, {}
for f, t in trees.items():
    if t.getroot().tag != "Defs":
        continue
    for node in t.getroot():
        name_el = node.find("defName")
        if name_el is None:
            continue
        if node.tag == "ThingDef":
            thing_defs[name_el.text] = node
        elif node.tag == "PawnKindDef":
            kind_defs[name_el.text] = node
        elif node.tag == "GauranlenTreeModeDef":
            mode_defs[name_el.text] = node

for name, node in mode_defs.items():
    pk = node.findtext("pawnKindDef")
    if pk not in kind_defs:
        errors.append(f"modeDef {name}: pawnKindDef {pk} 不存在")

for name, node in kind_defs.items():
    race = node.findtext("race")
    if race not in thing_defs:
        errors.append(f"PawnKindDef {name}: race {race} 不存在")
    for tex_el in node.iter("texPath"):
        east = ROOT / "Textures" / (tex_el.text + "_east.png")
        if not east.exists():
            errors.append(f"PawnKindDef {name}: 缺貼圖 {east}")

if errors:
    print("FAIL")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print(f"PASS: {len(xml_files)} xml / {len(thing_defs)} ThingDef / "
      f"{len(kind_defs)} PawnKindDef / {len(mode_defs)} GauranlenTreeModeDef")
