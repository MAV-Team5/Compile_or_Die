"""
증강 아이콘 네온 생성기.

모든 증강 아이콘을 한 스타일로 만든다 — 투명 배경 위에 개념을 그린 기호 하나,
증강 고유색의 네온(넓게 번지는 빛 + 또렷한 선 + 하얗게 달아오른 심).
계열(카테고리)마다 색 범위를 정하고, 그 안에서 증강마다 다른 색을 준다.

    탐색     파랑 계열            LinearSearch · BFS · DFS
    자료구조 청록·초록            Graph · Tree
    언어     마젠타·노랑·주황·라임 Bash · C · Java · Python · Bash:kill
    최적화   빨강·주황            BruteForce · BruteForce 양방향 · Firewall
    코드     호박색               Iteration · Iteration While
    정렬     보라                 Bubble Sort · Quick sort
    아이템   개별색               0xBEEF(회복) · 0xCAFE(속도) · GC(경험치 흡수)

색표는 게임 쪽 AugmentPalette.cs 와 같아야 한다 — 이펙트 색이 아이콘 색을 따른다.

실행:  python3 Tools/neon_icons.py [ID ...]   (Pillow 필요. ID 를 주면 그것만)
출력:  Assets/_Project/Art/Augment/Neon/Neon_<ID>.png  (512×512)
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets/_Project/Art/Augment/Neon")
FONT = os.path.join(ROOT, "Assets/_Project/Art/Fonts/D2Coding-Ver1.3.2-20180524.ttf")

S = 512          # 결과 크기
SS = 2           # 내부 배율 — 크게 그려 줄이면 선이 매끄럽다
W = 20           # 기본 선 굵기(결과 기준 px)

PALETTE = {
    "LINEAR_SEARCH": "#4FC3FF",
    "BFS": "#3D7BFF",
    "DFS": "#8A6BFF",
    "GRAPH": "#22F0FF",
    "TREE": "#4DFF7A",
    "BASH": "#FF3DDC",
    "C_LAN": "#FFE14D",
    "JAVA": "#FF8A3D",
    "PYTHON": "#B6FF3D",
    "BASH:KILL": "#FF3D7F",
    "BRUTE_FORCE": "#FF4B3D",
    "BRUTE_FORCE_OPTIAML": "#FF7A5C",
    "FIREWALL": "#FF8F1F",
    "ITERATION": "#FFB13D",
    "ITERATION_WHILE": "#FFCF6B",
    "BUBBLE_SORT": "#C08CFF",
    "QUICK_SORT": "#9B5BFF",
    "ITEM_0XBEEF": "#FF6B8F",
    "ITEM_0XCAFE": "#7DF9FF",
    "ITEM_GC": "#7DFFB2",
}


def rgb(hexs):
    hexs = hexs.lstrip("#")
    return tuple(int(hexs[i:i + 2], 16) for i in (0, 2, 4))


def P(x, y):
    """결과 좌표(0~512)를 내부 좌표로."""
    return (x * SS, y * SS)


class Pen:
    """한 굵기로 마스크에 그리는 붓. 같은 그림을 굵기만 바꿔 여러 번 그려 네온 층을 만든다."""

    def __init__(self, draw, width):
        self.d = draw
        self.w = width * SS

    def line(self, pts, closed=False):
        pts = [P(*p) for p in pts]
        if closed:
            pts = pts + [pts[0]]
        self.d.line(pts, fill=255, width=int(self.w), joint="curve")
        r = self.w / 2
        for (x, y) in pts:   # 꺾인 곳·끝을 둥글게
            self.d.ellipse((x - r, y - r, x + r, y + r), fill=255)

    def circle(self, cx, cy, rad):
        cx, cy, rad = cx * SS, cy * SS, rad * SS
        half = self.w / 2
        self.d.ellipse((cx - rad - half, cy - rad - half, cx + rad + half, cy + rad + half),
                       outline=255, width=int(self.w))

    def dot(self, cx, cy, rad):
        cx, cy, rad = cx * SS, cy * SS, (rad * SS + self.w * 0.25)
        self.d.ellipse((cx - rad, cy - rad, cx + rad, cy + rad), fill=255)

    def arc(self, cx, cy, rad, a0, a1, arrow=False):
        steps = max(8, int(abs(a1 - a0) / 4))
        pts = [(cx + rad * math.cos(math.radians(a0 + (a1 - a0) * i / steps)),
                cy + rad * math.sin(math.radians(a0 + (a1 - a0) * i / steps))) for i in range(steps + 1)]
        self.line(pts)
        if arrow:
            ex, ey = pts[-1]
            tang = math.radians(a1 + (90 if a1 > a0 else -90))
            self.arrowhead(ex, ey, tang)

    def arrowhead(self, x, y, angle, size=46):
        a1 = angle + math.radians(150)
        a2 = angle - math.radians(150)
        self.line([(x + size * math.cos(a1), y + size * math.sin(a1)), (x, y),
                   (x + size * math.cos(a2), y + size * math.sin(a2))])

    def text(self, cx, cy, s, size):
        font = ImageFont.truetype(FONT, int(size * SS))
        stroke = int(self.w * 0.22)
        self.d.text((cx * SS, cy * SS), s, font=font, fill=255, anchor="mm",
                    stroke_width=stroke, stroke_fill=255)


# ── 기호 ────────────────────────────────────────────────

def g_bash(p):            # >_  와 파이프 |
    p.line([(110, 160), (210, 250), (110, 340)])
    p.line([(240, 345), (340, 345)])
    p.line([(400, 140), (400, 380)])


def g_c(p):               # C 와 그 속을 꿰뚫는 포인터 화살
    p.arc(250, 256, 140, 45, 315)
    p.line([(170, 256), (440, 256)])
    p.arrowhead(440, 256, 0, 52)


def g_java(p):            # 컵과 김
    p.line([(140, 230), (150, 390), (330, 390), (340, 230)], closed=True)
    p.arc(370, 290, 42, -80, 90)
    for x in (190, 240, 290):
        p.line([(x, 190), (x - 18, 160), (x, 130), (x - 18, 100)])


def g_python(p):          # S 자로 휘감는 뱀
    pts = []
    for i in range(41):
        t = i / 40
        x = 140 + 230 * t
        y = 256 + 95 * math.sin(t * math.pi * 2.2)
        pts.append((x, y))
    p.line(pts)
    p.dot(140, 256, 22)
    p.line([(395, 210), (440, 190)])


def g_bash_kill(p):       # >_ 에 조준선 — kill
    p.line([(100, 170), (190, 250), (100, 330)])
    p.line([(210, 335), (290, 335)])
    p.circle(355, 250, 70)
    p.line([(355, 160), (355, 340)])
    p.line([(265, 250), (445, 250)])


def g_dfs(p):             # 한 가지를 끝까지 파고드는 경로
    nodes = [(256, 90), (180, 185), (230, 290), (160, 400)]
    p.line(nodes)
    for (x, y) in nodes:
        p.dot(x, y, 26)
    p.line([(256, 90), (360, 170)])        # 안 간 가지(짧게)
    p.line([(180, 185), (110, 250)])
    p.arrowhead(160, 400, math.radians(120), 40)


def g_bfs(p):             # 가운데서 겹겹이 퍼지는 원
    p.dot(256, 256, 26)
    for r in (70, 125, 178):
        p.circle(256, 256, r)


def g_linear(p):          # 한 줄을 끝까지 훑는 파동
    p.line([(70, 256), (430, 256)])
    p.arrowhead(440, 256, 0, 56)
    for x in (130, 200, 270, 340):
        p.line([(x, 215), (x, 297)])


def g_graph(p):           # 그물처럼 얽힌 노드
    nodes = [(256, 100), (400, 200), (350, 380), (160, 380), (110, 200), (256, 256)]
    edges = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 0), (5, 0), (5, 2), (5, 3), (5, 1), (4, 5)]
    for a, b in edges:
        p.line([nodes[a], nodes[b]])
    for (x, y) in nodes:
        p.dot(x, y, 28)


def g_tree(p):            # 뿌리에서 갈라지는 나무
    root = (256, 95)
    mid = [(160, 235), (352, 235)]
    leaf = [(110, 390), (210, 390), (302, 390), (402, 390)]
    for m in mid:
        p.line([root, m])
    p.line([mid[0], leaf[0]]); p.line([mid[0], leaf[1]])
    p.line([mid[1], leaf[2]]); p.line([mid[1], leaf[3]])
    for (x, y) in [root] + mid + leaf:
        p.dot(x, y, 26)


def g_brute(p):           # 앞을 휩쓰는 부채꼴 일격
    cx, cy = 150, 380
    p.arc(cx, cy, 260, -85, -5)
    for a in (-80, -45, -10):
        r = math.radians(a)
        p.line([(cx, cy), (cx + 260 * math.cos(r), cy + 260 * math.sin(r))])


def g_brute_opt(p):       # 앞뒤 양쪽 부채꼴
    cx, cy = 256, 256
    p.arc(cx, cy, 190, -60, 60)
    p.arc(cx, cy, 190, 120, 240)
    for a in (-35, 35, 145, 215):
        r = math.radians(a)
        p.line([(cx, cy), (cx + 190 * math.cos(r), cy + 190 * math.sin(r))])
    p.dot(cx, cy, 24)


def g_firewall(p):        # 벽돌 방벽
    top, h, rows = 150, 70, 3
    left, right = 90, 422
    for i in range(rows + 1):
        y = top + i * h
        p.line([(left, y), (right, y)])
    for i in range(rows):
        y0, y1 = top + i * h, top + (i + 1) * h
        off = 0 if i % 2 == 0 else 55
        x = left + 55 + off
        while x < right - 20:
            p.line([(x, y0), (x, y1)])
            x += 110
    p.line([(left, top), (left, top + rows * h)])
    p.line([(right, top), (right, top + rows * h)])


def g_iter(p):            # 되돌아오는 화살 + i
    p.arc(256, 256, 160, -60, 240, arrow=True)
    p.text(256, 262, "i", 190)


def g_iter_while(p):      # 끝없는 반복 ∞
    pts = []
    for i in range(121):
        t = i / 120 * math.pi * 2
        d = 1 + math.sin(t) ** 2
        pts.append((256 + 175 * math.cos(t) / d, 256 + 175 * math.sin(t) * math.cos(t) / d))
    p.line(pts)


def g_bubble(p):          # 커지며 떠오르는 거품
    for (x, y, r) in [(150, 380, 40), (240, 300, 60), (350, 190, 88)]:
        p.circle(x, y, r)
    p.dot(130, 230, 12)
    p.dot(400, 340, 10)


def g_quick(p):           # 기준점을 사이에 두고 갈라지는 막대
    p.line([(256, 80), (256, 432)])
    for i, h in enumerate([90, 150, 120]):
        x = 110 + i * 45
        p.line([(x, 410), (x, 410 - h)])
    for i, h in enumerate([210, 260, 300]):
        x = 312 + i * 45
        p.line([(x, 410), (x, 410 - h)])


def g_heal(p):            # 하트와 +
    pts = []
    for i in range(81):
        t = i / 80 * math.pi * 2
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((256 + x * 11, 250 - y * 11))
    p.line(pts)
    p.line([(256, 200), (256, 300)])
    p.line([(206, 250), (306, 250)])


def g_speed(p):           # 번개
    p.line([(300, 70), (170, 270), (270, 270), (200, 440), (360, 210), (255, 210), (320, 70)], closed=True)


def g_gc(p):              # 휴지통 — 쓰레기(경험치)를 모은다
    p.line([(160, 190), (180, 420), (332, 420), (352, 190)])
    p.line([(130, 190), (382, 190)])
    p.line([(220, 190), (230, 150), (282, 150), (292, 190)])
    for x in (220, 256, 292):
        p.line([(x, 240), (x, 370)])


GLYPHS = {
    "BASH": g_bash, "C_LAN": g_c, "JAVA": g_java, "PYTHON": g_python, "BASH:KILL": g_bash_kill,
    "DFS": g_dfs, "BFS": g_bfs, "LINEAR_SEARCH": g_linear,
    "GRAPH": g_graph, "TREE": g_tree,
    "BRUTE_FORCE": g_brute, "BRUTE_FORCE_OPTIAML": g_brute_opt, "FIREWALL": g_firewall,
    "ITERATION": g_iter, "ITERATION_WHILE": g_iter_while,
    "BUBBLE_SORT": g_bubble, "QUICK_SORT": g_quick,
    "ITEM_0XBEEF": g_heal, "ITEM_0XCAFE": g_speed, "ITEM_GC": g_gc,
}


def mask(glyph, width):
    img = Image.new("L", (S * SS, S * SS), 0)
    glyph(Pen(ImageDraw.Draw(img), width))
    return img


def tint(m, color, alpha_scale=1.0):
    r, g, b = color
    layer = Image.new("RGBA", m.size, (r, g, b, 0))
    a = m.point(lambda v: int(min(255, v * alpha_scale)))
    layer.putalpha(a)
    return layer


def render(glyph, hexcolor):
    color = rgb(hexcolor)
    light = tuple(int(c + (255 - c) * 0.55) for c in color)

    body = mask(glyph, W)
    core = mask(glyph, W * 0.38)

    out = Image.new("RGBA", body.size, (0, 0, 0, 0))
    # 넓게 번지는 빛 두 겹 — 멀리서도 색이 보이게
    out = Image.alpha_composite(out, tint(body.filter(ImageFilter.GaussianBlur(30 * SS)), color, 1.6))
    out = Image.alpha_composite(out, tint(body.filter(ImageFilter.GaussianBlur(10 * SS)), color, 1.4))
    # 또렷한 선과 하얗게 달아오른 심
    out = Image.alpha_composite(out, tint(body, light))
    out = Image.alpha_composite(out, tint(core.filter(ImageFilter.GaussianBlur(1.2 * SS)), (255, 255, 255), 1.0))

    return out.resize((S, S), Image.LANCZOS)


def safe(name):
    return name.replace(":", "_")


def main():
    os.makedirs(OUT, exist_ok=True)
    only = set(sys.argv[1:])
    for key, glyph in GLYPHS.items():
        if only and key not in only:
            continue
        img = render(glyph, PALETTE[key])
        path = os.path.join(OUT, f"Neon_{safe(key)}.png")
        img.save(path)
        print("saved", os.path.relpath(path, ROOT))


if __name__ == "__main__":
    main()
