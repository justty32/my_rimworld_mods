#!/usr/bin/env python3
"""faction-relation-seeder 靜態健檢。無遊戲環境下抓 XML/交叉引用/不變式錯誤。"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent          # faction-relation-seeder/
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


# 1. 所有 XML well-formed（Defs/About/loadFolders）
xml_files = list(ROOT.glob("1.6/Defs/**/*.xml")) \
    + [ROOT / "About" / "About.xml", ROOT / "loadFolders.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

# 2. About.xml：packageId 正確；零硬相依（不得有 modDependencies）；僅支援 1.6
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.relations.community", "About packageId 應為 pas.relations.community")
    check(r.find("modDependencies") is None, "零硬相依不變式：About 不得有 modDependencies")
    vers = [li.text for li in r.findall("supportedVersions/li")]
    check("1.6" in vers, "About supportedVersions 缺 1.6")

# 3. loadFolders.xml：含 / 與 1.6
lf = parsed.get(ROOT / "loadFolders.xml")
if lf is not None:
    folders = [li.text for li in lf.getroot().iter("li")]
    check("/" in folders and "1.6" in folders, "loadFolders 需含 / 與 1.6")

# 4. 引擎不自帶關係資料（數據源/執行層分離）；若有任何 RelationSeedDef 仍驗其條目。
#    實際的關係表由消費端內容 mod 提供（見 opening-world-demo）。
for f, tree in parsed.items():
    for node in tree.getroot().iter("pas.relations.RelationSeedDef"):
        name = node.findtext("defName") or "?"
        for i, li in enumerate(node.findall("relations/li")):
            a = (li.findtext("a") or "").strip()
            b = (li.findtext("b") or "").strip()
            g = int(li.findtext("goodwill", "0"))
            check(bool(a) and bool(b), f"{name} relations[{i}] a/b 不可空")
            check(a != b or not a, f"{name} relations[{i}] a 與 b 不可相同：{a}")
            check(-100 <= g <= 100, f"{name} relations[{i}] goodwill 越界：{g}")

# 5. XML 引用的 pas.relations.* 類存在於 Source/
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in ROOT.glob("Source/**/*.cs"))
classes_in_xml = set()
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        if node.tag.startswith("pas.relations."):
            classes_in_xml.add(node.tag.split(".")[-1])
for cls in sorted(classes_in_xml):
    check(re.search(rf"class\s+{cls}\b", src) is not None,
          f"XML 引用的類 pas.relations.{cls} 不存在於 Source/")

# 6. 零 Harmony 不變式：Source/ 與 csproj 皆不得引用 Harmony
check("HarmonyLib" not in src and "new Harmony(" not in src,
      "零 Harmony 不變式：Source/ 不得引用 HarmonyLib")
csproj = (ROOT / "Source" / "FactionRelationSeeder.csproj").read_text(encoding="utf-8")
check("Harmony" not in csproj, "零 Harmony 不變式：csproj 不得引用 Harmony")

# 7. 有 WorldComponent 播種器 + 用 FinalizeInit 鉤子（非 Harmony）
check("class WorldComponent_RelationSeeder" in src, "缺 WorldComponent_RelationSeeder")
check("FinalizeInit" in src, "缺 FinalizeInit 原生鉤子")

# 8. 建置產物存在
dll = ROOT / "1.6" / "Assemblies" / "FactionRelationSeeder.dll"
check(dll.exists(), f"建置產物不存在（未 dotnet build？）：{dll}")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
