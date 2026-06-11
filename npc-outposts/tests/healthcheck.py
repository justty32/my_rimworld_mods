#!/usr/bin/env python3
"""npc-outposts 靜態健檢。無遊戲環境下抓 XML/交叉引用/相依鏈錯誤。"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent          # npc-outposts/
SIMS = ROOT.parent / "sims-mode-community"             # 硬相依對象
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


# 1. 所有 XML well-formed（Defs/Patches/Languages/About）
xml_files = list(ROOT.glob("Defs/**/*.xml")) + list(ROOT.glob("Patches/**/*.xml")) \
    + list(ROOT.glob("Languages/**/*.xml")) + [ROOT / "About" / "About.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

# 2. About.xml：packageId / modDependencies / loadAfter 指向 pas.sims.community
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.outposts.community", "About packageId")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    check("pas.sims.community" in deps, "About modDependencies 缺 pas.sims.community")
    after = [li.text for li in r.findall("loadAfter/li")]
    check("pas.sims.community" in after, "About loadAfter 缺 pas.sims.community")

# 3. 交叉引用：Profile→Type→WorldObjectDef；TypeDef defenderPointsFactor 在 (0,1]
type_defs = {}
wo_defs = set()
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        if node.tag == "pas.outposts.OutpostTypeDef":
            type_defs[node.findtext("defName")] = node
        if node.tag == "WorldObjectDef":
            wo_defs.add(node.findtext("defName"))
for f, tree in parsed.items():
    for node in tree.getroot().iter("pas.outposts.OutpostProfileDef"):
        for li in node.findall("types/li"):
            t = li.findtext("type")
            check(t in type_defs, f"Profile 引用不存在的 type: {t}")
for name, node in type_defs.items():
    wod = node.findtext("worldObjectDef")
    check(wod in wo_defs, f"Type {name} 引用不存在的 WorldObjectDef: {wod}")
    factor = float(node.findtext("defenderPointsFactor", "1"))
    check(0 < factor <= 1, f"Type {name} defenderPointsFactor 超界: {factor}")

# 4. 恰好一個 isDefault profile
defaults = sum(1 for f, tree in parsed.items()
               for node in tree.getroot().iter("pas.outposts.OutpostProfileDef")
               if (node.findtext("isDefault") or "").strip() == "true")
check(defaults == 1, f"isDefault profile 數量 != 1: {defaults}")

# 5. XML 引用的 pas.outposts.* 類存在於 Source/（只看 Class= 屬性與節點 tag，避免 packageId 誤報）
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in ROOT.glob("Source/**/*.cs"))
classes_in_xml = set()
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        cls = node.get("Class")
        if cls and cls.startswith("pas.outposts."):
            classes_in_xml.add(cls.split(".")[-1])
        if node.tag.startswith("pas.outposts."):
            classes_in_xml.add(node.tag.split(".")[-1])
        for child in node:
            if child.tag == "worldObjectClass" and (child.text or "").startswith("pas.outposts."):
                classes_in_xml.add(child.text.split(".")[-1])
for cls in sorted(classes_in_xml):
    check(re.search(rf"class\s+{cls}\b", src) is not None, f"XML 引用的類 pas.outposts.{cls} 不存在於 Source/")

# 6. C# 引用的 pas_outposts_* defName/key 都在 XML（DefOf/Translate 防呆）
xml_all = "\n".join(f.read_text(encoding="utf-8", errors="ignore") for f in xml_files)
for ref in set(re.findall(r"pas_outposts_\w+", src)):
    check(ref in xml_all, f"C# 引用的 defName/key 不在任何 XML: {ref}")

# 7. 相依鏈：sims-mode 的 VisitMap 類存在 + 其 patch 檔存在（Task 3 交付完整性）
sims_src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in SIMS.glob("Source/**/*.cs"))
check("class CaravanArrivalAction_VisitMap" in sims_src, "sims-mode 缺 CaravanArrivalAction_VisitMap（Task 3 未交付？）")
check((SIMS / "Patches" / "Settlement_VisitMap.xml").exists(), "sims-mode 缺 Settlement_VisitMap.xml patch")

# 8. GenStepDef order < 9999（必須跑在 sims-mode SettlementLife 之前）
for f, tree in parsed.items():
    for node in tree.getroot().iter("GenStepDef"):
        if node.findtext("defName") == "pas_outposts_TrimDefenders":
            check(float(node.findtext("order", "0")) < 9999, "TrimDefenders order 必須 < 9999")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
