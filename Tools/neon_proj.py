"""증강 투사체·빔 네온 스프라이트 (흰 심지 + 번지는 빛). 게임에서 NeonRecolor 셰이더가 아이콘 색으로 칠한다.
사용: python3 Tools/neon_proj.py <bolt.png> <beam.png>"""
import math
import sys
import numpy as np
from PIL import Image

N = 256


def save(rgba, path):
    rgba = np.clip(rgba, 0, 1)
    Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(path)


def bolt(path):
    # 가로 혜성: 앞(오른쪽)이 둥글고 밝은 머리, 뒤로 가늘어지며 흐려지는 꼬리.
    # 원래 막대와 같은 자리(세로 가운데 띠)에 둔다 — 프리팹 크기·콜라이더 그대로
    y, x = np.mgrid[0:N, 0:N].astype(np.float32)
    cy = N / 2
    head_x = N * 0.80
    t = np.clip(x / head_x, 0, 1)                    # 0 꼬리 끝 → 1 머리
    half = 4 + 22 * t ** 1.6                          # 몸 두께(반)
    # 머리 앞쪽은 둥글게
    dx = np.maximum(0, x - head_x)
    d = np.sqrt(((y - cy) / np.maximum(half, 1)) ** 2 + (dx / 26) ** 2)
    core = np.clip(1.15 - d, 0, 1) ** 1.2
    glow = np.exp(-np.maximum(d - 0.6, 0) ** 2 * 2.2) * (0.25 + 0.75 * t)
    glow *= (x < head_x + 60)
    a = np.clip(core + glow * 0.75, 0, 1)
    white = np.clip(core * 1.25, 0, 1) ** 1.5        # 가운데만 하얗게
    # 밝기(최대 채널)=빛의 세기, 흰기(최소 채널)=심지 → 셰이더가 색을 입힌다
    lum = np.clip(0.55 + 0.45 * core, 0, 1)
    rgb_max = lum
    rgb_min = white
    img = np.zeros((N, N, 4), np.float32)
    img[..., 0] = rgb_max                            # R = 밝기
    img[..., 1] = rgb_min + (rgb_max - rgb_min) * 0.0
    img[..., 2] = rgb_min
    # 채널을 섞어 max=lum, min=white 가 되게 (G 를 중간값으로)
    img[..., 1] = (rgb_max + rgb_min) * 0.5
    img[..., 3] = a
    save(img, path)


def beam(path):
    # 세로로 늘어나는 빔: 가운데 흰 심지 + 양옆 번짐, 위아래 끝은 살짝 흐려짐
    y, x = np.mgrid[0:N, 0:N].astype(np.float32)
    cx = N / 2
    u = np.abs(x - cx) / (N / 2)                     # 0 가운데 → 1 가장자리
    v = y / (N - 1)
    core = np.clip(1 - u / 0.22, 0, 1) ** 1.4
    glow = np.exp(-(u / 0.45) ** 2)
    end = np.clip(np.minimum(v, 1 - v) / 0.08, 0, 1)
    a = np.clip(core + glow * 0.8, 0, 1) * (0.35 + 0.65 * end)
    # 흐르는 줄무늬 — 빔 안을 데이터가 지나가는 느낌
    stripe = 0.85 + 0.15 * np.sin(v * math.pi * 10)
    lum = np.clip((0.6 + 0.4 * glow) * stripe, 0, 1)
    white = np.clip(core * 1.3, 0, 1) ** 1.3
    img = np.zeros((N, N, 4), np.float32)
    img[..., 0] = lum
    img[..., 1] = (lum + white) * 0.5
    img[..., 2] = white
    img[..., 3] = a
    save(img, path)


bolt(sys.argv[1])
beam(sys.argv[2])
print("saved")
