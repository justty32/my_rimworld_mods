#!/usr/bin/env python3
"""cqf-example-quests 離線健檢（不啟動遊戲）。

檢查項目：
 1. 所有 XML well-formed
 2. Class="QuestEditor_Library.*" 類名存在於 CQF 反編譯源
 3. CQF 物件的 XML 子元素名 ⊆ 該類（含基底鏈）宣告的 public 欄位名（靜態 schema 比對）
 4. def 交叉引用閉合（quest= / tree= / thingSetMaker= 指向本 mod 內的 defName）
 5. 翻譯 key 三語齊備（message/text/title/failReason/dialogReportKey 引用的 CQFExample_* key）
 6. DialogTree 結構：nodeMoulds 含 key 0、nextIndex 指向存在節點、curIndex > 最大 key

用法：python3 tests/healthcheck.py [反編譯源路徑]
"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

MOD_ROOT = Path(__file__).resolve().parent.parent
DECOMPILED = Path(
    sys.argv[1] if len(sys.argv) > 1 else
    Path.home() / "repo/pas/projects/rimworld_mods/custom-quest-framework/decompiled/QuestEditor_Library/QuestEditor_Library.decompiled.cs"
)

errors, warnings = [], []


# ---------- 反編譯源 → {class: (base, {fields})} ----------
def parse_decompiled(path):
    cls_re = re.compile(r'^\t(?:public|internal)\s+(?:abstract\s+)?(?:sealed\s+)?class\s+(\w+)\s*(?::\s*([\w<>,\s.]+))?')
    fld_re = re.compile(r'^\t\t(?:\[NoTranslate\]\s*)?public\s+(?!static|const|override|virtual|abstract|event)[\w<>,\[\]\s?.]+?\s+(\w+)(?:\s*=\s*[^;]+)?;')
    classes, cur = {}, None
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        m = cls_re.match(line)
        if m:
            base = (m.group(2) or "").split(",")[0].strip()
            cur = m.group(1)
            classes[cur] = (base, set())
            continue
        if cur:
            f = fld_re.match(line)
            if f:
                classes[cur][1].add(f.group(1))
    return classes


def all_fields(classes, name):
    fields, seen = set(), set()
    while name in classes and name not in seen:
        seen.add(name)
        base, flds = classes[name]
        fields |= flds
        name = base
    # Def 基底欄位（Verse.Def）
    fields |= {"defName", "label", "description"}
    return fields


# ---------- 收集 XML ----------
xml_files = sorted(MOD_ROOT.rglob("*.xml"))
trees = {}
for f in xml_files:
    try:
        trees[f] = ET.parse(f)
    except ET.ParseError as e:
        errors.append(f"[well-formed] {f.relative_to(MOD_ROOT)}: {e}")
print(f"XML well-formed：{len(trees)}/{len(xml_files)} 通過")

if not DECOMPILED.exists():
    warnings.append(f"[schema] 找不到反編譯源 {DECOMPILED}，跳過類/欄位比對")
    classes = {}
else:
    classes = parse_decompiled(DECOMPILED)
    print(f"反編譯源解析：{len(classes)} 類")

# ---------- Class 屬性 + 欄位比對 ----------
QEL = "QuestEditor_Library."
checked = 0
for f, tree in trees.items():
    for el in tree.iter():
        cls = el.get("Class") or ""
        tag_cls = el.tag[len(QEL):] if el.tag.startswith(QEL) else None
        name = cls[len(QEL):] if cls.startswith(QEL) else tag_cls
        if not name or not classes:
            continue
        checked += 1
        if name not in classes:
            errors.append(f"[class] {f.name}: 類不存在 {name}")
            continue
        fields = all_fields(classes, name)
        for child in el:
            if child.tag not in fields:
                errors.append(f"[field] {f.name}: {name} 無欄位 <{child.tag}>")
print(f"CQF 類/欄位比對：檢查 {checked} 個節點")

# ---------- def 交叉引用 ----------
defnames = set()
for f, tree in trees.items():
    for el in tree.getroot():
        d = el.find("defName")
        if d is not None:
            defnames.add(d.text)
refs = []
for f, tree in trees.items():
    for el in tree.iter():
        if el.tag in ("quest", "tree", "thingSetMaker") and el.text:
            refs.append((f.name, el.tag, el.text.strip()))
for fname, tag, val in refs:
    if val.startswith("CQFExample") and val not in defnames:
        errors.append(f"[xref] {fname}: <{tag}>{val}</> 不在本 mod defName 集合")
print(f"def 交叉引用：{len(refs)} 條")

# ---------- 翻譯 key ----------
langs = {}
for lang in ("ChineseTraditional", "ChineseSimplified", "English"):
    keys = set()
    for f in (MOD_ROOT / "Languages" / lang).rglob("*.xml"):
        if f in trees:
            keys = {el.tag for el in trees[f].getroot()}
    langs[lang] = keys
used_keys = set()
for f, tree in trees.items():
    if "Languages" in str(f):
        continue
    for el in tree.iter():
        if el.tag in ("message", "text", "title", "failReason", "dialogReportKey") and el.text:
            t = el.text.strip()
            if t.startswith("CQFExample_"):
                used_keys.add(t)
        for li in el.findall("li"):
            pass
# extraText 的 li 也是 key
for f, tree in trees.items():
    if "Languages" in str(f):
        continue
    for el in tree.iter("extraText"):
        for li in el.findall("li"):
            if li.text and li.text.strip().startswith("CQFExample_"):
                used_keys.add(li.text.strip())
for lang, keys in langs.items():
    missing = used_keys - keys
    if missing:
        errors.append(f"[i18n] {lang} 缺 key：{sorted(missing)}")
    extra = {k for k in keys if k.startswith("CQFExample_")} - used_keys
    if extra:
        warnings.append(f"[i18n] {lang} 多餘 key：{sorted(extra)}")
print(f"翻譯 key：{len(used_keys)} 個被引用，三語比對完成")

# ---------- DialogTree 結構 ----------
for f, tree in trees.items():
    for dt in tree.iter(QEL + "DialogTreeDef"):
        nm = dt.find("nodeMoulds")
        node_keys = set()
        next_targets = []
        if nm is None:
            errors.append(f"[dialog] {f.name}: 無 nodeMoulds")
            continue
        for li in nm.findall("li"):
            k = li.find("key")
            if k is not None:
                node_keys.add(int(k.text))
            for ni in li.iter("nextIndex"):
                next_targets.append(int(ni.text))
        if 0 not in node_keys:
            errors.append(f"[dialog] {f.name}: nodeMoulds 缺開場節點 key=0")
        for t in next_targets:
            if t not in node_keys:
                errors.append(f"[dialog] {f.name}: nextIndex={t} 指向不存在的節點")
        ci = dt.find("curIndex")
        if ci is not None and node_keys and int(ci.text) <= max(node_keys):
            errors.append(f"[dialog] {f.name}: curIndex({ci.text}) 應 > 最大節點 key({max(node_keys)})")
        print(f"DialogTree 結構：{f.name} 節點 {sorted(node_keys)}，跳轉 {sorted(set(next_targets))}")

# ---------- 匯總 ----------
print()
for w in warnings:
    print("WARN ", w)
for e in errors:
    print("ERROR", e)
print()
if errors:
    print(f"健檢失敗：{len(errors)} 錯誤，{len(warnings)} 警告")
    sys.exit(1)
print(f"健檢通過（{len(warnings)} 警告）")
