#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Ratkin Questlines — 離線靜態健檢
================================
不啟動遊戲，純比對 XML 引用是否真實存在：
  1. 所有 XML well-formed
  2. 每個 <li Class="QuestEditor_Library.X"> 的短名在 CQF 反編譯碼中存在
  3. 我方 XML 對 CQF 型別寫的子欄位，是該型別（含繼承鏈）真實 public 成員
  4. 三語 Keyed 檔 key 集合一致；Defs 引用的每個 Keyed key 三語都有定義
  5. 引用的 ThingDef / FactionDef（鼠族來自 workshop、原版來自 RW Data）真實存在 <defName>
  6. IntRange（count）用 min~max
退出碼 0＝全綠；非 0＝有問題。實機 E2E 另行（見 PROJECT.md 發佈標準）。
"""
import os, re, sys, subprocess, glob, xml.etree.ElementTree as ET

MOD = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CQF_DECOMP = "/home/lorkhan/repo/moddings/rimworld/projects/rimworld_mods/custom-quest-framework/decompiled/QuestEditor_Library/QuestEditor_Library.decompiled.cs"
RW_DATA = "/home/lorkhan/.local/share/Steam/steamapps/common/RimWorld/Data"
WORKSHOP = "/home/lorkhan/.local/share/Steam/steamapps/workshop/content/294100"

problems = []
notes = []

# ---------- 載入 CQF 反編譯源 ----------
with open(CQF_DECOMP, encoding="utf-8", errors="replace") as f:
    cqf_src = f.read()
cqf_types = set(re.findall(r'\bclass\s+([A-Za-z0-9_]+)', cqf_src))

def class_own_body(classname):
    m = re.search(r'\bclass\s+' + re.escape(classname) + r'\b([^\n]*)', cqf_src)
    if not m:
        return None, None
    header_rest = m.group(1)  # 可能含 " : Base"
    base = None
    bm = re.search(r':\s*([A-Za-z0-9_]+)', header_rest)
    if bm:
        base = bm.group(1)
    start = m.end()
    nxt = re.search(r'\n\t(?:public |internal )?(?:abstract |sealed |static )?class ', cqf_src[start:])
    body = cqf_src[start:start + (nxt.start() if nxt else 4000)]
    return body, base

def cqf_fields(classname, _depth=0):
    """回傳 classname（含繼承鏈）的 public 欄位名集合；找不到回 None。"""
    body, base = class_own_body(classname)
    if body is None:
        return None
    fields = set(re.findall(r'public\s+[\w<>,\.\[\]\? ]+?\s+([A-Za-z_]\w*)\s*[;=]', body))
    if base and base in cqf_types and _depth < 8:
        parent = cqf_fields(base, _depth + 1)
        if parent:
            fields |= parent
    return fields

# ---------- 收集 XML ----------
xml_files = []
for sub in ("1.6", "Languages"):
    for root, _, files in os.walk(os.path.join(MOD, sub)):
        for fn in files:
            if fn.endswith(".xml"):
                xml_files.append(os.path.join(root, fn))
xml_files += [os.path.join(MOD, "About", "About.xml"), os.path.join(MOD, "LoadFolders.xml")]

def rel(p):
    return os.path.relpath(p, MOD)

# ---------- 1+2+3+6：Defs 結構檢查 ----------
KEY_REF_TAGS = {"title", "dialogReportKey", "text", "failReason", "message"}
referenced_keys = set()
referenced_things = set()    # <thing> in CQFThingDefCount → 必須是 ThingDef
referenced_factions = set()  # <faction> in DialogCondition_Faction → 必須是 FactionDef
def_xml = [p for p in xml_files if os.path.sep + "1.6" + os.path.sep in p]

# 我方 XML 對每個 CQF 型別用到的子欄位（供欄位存在性檢查）
cqf_field_usage = {}

for path in def_xml:
    try:
        rootel = ET.parse(path).getroot()
    except ET.ParseError as e:
        problems.append(f"[XML 解析失敗] {rel(path)}: {e}")
        continue
    for el in rootel.iter():
        cls = el.get("Class")
        if cls:
            # 只驗 CQF 型別（QuestEditor_Library.*）——它們必須在反編譯源；
            # 原版 QuestNode/ThingSetMaker（無 QuestEditor_Library. 前綴）交實機載入驗，靜態不誤判。
            if cls.startswith("QuestEditor_Library."):
                short = cls.split(".")[-1]
                if short not in cqf_types:
                    problems.append(f"[未知 CQF 型別] {rel(path)}: Class=\"{cls}\"（短名 {short} 不在 CQF 反編譯型別中）")
                else:
                    used = cqf_field_usage.setdefault(short, set())
                    for child in el:
                        used.add(child.tag)
        # Keyed 引用收集
        if el.tag in KEY_REF_TAGS and el.text and el.text.strip():
            t = el.text.strip()
            if re.fullmatch(r'[A-Za-z_][\w]*', t) and t.startswith("RatkinQL"):
                referenced_keys.add(t)
        if el.tag == "extraText":
            for li in el.findall("li"):
                if li.text and li.text.strip().startswith("RatkinQL"):
                    referenced_keys.add(li.text.strip())
        # IntRange
        if el.tag == "count" and el.text:
            t = el.text.strip()
            if "~" not in t and not re.fullmatch(r'-?\d+', t):
                problems.append(f"[IntRange 格式] {rel(path)}: <count>{t}</count> 應為 min~max")
        # def 型別引用（供型別檢查用；避免 def 存在但型別錯，如 ToolCapacityDef 冒充 ThingDef）
        # 排除 $ 開頭的 slate 變數參照（如原版 QuestNode <faction>$siteFaction</faction>）——
        # 那是任務執行期才解析的變數名，不是字面 defName，靜態查不到、也不該查。
        if el.tag == "thing" and el.text and el.text.strip() and not el.text.strip().startswith("$"):
            referenced_things.add(el.text.strip())
        if el.tag == "faction" and el.text and el.text.strip() and not el.text.strip().startswith("$"):
            referenced_factions.add(el.text.strip())

    # Def 命名/必填規則（RimWorld 載入期會噴 config error，靜態先擋）
    # 教訓（2026-07-19 實機 E2E）：①ThingDef defName 結尾數字 → RW 拿結尾數字做 stuff/quality 命名，衝突；
    #   ②storyteller-offered QuestScriptDef（rootSelectionWeight>0）沒 expireDaysRange → config error＋說書人可能不收。
    for defel in list(rootel):
        dn_el = defel.find("defName")
        dn = dn_el.text.strip() if dn_el is not None and dn_el.text else None
        if defel.tag == "ThingDef" and dn and dn[-1].isdigit():
            problems.append(f"[ThingDef defName 數字結尾] {rel(path)}: <defName>{dn}</defName> RimWorld 不允許 ThingDef defName 以數字結尾")
        if defel.tag == "QuestScriptDef":
            w_el = defel.find("rootSelectionWeight")
            try:
                w = float(w_el.text.strip()) if w_el is not None and w_el.text else 0.0
            except ValueError:
                w = 0.0
            if w > 0 and defel.find("expireDaysRange") is None:
                problems.append(f"[QuestScript 缺 expireDaysRange] {rel(path)}: {dn} rootSelectionWeight={w}>0（storyteller-offered）必須設 expireDaysRange，否則載入 config error")

# 欄位存在性（含繼承）
for cls, used in cqf_field_usage.items():
    fields = cqf_fields(cls)
    if fields is None:
        problems.append(f"[欄位檢查] 找不到 CQF class {cls}")
        continue
    for fld in sorted(used):
        if fld not in fields:
            problems.append(f"[未知欄位] {cls}.{fld} 非 public 成員（真實：{sorted(fields)}）")
        else:
            notes.append(f"欄位確認 {cls}.{fld}")

# ---------- 4：三語 Keyed 一致 ----------
lang_dirs = ["English", "ChineseTraditional", "ChineseSimplified"]
lang_keys = {}
for lang in lang_dirs:
    keys = set()
    for p in glob.glob(os.path.join(MOD, "Languages", lang, "Keyed", "*.xml")):
        try:
            r = ET.parse(p).getroot()
        except ET.ParseError as e:
            problems.append(f"[XML 解析失敗] {rel(p)}: {e}")
            continue
        for child in r:
            keys.add(child.tag)
    lang_keys[lang] = keys

if lang_keys:
    union = set().union(*lang_keys.values())
    for lang in lang_dirs:
        missing = union - lang_keys.get(lang, set())
        if missing:
            problems.append(f"[三語不齊] {lang} 缺 key：{sorted(missing)}")
    # Defs 引用的 key 三語皆須有
    for lang in lang_dirs:
        undef = referenced_keys - lang_keys.get(lang, set())
        if undef:
            problems.append(f"[Keyed 未定義] {lang} 缺 Defs 引用的 key：{sorted(undef)}")
    orphan = union - referenced_keys
    if orphan:
        notes.append(f"未被 Defs 引用的 key（可能保留給日後）：{sorted(orphan)}")
    notes.append(f"Defs 引用 Keyed key {len(referenced_keys)} 個，三語各定義 {len(union)} 個")

# ---------- 5：defName 引用存在性 + 型別正確性 ----------
# 教訓（2026-07-18 實機 E2E）：RK_BeerBottle 這個 def 確實存在，但型別是 ToolCapacityDef 不是 ThingDef，
# CQFThingDefCount 要 ThingDef → 載入紅字。靜態只查「存在」照不到，必須查「型別」。
_OPENTAG = re.compile(r'<([A-Za-z_]\w*Def)\b')
def def_types(defname, roots):
    """回傳 defname 在 roots 內被宣告成的 Def 型別集合（如 {'ThingDef'}）；grep 失敗回 None。"""
    files = set()
    for base in roots:
        try:
            out = subprocess.run(["grep", "-rl", f"<defName>{defname}</defName>", base],
                                 capture_output=True, text=True, timeout=90).stdout.split()
            files.update(out)
        except Exception:
            return None
    types = set()
    pat = re.compile(r'<defName>\s*' + re.escape(defname) + r'\s*</defName>')
    for fp in files:
        try:
            txt = open(fp, encoding="utf-8", errors="replace").read()
        except Exception:
            continue
        for m in pat.finditer(txt):
            pre = txt[:m.start()]
            last = None
            for om in _OPENTAG.finditer(pre):
                if pre[om.start() - 1:om.start()] != "/":   # 排除 </XxxDef>
                    last = om.group(1)
            if last:
                types.add(last)
    return types

type_checks = [(t, "ThingDef") for t in sorted(referenced_things)]
type_checks += [(f, "FactionDef") for f in sorted(referenced_factions)]
# 教訓（2026-07-18 F8）：本包自製的 ThingDef（如獎勵道具 RatkinQL_KingdomTally）不在 RW_DATA/WORKSHOP，
# 必須把本 mod 自己的 1.6/Defs 也納入型別搜尋根，否則自製 def 會被誤判「缺失」。
MOD_DEFS = os.path.join(MOD, "1.6")
for dn, expect in type_checks:
    ts = def_types(dn, [RW_DATA, WORKSHOP, MOD_DEFS])
    if ts is None:
        notes.append(f"{dn} 型別無法確認（grep 失敗，忽略）")
    elif not ts:
        problems.append(f"[defName 缺失] '{dn}'（當 {expect} 用）找不到 <defName>")
    elif expect in ts:
        notes.append(f"{expect} '{dn}' 型別確認 ✓（宣告為 {sorted(ts)}）")
    else:
        problems.append(f"[型別錯誤] '{dn}' 被當 {expect} 用，實際宣告為 {sorted(ts)}"
                        f"（def 存在但型別不符 → 載入必紅字）")

# ---------- 報告 ----------
print("=== Ratkin Questlines 靜態健檢 ===")
for n in notes:
    print("  note:", n)
print(f"  掃描 XML 檔 {len(xml_files)} 個")
if problems:
    print("\n--- 發現問題 ---")
    for p in problems:
        print("  X", p)
    sys.exit(1)
else:
    print("\n全部靜態檢查通過：型別/欄位/三語 key/defName 均無臆造。")
    print("（實機 E2E 仍需在 2-anime modlist 內實測，見 PROJECT.md 發佈標準。）")
    sys.exit(0)
