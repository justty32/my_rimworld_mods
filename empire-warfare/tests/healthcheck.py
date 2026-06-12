#!/usr/bin/env python3
"""empire-warfare 靜態健檢。無遊戲環境下抓 XML/相依宣告/翻譯/csproj 不變式錯誤。"""
import re
import sys
from pathlib import Path

try:  # 防 XXE：優先 defusedxml，無則退回 stdlib（健檢對象皆本倉庫自有檔案）
    import defusedxml.ElementTree as ET
except ImportError:
    import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent          # empire-warfare/
errors = []


def check(cond, msg):
    if not cond:
        errors.append(msg)


# 1. 所有 XML well-formed（About/Languages）
xml_files = list(ROOT.glob("Languages/**/*.xml")) + [ROOT / "About" / "About.xml"]
parsed = {}
for f in xml_files:
    try:
        parsed[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"XML parse error: {f}: {e}")

# 2. About.xml：packageId / 1.6 / 三硬相依 / loadAfter
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.empire.warfare", "About packageId 應為 pas.empire.warfare")
    check("1.6" in [li.text for li in r.findall("supportedVersions/li")], "About 缺 supportedVersions 1.6")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    for dep in ("brrainz.harmony", "Matathias.Empire", "Torann.RimWar"):
        check(dep in deps, f"About modDependencies 缺 {dep}")
    after = [li.text for li in r.findall("loadAfter/li")]
    for dep in ("Matathias.Empire", "Torann.RimWar"):
        check(dep in after, f"About loadAfter 缺 {dep}")

# 3. csproj 不變式：輸出到 1.6/Assemblies、所有外部 Reference Private=False
csproj_path = ROOT / "Source" / "EmpireWarfare.csproj"
csproj = csproj_path.read_text(encoding="utf-8")
check("..\\1.6\\Assemblies" in csproj, "csproj OutputPath 應指向 ..\\1.6\\Assemblies\\")
ref_blocks = re.findall(r"<Reference Include=\"[^\"]+\">(.*?)</Reference>", csproj, re.S)
for blk in ref_blocks:
    check("<Private>False</Private>" in blk, "csproj 有外部 Reference 未設 Private=False")
for name in ("Empire.dll", "Empire.RW.dll", "RimWar.dll", "0Harmony.dll"):
    check(name in csproj, f"csproj 缺參考 {name}")

# 4. 參考的工作坊 DLL 在本機存在（HintPath 解析）
home = Path.home()
workshop = home / ".local/share/Steam/steamapps/workshop/content/294100"
for rel in ("3701480464/1.6/Assemblies/Empire.dll",
            "3701480464/Compat/1.6/RimWar/Assemblies/Empire.RW.dll",
            "2222935097/v1.6/Assemblies/RimWar.dll",
            "2009463077/Current/Assemblies/0Harmony.dll"):
    check((workshop / rel).exists(), f"工作坊 DLL 不存在：{rel}")

# 5. 建置產物存在
check((ROOT / "1.6" / "Assemblies" / "EmpireWarfare.dll").exists(),
      "1.6/Assemblies/EmpireWarfare.dll 不存在（未建置？）")

# 6. C# 引用的 pas_warfare_* 翻譯 key 都在 Keyed XML
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in ROOT.glob("Source/**/*.cs"))
known = set()
for f, tree in parsed.items():
    if "Languages" in str(f) and tree.getroot().tag == "LanguageData":
        known.update(child.tag for child in tree.getroot())
for ref in set(re.findall(r"pas_warfare_\w+", src)):
    check(ref in known, f"C# 引用的翻譯 key 不在任何 Keyed XML: {ref}")

# 7. 翻譯完整性：English 與 ChineseTraditional 的 Keyed key 集合一致
def keyed(lang):
    keys = set()
    for f in ROOT.glob(f"Languages/{lang}/Keyed/*.xml"):
        if f in parsed:
            keys.update(child.tag for child in parsed[f].getroot())
    return keys
en, zh = keyed("English"), keyed("ChineseTraditional")
check(en == zh, f"翻譯 key 集合不一致：EN-only={sorted(en - zh)} ZH-only={sorted(zh - en)}")
check(len(en) > 0, "English Keyed 為空")

# 8. 防衛式不變式：Init 防護 / Registry 重註冊 / 不呼叫 ConvertSettlement
check("StaticConstructorOnStartup" in src, "缺 StaticConstructorOnStartup 入口")
check("RegisterCacheInvalidator" in src, "缺 EmpireCacheUtil.RegisterCacheInvalidator 重註冊防護（ClearCaches 會清空 Registry）")
check("AccessTools.Method" in src, "缺 Harmony 目標簽章探測（fail-soft）")
check(re.search(r"ConvertSettlement\s*\(", src) is None,
      "不得呼叫 RimWar 的 ConvertSettlement（會撞 Empire prefix 與 destroyFlag 守衛）")
check("RemovePlayerSettlement" in src, "附庸退場必須走 ColonyUtil.RemovePlayerSettlement")
check("CreateRimWarSettlementWithPoints" in src, "新 NPC 聚落必須註冊進 RimWarData")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
