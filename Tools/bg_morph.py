"""
할로윈 배경 3x3 콜라주의 칸 경계를 '모프'처럼 녹여서 다시 굽는다.

- 각 칸 그림은 자기 칸 밖으로 거울처럼 이어진다(이음새 없음).
- 이웃 칸과의 경계 띠(BAND)에서 두 그림이 섞이는데, 경계선이 저주파 노이즈로 일렁이고
  섞이는 동안 샘플 위치가 흐름장(FLOW)을 따라 밀려서 얼굴이 바뀌듯 녹아든다.
- 결과는 3칸 주기로 완전히 이어진다 → Unity Tiled 스프라이트 그대로 끝없이 깔린다.

사용: python3 Tools/bg_morph.py <원본 콜라주.jpg> <출력.jpg>
"""
import math
import sys

import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None

BAND = 0.32      # 경계 양쪽으로 칸의 몇 할을 섞을지 (0.5 면 순수 영역 없음)
WOBBLE = 0.10    # 경계선이 노이즈로 흔들리는 폭 (칸 비율)
FLOW = 0.045     # 섞이는 동안 샘플이 밀리는 폭 (칸 비율)
ROWS = 240       # 한 번에 처리할 줄 수

src_path, out_path = sys.argv[1], sys.argv[2]
src = np.asarray(Image.open(src_path).convert("RGB"))
TH, TW = src.shape[:2]
H, W = TH // 3, TW // 3
print(f"atlas {TW}x{TH}, cell {W}x{H}")

rng = np.random.default_rng(1031)


def periodic_noise(n_terms, max_freq):
    """주기 (3W, 3H) 로 이어지는 사인 합 노이즈. 함수 (X, Y) -> [-1, 1] 근처"""
    terms = []
    for _ in range(n_terms):
        kx = int(rng.integers(-max_freq, max_freq + 1))
        ky = int(rng.integers(-max_freq, max_freq + 1))
        if kx == 0 and ky == 0:
            kx = 1
        terms.append((kx, ky, rng.uniform(0, 2 * math.pi), 1.0 / math.hypot(kx, ky)))
    norm = sum(t[3] for t in terms)

    def f(X, Y):
        acc = np.zeros(np.broadcast(X, Y).shape, np.float32)
        for kx, ky, ph, a in terms:
            acc += a * np.sin(2 * np.pi * (kx * X / TW + ky * Y / TH) + ph)
        return acc / norm * 1.8

    return f


wob_x = periodic_noise(10, 9)
wob_y = periodic_noise(10, 9)
flow_x = periodic_noise(12, 12)
flow_y = periodic_noise(12, 12)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def mirror(u):
    # 자기 칸 [0,1] 밖은 거울로 접는다 (u 는 -0.5..1.5 범위)
    u = np.where(u < 0, -u, u)
    return np.where(u > 1, 2 - u, u)


out = np.empty_like(src)
X = np.arange(TW, dtype=np.float32)[None, :]

for y0 in range(0, TH, ROWS):
    y1 = min(TH, y0 + ROWS)
    Y = np.arange(y0, y1, dtype=np.float32)[:, None]
    XX = np.broadcast_to(X, (y1 - y0, TW))
    YY = np.broadcast_to(Y, (y1 - y0, TW))

    cx = XX / W
    cy = YY / H
    i0 = np.floor(cx - 0.5)
    j0 = np.floor(cy - 0.5)
    fx = cx - 0.5 - i0          # 0 = 칸 i0 중심, 1 = 칸 i0+1 중심
    fy = cy - 0.5 - j0

    sx = smooth(0.5 - BAND, 0.5 + BAND, fx + WOBBLE * wob_x(XX, YY))
    sy = smooth(0.5 - BAND, 0.5 + BAND, fy + WOBBLE * wob_y(XX, YY))

    dx = FLOW * flow_x(XX, YY)
    dy = FLOW * flow_y(XX, YY)

    acc = np.zeros((y1 - y0, TW, 3), np.float32)
    for di, wx in ((0, 1 - sx), (1, sx)):
        for dj, wy in ((0, 1 - sy), (1, sy)):
            w = wx * wy
            ik = i0 + di
            jk = j0 + dj
            # 섞이는 중일수록(가중치가 1 에서 멀수록) 흐름장 따라 밀려서 샘플
            push = 1 - w
            u = mirror(cx - ik + dx * push)
            v = mirror(cy - jk + dy * push)
            ax = (np.mod(ik, 3).astype(np.int64) * W + np.clip((u * (W - 1)).astype(np.int64), 0, W - 1))
            ay = (np.mod(jk, 3).astype(np.int64) * H + np.clip((v * (H - 1)).astype(np.int64), 0, H - 1))
            acc += w[..., None] * src[ay, ax].astype(np.float32)

    out[y0:y1] = np.clip(acc + 0.5, 0, 255).astype(np.uint8)
    print(f"rows {y1}/{TH}", flush=True)

Image.fromarray(out).save(out_path, quality=93, subsampling=0)
print("saved", out_path)
