# Renders logo_mark, icon_star_full/empty and fx_spark in the jelly style of Yağmur's tiles.
# Usage: python3 make_art.py <out_dir>   (writes @2x PNGs)
import math, os, sys, io
from PIL import Image
from playwright.sync_api import sync_playwright

OUT = sys.argv[1]
CHROME = os.environ.get("CHROME")  # optional browser binary; Playwright's own Chromium otherwise

# light (top of body), base, deep (bottom rim and shadow) per tile colour
PAL = {
    "red":    ("#FF7480", "#FF3848", "#D11F35"),
    "blue":   ("#66A6FF", "#297FFF", "#155AD1"),
    "green":  ("#6AE888", "#30D754", "#17A23A"),
    "yellow": ("#FFE766", "#FFD414", "#E0A600"),
    "purple": ("#CF75FF", "#B638FF", "#861ACC"),
    "white":  ("#FFFFFF", "#F0F0F0", "#C9C9C9"),
    "empty":  ("#E0E0E0", "#DBDBDB", "#CDCDCD"),
}

def star_points(cx, cy, R, r):
    pts = []
    for i in range(10):
        a = math.radians(-90 + i * 36)
        rad = R if i % 2 == 0 else r
        pts.append(f"{cx + rad * math.cos(a):.2f},{cy + rad * math.sin(a):.2f}")
    return " ".join(pts)

# Each silhouette is drawn white in a 100x100 box; strokes round the corners.
SHAPES = {
    "sphere": '<circle cx="50" cy="50" r="40"/>',
    "cube":   '<polygon points="50,10 86,29 86,69 50,89 14,69 14,29" stroke="#fff" stroke-width="10" stroke-linejoin="round"/>',
    "star":   f'<polygon points="{star_points(50, 54, 37, 20)}" stroke="#fff" stroke-width="15" stroke-linejoin="round"/>',
    "heart":  '<path d="M50,86 C22,66 10,52 10,35 C10,21 20,12 32,12 C40,12 46,16 50,23 C54,16 60,12 68,12 C80,12 90,21 90,35 C90,52 78,66 50,86 Z" stroke="#fff" stroke-width="6" stroke-linejoin="round"/>',
    "flower": "".join(f'<circle cx="{50 + 25 * math.cos(math.radians(-90 + 72 * i)):.2f}" cy="{53 + 25 * math.sin(math.radians(-90 + 72 * i)):.2f}" r="18"/>' for i in range(5)) + '<circle cx="50" cy="53" r="27"/>',
}
GLOSS = {  # cx, cy, rx, ry, rotation
    "sphere": (35, 30, 6.5, 10, 35),
    "cube":   (38, 25, 7, 3.6, -28),
    "star":   (41, 40, 5, 7.5, 35),
    "heart":  (29, 28, 5.5, 8.5, 35),
    "flower": (57, 22, 4.5, 7, 35),
}

uid = 0
def jelly(shape, colour, x, y, size, shadow=True, gloss=True, rim=4.0):
    """One jelly shape placed at (x, y) with the given size, as an SVG group."""
    global uid
    uid += 1
    k = f"j{uid}"
    light, base, deep = PAL[colour]
    s = size / 100
    if shape == "cube":
        # faces run past the silhouette and get cut by its mask
        body = (f'<polygon points="50,49 -30,7 -30,-30 130,-30 130,7" fill="{light}"/>'
                f'<polygon points="50,49 -30,7 -30,130 50,130" fill="{base}"/>'
                f'<polygon points="50,49 130,7 130,130 50,130" fill="url(#{k}r)"/>')
    else:
        body = f'<rect x="-10" y="-10" width="120" height="120" fill="url(#{k}g)"/>'
    gx, gy, grx, gry, grot = GLOSS[shape]
    return f'''
<g transform="translate({x - size / 2:.2f},{y - size / 2:.2f}) scale({s:.4f})">
  <defs>
    <mask id="{k}m" maskUnits="userSpaceOnUse" x="-20" y="-20" width="140" height="140"><g fill="#fff">{SHAPES[shape]}</g></mask>
    <linearGradient id="{k}g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{light}"/><stop offset="0.75" stop-color="{base}"/></linearGradient>
    <linearGradient id="{k}r" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{base}"/><stop offset="1" stop-color="{deep}" stop-opacity="0.55"/></linearGradient>
    <radialGradient id="{k}l" cx="0.35" cy="0.25" r="0.6"><stop offset="0" stop-color="#fff" stop-opacity="0.35"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
    <filter id="{k}b" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="3.2"/></filter>
    <filter id="{k}s" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="0.9"/></filter>
  </defs>
  {f'<g transform="translate(0,6)" filter="url(#{k}b)" opacity="0.30"><rect x="-20" y="-20" width="140" height="140" fill="{deep}" mask="url(#{k}m)"/></g>' if shadow else ''}
  <g mask="url(#{k}m)">
    <rect x="-10" y="-10" width="120" height="120" fill="{deep}"/>
    <g transform="translate(0,{-rim})" filter="url(#{k}s)"><g mask="url(#{k}m)">{body}</g></g>
    <rect x="-10" y="-10" width="120" height="120" fill="url(#{k}l)"/>
  </g>
  {f'<ellipse cx="{gx}" cy="{gy}" rx="{grx}" ry="{gry}" transform="rotate({grot} {gx} {gy})" fill="#fff" opacity="0.92"/>' if gloss else ''}
</g>'''

def svg(w, h, inner):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">{inner}</svg>'

def logo():
    # five jelly shapes on a ring, one per tile colour, in board order
    order = [("sphere", "red"), ("cube", "blue"), ("star", "green"), ("heart", "yellow"), ("flower", "purple")]
    parts = []
    for i, (shape, colour) in enumerate(order):
        a = math.radians(-90 + 72 * i)
        parts.append(jelly(shape, colour, 120 + 70 * math.cos(a), 117 + 70 * math.sin(a), 76))
    return svg(240, 240, "".join(parts))

ASSETS = {
    "logo_mark": (240, 240, logo()),
    "icon_star_full": (150, 150, svg(150, 150, jelly("star", "yellow", 75, 70, 134))),
    "icon_star_empty": (150, 150, svg(150, 150, jelly("star", "empty", 75, 70, 134, shadow=False, gloss=False, rim=3))),
    "fx_spark": (32, 32, svg(32, 32, jelly("sphere", "white", 16, 15.5, 34, shadow=False, rim=6))),
}

SS = 3  # supersample, then scale down
with sync_playwright() as p:
    b = p.chromium.launch(headless=True, executable_path=CHROME)
    for name, (w, h, markup) in ASSETS.items():
        pg = b.new_page(viewport={"width": w, "height": h}, device_scale_factor=SS)
        pg.set_content(f'<html><body style="margin:0;background:transparent">{markup}</body></html>')
        png = pg.screenshot(omit_background=True, clip={"x": 0, "y": 0, "width": w, "height": h})
        Image.open(io.BytesIO(png)).convert("RGBA").resize((w, h), Image.LANCZOS).save(f"{OUT}/{name}.png")
        pg.close()
        print("wrote", name, w, h)
    b.close()
