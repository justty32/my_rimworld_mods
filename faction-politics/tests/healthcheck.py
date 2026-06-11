#!/usr/bin/env python3
"""faction-politics 靜態健檢。無遊戲環境下抓 XML/交叉引用/軟相容不變式錯誤。"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent          # faction-politics/
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


# 1. 所有 XML well-formed（Defs/Languages/About/loadFolders）
xml_files = list(ROOT.glob("Defs/**/*.xml")) + list(ROOT.glob("Languages/**/*.xml")) \
    + [ROOT / "About" / "About.xml", ROOT / "loadFolders.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

# 2. About.xml：packageId 正確；零硬相依（不得有 modDependencies）；loadAfter 含兩姊妹 mod
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.politics.community", "About packageId")
    check(r.find("modDependencies") is None, "零硬相依不變式：About 不得有 modDependencies")
    after = [li.text for li in r.findall("loadAfter/li")]
    check("pas.sims.community" in after, "About loadAfter 缺 pas.sims.community")
    check("pas.outposts.community" in after, "About loadAfter 缺 pas.outposts.community")

# 3. loadFolders.xml：IfModActive 條目指向 pas.outposts.community 且資料夾存在（建置後）
lf = parsed.get(ROOT / "loadFolders.xml")
if lf is not None:
    conds = [(li.get("IfModActive"), li.text) for li in lf.getroot().iter("li") if li.get("IfModActive")]
    check(("pas.outposts.community", "Compat/NpcOutposts") in conds,
          "loadFolders 缺 IfModActive=pas.outposts.community → Compat/NpcOutposts")
    check((ROOT / "Compat" / "NpcOutposts" / "Assemblies").exists(),
          "Compat/NpcOutposts/Assemblies 不存在（bridge 未建置？）")

# 4. 恰好 1 個 isDefault profile、恰好 1 個 PoliticsSettingsDef；數值邊界
defaults = 0
settings = 0
for f, tree in parsed.items():
    for node in tree.getroot().iter("pas.politics.RebellionProfileDef"):
        if (node.findtext("isDefault") or "").strip() == "true":
            defaults += 1
        thr = float(node.findtext("threshold", "100"))
        check(thr > 0, f"{node.findtext('defName')} threshold 必須 > 0")
        ms = int(node.findtext("minSettlements", "2"))
        check(ms >= 2, f"{node.findtext('defName')} minSettlements 必須 >= 2")
    for node in tree.getroot().iter("pas.politics.PoliticsSettingsDef"):
        settings += 1
        check(int(node.findtext("maxDynamicFactions", "5")) >= 1, "maxDynamicFactions 必須 >= 1")
check(defaults == 1, f"isDefault profile 數量 != 1: {defaults}")
check(settings == 1, f"PoliticsSettingsDef 數量 != 1: {settings}")

# 5. XML 引用的 pas.politics.* 類存在於 Source/（結構化掃描，防 packageId 誤報）
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in ROOT.glob("Source/**/*.cs"))
classes_in_xml = set()
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        cls = node.get("Class")
        if cls and cls.startswith("pas.politics."):
            classes_in_xml.add(cls.split(".")[-1])
        if node.tag.startswith("pas.politics."):
            classes_in_xml.add(node.tag.split(".")[-1])
for cls in sorted(classes_in_xml):
    check(re.search(rf"class\s+{cls}\b", src) is not None, f"XML 引用的類 pas.politics.{cls} 不存在於 Source/")

# 6. C# 引用的 pas_politics_* defName/key 都在 XML（Defs defName ∪ Languages Keyed key）
known = set()
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        if node.tag == "defName" and node.text:
            known.add(node.text.strip())
    if "Languages" in str(f):
        root = tree.getroot()
        if root.tag == "LanguageData":
            known.update(child.tag for child in root)
for ref in set(re.findall(r"pas_politics_\w+", src)):
    check(ref in known, f"C# 引用的 defName/key 不在任何 XML: {ref}")

# 7. 軟相容不變式：主 Source/ 不得認識第三方型別；bridge 引用的 DLL 存在
check("using pas.outposts" not in src and "NpcOutpost" not in src,
      "主 Source/ 不得引用 pas.outposts（軟相容不變式，bridge 才可）")
check('Reference Include="RimWar' not in (ROOT / "Source" / "FactionPolitics.csproj").read_text(encoding="utf-8"),
      "主 csproj 不得引用 RimWar")
bridge_csproj = ROOT / "SourceBridgeOutposts" / "FactionPoliticsOutpostsBridge.csproj"
if bridge_csproj.exists():
    check((ROOT.parent / "npc-outposts" / "1.6" / "Assemblies" / "NpcOutposts.dll").exists(),
          "bridge 引用的 NpcOutposts.dll 不存在（npc-outposts 未建置？）")

# 8. 翻譯完整性：English 與 ChineseTraditional 的 Keyed key 集合一致
def keyed(lang):
    keys = set()
    for f in ROOT.glob(f"Languages/{lang}/Keyed/*.xml"):
        if f in parsed:
            keys.update(child.tag for child in parsed[f].getroot())
    return keys
en, zh = keyed("English"), keyed("ChineseTraditional")
check(en == zh, f"翻譯 key 集合不一致：EN-only={sorted(en - zh)} ZH-only={sorted(zh - en)}")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
