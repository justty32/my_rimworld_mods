#!/usr/bin/env python3
"""opening-world-demo 靜態健檢（不啟動遊戲）。

驗證 worldbuilder preset + RelationSeedDef 這條管線的資料一致性：
1. 全部 XML well-formed。
2. Preset 頂層欄位在 worldbuilder schema 白名單內；根節點 worldPreset。
3. saveFactionCustomizations 開關為 True（否則 name/color override 不生效，
   worldbuilder World_FinalizeInit_Patch.cs:67 gate）。
4. Preset 引用的 FactionDef / override 鍵 / factionCountsStrings 都是真實原版 Core 派系。
5. RelationSeedDef 條目：a/b 非空且 a!=b、goodwill 在 [-100,100]、
   引用的派系都在 preset 陣容內（管線一致性）；且不指向永久敵派系（會被略過）。
6. About.xml：packageId 正確；硬相依 pas.relations.community（RelationSeedDef 型別來源）。
"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

MOD = Path(__file__).resolve().parent.parent
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

# ---------- 原版 Core 派系索引 ----------
def collect_defnames(root_dir: Path) -> set[str]:
    names: set[str] = set()
    for f in root_dir.rglob("*.xml"):
        try:
            text = f.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        names.update(re.findall(r"<defName>([^<]+)</defName>", text))
    return names

core_factions: set[str] = set()
perm_enemy: set[str] = set()
fac_dir = CORE / "Defs" / "FactionDefs"
if not fac_dir.exists():
    err(f"找不到原版 FactionDefs 目錄: {fac_dir}")
for f in fac_dir.rglob("*.xml"):
    for fac in trees.get(f, ET.ElementTree(ET.fromstring(f.read_text(encoding="utf-8")))).getroot().iter("FactionDef") \
            if f in trees else ET.parse(f).getroot().iter("FactionDef"):
        dn = fac.findtext("defName")
        if dn:
            core_factions.add(dn.strip())
            if (fac.findtext("permanentEnemy") or "").strip().lower() == "true":
                perm_enemy.add(dn.strip())
if not core_factions:
    err("原版派系索引為空，請確認 Core 路徑")

# ---------- 2/3/4. Preset ----------
KNOWN_TOP = {  # worldbuilder Source/WorldPreset.cs:165-230 ExposeData 欄位鍵
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
preset_files = list(MOD.glob("Worldbuilder/*/Preset.xml"))
if not preset_files:
    err("找不到 Worldbuilder/<名>/Preset.xml")
preset = trees[preset_files[0]].getroot() if preset_files else None
preset_factions: list[str] = []
if preset is not None:
    if preset.tag != "worldPreset":
        err(f"Preset 根節點應為 worldPreset，實為 {preset.tag}")
    for child in preset:
        if child.tag not in KNOWN_TOP:
            err(f"Preset 未知頂層欄位: {child.tag}")
    # 3. name/color override 需 saveFactionCustomizations=True
    has_overrides = preset.find("factionNameOverrides") is not None or preset.find("factionColorOverrides") is not None
    scust = (preset.findtext("saveFactionCustomizations") or "False").strip().lower() == "true"
    if has_overrides and not scust:
        err("有 factionName/ColorOverrides 但 saveFactionCustomizations 非 True → override 不會生效")
    # 4. 派系 defName 存在性
    preset_factions = [li.text.strip() for li in preset.findall("savedFactionDefs/li")]
    counts = [li.text.strip() for li in preset.findall("generationData/factionCountsStrings/li")]
    override_keys = []
    for tag in ["factionNameOverrides", "factionColorOverrides", "factionDescriptionOverrides"]:
        node = preset.find(tag)
        if node is not None:
            override_keys += [li.text.strip() for li in node.findall("keys/li")]
    for n in set(preset_factions + counts + override_keys):
        if n not in core_factions:
            err(f"Preset 引用的 FactionDef 不存在於原版 Core: {n}")
    if set(counts) - set(preset_factions):
        err(f"factionCountsStrings 有派系不在 savedFactionDefs: {set(counts) - set(preset_factions)}")
    if set(override_keys) - set(preset_factions):
        err(f"override 鍵有派系不在 savedFactionDefs: {set(override_keys) - set(preset_factions)}")
print(f"[2-4] Preset 驗證完成（{len(preset_factions)} 個派系）")

# ---------- 5. RelationSeedDef ----------
seed_files = list(MOD.glob("1.6/Defs/**/*.xml"))
seed_pairs = 0
for f in seed_files:
    for node in trees[f].getroot().iter("pas.relations.RelationSeedDef"):
        name = node.findtext("defName") or "?"
        for i, li in enumerate(node.findall("relations/li")):
            a = (li.findtext("a") or "").strip()
            b = (li.findtext("b") or "").strip()
            g = int(li.findtext("goodwill", "0"))
            seed_pairs += 1
            if not a or not b:
                err(f"{name} relations[{i}] a/b 不可空")
            elif a == b:
                err(f"{name} relations[{i}] a 與 b 不可相同: {a}")
            if not (-100 <= g <= 100):
                err(f"{name} relations[{i}] goodwill 越界: {g}")
            for x in (a, b):
                if x and preset_factions and x not in preset_factions:
                    err(f"{name} relations[{i}] 派系 {x} 不在 preset 陣容內（管線不一致）")
                if x in perm_enemy:
                    err(f"{name} relations[{i}] 指向永久敵派系 {x}（會被 seeder 略過，勿列）")
if seed_pairs == 0:
    err("找不到任何 RelationSeedDef 關係條目")
print(f"[5] RelationSeedDef 驗證完成（{seed_pairs} 對關係）")

# ---------- 6. About ----------
about = trees.get(MOD / "About" / "About.xml")
if about is not None:
    r = about.getroot()
    if r.findtext("packageId") != "pas.openingworld.demo":
        err("About packageId 應為 pas.openingworld.demo")
    deps = [li.findtext("packageId") for li in r.findall("modDependencies/li")]
    if "pas.relations.community" not in deps:
        err("About 缺硬相依 pas.relations.community（RelationSeedDef 型別來源）")
print("[6] About 驗證完成")

# ---------- 結果 ----------
if errors:
    print("\nFAIL:"); print("\n".join(" - " + e for e in errors)); sys.exit(1)
print("\nPASS: 全部靜態檢查通過")
