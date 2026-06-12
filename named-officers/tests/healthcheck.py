#!/usr/bin/env python3
"""named-officers 靜態健檢。無遊戲環境下抓 XML/交叉引用/鐵律違規（零 Harmony、零硬相依）。"""
import re
import sys
from pathlib import Path

try:
    import defusedxml.ElementTree as ET          # 防 XXE/billion-laughs（若環境有裝）
    from xml.etree.ElementTree import ParseError
except ImportError:                              # 後備：stdlib（輸入皆 repo 內信任檔案）
    import xml.etree.ElementTree as ET
    from xml.etree.ElementTree import ParseError

ROOT = Path(__file__).resolve().parent.parent          # named-officers/
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


# 1. 所有 XML well-formed（Defs/Languages/About）
xml_files = list(ROOT.glob("Defs/**/*.xml")) + list(ROOT.glob("Languages/**/*.xml")) \
    + [ROOT / "About" / "About.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

# 2. About：packageId 正確；無 modDependencies（零硬相依鐵律，防手滑加依賴）
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.officers.community", "About packageId")
    check(r.find("modDependencies") is None, "About 不得有 modDependencies（零硬相依鐵律）")

# 3. csproj：無第三方 DLL Reference（雙保險）；RootNamespace 正確
csproj = (ROOT / "Source" / "NamedOfficers.csproj").read_text(encoding="utf-8")
check("<Reference Include=" not in csproj, "csproj 不得 Reference 第三方 DLL（零硬相依鐵律）")
check("<RootNamespace>pas.officers</RootNamespace>" in csproj, "csproj RootNamespace != pas.officers")

# 4. XML 引用的 pas.officers.* 類存在於 Source/
src_files = [p for p in ROOT.glob("Source/**/*.cs") if "obj" not in p.parts]
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in src_files)
classes_in_xml = set()
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        cls = node.get("Class")
        if cls and cls.startswith("pas.officers."):
            classes_in_xml.add(cls.split(".")[-1])
        if node.tag.startswith("pas.officers."):
            classes_in_xml.add(node.tag.split(".")[-1])
for cls in sorted(classes_in_xml):
    check(re.search(rf"class\s+{cls}\b", src) is not None,
          f"XML 引用的類 pas.officers.{cls} 不存在於 Source/")

# 5. C# 引用的 pas_officers_* defName/key 都在 XML（DefOf/Translate 防呆）
xml_all = "\n".join(f.read_text(encoding="utf-8", errors="ignore") for f in xml_files)
for ref in set(re.findall(r"pas_officers_\w+", src)):
    check(ref in xml_all, f"C# 引用的 defName/key 不在任何 XML: {ref}")

# 6. Def 完整性：恰好 1 個 SettingsDef、參數界限、兩個 reflexive PawnRelationDef
settings = [node for f, tree in parsed.items()
            for node in tree.getroot().iter("pas.officers.OfficersSettingsDef")]
check(len(settings) == 1, f"OfficersSettingsDef 數量 != 1: {len(settings)}")
if settings:
    check(int(settings[0].findtext("checkIntervalTicks", "0")) > 0, "checkIntervalTicks 必須 > 0")
    check(int(settings[0].findtext("maxOfficersPerObject", "0")) >= 1, "maxOfficersPerObject 必須 >= 1")
relations = {node.findtext("defName"): node for f, tree in parsed.items()
             for node in tree.getroot().iter("PawnRelationDef")}
for name in ("pas_officers_SwornBrother", "pas_officers_BloodFeud"):
    check(name in relations, f"缺 PawnRelationDef: {name}")
    if name in relations:
        check((relations[name].findtext("reflexive") or "").strip() == "true",
              f"{name} 必須 reflexive=true")

# 7. OfficerRecord.ExposeData 七維欄位全 scribe（防「加欄位忘 scribe」存檔 bug）
record_src = (ROOT / "Source" / "Data" / "OfficerRecord.cs").read_text(encoding="utf-8")
for field in ("might", "command", "polity", "charisma", "loyalty", "intellect", "morale"):
    check(re.search(rf"Scribe_Values\.Look\(ref {field}\b", record_src) is not None,
          f"OfficerRecord.ExposeData 缺七維欄位 scribe: {field}")

# 8. 守住 G3/零相依決議：Source/ 內 grep 不到 Harmony / RimWar / FactionColonies
for banned in ("Harmony", "RimWar", "FactionColonies"):
    check(banned not in src, f"Source/ 出現禁用字串 {banned}（零 Harmony/零硬相依鐵律）")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
