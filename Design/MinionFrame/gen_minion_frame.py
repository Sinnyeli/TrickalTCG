"""Draws the minion frame sprites in Assets/Img/MinionUI. Units match Minion.prefab (1 unit = 1 UI px).

Usage: python3 Design/MinionFrame/gen_minion_frame.py Assets/Img/MinionUI
"""
import math, sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops

OUT = sys.argv[1]
SS = 8  # supersample px per unit while drawing
PX = 2  # output px per unit

def canvas(w, h):
    return Image.new("RGBA", (int(w * SS), int(h * SS)), (0, 0, 0, 0))

def finish(im, w, h, name):
    im = im.resize((int(w * PX), int(h * PX)), Image.LANCZOS)
    im.save(f"{OUT}/{name}.png")
    return im

class U:
    """Unit-space drawer centred on the image centre, y up."""
    def __init__(self, im):
        self.im, self.d = im, ImageDraw.Draw(im)
        self.cx, self.cy = im.width / 2, im.height / 2
    def p(self, x, y):
        return (self.cx + x * SS, self.cy - y * SS)
    def circle(self, x, y, r, **kw):
        if "width" in kw: kw["width"] = int(kw["width"] * SS)
        self.d.ellipse([self.p(x - r, y + r), self.p(x + r, y - r)], **kw)
    def poly(self, pts, **kw):
        if "width" in kw: kw["width"] = int(kw["width"] * SS)
        self.d.polygon([self.p(x, y) for x, y in pts], **kw)

def flower(u, r_core, r_bump, n, fill):
    u.circle(0, 0, r_core, fill=fill)
    for i in range(n):
        a = math.pi / 2 + i * 2 * math.pi / n
        u.circle(r_core * math.cos(a), r_core * math.sin(a), r_bump, fill=fill)

def mask_of(fn, w, h):
    m = canvas(w, h); fn(U(m)); return m.split()[3]

FRAME = 240
CORE, BUMP, N = 86, 10, 18
WINDOW, GOLD = 70, 77

# ---------- band (tintable, light grayscale) ----------
def band():
    sil = mask_of(lambda u: flower(u, CORE, BUMP, N, (255,) * 4), FRAME, FRAME)
    im = canvas(FRAME, FRAME)
    # drop shadow
    sh = Image.new("RGBA", im.size, (40, 30, 60, 0))
    sh.putalpha(sil.point(lambda a: a * 0.35))
    sh = ImageChops.offset(sh, 0, int(4 * SS)).filter(ImageFilter.GaussianBlur(3 * SS))
    im.alpha_composite(sh)
    # dark outline then body with soft vertical shading
    out = sil.filter(ImageFilter.MaxFilter(int(2.4 * SS) | 1))
    o = Image.new("RGBA", im.size, (70, 70, 90, 255)); o.putalpha(out)
    im.alpha_composite(o)
    body = Image.new("RGBA", im.size)
    grad = Image.linear_gradient("L").resize(im.size)  # black top -> white bottom
    shade = grad.point(lambda v: 255 - int(v * 0.22))
    body = Image.merge("RGBA", (shade, shade, shade, sil))
    im.alpha_composite(body)
    # inner highlight ring near each scallop
    hl = canvas(FRAME, FRAME); u = U(hl)
    flower(u, CORE - 3, BUMP - 3, N, (255, 255, 255, 255))
    hlm = ImageChops.subtract(hl.split()[3], mask_of(lambda u: flower(u, CORE - 5, BUMP - 5, N, (255,) * 4), FRAME, FRAME))
    h = Image.new("RGBA", im.size, (255, 255, 255, 0)); h.putalpha(hlm.point(lambda a: a * 0.8))
    im.alpha_composite(h)
    return finish(im, FRAME, FRAME, "MinionFrameBand")

# ---------- glow (silhouette for the action highlight) ----------
def glow():
    sil = mask_of(lambda u: flower(u, CORE + 6, BUMP + 4, N, (255,) * 4), FRAME, FRAME)
    sil = sil.filter(ImageFilter.GaussianBlur(5 * SS))
    im = Image.new("RGBA", sil.size, (255, 255, 255, 0)); im.putalpha(sil)
    return finish(im, FRAME, FRAME, "MinionFrameGlow")

GOLD_FILL = (246, 214, 140, 255)
GOLD_DARK = (176, 122, 60, 255)
GOLD_LIGHT = (255, 243, 205, 255)

def star4(u, x, y, rx, ry, fill, waist=0.28):
    pts = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = (ry if i % 4 == 0 else rx) if i % 2 == 0 else waist * min(rx, ry)
        rr = r if i % 2 == 0 else r
        pts.append((x + rr * math.cos(a), y + rr * math.sin(a)))
    u.poly(pts, fill=fill)

def leaf(u, x, y, ang, length, width, fill, edge):
    pts = []
    for t in range(41):
        s = t / 40
        w = math.sin(math.pi * s) ** 0.9 * width / 2
        pts.append((s * length, w))
    pts += [(s, -w) for s, w in reversed(pts)]
    c, sn = math.cos(ang), math.sin(ang)
    tp = lambda px, py: (x + px * c - py * sn, y + px * sn + py * c)
    big = [tp(px * 1.0, py * 1.0) for px, py in pts]
    u.poly(big, fill=edge)
    inner = [tp(length * 0.06 + px * 0.88, py * 0.7) for px, py in pts]
    u.poly(inner, fill=fill)

# ---------- trim (gold ring, ornaments; window transparent) ----------
def trim():
    im = canvas(FRAME, FRAME); u = U(im)
    # sparkles + beads on the band
    for i in range(N):
        a = math.pi / 2 + (i + 0.5) * 2 * math.pi / N
        u.circle(83 * math.cos(a), 83 * math.sin(a), 1.6, fill=(255, 255, 255, 230))
    # gold ring
    u.circle(0, 0, GOLD + 1.2, fill=GOLD_DARK)
    u.circle(0, 0, GOLD, fill=GOLD_FILL)
    u.circle(0, 0, GOLD - 2.2, fill=GOLD_LIGHT)
    u.circle(0, 0, GOLD - 3.4, fill=GOLD_FILL)
    u.circle(0, 0, WINDOW + 1.2, fill=GOLD_DARK)
    hole = mask_of(lambda v: v.circle(0, 0, WINDOW, fill=(255,) * 4), FRAME, FRAME)
    a = im.split()[3]; im.putalpha(ImageChops.subtract(a, hole))
    u = U(im)
    # laurel sprigs on both sides, rising from the bottom
    for side in (-1, 1):
        for k in range(5):
            ang = math.radians(-70 + k * 18)  # angle around the ring from the right
            r = 88
            x, y = side * r * math.cos(ang), r * math.sin(ang)
            tang = ang + math.pi / 2  # pointing up along the ring
            out = 0.55
            for flip in (-1, 1):
                d = tang + flip * out
                d = d if side > 0 else math.pi - d
                leaf(u, x, y, d, 15 - k * 0.8, 7.5, (138, 196, 110, 255), (72, 128, 70, 255))
    # top jewel like the card diamond
    star4(u, 0, 92, 13, 20, GOLD_DARK, 0.35)
    star4(u, 0, 92, 11, 17.5, GOLD_FILL, 0.33)
    star4(u, 0, 92, 6.5, 11, (120, 180, 245, 255), 0.36)
    star4(u, -1, 94, 2.5, 4.5, (235, 246, 255, 255), 0.4)
    # small side stars
    for side in (-1, 1):
        star4(u, side * 30, 98, 4, 6, (255, 255, 255, 255), 0.3)
    return finish(im, FRAME, FRAME, "MinionFrameTrim")

# ---------- portrait background (also the mask shape) ----------
def portrait():
    D = 2 * (WINDOW + 2)
    im = canvas(D, D)
    w, h = im.size
    g = Image.radial_gradient("L").resize((w, h))
    inner, outer = (255, 250, 238), (196, 224, 252)
    bands = [Image.new("L", (w, h), 0) for _ in range(3)]
    rgb = Image.merge("RGB", [g.point(lambda v, i=i: int(inner[i] + (outer[i] - inner[i]) * min(1, v / 255 * 1.25))) for i in range(3)])
    m = mask_of(lambda u: u.circle(0, 0, WINDOW + 2, fill=(255,) * 4), D, D)
    im = rgb.convert("RGBA"); im.putalpha(m)
    u = U(im)
    for x, y, r in [(-40, 30, 3), (44, 22, 2.4), (-22, -46, 2), (36, -40, 2.8), (6, 52, 1.8), (-56, -6, 1.6), (58, 0, 2)]:
        u.circle(x, y, r, fill=(255, 255, 255, 200))
    return finish(im, D, D, "MinionPortrait")

# ---------- name ribbon ----------
def ribbon():
    W, H = 160, 44
    im = canvas(W, H); u = U(im)
    sag = 5
    def arc(x0, x1, ytop, ybot, n=40):
        top = [(x0 + (x1 - x0) * t / n, ytop - sag * math.sin(math.pi * t / n)) for t in range(n + 1)]
        bot = [(x1 - (x1 - x0) * t / n, ybot - sag * math.sin(math.pi * (1 - t / n))) for t in range(n + 1)]
        return top + bot
    # tails
    for s in (-1, 1):
        tail = [(s * 52, 6), (s * 78, 6), (s * 70, -5), (s * 78, -16), (s * 52, -16)]
        u.poly(tail, fill=GOLD_DARK)
        inner = [(s * 53, 4.5), (s * 74.5, 4.5), (s * 67.5, -5), (s * 74.5, -14.5), (s * 53, -14.5)]
        u.poly(inner, fill=(236, 186, 112, 255))
    # body
    u.poly(arc(-62, 62, 17.5, -11.5), fill=GOLD_DARK)
    u.poly(arc(-60.5, 60.5, 16, -10), fill=GOLD_FILL)
    u.poly(arc(-58, 58, 13.5, -7.5), fill=(255, 248, 232, 255))
    return finish(im, W, H, "MinionNameRibbon")

def race_badge():
    D = 40
    im = canvas(D, D); u = U(im)
    u.circle(0, 0, 19, fill=GOLD_DARK)
    u.circle(0, 0, 17.6, fill=GOLD_FILL)
    u.circle(0, 0, 15, fill=GOLD_DARK)
    u.circle(0, 0, 14, fill=(255, 248, 232, 255))
    return finish(im, D, D, "MinionRaceBadge")

def artifact_ring():
    # Drawn over a square artwork: the opaque ring hides the corners of a 22x22 icon.
    D = 34
    im = canvas(D, D); u = U(im)
    u.circle(0, 0, 16.5, fill=GOLD_DARK)
    u.circle(0, 0, 15.3, fill=GOLD_FILL)
    u.circle(0, 0, 12.6, fill=GOLD_DARK)
    hole = mask_of(lambda v: v.circle(0, 0, 11.4, fill=(255,) * 4), D, D)
    im.putalpha(ImageChops.subtract(im.split()[3], hole))
    u = U(im)
    u.circle(-6, 10.5, 1.6, fill=GOLD_LIGHT)
    return finish(im, D, D, "MinionArtifactRing")

for f in (band, glow, trim, portrait, ribbon, race_badge, artifact_ring):
    f()
