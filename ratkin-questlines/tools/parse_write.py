#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re, sys, os

src = sys.argv[1]
MODLANG = "/home/lorkhan/repo/moddings/rimworld/projects/my_rimworld_mods/ratkin-questlines/Languages"
LINE_MAP = {"SCOUT": "RatkinQL_Scout", "LOSTKIT": "RatkinQL_LostKit", "TOLL": "RatkinQL_Toll",
            "VETERAN": "RatkinQL_Veteran", "MINSTREL": "RatkinQL_Minstrel",
            "HEALER": "RatkinQL_Healer", "JUNK": "RatkinQL_Junk", "CONTRACT": "RatkinQL_Contract", "REFUGEE": "RatkinQL_Refugee", "HUNTED": "RatkinQL_Hunted", "FESTIVAL": "RatkinQL_Festival", "LOREKEEPER": "RatkinQL_Lorekeeper", "MOURNER": "RatkinQL_Mourner",
            # F8 新增：商團首領佩林 三章 ＋ 武器商線
            "PEIRIN1": "RatkinQL_PeirinCh1", "PEIRIN2": "RatkinQL_PeirinCh2", "PEIRIN3": "RatkinQL_PeirinCh3",
            "ARMSENVOY": "RatkinQL_ArmsEnvoy", "ARMSDELIVER": "RatkinQL_ArmsDeliver",
            "ARMSENVOYMAJOR": "RatkinQL_ArmsEnvoyMajor", "ARMSDELIVERMAJOR": "RatkinQL_ArmsDeliverMajor"}
LANG_MAP = {"English": "English", "繁體中文": "ChineseTraditional", "简体中文": "ChineseSimplified",
            "簡體中文": "ChineseTraditional"}

txt = open(src, encoding="utf-8").read()
# split into blocks by header
parts = re.split(r'^===\s*(.+?)\s*===\s*$', txt, flags=re.M)
# parts: ['', header1, body1, header2, body2, ...]
written = 0
for i in range(1, len(parts), 2):
    header = parts[i].strip()
    body = parts[i + 1]
    m = re.search(r'(<LanguageData>.*?</LanguageData>)', body, flags=re.S)
    if not m:
        print(f"!! no xml block for header: {header}", file=sys.stderr); continue
    xml = m.group(1).strip()
    line_key, lang_key = [x.strip() for x in header.split("/")]
    line = LINE_MAP.get(line_key.upper())
    lang = LANG_MAP.get(lang_key)
    if not line or not lang:
        print(f"!! unmapped header: {header}", file=sys.stderr); continue
    out = os.path.join(MODLANG, lang, "Keyed", f"{line}.xml")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    content = '<?xml version="1.0" encoding="utf-8" ?>\n' + xml + '\n'
    open(out, "w", encoding="utf-8").write(content)
    print(f"wrote {lang}/{line}.xml ({len(xml)} chars)")
    written += 1
print(f"total {written} files")
