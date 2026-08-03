# 증강 시스템 — 팀원용 가이드

## 1. 증강은 부품 4개다

```
언제 터지나  →  누구를 노리나  →  어떻게 닿나  →  무슨 일이 일어나나
 Trigger        Targeting        Delivery        Effect
```

부품이 이미 있으면 **코드 없이 인스펙터 조립만으로** 새 증강이 나온다.
없는 부품이 필요할 때만 클래스를 하나 추가한다.

Bash의 경우:

| 축 | 선택 | 뜻 |
|---|---|---|
| Trigger | `CooldownTrigger` | 쿨타임마다 |
| Targeting | `NearestTargeting` | 가장 가까운 적을 |
| Delivery | `ChainDelivery` | 연쇄 투사체로 |
| Effect | `DamageEffect` | 피해를 준다 |

시트의 **발동조건 / 목표대상 / 공격형태 / 효과** 컬럼이 그대로 4축이다.

> **Effect는 시각효과가 아니다.** 실제로 게임에 일어나는 일(피해·탐색등록·CC·링크·중첩)이다.
> 파티클은 `AugmentData`의 Visual 항목에 따로 있다.

---

## 2. 클래스 — 3계층으로 외운다

### 설계도 — 안 변함, 에셋에 저장

| 클래스 | 역할 |
|---|---|
| `AugmentData` | 증강 1종의 설계도. 모듈 조립 + 레벨 테이블. **런타임 불변** |
| `AugmentLevelData` | 레벨별 수치 7종 (struct) |
| `AugmentCategory` | 증강의 분류. CS 개념의 출처 |
| `AugmentModule` | 모듈 4축의 공통 베이스 |

### 실물 — 변함, 런타임에만 존재

| 클래스 | 역할 |
|---|---|
| `AugmentManager` | 보유 목록 관리(지급·레벨업·조회) + 매 프레임 Tick 순회 |
| `AugmentRunner` | 증강 **1개**의 실행. 자기 Instance와 ctx 소유. 오브젝트로 존재 |
| `AugmentInstance` | 현재 레벨과 모듈 상태 보관함 |

### 전달물 — 1회용, 돌려가며 채움

| 클래스 | 역할 |
|---|---|
| `AugmentContext` | **주문서.** 모듈들이 공유하며 채운다 |
| `TargetSet` | 타겟팅 결과 (적 + 좌표) |
| `HitInfo` | 적중 1회 정보 |

### 전투

`DamageContext` · `DamagePipeline` · `IDamageReceiver` · `DummyTarget`

---

## 3. 한 발 쏘는 흐름

```
AugmentManager.Update()          보유 Runner 순회
  └ AugmentRunner.Tick(dt)
      ├ ctx.Begin()              주문서 초기화
      ├ ① trigger.Evaluate()     쿨타임 미달이면 종료
      ├ ② targeting.Resolve()    ctx.Targets 에 대상 채움
      ├ ③ delivery.Execute()     투사체 발사 후 즉시 반환
      │        ⋮ 비행
      └ ④ effect.Apply()         DamageContext 생성
                                  → DamagePipeline
                                  → TakeDamage()
```

**핵심:** 주문서 하나가 부품 4개를 돌면서 채워진다.
부품끼리는 서로 모르고 주문서만 본다. 그래서 Targeting만 바꿔도 나머지는 코드가 안 바뀐다.

---

## 4. 증강 만들기 — 코드 없이

1. `Data/Augments/{분류}/` 우클릭 → **Create → CoD → Augment**
2. 파일명은 **시트의 `id`와 동일하게** (예: `BASH`)
3. `id` / `displayName` / `category` 입력
4. `description`에 토큰 사용 — `{count}`, `{effectDamage}` 는 현재 레벨 수치로 자동 치환
5. `Trigger` 드롭다운에서 모듈 선택 → 펼쳐서 세부값 조정
6. `Deliveries` / `Effects`는 `+` 로 여러 개 가능
7. `levelStats` 배열에 시트 수치 입력 — **안 쓰는 항목은 비워둘 것**
8. `Ctrl+S` 후 유니티 재시작해서 값이 남는지 확인

> 드롭다운에는 해당 축의 모듈만 나온다. Targeting 칸에 Delivery를 넣는 실수는 불가능하다.

---

## 5. 새 모듈 만들기

```csharp
[System.Serializable]
public class ConeAreaDelivery : DeliveryModule
{
    public float angle = 60f;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit) { }
}
```

- `[System.Serializable]` **필수** — 없으면 인스펙터에서 조용히 사라진다
- `MonoBehaviour` 상속 **금지** — 모듈은 씬에 없는 데이터다
- 상태가 필요하면 중첩 `class State`를 만들고 `ctx.GetState<State>(this)` 로 꺼낸다. **모듈에 필드로 두면 안 된다**

**필드에 뭘 넣나** — 레벨에 따라 변하면 `AugmentLevelData`, 증강마다 고정이면 모듈 필드.

**이름을 확정하고 시작할 것.** `[SerializeReference]`는 클래스 이름을 문자열로 저장한다. 나중에 바꾸면 기존 에셋의 조립이 전부 `None`으로 날아간다. 불가피하면 `[MovedFrom(false, null, null, "예전이름")]`.

---

## 6. 절대 규칙

1. **모든 피해는 `DamageContext`를 통과한다.** `health -= n` 직접 호출 금지
2. **SO는 불변, Instance는 가변.** 모듈이나 SO에 런타임 상태를 두면 에디터에서 영구 오염된다
3. **파이프라인은 순수 C#.** 소환물이 자기 파이프라인을 들고 돌아야 한다
4. **추상 베이스에 `[System.Serializable]`**
5. **적의 상태는 마커, 수명은 정책.** 증강 이름으로 `if` 분기를 시작하면 무너진다

---

## 7. 자주 하는 실수

| 증상 | 원인 |
|---|---|
| 인스펙터에 필드가 안 뜬다 | 추상 베이스에 `[System.Serializable]` 누락 |
| 컴포넌트가 안 붙는다 | MonoBehaviour의 **파일명 ≠ 클래스명** |
| 조립해둔 모듈이 `None`이 됐다 | 클래스 이름을 바꿈 |
| 플레이 끝내니 값이 이상하다 | 모듈이나 SO에 런타임 상태를 넣음 |
| 에셋 참조가 깨졌다 | 탐색기에서 파일 이동 (반드시 Project 창 안에서) |
| 빌드가 안 된다 | 런타임 스크립트의 `using UnityEditor;` |

---

## 8. 진행 상황

| 레이어 | 상태 |
|---|---|
| 설계도 (`AugmentData` · `AugmentLevelData` · 모듈 베이스) | ✅ |
| 실물 (`AugmentManager` · `AugmentRunner` · `AugmentInstance`) | ✅ |
| 전달물 (`AugmentContext` · `TargetSet` · `HitInfo`) | ✅ |
| `AugmentModuleDrawer` (인스펙터 타입 피커) | ✅ |
| ① `CooldownTrigger` | ✅ |
| ② Targeting 구현 | ⬜ |
| ③ Delivery 구현 + `AugmentProjectile` | ⬜ |
| ④ Effect 구현 + `DamagePipeline` | ⬜ |
| 내부 증강 (`AugmentModifier`) | ⬜ |
| 마커 시스템 (탐색 · 비트 · 링크) | ⬜ |
| `SearchPoolManager` · `AugmentPicker` · `EnemyQuery` | ⬜ |

**①까지만 동작한다.** 지금 조립해도 쿨타임 로그만 찍히고 실제 공격은 없다.
