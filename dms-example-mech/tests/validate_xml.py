#!/usr/bin/env python3
"""離線健檢：驗證本 mod 所有 XML well-formed。不啟動遊戲。"""
import sys, pathlib, xml.etree.ElementTree as ET

root = pathlib.Path(__file__).resolve().parent.parent
bad = 0
files = sorted(root.rglob("*.xml"))
for f in files:
    try:
        ET.parse(f)
        print(f"OK   {f.relative_to(root)}")
    except ET.ParseError as e:
        print(f"FAIL {f.relative_to(root)}: {e}")
        bad += 1
print(f"\n{len(files)} files, {bad} failed")
sys.exit(1 if bad else 0)
