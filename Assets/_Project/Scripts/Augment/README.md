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

베이스에 **시전 연출**(`castVfx` · `castVfxScale` · `castSfx`)이 있어 모든 Trigger가 공유한다.

### Targeting
| 모듈 | 설명 |
|---|---|
| `Nearest` | 가장 가까운 적 1체 |
| `Random` | 무작위 적 N체 |
| `AllInRange` | 사거리 안 전부 |
| `RandomPoint` | 무작위 **좌표** N곳 |
| `OwnerPoint` | 원점 그 자리를 좌표로 |

전부 `rangeOverride`가 있다. **0이면 레벨 수치의 `range`를 쓴다.**

### Delivery
| 모듈 | 설명 |
|---|---|
| `Projectile` | 타겟마다 투사체 1발 |
| `Instant` | 비행 없이 즉시 적중 |
| `Area` | 타겟 지점마다 원형 폭발 |
| `Radial` | 원점에서 사방으로 방사 |

### Effect
| 모듈 | 설명 |
|---|---|
| `Damage` | 피해 |
| `Knockback` | 밀거나 당김 |
| `Vfx` / `Sfx` | 적중 연출 |
| `Chain` | **반복.** 같은 파이프라인을 depth 만큼 |
| `SubPipeline` | **1회.** 다른 파이프라인을 한 번만 |

---

## 5. 수치 규칙 — 여기가 제일 헷갈린다

### `count` 와 `depth`

```
count   몇 개    타겟 수 · 투사체 수 · 좌표 수 · 스택량
depth   몇 단계  연쇄 단계 · 탐색 전파 · 트리 깊이
```

**섞지 말 것.** `ChainEffect`는 `depth`를 쓴다. `count`를 넣으면 연쇄가 안 돈다.

### `range` 와 `radius`

```
range    증강의 사거리.   레벨 수치(시트). 레벨업으로 성장
radius   그 모듈의 반경.  모듈 필드. 레벨과 무관
```

### 오버라이드 패턴

모듈 필드가 **0이면 레벨 수치를 쓰고, 0보다 크면 그 값을 쓴다.**

`rangeOverride` · `pickCount` · `maxTargets` · `pointCount` · `projectileCount` · `maxDepthOverride` 전부 같은 규칙.

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
| 사거리 밖 적을 때린다 | 투사체 비행 거리가 사거리보다 김 (아래 미해결 2번) |
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

### 🔴 2. 탐지 사거리와 비행 거리가 어긋난다

`Targeting.rangeOverride` 를 쓰면 **Delivery는 그걸 모른다.** 여전히 `Stat.range × travelRangeMultiplier` 까지 날아가서 사거리 밖 적을 맞힌다.

> **`rangeOverride` 를 쓸 때는 `travelRangeMultiplier` 도 같이 맞출 것.**

해결안: `ctx.EffectiveRange` 를 두고 Targeting이 기록 → Delivery가 참조.

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
| ① Trigger — `Cooldown` + 시전 연출 | ✅ |
| ② Targeting — 5종 + `rangeOverride` | ✅ |
| ③ Delivery — 4종 + 발사/폭발 연출 | ✅ |
| ④ Effect — Damage · Knockback · Vfx · Sfx | ✅ |
| 연쇄 — `Chain`(반복) · `SubPipeline`(1회) | ✅ |
| 투사체 풀링 (`PoolManager.Get(prefab)`) | ✅ |
| **탐색 마커 시스템** | ⬜ 설계안만 |
| **내부 증강 (`AugmentModifier`)** | ⬜ |
| **`AugmentPicker` (레벨업 3택)** | ⬜ |
| **설명문 토큰 치환** | ⬜ |
| **시트 임포터** | ⬜ |

---

## 12. 참고

- 전체 구조 흐름도 — `증강시스템_구조흐름도.html`
- 상세 설계 문서 — `증강시스템_구조설계.md`
- 밸런싱 시트 — Google Drive `CoD`
