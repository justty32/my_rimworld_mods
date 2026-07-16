#!/usr/bin/env python3
"""產生 dryads-example-pack 佔位貼圖：256x256 RGBA，簡單樹精剪影（身體橢圓＋頭＋葉冠），
方向 north/south/east 三張（west 由引擎鏡射 east），另附乾屍 _east 一張（灰化）。"""
from PIL import Image, ImageDraw
import os

BASE = os.path.expanduser("~/repo/my_rimworld_mods/dryads-example-pack/Textures/Things/Pawn/Animal")
S = 256

def draw_dryad(body, accent, facing, dessicated=False):
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if dessicated:
        body, accent = (110, 100, 90, 255), (80, 72, 64, 255)
    outline = tuple(max(0, c - 60) for c in body[:3]) + (255,)
    # 身體
    d.ellipse([78, 110, 178, 210], fill=body, outline=outline, width=4)
    # 頭（依方向偏移）
    off = {"south": (0, 0), "north": (0, -6), "east": (26, -2)}[facing]
    hx, hy = 128 + off[0], 92 + off[1]
    d.ellipse([hx - 32, hy - 32, hx + 32, hy + 32], fill=body, outline=outline, width=4)
    # 葉冠（accent 三葉）
    for dx in (-22, 0, 22):
        d.polygon([(hx + dx, hy - 58), (hx + dx - 10, hy - 26), (hx + dx + 10, hy - 26)], fill=accent)
    # 眼睛（north 背對不畫）
    if facing != "north" and not dessicated:
        eye = (20, 24, 18, 255)
        if facing == "south":
            d.ellipse([hx - 16, hy - 6, hx - 8, hy + 2], fill=eye)
            d.ellipse([hx + 8, hy - 6, hx + 16, hy + 2], fill=eye)
        else:  # east
            d.ellipse([hx + 8, hy - 6, hx + 16, hy + 2], fill=eye)
    # 四足
    for lx in (88, 158):
        d.ellipse([lx, 196, lx + 20, 224], fill=accent)
    return img

KINDS = {
    "DEP_Dryad_Resinmaker": ("DEP_DryadResinmaker", (196, 130, 60, 255), (240, 190, 70, 255)),   # 琥珀樹脂色
    "DEP_Dryad_Rescuer":    ("DEP_DryadRescuer",    (120, 170, 110, 255), (230, 240, 230, 255)), # 綠身白冠（醫療感）
}

for folder, (stem, body, accent) in KINDS.items():
    outdir = os.path.join(BASE, folder)
    os.makedirs(outdir, exist_ok=True)
    for facing in ("north", "south", "east"):
        draw_dryad(body, accent, facing).save(os.path.join(outdir, f"{stem}_{facing}.png"))
    draw_dryad(body, accent, "east", dessicated=True).save(os.path.join(outdir, f"Dessicated_{stem}_east.png"))
    print("ok", folder)
