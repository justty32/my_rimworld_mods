#!/usr/bin/env python3
"""faction-gear-seeder 靜態健檢。無遊戲環境下抓 XML/交叉引用/不變式錯誤。

注意：本 mod 與姊妹 relation-seeder 相反——它「破例引 Harmony」。因此第 6 項是
**反向不變式**：斷言確實用了 Harmony、且只掛在單一目標 PawnGenerator.GeneratePawn。"""
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent          # faction-gear-seeder/
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

# 2. About.xml：packageId 正確；含 Harmony 硬相依；僅支援 1.6
about = parsed.get(ROOT / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    check(r.findtext("packageId") == "pas.gear.community", "About packageId 應為 pas.gear.community")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    check("brrainz.harmony" in deps, "本 mod 用 Harmony：About 需宣告 brrainz.harmony 硬相依")
    vers = [li.text for li in r.findall("supportedVersions/li")]
    check("1.6" in vers, "About supportedVersions 缺 1.6")

# 3. loadFolders.xml：含 / 與 1.6
lf = parsed.get(ROOT / "loadFolders.xml")
if lf is not None:
    folders = [li.text for li in lf.getroot().iter("li")]
    check("/" in folders and "1.6" in folders, "loadFolders 需含 / 與 1.6")

# 4. 引擎不自帶裝備資料（數據源/執行層分離）；若有任何 FactionGearSeedDef 仍驗其結構。
#    實際裝備表由消費端內容 mod 提供（由 yc preset 謄回）。
for f, tree in parsed.items():
    for node in tree.getroot().iter("pas.gear.FactionGearSeedDef"):
        name = node.findtext("defName") or "?"
        check(bool((node.findtext("factionDef") or "").strip()), f"{name} 缺 factionDef")
        kinds = node.findall("kinds/li")
        check(len(kinds) > 0, f"{name} kinds 為空")
        for i, k in enumerate(kinds):
            check(bool((k.findtext("kindDef") or "").strip()), f"{name} kinds[{i}] 缺 kindDef")

# 5. XML 引用的 pas.gear.* 類存在於 Source/
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in ROOT.glob("Source/**/*.cs"))
classes_in_xml = set()
for f, tree in parsed.items():
    for node in tree.getroot().iter():
        if node.tag.startswith("pas.gear."):
            classes_in_xml.add(node.tag.split(".")[-1])
for cls in sorted(classes_in_xml):
    check(re.search(rf"class\s+{cls}\b", src) is not None,
          f"XML 引用的類 pas.gear.{cls} 不存在於 Source/")

# 6. 反向不變式：確實引 Harmony，且只掛單一目標 PawnGenerator.GeneratePawn
check("new Harmony(" in src, "本 mod 應初始化 Harmony（GearSeederBootstrap）")
patch_targets = re.findall(r"\[HarmonyPatch\(typeof\((\w+)\)\s*,\s*nameof\((\w+)\.(\w+)\)", src)
check(patch_targets == [("PawnGenerator", "PawnGenerator", "GeneratePawn")],
      f"補丁面應僅為 PawnGenerator.GeneratePawn，實得：{patch_targets}")
check("[ThreadStatic]" in src, "缺防遞迴 [ThreadStatic] 旗標")

# 7. 引擎關鍵件存在
check("class FactionGearSeedDef" in src, "缺 FactionGearSeedDef")
check("class GearSeedApplier" in src, "缺 GearSeedApplier")
check("pawn.apparel.Wear(" in src, "缺穿戴邏輯 pawn.apparel.Wear")
check("pawn.equipment.AddEquipment(" in src, "缺裝備武器邏輯 pawn.equipment.AddEquipment")

# 8. 建置產物存在，且 Assemblies 只含自家 DLL（未洩漏 Harmony/RimWorld 參照）
asm = ROOT / "1.6" / "Assemblies"
dll = asm / "FactionGearSeeder.dll"
check(dll.exists(), f"建置產物不存在（未 dotnet build？）：{dll}")
stray = [p.name for p in asm.glob("*.dll") if p.name != "FactionGearSeeder.dll"]
check(not stray, f"Assemblies 只應含 FactionGearSeeder.dll，洩漏：{stray}")

# 9. 轉換器煙霧測試：合成 yc preset → 轉出、well-formed、含預期 Def
sample = '''<?xml version="1.0" encoding="utf-8"?>
<SettingsBlock><ModSettings Class="FactionGearCustomizer.FactionGearCustomizerSettings">
<presets><li><name>hc</name><factionGearData><li>
<factionDefName>OutlanderCivil</factionDefName>
<kindGearData><li>
<kindDefName>Town_Guard</kindDefName><itemQuality>Excellent</itemQuality>
<weapons><li><thingDefName>Gun_AssaultRifle</thingDefName></li></weapons>
<specificApparel><li><thing>Apparel_FlakVest</thing><material>Steel</material><quality>Masterwork</quality></li></specificApparel>
</li></kindGearData>
</li></factionGearData></li></presets>
</ModSettings></SettingsBlock>'''
with tempfile.NamedTemporaryFile("w", suffix=".xml", delete=False, encoding="utf-8") as tf:
    tf.write(sample)
    sample_path = tf.name
tool = ROOT / "tools" / "transcribe_yc_preset.py"
res = subprocess.run([sys.executable, str(tool), "--config", sample_path, "--preset", "hc"],
                     capture_output=True, text=True)
check(res.returncode == 0, f"轉換器煙霧測試失敗：{res.stderr.strip()}")
if res.returncode == 0:
    try:
        troot = ET.fromstring(res.stdout)
        seeds = troot.findall("pas.gear.FactionGearSeedDef")
        check(len(seeds) == 1, f"轉換器應輸出 1 個 Def，實得 {len(seeds)}")
        if seeds:
            check(seeds[0].findtext("factionDef") == "OutlanderCivil", "轉換器 factionDef 錯")
            check(seeds[0].findtext("kinds/li/quality") == "Excellent", "轉換器 kind quality 遺失")
            check("Apparel_FlakVest" in res.stdout and "Steel" in res.stdout, "轉換器進階裝備 stuff 遺失")
    except ET.ParseError as e:
        errors.append(f"轉換器輸出非 well-formed XML：{e}")

if errors:
    print("healthcheck FAILED:")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
