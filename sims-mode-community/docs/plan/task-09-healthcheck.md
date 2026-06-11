# Task 9: 靜態健檢（healthcheck.py）

> 屬於 `../2026-06-11-implementation-plan.md`。

**Files:**
- Create: `sims-mode-community/tests/healthcheck.py`

- [ ] **Step 1: 寫 healthcheck.py**

```python
"""sims-mode-community 靜態健檢（仿 colony-archival-outpost/tests/healthcheck.py）。
跑法：python tests/healthcheck.py（在 sims-mode-community/ 下）"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
errors = []

# 1) 所有 XML well-formed
xml_files = [p for p in ROOT.rglob("*.xml") if "Assemblies" not in p.parts and "obj" not in p.parts and "bin" not in p.parts]
trees = {}
for p in xml_files:
    try:
        trees[p] = ET.parse(p)
    except ET.ParseError as e:
        errors.append(f"XML parse error {p}: {e}")

def defs_of(tag):
    """收集指定 root child tag 的所有 defName。"""
    found = set()
    for p, t in trees.items():
        if t.getroot().tag != "Defs":
            continue
        for node in t.getroot():
            if node.tag == tag:
                dn = node.find("defName")
                if dn is not None:
                    found.add(dn.text)
    return found

duty_defs = defs_of("DutyDef")
facility_defs = defs_of("pas.sims.FacilityTagDef")
role_defs = defs_of("pas.sims.LifeRoleDef")
profile_defs = defs_of("pas.sims.LifeProfileDef")
genstep_defs = defs_of("GenStepDef")
job_defs = defs_of("JobDef")

# 2) 角色作息表的交叉引用：duty / focusFacility 都存在
for p, t in trees.items():
    if t.getroot().tag != "Defs":
        continue
    for node in t.getroot():
        if node.tag != "pas.sims.LifeRoleDef":
            continue
        name = node.findtext("defName")
        req = node.findtext("requiredFacility")
        if req and req not in facility_defs:
            errors.append(f"LifeRoleDef {name}: requiredFacility {req} 不存在")
        for li in node.findall("./schedule/li"):
            duty = li.findtext("duty")
            if duty and duty.startswith("pas_sims_") and duty not in duty_defs:
                errors.append(f"LifeRoleDef {name}: duty {duty} 不存在")
            ff = li.findtext("focusFacility")
            if ff and ff not in facility_defs:
                errors.append(f"LifeRoleDef {name}: focusFacility {ff} 不存在")
            f, to = li.findtext("from"), li.findtext("to")
            if f is None or to is None:
                errors.append(f"LifeRoleDef {name}: schedule li 缺 from/to")

# 3) profile 的 role 引用存在；恰一個 isDefault
default_count = 0
for p, t in trees.items():
    if t.getroot().tag != "Defs":
        continue
    for node in t.getroot():
        if node.tag != "pas.sims.LifeProfileDef":
            continue
        name = node.findtext("defName")
        if node.findtext("isDefault") == "true":
            default_count += 1
        for li in node.findall("./roles/li"):
            r = li.findtext("role")
            if r and r not in role_defs:
                errors.append(f"LifeProfileDef {name}: role {r} 不存在")
if default_count != 1:
    errors.append(f"isDefault profile 數量應為 1，實際 {default_count}")

# 4) GenStep patch 鏈：GenStepDef 存在、patch 引用一致、目標為 Base_Faction
patch = ROOT / "Patches" / "MapGenerator_SettlementLife.xml"
if patch in trees:
    text = patch.read_text(encoding="utf-8")
    if 'defName="Base_Faction"' not in text:
        errors.append("Patch 未指向 Base_Faction MapGeneratorDef")
    if "pas_sims_SettlementLife" not in text:
        errors.append("Patch 未引用 pas_sims_SettlementLife")
if "pas_sims_SettlementLife" not in genstep_defs:
    errors.append("GenStepDef pas_sims_SettlementLife 不存在")

# 5) XML 引用的 pas.sims.* C# 類別都在 Source/ 出現
src = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (ROOT / "Source").rglob("*.cs"))
classes_in_xml = set()
for p, t in trees.items():
    for node in t.iter():
        cls = node.get("Class")
        if cls and cls.startswith("pas.sims."):
            classes_in_xml.add(cls.split(".")[-1])
        if node.tag.startswith("pas.sims."):
            classes_in_xml.add(node.tag.split(".")[-1])
for cls in sorted(classes_in_xml):
    if not re.search(rf"class\s+{cls}\b", src):
        errors.append(f"XML 引用的類別 {cls} 不在 Source/ 中")

# 6) C# 引用的 pas_sims_* defName 都在 XML 定義（DefOf 防呆）
for m in set(re.findall(r"pas_sims_\w+", src)):
    if m not in (duty_defs | facility_defs | role_defs | profile_defs | genstep_defs | job_defs):
        errors.append(f"C# 引用的 defName {m} 未在任何 Defs XML 定義")

# 7) About.xml
about = ROOT / "About" / "About.xml"
if about in trees:
    a = trees[about].getroot()
    if a.findtext("packageId") != "pas.sims.community":
        errors.append("About.xml packageId 不符")
    if "1.6" not in [li.text for li in a.findall("./supportedVersions/li")]:
        errors.append("About.xml 缺 supportedVersions 1.6")

if errors:
    print(f"FAIL ({len(errors)}):")
    for e in errors:
        print(" -", e)
    sys.exit(1)
print("healthcheck OK")
```

- [ ] **Step 2: 跑健檢**

Run: `python sims-mode-community/tests/healthcheck.py`（或 cd 進 mod 目錄跑）
Expected: `healthcheck OK`。有 FAIL 就修到綠。

- [ ] **Step 3: Commit**

```
git add sims-mode-community/tests
git commit -m "test: 靜態健檢（XML/交叉引用/patch 鏈/DefOf 防呆）"
```
