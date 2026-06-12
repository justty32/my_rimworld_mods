#!/usr/bin/env python3
"""city-economy 靜態健檢。無遊戲環境下抓 XML/交叉引用/相依/鐵律錯誤（仿 P1/P2 風格）。"""
import re
import sys
from pathlib import Path

try:                                    # 防 XXE/billion-laughs；無 defusedxml 時退回 stdlib（只 parse 本倉庫檔案）
    import defusedxml.ElementTree as ET
    from xml.etree.ElementTree import ParseError
except ImportError:
    import xml.etree.ElementTree as ET
    from xml.etree.ElementTree import ParseError

ROOT = Path(__file__).resolve().parent.parent          # city-economy/
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


# 1. 所有 XML well-formed（Patches/Languages/About）
xml_files = list(ROOT.glob("Patches/**/*.xml")) + list(ROOT.glob("Defs/**/*.xml")) \
    + list(ROOT.glob("Languages/**/*.xml")) + [ROOT / "About" / "About.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

# 2. About：packageId；硬相依兩件套（Harmony/RimWar）；officers/settlements 必須是
#    soft-optional——只准 loadAfter、不准 modDependencies（P3 鐵律）
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.sanguo.cityeconomy",
          "About packageId 應為 pas.sanguo.cityeconomy")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    after = [li.text for li in r.findall("loadAfter/li")]
    for dep in ("brrainz.harmony", "Torann.RimWar"):
        check(dep in deps, f"About modDependencies 缺 {dep}")
        check(dep in after, f"About loadAfter 缺 {dep}")
    for soft in ("pas.officers.community", "pas.officers.settlements"):
        check(soft not in deps, f"{soft} 不得列為 modDependencies（soft-optional 鐵律）")
        check(soft in after, f"About loadAfter 缺 {soft}（反射橋需 P2 先載）")

# 3. Patches XML 引用的 pas.sanguo.cityeconomy.* 類存在於 Source/
src_files = [p for p in ROOT.glob("Source/**/*.cs") if "obj" not in p.parts]
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in src_files)
comp_props_refs = 0
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        cls = node.get("Class")
        if cls and cls.startswith("pas.sanguo.cityeconomy."):
            comp_props_refs += 1
            check(re.search(rf"class\s+{cls.split('.')[-1]}\b", src) is not None,
                  f"XML 引用的類 {cls} 不存在於 Source/")
check(comp_props_refs >= 1, "Patches XML 未注入任何 pas.sanguo.cityeconomy comp（H：XML 注入鐵律）")

# 4. C# 引用的 pas_cityecon_* key 都在 XML（Translate 防呆）
xml_all = "\n".join(f.read_text(encoding="utf-8", errors="ignore") for f in xml_files)
for ref in set(re.findall(r"pas_cityecon_\w+", src)):
    if re.search(rf'"{ref}"\s*\.Translate', src):
        check(ref in xml_all, f"C# 引用的 key 不在任何 XML: {ref}")

# 5. 翻譯完整性：English / ChineseTraditional / ChineseSimplified key 集合一致
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

# 6. Harmony 接點齊全且走 TryPatch fail-soft
for name in ("ResolveCombat_Settlement", "ResolveBattle_Settlement", "RegenerateStock",
             "GiveSoldThingToTrader", "GiveSoldThingToPlayer", "GetInspectString"):
    check(name in src, f"Source/ 缺接點 {name}")
check("TryPatch" in src, "HarmonyInit 缺 TryPatch fail-soft 框架")

# 7. 鐵律 guard：
#    a) 絕不觸碰派系級係數（01-architecture：勿動 combatAttribute/growthAttribute）
#    b) 勿選 ThingSetMaker 當貨架注入點（M 段：RimWar 在 RW:6089 patch 它）
for banned in ("combatAttribute", "growthAttribute", "ThingSetMakerDefOf"):
    check(banned not in src, f"Source/ 出現禁用字串 {banned}（鐵律）")

# 8. soft-optional 鐵律：csproj 不得引用 NamedOfficers / SettlementLords（反射橋接）。
#    只檢查真正的 Reference/HintPath 節點，註解裡的說明文字不算。
csproj_path = ROOT / "Source" / "CityEconomy.csproj"
try:
    csproj_root = ET.parse(csproj_path).getroot()
    refs = [el.get("Include", "") for el in csproj_root.iter("Reference")]
    hints = [el.text or "" for el in csproj_root.iter("HintPath")]
    for banned in ("NamedOfficers", "SettlementLords"):
        check(all(banned not in r for r in refs + hints),
              f"csproj 引用了 {banned}（P0/P2 必須 soft-optional 反射）")
except ParseError as e:
    errors.append(f"csproj parse error: {csproj_path}: {e}")
check("pas.officers" in src and "AccessTools.TypeByName" in src,
      "Source/ 缺 P2 反射橋（LordGovernanceBridge）")

# 9. 相依 DLL 存在（建置前提）：RimWar.dll／0Harmony.dll（本機 Steam 路徑）
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
