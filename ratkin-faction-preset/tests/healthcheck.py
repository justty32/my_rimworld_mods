#!/usr/bin/env python3
"""ratkin-faction-preset 靜態健檢（不啟動遊戲）。

檢查項目：
1. 本 mod 全部 XML well-formed。
2. FactionDef 引用的外部 defName（PawnKindDef / TraderKindDef / CultureDef /
   XenotypeDef / RulePackDef）都真實存在於 NewRatkinPlus 或原版 Core 或本 mod。
3. RulePack rulesFiles 指到的詞庫檔存在於 NewRatkinPlus 英文語系。
4. factionIconPath / settlementTexturePath 對應貼圖存在。
5. Preset.xml 頂層欄位都在已知 schema 白名單內；
   savedFactionDefs / factionCountsStrings 引用的派系 defName 都存在。
"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

MOD = Path(__file__).resolve().parent.parent
RATKIN = Path.home() / ".steam/steam/steamapps/workshop/content/294100/1578693166"
CORE = Path.home() / ".steam/steam/steamapps/common/RimWorld/Data/Core"

errors: list[str] = []
def err(msg: str) -> None:
    errors.append(msg)

# ---------- 1. well-formed ----------
xml_files = sorted(MOD.rglob("*.xml"))
trees: dict[Path, ET.ElementTree] = {}
for f in xml_files:
    try:
        trees[f] = ET.parse(f)
    except ET.ParseError as e:
        err(f"XML 不合法: {f}: {e}")
if errors:
    print("\n".join(errors)); sys.exit(1)
print(f"[1] {len(xml_files)} 個 XML 全部 well-formed")

# ---------- 建立外部 defName 索引 ----------
def collect_defnames(root_dir: Path) -> set[str]:
    names: set[str] = set()
    for f in root_dir.rglob("*.xml"):
        try:
            text = f.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        names.update(re.findall(r"<defName>([^<]+)</defName>", text))
    return names

ext_names: set[str] = set()
for src in [RATKIN / "1.6", RATKIN / "Contents", RATKIN / "Biotech",
            RATKIN / "Royalty", RATKIN / "Ideology", RATKIN / "Odyssey",
            RATKIN / "Anomaly", CORE / "Defs"]:
    if src.exists():
        ext_names |= collect_defnames(src)
own_names = collect_defnames(MOD / "Defs")
all_names = ext_names | own_names
if not ext_names:
    err("找不到 NewRatkinPlus / Core 的 def 來源目錄，請確認路徑")

# ---------- 2. FactionDef 引用交叉驗證 ----------
fac_file = MOD / "Defs/FactionDefs/Factions_AcornGuild.xml"
fac_root = trees[fac_file].getroot()
fac = fac_root.find("FactionDef")

refs: list[tuple[str, str]] = []  # (defName, 用途)
for tag in ["caravanTraderKinds", "visitorTraderKinds", "baseTraderKinds",
            "allowedCultures", "backstoryCategories"]:
    node = fac.find(tag)
    if node is not None and tag != "backstoryCategories":
        for li in node.findall("li"):
            refs.append((li.text.strip(), tag))
refs.append((fac.findtext("basicMemberKind").strip(), "basicMemberKind"))
refs.append((fac.findtext("factionNameMaker").strip(), "factionNameMaker"))
refs.append((fac.findtext("settlementNameMaker").strip(), "settlementNameMaker"))
xeno = fac.find("xenotypeSet/xenotypeChances")
if xeno is not None:
    for child in xeno:
        refs.append((child.tag, "xenotype"))
for pgm in fac.findall("pawnGroupMakers/li"):
    for group in ["options", "traders", "carriers", "guards"]:
        g = pgm.find(group)
        if g is not None:
            for child in g:
                refs.append((child.tag, f"pawnGroupMakers/{group}"))

for name, use in refs:
    if name not in all_names:
        err(f"引用的 defName 不存在: {name}（{use}）")
print(f"[2] FactionDef 交叉驗證 {len(refs)} 個引用")

# ---------- 3. RulePack rulesFiles 詞庫檔 ----------
rp_file = MOD / "Defs/RulePack/RulePacks_Namers_AcornGuild.xml"
for li in trees[rp_file].getroot().iter("li"):
    if li.text and "->" in li.text and "/" in li.text:
        path = li.text.split("->", 1)[1].strip()
        target = RATKIN / "Contents/Languages/English/Strings" / (path + ".txt")
        if not target.exists():
            err(f"rulesFiles 詞庫檔不存在: {target}")
print("[3] rulesFiles 詞庫檔驗證完成")

# ---------- 4. 貼圖 ----------
for tag, roots in [("factionIconPath", [MOD / "Textures", RATKIN / "Contents/Textures"]),
                   ("settlementTexturePath", [MOD / "Textures", RATKIN / "Contents/Textures"])]:
    p = fac.findtext(tag).strip()
    if not any((r / (p + ".png")).exists() for r in roots):
        err(f"{tag} 貼圖不存在: {p}")
print("[4] 貼圖路徑驗證完成")

# ---------- 5. Preset ----------
KNOWN_TOP = {  # Source/WorldPreset.cs:165-230 ExposeData 的欄位鍵
    "name", "label", "description", "planetType", "difficulty", "sortPriority",
    "biomes", "landmarks", "features",
    "saveFactions", "saveIdeologies", "saveTerrain", "saveBases",
    "saveMapMarkers", "saveMapText", "saveStorykeeperEntries",
    "saveWorldTechLevel", "saveGenerationParameters", "disableExtraBiomes",
    "saveFactionCustomizations",
    "savedFactionDefs", "factionNameOverrides", "factionDescriptionOverrides",
    "factionIconOverrides", "factionIdeoIconOverrides", "factionColorOverrides",
    "factionPopulationOverrides", "savedIdeoFactionMapping",
    "generationData", "worldInfo", "savedSettlementsData",
    "savedMapMarkersData", "savedMapTextFeaturesData", "presetStories",
    "scenParts", "scenPartDefs", "worldTechLevel", "myLittlePlanetSubcount",
}
preset = trees[MOD / "Worldbuilder/AcornWorld/Preset.xml"].getroot()
if preset.tag != "worldPreset":
    err(f"Preset 根節點應為 worldPreset，實為 {preset.tag}")
for child in preset:
    if child.tag not in KNOWN_TOP:
        err(f"Preset 未知頂層欄位: {child.tag}")
preset_factions = [li.text.strip() for li in preset.findall("savedFactionDefs/li")]
preset_counts = [li.text.strip() for li in preset.findall("generationData/factionCountsStrings/li")]
for n in preset_factions + preset_counts:
    if n not in all_names:
        err(f"Preset 引用的 FactionDef 不存在: {n}")
if set(preset_counts) - set(preset_factions):
    err(f"factionCountsStrings 有派系不在 savedFactionDefs: {set(preset_counts) - set(preset_factions)}")
for n in ["TemperateForest", "BorealForest"]:
    if n not in all_names:
        err(f"BiomeDef 不存在: {n}")
print(f"[5] Preset 驗證完成（{len(preset_factions)} 個派系、{len(preset_counts)} 個實例）")

# ---------- 結果 ----------
if errors:
    print("\nFAIL:"); print("\n".join(" - " + e for e in errors)); sys.exit(1)
print("\nPASS: 全部靜態檢查通過")
