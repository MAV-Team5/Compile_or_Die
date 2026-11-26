# Compile or Die — Art Direction & Color Palette Guide

> **Style Base:** Style A (Hi-Bit Cyberpunk Pixel Art)  
> **Core Principle:** **Clean, hand-crafted pixel readability.** Avoid "AI-generated" noise, mixels, blurry gradients, and over-rendered details.

---

## 1. Anti-AI Pixel Discipline (과한 디테일 배제 원칙)

1. **No Mixels (픽셀 크기 통일)**  
   - 모든 스프라이트는 **동일한 기준 픽셀 그리드(1px = 1 논리 픽셀)**를 유지합니다.
   - 회전·스케일 왜곡이나 서로 다른 해상도의 픽셀을 한 이미지에 섞지 않습니다.
2. **Limited Ramp (개체당 3~5색 제한)**  
   - 하나의 몬스터·캐릭터는 **어두운 외곽선/그림자 1색 + 기본색 1~2색 + 하이라이트 1색 (+ 코어 흰색)** 으로만 구성합니다.
   - 뭉개진 그라데이션, 에어브러시 음영, 불필요한 표면 노이즈를 넣지 않습니다.
3. **Clean Silhouette & 1px Outline**  
   - 다수의 적이 겹치는 서바이벌 장르 특성상, **실루엣과 외곽선**으로 즉시 구분되어야 합니다.
   - 외곽선은 배경(`#0A0E14`)보다 어둡거나 짙은 톤(`#06090F` 또는 개체 암부색)을 사용합니다.
4. **Restrained Glow (절제된 발광)**  
   - 스프라이트 PNG 자체에 뿌연 블룸(Bloom)을 과하게 굽지 않습니다.
   - 발광은 Unity의 `NeonRecolor.shader` · URP Bloom · 최소한의 1px 밝은 심지(Core)로만 처리합니다.
5. **Filter Mode & Import**  
   - Unity 임포트 설정: `Filter Mode = Point (no filter)`, `Compression = None` (또는 무손실).

---

## 2. Role-Based Color Palette (역할별 컬러 팔레트)

플레이어가 탄막과 다수의 적 사이에서 **0.1초 만에 피아·위험도를 구분**할 수 있도록 채도와 명도를 역할별로 분리합니다.

### A. Background & Environment (저채도 · 저명도)
| Role | Hex | Usage |
|---|---|---|
| **Void BG** | `#0A0E14` | 메인 바닥, `UiTheme.background` |
| **Surface** | `#0F1624` | UI 패널·카드 바탕 |
| **Surface Dim** | `#1A2333` | 아이콘 슬롯, 비활성 영역 |
| **Grid Line** | `#163038` | 터미널 좌표 그리드 (은은한 청록) |
| **Faint Code** | `#1E3A42` | 바닥에 깔리는 희미한 코드 문법 장식 |

### B. Player & Friendly (시안 · 민트 계열)
| Role | Hex | Usage |
|---|---|---|
| **Player Primary** | `#4DD0E1` | 플레이어(`Bash`, `Zsh`, `CMD`) 메인 톤, `UiTheme.accent` |
| **Player Core** | `#B8F8FF` | 커서·심볼(`$`, `>`, `_`) 하이라이트 |
| **Player Mint** | `#4DFF7A` | 회복/버프/보조 포인트 |
| **Player Shadow** | `#1A5E6B` | 캐릭터 암부 및 1px 음영 |

### C. Enemies & Hazards (레드 · 코랄 · 앰버 · 바이올렛)
> **규칙:** 적에게는 시안/민트 계열을 주지 않습니다(단, `DeadLock`의 사슬이나 `BlueScreen`의 고유 기믹 등 특수 강조만 제한적 허용).

| Enemy Tier | Primary | Highlight | Shadow | Target Enemies |
|---|---|---|---|---|
| **Swarm / Melee** | `#FF4B3D` | `#FF8A7A` | `#6E161E` | `001 No Image`, `002 Prontf`, `005 Missing SemiColon` |
| **Ranged / Caster** | `#FFB13D` | `#FFE17D` | `#6E3F12` | `003 Using SemiColon`, `004 Warning`, `006 Colon`, `007 Why Note` |
| **Heavy / Elite** | `#FF3D7F` | `#FF8AB5` | `#5C1232` | `008 DeadLock`, `009 Zero` |
| **Boss / BSOD** | `#2E6BFF` | `#9B5BFF` | `#14235C` | `201 BlueScreen` (글리치 보라 + 블루스크린) |

### D. Drops & Pickups
| Item | Hex | Usage |
|---|---|---|
| **EXP Bit** | `#7DF9FF` | 기본 경험치 젬 (작은 다이아몬드/비트 픽셀) |
| **0xBEEF (Heal)** | `#FF6B8F` | 체력 회복 아이템 |
| **0xCAFE (Speed)** | `#7DF9FF` | 이동/공격 속도 버프 |
| **GC (Magnet)** | `#7DFFB2` | 경험치 자석 |

### E. Augments & VFX (`AugmentPalette.cs` 동기화)
기존 `AugmentPalette.cs` 및 `NeonRecolor.shader` 체계를 그대로 유지합니다.
- **탐색 (Search)**: `#4FC3FF` (Linear), `#3D7BFF` (BFS), `#8A6BFF` (DFS)
- **자료구조 (DataStruct)**: `#22F0FF` (Graph), `#4DFF7A` (Tree)
- **언어 (Language)**: `#FF3DDC` (Bash), `#FFE14D` (C), `#FF8A3D` (Java), `#B6FF3D` (Python), `#FF3D7F` (Bash:kill)
- **최적화 (Optimize)**: `#FF4B3D` (BruteForce), `#FF7A5C` (Optimal), `#FF8F1F` (Firewall)
- **코드 / 정렬 (Code / Sort)**: `#FFB13D` / `#FFCF6B` (Iteration), `#C08CFF` / `#9B5BFF` (Bubble / Quick Sort)

### F. UI / HUD (`UiTheme.asset` 동기화)
> **원칙:** 기존 UI 레이아웃·형태는 그대로 유지하고, **배경 채도는 낮추고(`#0A0E14` / `#0F1624`) 포인트·에셋에만 채도를 주는 방식**으로 색감과 아트 리소스만 교체합니다.
- **Text Primary**: `#C8D3E0` / **Text Dim**: `#5A6675`
- **Good**: `#7FD87F` / **Warn**: `#FF6B6B`
- **Font**: `D2Coding` 고정폭 유지 (픽셀/터미널 콘셉트 일치)

---

## 3. Sprite & Canvas Specs (에셋 규격)

| Asset Type | Logical Canvas | Export Scale | Animation Frames | Notes |
|---|---|---|---|---|
| **Player (`Bash`, `Zsh`, `CMD`)** | `32×32` px | `128×128` (4x Nearest) | Idle 2f, Skill 2f, Dead 1f | 단순한 기호 + 커서 실루엣 |
| **Normal Enemy** | `32×32` px | `128×128` (4x Nearest) | Walk/Idle 2~4f, Death 1~2f | 장식 최소화, 심볼 가독성 우선 |
| **Elite / Heavy (`DeadLock` 등)** | `48×48` px | `192×192` (4x Nearest) | Idle 2f, Action 2f | 묵직한 실루엣 |
| **Boss (`BlueScreen`)** | `64×64` px | `256×256` (4x Nearest) | Idle 2~4f, Cast 2f | 글리치 프레임 포함 |
| **Pickup / Projectile** | `16×16` px | `64×64` (4x Nearest) | 1~2f | 투사체는 `NeonRecolor` 연동 |
| **Background Tile** | `64×64` px | `256×256` (4x Nearest) | Tileable | 그리드 + 희미한 코드 패턴 |

---

## 4. Production Workflow (제작 순서)

1. **Figma 디자인 시스템 구축**  
   - 컬러 팔레트 스타일, UI 컴포넌트(HUD 바, 증강 카드, 상점/로비 프레임), 픽셀 규격 보드 정리
2. **파일럿 에셋 1세트 검증**  
   - 플레이어 1종(`Bash`), 적 2종(`Prontf`, `DeadLock`), 배경 타일 1종을 가이드에 맞춰 제작 후 인게임 비율/가독성 확인
3. **전체 에셋 양산 및 Unity 반영**  
   - 나머지 적 6종 + 보스, 캐릭터 `Zsh`/`CMD`, 드랍템/투사체, `UiTheme.asset` 수치 동기화
