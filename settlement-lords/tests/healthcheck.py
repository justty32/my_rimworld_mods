#!/usr/bin/env python3
"""settlement-lords 靜態健檢。無遊戲環境下抓 XML/交叉引用/相依/鐵律錯誤（仿 P1/Mod 1 風格）。"""
import re
import sys
from pathlib import Path

try:                                    # 防 XXE/billion-laughs；無 defusedxml 時退回 stdlib（只 parse 本倉庫檔案）
    import defusedxml.ElementTree as ET
    from xml.etree.ElementTree import ParseError
except ImportError:
    import xml.etree.ElementTree as ET
    from xml.etree.ElementTree import ParseError

ROOT = Path(__file__).resolve().parent.parent          # settlement-lords/
OFFICERS = ROOT.parent / "named-officers"
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

# 2. About：packageId；硬相依三件套（Harmony/RimWar/named-officers）；loadAfter 同三者
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.officers.settlements",
          "About packageId 應為 pas.officers.settlements")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    after = [li.text for li in r.findall("loadAfter/li")]
    for dep in ("brrainz.harmony", "Torann.RimWar", "pas.officers.community"):
        check(dep in deps, f"About modDependencies 缺 {dep}")
        check(dep in after, f"About loadAfter 缺 {dep}")

# 3. 角色 def：恰好 1 個 pas.officers.OfficerRoleDef、defName 正確；
#    類 OfficerRoleDef 真的存在於 named-officers Source（P0 契約跨倉核對）
roles = [node for f, tree in parsed.items()
         for node in tree.getroot().iter("pas.officers.OfficerRoleDef")]
check(len(roles) == 1, f"pas.officers.OfficerRoleDef 數量 != 1: {len(roles)}")
if roles:
    check(roles[0].findtext("defName") == "pas_settlement_Lord",
          "角色 defName 應為 pas_settlement_Lord")
officers_src_files = [p for p in OFFICERS.glob("Source/**/*.cs") if "obj" not in p.parts]
officers_src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in officers_src_files)
check(re.search(r"class\s+OfficerRoleDef\b", officers_src) is not None,
      "named-officers Source 缺 OfficerRoleDef（P0 契約破裂？）")

# 4. XML 引用的 pas.officers.settlements.* 類存在於 Source/
src_files = [p for p in ROOT.glob("Source/**/*.cs") if "obj" not in p.parts]
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in src_files)
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        cls = node.get("Class")
        if cls and cls.startswith("pas.officers.settlements."):
            check(re.search(rf"class\s+{cls.split('.')[-1]}\b", src) is not None,
                  f"XML 引用的類 {cls} 不存在於 Source/")

# 5. C# 引用的 pas_settlement_* key/defName 都在 XML（Translate/DefDatabase 防呆）
xml_all = "\n".join(f.read_text(encoding="utf-8", errors="ignore") for f in xml_files)
for ref in set(re.findall(r"pas_settlement_\w+", src)):
    check(ref in xml_all, f"C# 引用的 key/defName 不在任何 XML: {ref}")

# 6. 翻譯完整性：English / ChineseTraditional / ChineseSimplified key 集合一致
def keyed(lang):
    keys = set()
    for f in ROOT.glob(f"Languages/{lang}/Keyed/*.xml"):
        if f in parsed and parsed[f].getroot().tag == "LanguageData":
            keys.update(child.tag for child in parsed[f].getroot())
    return keys
en, zht, zhs = keyed("English"), keyed("ChineseTraditional"), keyed("ChineseSimplified")
check(en == zht, f"翻譯 key 不一致 EN/zh-TW：EN-only={sorted(en - zht)} TW-only={sorted(zht - en)}")
check(en == zhs, f"翻譯 key 不一致 EN/zh-CN：EN-only={sorted(en - zhs)} CN-only={sorted(zhs - en)}")
check(len(en) > 0, "English Keyed 為空")

# 7. Harmony 接點齊全且走 TryPatch fail-soft
for name in ("IncrementSettlementGrowth", "GetInspectString"):
    check(name in src, f"Source/ 缺 RimWar 接點 {name}")
check("TryPatch" in src, "HarmonyInit 缺 TryPatch fail-soft 框架")

# 8. 鐵律 guard：絕不觸碰派系級係數（01-architecture：勿動 combatAttribute/growthAttribute）
for banned in ("combatAttribute", "growthAttribute"):
    check(banned not in src, f"Source/ 出現禁用字串 {banned}（派系級係數鐵律）")

# 9. 相依 DLL 存在（建置前提）：NamedOfficers.dll；RimWar.dll／0Harmony.dll（本機 Steam 路徑）
check((OFFICERS / "1.6" / "Assemblies" / "NamedOfficers.dll").exists(),
      "NamedOfficers.dll 不存在（named-officers 未建置？）")
rimwar_dll = Path.home() / ".local/share/Steam/steamapps/workshop/content/294100/2222935097/v1.6/Assemblies/RimWar.dll"
check(rimwar_dll.exists(), f"RimWar.dll 不存在：{rimwar_dll}")
harmony_dll = Path.home() / ".local/share/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies/0Harmony.dll"
check(harmony_dll.exists(), f"0Harmony.dll 不存在：{harmony_dll}")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
