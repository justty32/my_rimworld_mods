#!/usr/bin/env python3
"""謄回工具：把 yc's Faction Editor（yancy.factiongearcustomizer）的 preset / 存檔資料
轉成本 mod 的純資料 FactionGearSeedDef XML。

yc 用法（桌上打樣）：遊戲內把某派系某兵種的裝備/武器/品質調好 → Save Preset。
本工具讀 yc 的 ModSettings config（或存檔的 GameComponent 區塊），把每個 FactionGearData
謄成一張 FactionGearSeedDef，交給 pas.gear.community 引擎在生成時套用——發佈物不需玩家裝 yc。

對照（yc Scribe 欄位 → 本 Def）：
  FactionGearData.factionDefName            → <factionDef>
  KindGearData.kindDefName                  → kinds[].kindDef
  KindGearData.forceOnlySelected/forceNaked → kinds[].forceOnlySelected/forceNaked
  KindGearData.itemQuality                  → kinds[].quality（kind 層）
  簡單池 weapons                            → kinds[].weapons[]（只 thingDef）
  簡單池 apparel+armors+others              → kinds[].apparel[]（只 thingDef）
  進階 specificWeapons (SpecRequirementEdit) → kinds[].weapons[]（thingDef+stuff+quality+color）
  進階 specificApparel  (SpecRequirementEdit) → kinds[].apparel[]（thingDef+stuff+quality+color）

不謄（v0.1.0 範圍外，引擎尚未支援）：forcedTraits/Skills/Genes/Appearance、inventory、
budget/pool、CE ammo、xenotype、age、pawnGroupMakers（後者屬純資料 FactionDef patch，另路）。

用法：
  # 從 mod config 的具名 preset
  transcribe_yc_preset.py --config "<Mod_..._FactionGearCustomizerMod.xml>" --preset aa --out out.xml
  # 從存檔的 GameComponent
  transcribe_yc_preset.py --save "<Autosave-1.rws>" --out out.xml
  # 從 config 的即時全域 factionGearData（未存成 preset）
  transcribe_yc_preset.py --config "<...>" --live --out out.xml
"""
import argparse
import re
import sys
import xml.etree.ElementTree as ET


def parse_color(text):
    """RimWorld Color 序列化為 '(r, g, b, a)'。回傳 (r,g,b,a) 或 None（未設/預設全 0）。"""
    if not text:
        return None
    m = re.findall(r"[-+0-9.eE]+", text)
    if len(m) < 3:
        return None
    vals = [float(x) for x in m[:4]]
    while len(vals) < 4:
        vals.append(1.0)
    if all(v == 0.0 for v in vals):
        return None  # default(Color) = 未設
    return tuple(vals)


def text_of(el, tag):
    child = el.find(tag)
    if child is None or child.text is None:
        return None
    t = child.text.strip()
    return t if t else None


def bool_of(el, tag, default=False):
    t = text_of(el, tag)
    if t is None:
        return default
    return t.lower() == "true"


def gear_items_from_pool(kind_el, pool_tag):
    """簡單池（加權選池）：<pool_tag><li><thingDefName>X</thingDefName><weight>w</weight>。
    → {'thingDef':X, 'weight':w}（alwaysTake=false，參與加權挑選）。"""
    out = []
    pool = kind_el.find(pool_tag)
    if pool is None:
        return out
    for li in pool.findall("li"):
        td = text_of(li, "thingDefName")
        if not td:
            continue
        item = {"thingDef": td}
        w = text_of(li, "weight")
        if w is not None:
            try:
                if float(w) != 1.0:
                    item["weight"] = float(w)
            except ValueError:
                pass
        out.append(item)
    return out


def gear_items_from_specific(kind_el, spec_tag):
    """進階逐件強制：<spec_tag><li> SpecRequirementEdit：thing/material/quality/color。
    → alwaysTake=true（無條件強制）＋ stuff/quality/color 覆寫。
    僅取 SelectionMode=AlwaysTake（或未寫＝預設 AlwaysTake）者為強制件。"""
    out = []
    pool = kind_el.find(spec_tag)
    if pool is None:
        return out
    for li in pool.findall("li"):
        td = text_of(li, "thing")
        if not td:
            continue
        mode = text_of(li, "selectionMode")
        item = {"thingDef": td, "alwaysTake": mode is None or mode == "AlwaysTake"}
        stuff = text_of(li, "material")
        if stuff:
            item["stuff"] = stuff
        q = text_of(li, "quality")
        if q:
            item["quality"] = q
        color = parse_color(text_of(li, "color"))
        if color:
            item["color"] = color
        w = text_of(li, "weight")
        if w is not None:
            try:
                if float(w) != 1.0:
                    item["weight"] = float(w)
            except ValueError:
                pass
        out.append(item)
    return out


def kind_from_el(kind_el):
    kind_def = text_of(kind_el, "kindDefName")
    if not kind_def:
        return None
    weapons = gear_items_from_pool(kind_el, "weapons") \
        + gear_items_from_specific(kind_el, "specificWeapons")
    apparel = gear_items_from_pool(kind_el, "apparel") \
        + gear_items_from_pool(kind_el, "armors") \
        + gear_items_from_pool(kind_el, "others") \
        + gear_items_from_specific(kind_el, "specificApparel")
    if not weapons and not apparel:
        return None  # 該兵種沒調任何裝備 → 略過
    return {
        "kindDef": kind_def,
        "forceOnlySelected": bool_of(kind_el, "forceOnlySelected", True),
        "forceNaked": bool_of(kind_el, "forceNaked", False),
        "quality": text_of(kind_el, "itemQuality"),
        "weapons": weapons,
        "apparel": apparel,
    }


def faction_from_el(fg_el):
    faction_def = text_of(fg_el, "factionDefName")
    if not faction_def:
        return None
    kinds = []
    kgd = fg_el.find("kindGearData")
    if kgd is not None:
        for kind_el in kgd.findall("li"):
            k = kind_from_el(kind_el)
            if k:
                kinds.append(k)
    if not kinds:
        return None
    return {"factionDef": faction_def, "kinds": kinds}


def find_faction_gear_data_container(root, args):
    """回傳 <factionGearData> 或 <savedFactionGearData> 元素（內含 FactionGearData 的 <li>）。"""
    if args.save:
        # 存檔：<li Class="FactionGearCustomizer.Core.FactionGearGameComponent"><savedFactionGearData>
        for li in root.iter("li"):
            if li.get("Class", "").startswith("FactionGearCustomizer") and li.find("savedFactionGearData") is not None:
                return li.find("savedFactionGearData")
        return None
    # config
    ms = root.find("ModSettings")
    if ms is None:
        ms = root
    if args.live:
        return ms.find("factionGearData")
    # 具名 preset
    presets = ms.find("presets")
    if presets is None:
        return None
    for li in presets.findall("li"):
        name = text_of(li, "name")
        if name == args.preset:
            return li.find("factionGearData")
    return None


def emit_xml(factions, prefix):
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<Defs>"]
    for f in factions:
        safe = re.sub(r"[^A-Za-z0-9_]", "_", f["factionDef"])
        lines.append("")
        lines.append("  <pas.gear.FactionGearSeedDef>")
        lines.append(f"    <defName>{prefix}_{safe}</defName>")
        lines.append(f"    <factionDef>{f['factionDef']}</factionDef>")
        lines.append("    <kinds>")
        for k in f["kinds"]:
            lines.append("      <li>")
            lines.append(f"        <kindDef>{k['kindDef']}</kindDef>")
            if not k["forceOnlySelected"]:
                lines.append("        <forceOnlySelected>false</forceOnlySelected>")
            if k["forceNaked"]:
                lines.append("        <forceNaked>true</forceNaked>")
            if k["quality"]:
                lines.append(f"        <quality>{k['quality']}</quality>")
            lines.append(_emit_items("weapons", k["weapons"]))
            lines.append(_emit_items("apparel", k["apparel"]))
            lines.append("      </li>")
        lines.append("    </kinds>")
        lines.append("  </pas.gear.FactionGearSeedDef>")
    lines.append("")
    lines.append("</Defs>")
    return "\n".join(x for x in lines if x is not None) + "\n"


def _emit_items(tag, items):
    if not items:
        return None
    out = [f"        <{tag}>"]
    for it in items:
        parts = [f"<thingDef>{it['thingDef']}</thingDef>"]
        if it.get("alwaysTake"):
            parts.append("<alwaysTake>true</alwaysTake>")
        if it.get("weight") is not None:
            parts.append(f"<weight>{it['weight']}</weight>")
        if it.get("stuff"):
            parts.append(f"<stuff>{it['stuff']}</stuff>")
        if it.get("quality"):
            parts.append(f"<quality>{it['quality']}</quality>")
        if it.get("color"):
            r, g, b, a = it["color"]
            parts.append(f"<color>({r},{g},{b},{a})</color>")
        out.append("          <li>" + "".join(parts) + "</li>")
    out.append(f"        </{tag}>")
    return "\n".join(out)


def main():
    ap = argparse.ArgumentParser(description="謄回 yc Faction Editor preset → FactionGearSeedDef XML")
    src = ap.add_mutually_exclusive_group(required=True)
    src.add_argument("--config", help="yc ModSettings config XML 路徑")
    src.add_argument("--save", help="RimWorld 存檔 .rws 路徑（讀 GameComponent）")
    ap.add_argument("--preset", default="aa", help="config 內的具名 preset（預設 aa）")
    ap.add_argument("--live", action="store_true", help="改讀 config 的即時全域 factionGearData（非 preset）")
    ap.add_argument("--prefix", default="Gear", help="defName 前綴（預設 Gear → Gear_<派系>）")
    ap.add_argument("--out", help="輸出 XML 路徑（省略＝印到 stdout）")
    args = ap.parse_args()

    path = args.config or args.save
    try:
        tree = ET.parse(path)
    except ET.ParseError as e:
        sys.exit(f"[transcribe] 無法解析 XML：{path}\n  {e}")
    root = tree.getroot()

    container = find_faction_gear_data_container(root, args)
    if container is None:
        where = f"存檔 GameComponent" if args.save else (f"即時 factionGearData" if args.live else f"preset '{args.preset}'")
        sys.exit(f"[transcribe] 找不到 {where} 的 factionGearData 容器。")

    factions = []
    for fg_el in container.findall("li"):
        f = faction_from_el(fg_el)
        if f:
            factions.append(f)

    if not factions:
        sys.exit("[transcribe] 來源存在但沒有任何已調裝備的派系（factionGearData 空 / 兵種皆無裝備）。"
                 "\n  → 請先在 yc 裡挑一個派系、改一個兵種的裝備/武器，再 Save Preset。")

    xml = emit_xml(factions, args.prefix)
    if args.out:
        with open(args.out, "w", encoding="utf-8") as fh:
            fh.write(xml)
        n_kinds = sum(len(f["kinds"]) for f in factions)
        print(f"[transcribe] 寫出 {args.out}：{len(factions)} 派系 / {n_kinds} 兵種。")
    else:
        sys.stdout.write(xml)


if __name__ == "__main__":
    main()
