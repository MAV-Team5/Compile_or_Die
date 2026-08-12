# 증강 시스템 — 팀원용 가이드

## 1. 증강은 부품 4개다

```
언제 터지나  →  누구를 노리나  →  어떻게 닿나  →  무슨 일이 일어나나
 Trigger        Targeting        Delivery        Effect
```

부품이 이미 있으면 **코드 없이 인스펙터 조립만으로** 새 증강이 나온다.
없는 부품이 필요할 때만 클래스를 하나 추가한다.

시트의 **발동조건 / 목표대상 / 공격형태 / 효과** 컬럼이 그대로 4축이다.

> **Effect는 시각효과가 아니다.** 실제로 게임에 일어나는 일(피해·넉백·연쇄)이다.
> 이펙트는 각 모듈의 연출 필드나 `Vfx` / `Sfx` Effect가 담당한다.

---

## 2. 클래스 — 3계층으로 외운다

### 설계도 — 안 변함, 에셋에 저장

| 클래스 | 역할 |
|---|---|
| `AugmentData` | 증강 1종의 설계도. 모듈 조립 + 레벨 테이블 |
| `AugmentLevelData` | 레벨별 수치 7종 |
| `AugmentCategory` | 증강의 분류 |
| `AugmentModule` | 모듈 4축의 공통 베이스 |

### 실물 — 변함, 런타임에만 존재

| 클래스 | 역할 |
|---|---|
| `AugmentManager` | 보유 목록 관리 + 매 프레임 Tick 순회 |
| `AugmentRunner` | 증강 **1개**의 실행. 오브젝트로 존재 |
| `AugmentInstance` | 현재 레벨과 모듈 상태 보관함 |

### 전달물 — 1회용, 돌려가며 채움

| 클래스 | 역할 |
|---|---|
| `AugmentContext` | **주문서.** 모듈들이 공유하며 채운다 |
| `TargetSet` · `TargetRef` | 타겟 목록. 적과 좌표를 한 목록에 |
| `HitInfo` | 적중 1회 정보 |

### 공용

`AugmentPipeline` (3축 실행) · `TargetQuery` (적 검색) · `ProjectileSpawner` · `VfxSpawner` · `SfxPlayer`

### 전투

`DamageContext` · `DamagePipeline` · `IDamageReceiver` · `IDisplaceable` · `AugmentProjectile` · `DummyTarget`

### 탐색

```
SearchMark       표식 1개.  누가·얼마나·언제까지
MarkerHolder     적 1마리가 지닌 표식 목록. 자동으로 붙는다
SearchRegistry   표식이 붙은 적 전체. 전역 탐색풀
```

**`SearchRegistry` 가 "탐색풀"이다.** Stack·Queue·Graph·Flood Fill 처럼 *남이 탐색한 대상*을 쓰는 증강이 여기를 조회한다.

```csharp
SearchRegistry.CollectAll(buffer);          // 표식 붙은 적 전부
SearchRegistry.CollectBy(instance, buffer); // 이 증강이 표식한 적만
SearchRegistry.Version                      // 목록이 바뀔 때마다 +1 (폴링용)
```

> `Version` 은 이벤트 대신이다. "다음 탐색이 일어나면 발동" 같은 트리거는 이 값이 변했는지만 보면 된다.

**표식 해제 규칙** — 쿨타임이 돌아 다시 발동하면, 그 발동의 **첫 적중 순간**에 이 증강의 지난 표식이 전부 사라진다. 연쇄 도중에는 안 사라진다(`ctx.FiringId` 를 물려받으므로).
`SearchEffect.releaseOnRefire` 를 끄면 표식이 계속 쌓인다.

---

## 3. 한 발 쏘는 흐름

```
AugmentManager.Update()
  └ AugmentRunner.Tick(dt)
      ① trigger.Evaluate()          쿨타임. 아직 소비 안 함
      └ AugmentPipeline.Run(ctx)
          ② targeting.Resolve()     ctx.Targets 에 적/좌표 채움
          ③ delivery.Execute()      투사체·폭발·즉발
               ⋮ (투사체는 비행 시간)
          ④ effect.Apply()          적중마다
               ├ Damage    → DamagePipeline → TakeDamage
               ├ Knockback → IDisplaceable
               └ Chain / SubPipeline → AugmentPipeline.Run() ↺
      trigger.Consume(ctx)          발동 성사 시에만. 시전 연출도 여기서
```

**핵심:** 주문서 하나가 부품들을 돌면서 채워진다. 부품끼리는 서로 모른다.

---

## 4. 모듈 목록

### Trigger
| 모듈 | 설명 |
|---|---|
| `Cooldown` | 일정 주기마다 |

베이스에 **시전 연출**(`castFx`)이 있어 모든 Trigger가 공유한다.

### Targeting — 겹치지 않게 영역이 나뉜다

**무엇을 고르나 × 몇을 고르나**, 두 축으로 읽으면 헷갈리지 않는다.

| 모듈 | 무엇 | 몇 | 개수 필드 |
|---|---|---|---|
| `Nearest` | 적 | **1체** (가장 가까운) | 없음 |
| `Random` | 적 | **N체** (무작위) | `targetCount` |
| `AllInRange` | 적 | **전부** (기본 무제한) | `targetLimit` (안전장치) |
| `RandomPoint` | 좌표 | **N곳** | `pointCount` |
| `OwnerPoint` | 좌표 | **1곳** (원점 고정) | 없음 |

```
적을 고른다   → 주변에 적이 없으면 발동 자체가 안 된다
좌표를 찍는다 → 적이 없어도 무조건 발동한다
```

전부 **`rangeOverride`** 를 갖는다. **0이면 레벨 수치의 `range`.** 이 값이 곧 `ctx.EffectiveRange` 가 되어 뒤따르는 전달 모듈의 비행 거리·폭발 크기까지 결정한다.

> **모든 타겟팅의 공통 사거리**다. 탐색(BFS·DFS) 증강 전용이 아니라 `Damage` 든 뭐든 다 쓴다.

### Delivery

| 모듈 | 설명 | 거리 기준 |
|---|---|---|
| `Projectile` | 타겟을 **겨눠** 투사체 | `travelRangeMultiplier` |
| `Radial` | **각도로** 방사 (타겟 위치 무시) | `travelRangeMultiplier` |
| `Instant` | 비행 없이 즉시 적중 | — |
| `Area` | 타겟 지점마다 원형 폭발 | `blastRadius` (0이면 기준 거리) |
| `Line` | 원점에서 직선 관통 | `lengthMultiplier` |

### Effect

| 모듈 | 설명 |
|---|---|
| `Damage` | 피해 |
| `Knockback` | 밀거나 당김 |
| `Search` | 탐색 표식 (딜 증폭) |
| `Chain` | **반복.** 같은 파이프라인을 depth 만큼 |
| `SubPipeline` | **1회.** 다른 파이프라인을 한 번만 |
| `Vfx` / `Sfx` / `Log` | 연출 |

---

## 4-1. 인스펙터 읽는 법

### ＊ 가 붙은 필드는 필수다

비우면 **에러도 경고도 없이 그 모듈이 통째로 무시된다.** 비어 있으면 붉은 바탕으로 표시되니 그것만 보고 채우면 된다.

```
＊ Projectile Prefab     ← 비면 투사체가 안 나감
＊ Message               ← 비면 로그가 안 뜸
＊ Clip / Vfx Prefab     ← 비면 연출이 안 나옴
```

**모듈 칸(`None`)도 붉게 칠해진다.** `Targeting` 이 `None` 이면 그 파이프라인은 통째로 건너뛴다.

### 모듈 설명은 두 군데에 뜬다

**고를 때** — 드롭다운에 이름과 설명이 같이 나온다.

```
Nearest      —   적 1체 — 가장 가까운
Random       —   적 N체 — 무작위
All In Range —   적 전부 — 반경 안 전원
```

**고른 뒤** — 밑에 회색 한 줄로 남는다.

```
Targeting        [ All In Range ▾ ]
  적 전부 — 반경 안 전원   ·   기본이 무제한. 적이 뭉칠수록 강해진다
```

새 모듈을 만들면 클래스에 `[ModuleInfo("무엇을 한다", "이웃 모듈과 뭐가 다르다")]` 를 붙일 것.

### 연출은 한 줄로 접혀 있다

```
▶ 발사 연출        발사 원점  ·  비어 있음
▶ 폭발 연출        폭발 중심  ·  이펙트 · 효과음
```

**오른쪽에 부착 위치와 채움 여부**가 보인다. 펼치면 이펙트 · 크기 · 효과음 · 음량 4칸.

연출 말고도 긴 묶음은 같은 방식으로 접힌다.

```
▶ 다중 발사
```

새 묶음을 만들려면 `[Fold("제목")] public 묶음클래스 필드 = new();`

### 필드 순서

```
설정값        ← 위. 자주 만지는 것
중첩 파이프라인  ← 아래. 길어서 밀리는 것
```

`Chain` · `SubPipeline` 은 하위 파이프라인이 화면을 잡아먹으므로 **깊이·배율 같은 설정을 위에 두었다.**

---

## 5. 수치 규칙 — 여기가 제일 헷갈린다

### `count` 와 `depth`

```
count   몇 개    타겟 수 · 투사체 수 · 좌표 수 · 스택량
depth   몇 단계  연쇄 단계 · 트리 깊이     ← 횟수만. 거리가 아니다
```

**섞지 말 것.** `ChainEffect`는 `depth`를 쓴다. `count`를 넣으면 연쇄가 안 돈다.

> **탐색 증강의 "깊이"는 `depth` 가 아니라 `range` 다.**
> 기획상 탐색의 깊이는 *전파 반경*이지 횟수가 아니다. 반경 안이면 몇 마리든 전부 표식이 붙는다(상한 없음).
> 시트의 BFS·DFS 성장 항목도 `깊이` → `범위` 로 읽는다.

### `range` 와 `radius`

```
range    증강의 사거리.   레벨 수치(시트). 레벨업으로 성장
radius   그 모듈의 반경.  모듈 필드. 레벨과 무관
```

### `ctx.EffectiveRange`

**타겟팅이 실제로 쓴 반경**을 기록해두는 자리다. Delivery가 이걸 읽는다.

```
Targeting.rangeOverride = 3   →  ctx.EffectiveRange = 3
Projectile 비행 거리          =  3 × travelRangeMultiplier
Area 폭발 반경                =  3            (blastRadius = 0 일 때)
Line 레이저 길이              =  3 × lengthMultiplier
```

타겟팅에서 반경을 좁히면 뒤따르는 전달도 같이 짧아진다. 따로 맞출 필요 없다.

> **새 Targeting 모듈을 만들면 반드시 `ctx.EffectiveRange` 를 채울 것.** 안 채우면 전달이 엉뚱한 거리를 쓴다.

### 0 = 레벨 수치 규칙

모듈 필드가 **0이면 레벨 수치를 쓰고, 0보다 크면 그 값을 쓴다.** 예외 없다.

`rangeOverride` · `targetCount` · `targetLimit` · `pointCount` · `projectileCount` · `shotsPerTarget` · `maxDepthOverride` · `blastRadius` · `bonusOverride` · `durationOverride` 전부 같은 규칙.

### `damageScale`

추가 피해가 아니라 **배율**이다.

```
최종 피해 = 레벨 수치 damage × damageScale × 연쇄 증폭
```

Tree의 *"자식에게 50% 전이"* 처럼 한 증강 안에서 강약을 줄 때만 쓴다. 보통은 1로 둔다.

---

## 6. 증강 만들기 — 코드 없이

1. `Data/Augments/{분류}/` 우클릭 → **Create → CoD → Augment**
2. 파일명은 **시트의 `id`와 동일하게**
3. `id` / `displayName` / `category` / `icon` 입력
4. `levelStats` 배열에 시트 수치 — **안 쓰는 항목은 비워둘 것**
5. `trigger` / `targeting` 드롭다운에서 선택
6. `deliveries` / `effects` 는 `+` 로 여러 개 가능
7. `Ctrl+S` 후 유니티 재시작해서 값이 남는지 확인

드롭다운에는 **해당 축의 모듈만** 나온다. 인스펙터에서 모듈 왼쪽 **색 띠**가 축을 나타낸다 (주황 Trigger · 파랑 Targeting · 보라 Delivery · 초록 Effect).

---

## 7. 새 모듈 만들기

```csharp
[System.Serializable]
public class ConeAreaDelivery : DeliveryModule
{
    [Tooltip("설명을 반드시 달 것. 인스펙터에서 마우스 올리면 보인다.")]
    public float angle = 60f;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit) { }
}
```

**규칙**

- `[System.Serializable]` **필수** — 없으면 인스펙터에서 조용히 사라진다
- `MonoBehaviour` 상속 **금지** — 모듈은 씬에 없는 데이터다
- 상태가 필요하면 중첩 `class State` + `ctx.GetState<State>(this)`. **모듈에 필드로 두면 안 된다**
- 클래스명 접미사를 축에 맞출 것 (`~Targeting` `~Delivery` `~Effect` `~Trigger`) — 드로어가 이걸로 색을 정한다
- 모든 public 필드에 `[Tooltip]`
- 비우면 모듈이 죽는 필드에 `[Required("무슨 일이 생기는지")]`
- 연출은 `[Fx("이름", "부착 위치")] public FxGroup ○○Fx = new();` — 한 줄로 접힌다
- **Targeting 이라면 `ctx.EffectiveRange` 를 반드시 채울 것**
- 설정값은 위, 중첩 `[SerializeReference]` 리스트는 아래에 선언할 것 — 인스펙터가 그 순서대로 그려진다
- 요약 주석 첫 줄에 **담당 영역**을 적을 것. 기존 모듈과 겹치면 새로 만들지 말고 기존 것을 쓸 것

**이름을 확정하고 시작할 것.** `[SerializeReference]`는 클래스 이름을 문자열로 저장한다. 나중에 바꾸면 기존 에셋의 조립이 `None`으로 날아간다. 불가피하면 `[MovedFrom(false, null, null, "예전이름")]`.

---

## 8. 절대 규칙

1. **모든 피해는 `DamageContext`를 통과한다.** `health -= n` 직접 호출 금지
2. **SO는 불변, Instance는 가변.** 모듈이나 SO에 런타임 상태를 두면 에디터에서 영구 오염된다
3. **파이프라인은 순수 C#.** 소환물이 자기 파이프라인을 들고 돌아야 한다
4. **추상 베이스에 `[System.Serializable]`**
5. **모든 Delivery는 `TargetRef.Position` 으로 동작한다.** 적이 꼭 필요할 때만 `IsEnemy` 확인 — 이래야 타겟팅 × 전달 조합이 전부 성립한다
6. **`TargetQuery.Overlap` 결과를 들고 있는 동안 `onHit`을 부르지 말 것.** 공용 버퍼라 덮어써진다. 순회 전에 배열로 복사할 것

---

## 9. 자주 하는 실수

| 증상 | 원인 |
|---|---|
| 인스펙터에 필드가 안 뜬다 | 추상 베이스에 `[System.Serializable]` 누락 |
| 컴포넌트가 안 붙는다 | MonoBehaviour의 **파일명 ≠ 클래스명** |
| 조립해둔 모듈이 `None`이 됐다 | 클래스 이름을 바꿈 |
| 연쇄가 안 돈다 | `levelStats.depth`가 0 (`count`에 넣었을 가능성) |
| 사거리 밖 적을 때린다 | `travelRangeMultiplier` 가 1보다 큼 |
| 모듈이 조용히 아무것도 안 한다 | ＊ 필드가 비었음. 붉은 칸을 찾을 것 |
| 적이 스폰 직후 이상하게 잡힌다 | 물리 좌표 미동기화. `Spawner`가 활성화 후 이동 중 |
| 이펙트가 씬에 쌓인다 | 파티클에 `Stop Action = Destroy` 없음 |
| 빌드가 안 된다 | 런타임 스크립트의 `using UnityEditor;` |

---

## 10. 미해결 — 알고 쓰는 것들

감사에서 나온 항목. **고치기 전까지는 피해서 조립할 것.**

### 🔴 1. `Excluded` 가 동시 전달 사이에도 막는다

`deliveries` 에 **두 개 이상 넣으면 두 번째가 약해진다.** 첫 번째가 맞힌 적을 두 번째가 건너뛴다.

`Excluded` 는 연쇄 중복 방지용인데 같은 단계의 여러 전달까지 막고 있다. 구조 변경이 필요해서 보류.

> **당분간 `deliveries`는 1개만 쓸 것.**

### ✅ 2. 탐지 사거리와 비행 거리가 어긋난다 — 해결됨

`ctx.EffectiveRange` 도입. Targeting이 자기가 쓴 반경을 기록하고 Delivery가 그걸 읽는다.

> Targeting 없이 Delivery만 도는 경우는 없으므로 항상 채워져 있다.
> 새 Targeting 모듈을 만들 때는 **반드시 `ctx.EffectiveRange` 를 채울 것.**

### 🔴 3. `OwnerPoint` + `Projectile` = 무동작

원점과 목표가 같아서 방향이 0이 되고, 가드에 걸려 아무것도 안 나간다. 조립은 되는데 결과가 없다.

> **`OwnerPoint` 는 `Area` 나 `Radial` 과 함께 쓸 것.**

### 🔴 4. `Chain` 중첩 시 지수 폭발

`ChainEffect` 는 하위 효과에 **자기를 자동으로 붙인다.** 하위 `effects` 에 `Chain` 을 또 넣으면 한 단계에 두 번 분기해서 최대 256회까지 간다.

> **`Chain` 의 하위 `effects` 에 `Chain` 을 넣지 말 것.**

### 🔴 5. `speed = 0` 투사체가 제자리에 머문다

`travelRemain` 이 안 줄어서 `lifetime` 이 다할 때까지 떠 있다.

> **`speed` 는 반드시 0보다 크게.**

### 🟡 판단이 필요한 것

**`Radial` 은 타겟을 안 쓴다.** 그런데 타겟이 0이면 발동 자체가 막힌다.
→ **`OwnerPoint` + `Radial`** 이 정답 조합. `Nearest + Radial` 은 적이 있어야만 방사한다.

**`AllInRange` + `Area`** — 폭발 5번이 서로를 갉아먹는다(`Excluded`). 겹치는 폭발이 더 아파야 하는지는 밸런스 판단.

**Effect 순서가 결과를 바꾼다.** `Knockback → Chain` 은 밀려난 위치에서, `Chain → Knockback` 은 원래 위치에서 연쇄한다.

**연쇄 중 적이 죽으면** 원점이 비활성 오브젝트가 된다. 같은 프레임이라 지금은 동작하지만, 풀 재사용 타이밍에 따라 원점이 튈 수 있다.

---

## 11. 진행 상황

| 레이어 | 상태 |
|---|---|
| 설계도 · 실물 · 전달물 3계층 | ✅ |
| `AugmentModuleDrawer` (타입 피커 · 색 띠 · 이름 축약) | ✅ |
| `RequiredDrawer` (＊ 필수 표시) · `FxGroupDrawer` (연출 접기) | ✅ |
| ① Trigger — `Cooldown` + 시전 연출 | ✅ |
| ② Targeting — 5종 + `rangeOverride` | ✅ |
| ③ Delivery — 4종 + 발사/폭발 연출 | ✅ |
| ④ Effect — Damage · Knockback · Vfx · Sfx | ✅ |
| 연쇄 — `Chain`(반복) · `SubPipeline`(1회) | ✅ |
| 투사체 풀링 (`PoolManager.Get(prefab)`) | ✅ |
| 탐색 표식 (`SearchMark` · `MarkerHolder` · `SearchEffect`) | ✅ |
| 전역 탐색풀 (`SearchRegistry`) | ✅ |
| 사거리 일관성 (`ctx.EffectiveRange`) | ✅ |
| **시간차 전파 Delivery** (DFS/BFS 원뿔) | ⬜ 다음 |
| **간선 연결** (`LinkHolder` — Tree·Graph) | ⬜ |
| **내부 증강 (`AugmentModifier`)** | ⬜ |
| **`AugmentPicker` (레벨업 3택)** | ⬜ |
| **설명문 토큰 치환** | ⬜ |
| **시트 임포터** | ⬜ |

---

## 12. 참고

- 전체 구조 흐름도 — `증강시스템_구조흐름도.html`
- 상세 설계 문서 — `증강시스템_구조설계.md`
- 밸런싱 시트 — Google Drive `CoD`
