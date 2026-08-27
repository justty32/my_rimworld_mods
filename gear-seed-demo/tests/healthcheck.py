#!/usr/bin/env python3
"""gear-seed-demo 靜態健檢：驗 Def 結構、About 相依引擎、XML well-formed。"""
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


xml_files = list(ROOT.glob("1.6/Defs/**/*.xml")) + [ROOT / "About/About.xml", ROOT / "loadFolders.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

about = parsed.get(ROOT / "About/About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.gear.demo", "About packageId 應為 pas.gear.demo")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    check("pas.gear.community" in deps, "示範需硬相依引擎 pas.gear.community")

VALID_Q = {"Awful", "Poor", "Normal", "Good", "Excellent", "Masterwork", "Legendary"}
seed_count = 0
for f, tree in parsed.items():
    for node in tree.getroot().iter("pas.gear.FactionGearSeedDef"):
        seed_count += 1
        name = node.findtext("defName") or "?"
        check(bool((node.findtext("factionDef") or "").strip()), f"{name} 缺 factionDef")
        kinds = node.findall("kinds/li")
        check(len(kinds) > 0, f"{name} kinds 為空")
        for i, k in enumerate(kinds):
            check(bool((k.findtext("kindDef") or "").strip()), f"{name} kinds[{i}] 缺 kindDef")
            q = k.findtext("quality")
            check(q is None or q.strip() in VALID_Q, f"{name} kinds[{i}] quality 非法：{q}")
            # 每個 gear item 至少要有 thingDef
            for tag in ("apparel", "weapons"):
                for j, li in enumerate(k.findall(f"{tag}/li")):
                    check(bool((li.findtext("thingDef") or "").strip()),
                          f"{name} kinds[{i}].{tag}[{j}] 缺 thingDef")

check(seed_count > 0, "示範沒有任何 FactionGearSeedDef")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print(f"healthcheck OK（{seed_count} 個 FactionGearSeedDef）")
