"""귀여운 마녀 모자 스프라이트. 4배로 그려서 줄여 계단 없이 매끈하게.
사용: python3 Tools/witch_hat.py <출력.png>"""
import sys
from PIL import Image, ImageDraw, ImageFilter

S = 4
W, H = 256 * S, 256 * S
img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

OUT = (40, 18, 58, 255)        # 테두리 (진보라)
HAT = (128, 70, 200, 255)      # 몸통
HAT_HI = (165, 112, 230, 255)  # 밝은 면
BAND = (255, 150, 40, 255)     # 주황 띠
STAR = (255, 226, 80, 255)
OW = 7 * S                     # 테두리 두께

def poly(pts, fill):
    d.polygon(pts, fill=OUT)
    # 안쪽을 조금 줄여 칠하면 테두리가 남는다
    cx = sum(p[0] for p in pts) / len(pts)
    cy = sum(p[1] for p in pts) / len(pts)
    inner = []
    for x, y in pts:
        dx, dy = x - cx, y - cy
        l = max(1.0, (dx * dx + dy * dy) ** 0.5)
        inner.append((x - dx / l * OW, y - dy / l * OW))
    d.polygon(inner, fill=fill)

def s(x, y):
    return (x * S, y * S)

# 챙 (납작한 타원)
d.ellipse([s(14, 176), s(242, 236)], fill=OUT)
d.ellipse([(14 * S + OW, 176 * S + OW), (242 * S - OW, 236 * S - OW)], fill=HAT)
d.ellipse([(40 * S, 184 * S), (216 * S, 214 * S)], fill=HAT_HI)

# 원뿔 몸통 — 끝이 오른쪽으로 꺾인다
cone = [s(62, 200), s(194, 200), s(160, 112), s(150, 70), s(176, 40), s(212, 34),
        s(186, 22), s(138, 34), s(112, 80), s(92, 130)]
poly(cone, HAT)
# 왼쪽 밝은 면
d.polygon([s(84, 192), s(112, 192), s(124, 120), s(138, 76), s(128, 74), s(104, 128)], fill=HAT_HI)

# 주황 띠
d.polygon([s(70, 182), s(186, 182), s(180, 160), s(78, 160)], fill=OUT)
d.polygon([(70 * S + OW, 182 * S - OW), (186 * S - OW, 182 * S - OW),
           (180 * S - OW, 160 * S + OW), (78 * S + OW, 160 * S + OW)], fill=BAND)

# 별 버클
import math
cx, cy, R, r = 128 * S, 171 * S, 15 * S, 6.5 * S
star = []
for i in range(10):
    a = -math.pi / 2 + i * math.pi / 5
    rad = R if i % 2 == 0 else r
    star.append((cx + math.cos(a) * rad, cy + math.sin(a) * rad))
d.polygon([(x, y) for x, y in star], fill=OUT)
star_in = [(cx + (x - cx) * 0.72, cy + (y - cy) * 0.72) for x, y in star]
d.polygon(star_in, fill=STAR)

# 끝에 작은 방울
d.ellipse([s(198, 18), s(226, 46)], fill=OUT)
d.ellipse([s(203, 23), s(221, 41)], fill=STAR)

# 반짝이 두 개
for (x, y, k) in [(150, 120, 7), (176, 150, 5)]:
    d.polygon([s(x, y - k), s(x + k * 0.35, y), s(x, y + k), s(x - k * 0.35, y)], fill=(255, 255, 255, 230))
    d.polygon([s(x - k, y), s(x, y + k * 0.35), s(x + k, y), s(x, y - k * 0.35)], fill=(255, 255, 255, 230))

img = img.resize((256, 256), Image.LANCZOS)
img.save(sys.argv[1])
print("saved", sys.argv[1])
