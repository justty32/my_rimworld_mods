#!/usr/bin/env python3
"""empire-outposts-war 靜態健檢。無遊戲環境下抓 XML/交叉引用/相依/hook 對齊錯誤。"""
import re
import sys
from pathlib import Path

try:                                    # 防 XXE；無 defusedxml 時退回 stdlib（只 parse 本倉庫檔案）
    import defusedxml.ElementTree as ET
except ImportError:
    import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent          # empire-outposts-war/
MODS = ROOT.parent
OUTPOSTS = MODS / "npc-outposts"
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


# 1. 所有 XML well-formed（Patches/Defs/Languages/About）
xml_files = list(ROOT.glob("Patches/**/*.xml")) + list(ROOT.glob("Defs/**/*.xml")) \
    + list(ROOT.glob("Languages/**/*.xml")) + [ROOT / "About" / "About.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

# 2. About.xml：packageId；硬相依六件套；loadAfter 同六者
about = parsed.get(ROOT / "About" / "About.xml")
DEPS = ("brrainz.harmony", "Matathias.Empire", "Torann.RimWar",
        "pas.outposts.community", "pas.outposts.rimwar", "pas.empire.warfare")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.empire.outposts.war",
          "About packageId 應為 pas.empire.outposts.war")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    after = [li.text for li in r.findall("loadAfter/li")]
    for dep in DEPS:
        check(dep in deps, f"About modDependencies 缺 {dep}")
        check(dep in after, f"About loadAfter 缺 {dep}")

# 3. Defs：附庸 profile defName 存在；Patches 引用它且引用 npc-outposts 的 OutpostProfileExtension
src_defs = "\n".join(p.read_text(encoding="utf-8") for p in ROOT.glob("Defs/**/*.xml"))
check("pas_empire_war_Profile_Vassal" in src_defs, "Defs 缺附庸 profile pas_empire_war_Profile_Vassal")
check("pas.outposts.OutpostProfileDef" in src_defs, "Defs 附庸 profile 類名須為 pas.outposts.OutpostProfileDef")
patch_src = "\n".join(p.read_text(encoding="utf-8") for p in ROOT.glob("Patches/**/*.xml"))
check('PColony' in patch_src, "Patches 應對 PColony 派系掛 profile（功能 1）")
check("pas.outposts.OutpostProfileExtension" in patch_src,
      "Patches 應掛 npc-outposts 的 OutpostProfileExtension")
check("pas_empire_war_Profile_Vassal" in patch_src, "Patches 應指向附庸 profile")
# Patches 引用的 type 須真的存在於 npc-outposts Defs
outpost_types = set()
for f in OUTPOSTS.glob("Defs/**/*.xml"):
    try:
        for node in ET.parse(f).getroot().iter("defName"):
            if node.text:
                outpost_types.add(node.text.strip())
    except ET.ParseError:
        pass
for t in set(re.findall(r"<type>([^<]+)</type>", src_defs)):
    check(t in outpost_types, f"附庸 profile 引用的 OutpostTypeDef 不存在於 npc-outposts: {t}")

# 4. C# 三項功能接點齊備
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in ROOT.glob("Source/**/*.cs"))
# 功能 1：opt-in 母體覆寫 + 稅務參與者
check("ParentEligibilityOverride" in src, "Source/ 未註冊 npc-outposts ParentEligibilityOverride hook（功能 1 增生）")
check("ITaxTickParticipant" in src and "TaxTickRegistry.Register" in src,
      "Source/ 缺稅務參與者（功能 1 產出加成）")
# 功能 2：IBattleModifier 走 BattleModifierRegistry
check("IBattleModifier" in src and "BattleModifierRegistry.Register" in src,
      "Source/ 缺 IBattleModifier（功能 2 防守/前哨緩衝）")
check("CaptureContext" in src and "MilitaryJobHandler_Capture" in src,
      "Source/ 缺 Capture 上下文（功能 2 玩家側削防）")
# 功能 3：LifecycleParticipantBase 走 LifecycleRegistry，雙向
check("LifecycleParticipantBase" in src and "LifecycleRegistry.Register" in src,
      "Source/ 缺 LifecycleRegistry 參與者（功能 3 易主）")
check("OnSettlementCreated" in src and "OnSettlementRemoved" in src,
      "Source/ 功能 3 須同時處理 OnSettlementCreated（奪城）與 OnSettlementRemoved（淪陷）")
# 重註冊防護（Empire ClearCaches 會清 Registry）
check("EmpireCacheUtil.RegisterCacheInvalidator" in src,
      "Source/ 缺 Registry 重註冊防護（EmpireCacheUtil.RegisterCacheInvalidator）")

# 5. npc-outposts 本體接點對齊（唯一新增 hook）
spawner_src = (OUTPOSTS / "Source" / "World" / "WorldComponent_OutpostSpawner.cs").read_text(encoding="utf-8")
check("public static Func<Settlement, bool?> ParentEligibilityOverride" in spawner_src,
      "npc-outposts 缺 ParentEligibilityOverride hook（本 mod 功能 1 接點）")
check("IsEligibleParent" in spawner_src, "npc-outposts 未套用 ParentEligibilityOverride（IsEligibleParent gate）")

# 6. C# 引用的 pas_empire_war_* key 都在 Keyed；雙語一致
known = set()
for f, tree in parsed.items():
    if "Languages" in str(f) and tree.getroot().tag == "LanguageData":
        known.update(child.tag for child in tree.getroot())
for ref in set(re.findall(r"pas_empire_war_\w+", src)):
    check(ref in known, f"C# 引用的 key 不在任何 Keyed XML: {ref}")


def keyed(lang):
    keys = set()
    for f in ROOT.glob(f"Languages/{lang}/Keyed/*.xml"):
        if f in parsed:
            keys.update(child.tag for child in parsed[f].getroot())
    return keys


en, zh = keyed("English"), keyed("ChineseTraditional")
check(en == zh, f"翻譯 key 集合不一致：EN-only={sorted(en - zh)} ZH-only={sorted(zh - en)}")

# 7. 相依 DLL 存在（建置前提）
check((OUTPOSTS / "1.6" / "Assemblies" / "NpcOutposts.dll").exists(),
      "NpcOutposts.dll 不存在（npc-outposts 未建置？）")
check((MODS / "npc-outposts-rimwar" / "1.6" / "Assemblies" / "NpcOutpostsRimWar.dll").exists(),
      "NpcOutpostsRimWar.dll 不存在（Mod1 未建置？）")
check((MODS / "empire-warfare" / "1.6" / "Assemblies" / "EmpireWarfare.dll").exists(),
      "EmpireWarfare.dll 不存在（Mod2 未建置？）")
check((ROOT / "1.6" / "Assemblies" / "EmpireOutpostsWar.dll").exists(),
      "EmpireOutpostsWar.dll 不存在（本 mod 未建置？）")
empire_dll = Path.home() / ".local/share/Steam/steamapps/workshop/content/294100/3701480464/1.6/Assemblies/Empire.dll"
check(empire_dll.exists(), f"Empire.dll 不存在：{empire_dll}")
rimwar_dll = Path.home() / ".local/share/Steam/steamapps/workshop/content/294100/2222935097/v1.6/Assemblies/RimWar.dll"
check(rimwar_dll.exists(), f"RimWar.dll 不存在：{rimwar_dll}")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
