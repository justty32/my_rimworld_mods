#!/usr/bin/env python3
"""npc-outposts-rimwar 靜態健檢。無遊戲環境下抓 XML/交叉引用/相依錯誤。"""
import re
import sys
from pathlib import Path

try:                                    # 防 XXE/billion-laughs；無 defusedxml 時退回 stdlib（只 parse 本倉庫檔案）
    import defusedxml.ElementTree as ET
except ImportError:
    import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent          # npc-outposts-rimwar/
OUTPOSTS = ROOT.parent / "npc-outposts"
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


# 1. 所有 XML well-formed（Patches/Languages/About）
xml_files = list(ROOT.glob("Patches/**/*.xml")) + list(ROOT.glob("Languages/**/*.xml")) \
    + [ROOT / "About" / "About.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

# 2. About.xml：packageId；硬相依三件套（Harmony/RimWar/npc-outposts）；loadAfter 同三者
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.outposts.rimwar", "About packageId 應為 pas.outposts.rimwar")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    for dep in ("brrainz.harmony", "Torann.RimWar", "pas.outposts.community"):
        check(dep in deps, f"About modDependencies 缺 {dep}")
    after = [li.text for li in r.findall("loadAfter/li")]
    for dep in ("brrainz.harmony", "Torann.RimWar", "pas.outposts.community"):
        check(dep in after, f"About loadAfter 缺 {dep}")

# 3. Patches：xpath 引用的 defName 真的存在於 npc-outposts 的 Defs；
#    comp 注入類名＝RimWar 權威類名（深掘文件核對值）
outpost_defnames = set()
for f in OUTPOSTS.glob("Defs/**/*.xml"):
    try:
        for node in ET.parse(f).getroot().iter("defName"):
            if node.text:
                outpost_defnames.add(node.text.strip())
    except ET.ParseError as e:
        errors.append(f"npc-outposts XML parse error: {f}: {e}")
patch_src = "\n".join(p.read_text(encoding="utf-8") for p in ROOT.glob("Patches/**/*.xml"))
for defname in set(re.findall(r'defName="([^"]+)"', patch_src)):
    check(defname in outpost_defnames, f"Patches xpath 引用的 defName 不存在於 npc-outposts: {defname}")
check('RimWar.Planet.WorldObjectCompProperties_RimWarSettlement' in patch_src,
      "Patches 缺 RimWar 聚落 comp 注入（功能 2 核心）")

# 4. XML 引用的 pas.outposts.rimwar.* 類存在於 Source/
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in ROOT.glob("Source/**/*.cs"))
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        cls = node.get("Class")
        if cls and cls.startswith("pas.outposts.rimwar."):
            check(re.search(rf"class\s+{cls.split('.')[-1]}\b", src) is not None,
                  f"XML 引用的類 {cls} 不存在於 Source/")

# 5. C# 引用的 pas_outposts_rimwar_* key 都在 Languages Keyed
known = set()
for f, tree in parsed.items():
    if "Languages" in str(f) and tree.getroot().tag == "LanguageData":
        known.update(child.tag for child in tree.getroot())
for ref in set(re.findall(r"pas_outposts_rimwar_\w+", src)):
    check(ref in known, f"C# 引用的 key 不在任何 Keyed XML: {ref}")

# 6. 翻譯完整性：English 與 ChineseTraditional key 集合一致
def keyed(lang):
    keys = set()
    for f in ROOT.glob(f"Languages/{lang}/Keyed/*.xml"):
        if f in parsed:
            keys.update(child.tag for child in parsed[f].getroot())
    return keys
en, zh = keyed("English"), keyed("ChineseTraditional")
check(en == zh, f"翻譯 key 集合不一致：EN-only={sorted(en - zh)} ZH-only={sorted(zh - en)}")

# 7. Harmony 接點與 npc-outposts hook 對齊：
#    本 mod patch 的 RimWar 方法名出現在 HarmonyInit；hook 名與 npc-outposts 本體一致
for name in ("IncrementSettlementGrowth", "ConvertSettlement", "ResolveBattle_Settlement"):
    check(name in src, f"Source/ 缺 RimWar 接點 {name}")
spawner_src = (OUTPOSTS / "Source" / "World" / "WorldComponent_OutpostSpawner.cs").read_text(encoding="utf-8")
check("public static Func<Faction, float> GrowthRateMultiplier" in spawner_src,
      "npc-outposts 缺 GrowthRateMultiplier hook（本 mod 功能 4 接點）")
check("GrowthRateMultiplier" in src, "Source/ 未註冊 npc-outposts GrowthRateMultiplier hook")

# 8. 相依 DLL 存在（建置前提）：NpcOutposts.dll；RimWar.dll（本機 Steam 路徑，注意 v1.6）
check((OUTPOSTS / "1.6" / "Assemblies" / "NpcOutposts.dll").exists(),
      "NpcOutposts.dll 不存在（npc-outposts 未建置？）")
rimwar_dll = Path.home() / ".local/share/Steam/steamapps/workshop/content/294100/2222935097/v1.6/Assemblies/RimWar.dll"
check(rimwar_dll.exists(), f"RimWar.dll 不存在：{rimwar_dll}")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
