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
| `AugmentLevelData` | 레벨별 수치 10종 |
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

연출은 인터페이스로 프리팹에 사실만 건넨다. 어떻게 그릴지는 프리팹이 정한다.

| 통로 | 건네는 것 | 구현체 |
|---|---|---|
| `IDirectionalVisual` | 방향 | `RotateToAim` · `DirectionalSprite` · `SwingArc` |
| `ISizedVisual` | 판정 반경 | `RadarSweep` · `SwingArc` |
| `IArcVisual` | 부채꼴 각도 | `SwingArc` |

그리기 원시 도구는 `SweepFan`(부채꼴 메시) 하나고, 그 위에 `RadarSweep`(전방위 스캔) 과 `SwingArc`(휘두르기) 가 얹혀 있다.

### 전투

`DamageContext` · `DamagePipeline` · `IDamageReceiver` · `IDisplaceable` · `IFacingProvider` · `AugmentProjectile` · `DummyTarget`

전역 능력치는 `PlayerStats` + `StatKind` + `StatModifier`. 하드웨어 업그레이드가 여기로 들어온다.

### 탐색

```
SearchMark       표식 1개.  누가·얼마나·언제까지
MarkerHolder     적 1마리가 지닌 표식 목록. 자동으로 붙는다
MarkAnchor       표식이 붙을 자리. 적 프리팹에 직접 만든다
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

**표식 위치·크기는 적 프리팹의 `MarkAnchor` 가 정한다.** 코드는 몸집을 재지 않는다.

```
적 프리팹
 └ MarkAnchor      ← 빈 오브젝트. 머리 위에 두면 된다
      Size    0.5     표식이 들어갈 칸 크기 (월드 유닛)
      Spacing 0       쌓이는 간격. 0이면 칸 크기를 그대로 씀
```

**앵커는 "칸"이다.** 표식 프리팹이 512픽셀짜리든 64픽셀짜리든 **긴 변을 이 칸에 맞춰 줄인다.** 삐져나오지 않고 꽉 찬다.

씬 뷰에 칸이 네모로 그려지니 **눈으로 보면서 `Size` 를 맞추면 된다.** 적 스케일과 무관한 절대 크기다.

앵커가 없으면 본체 위 0.6 지점에 프리팹 크기 그대로 붙는다.

> 크기 조정은 **생성할 때 한 번**만 한다. 표식이 늘거나 줄어도 다시 계산하지 않으므로 배율이 겹쳐 불어나지 않는다.

**표식 발동 연출** — 추가 피해가 실제로 들어간 순간 `SearchEffect.burstFx` 가 표식 자리에서 터진다.

```
표식은 사라지지 않는다. 매 공격마다 추가 피해가 다시 얹히고 연출도 다시 난다
burstInterval 로 도배를 막는다 (기본 0.1초)
```

`DamagePipeline` 2단계의 `MarkerHolder.Consume` 이 피해를 더하면서 연출까지 낸다.

---

### 피해 숫자는 파이프라인이 띄운다

적이 자기 피격 UI를 아는 건 이상한 결합이라 `DamagePipeline` 6단계로 옮겼다. **크리티컬인지, 어느 증강이 때렸는지를 아는 유일한 자리**이기 때문이다.

색·크기를 고르는 순서.

```
1. 크리티컬            팔레트의 critical
2. Effect 가 지정      DamageEffect.accentText 를 켜고 스타일 지정
3. 증강 분류           팔레트의 byCategory — 탐색 / 정렬 / 자료구조 …
4. 없음                팔레트의 normal
```

**3번이 기본이다.** 분류별 색만 채워두면 증강마다 따로 정하지 않아도 자동으로 구분된다.

```
DamageTextPalette (SO)
  Normal      흰색 · 1배
  Critical    노랑 · 1.5배 · 빠르게 튐
  By Category  Search 하늘색 · Sort 보라 · …
```

`DamageTextManager` 인스펙터에 이 에셋을 물려두면 된다. 안 물려도 전부 흰색으로 나올 뿐 동작은 한다.

> ⚠️ 옛 무기(`Bullet`)는 `DamagePipeline` 을 안 거친다. 그래서 표식 보정도, 분류 색도 안 붙는다.
> `Enemy.OnTriggerEnter2D` 에서 숫자만 따로 띄우고 있으니, 증강으로 완전히 넘어가면 그 블록째 지울 것.

---

### 간선 — 남의 탐색에 얹혀 간다

```
SearchTrigger      쿨타임이 찬 뒤 다음 탐색이 일어나면 발동
SearchPool         표식 붙은 적을 노드로 집는다
LinkEffect         노드들을 간선으로 잇는다
LinkHolder         적이 지닌 간선 목록. 자동으로 붙는다
```

**이 게임 최초의 반응형 증강이다.** 나머지는 전부 쿨타임이 차면 스스로 터지는데, 자료구조 계열은 *남이 탐색해둔 것*에 얹혀 간다. 탐색 증강을 하나도 안 들고 있으면 영영 발동하지 않는다 — 그것이 이 계열의 성격이다.

쿨다운 중에 일어난 탐색은 **기억만 하고 흘려보낸다.** 안 그러면 밀린 탐색이 쌓였다가 쿨타임이 차는 순간 즉발한다.

**트리는 부모가 하나뿐이다.**

```
새 노드가 올 때마다  →  이미 놓인 노드 중 가장 가까운 하나에만 붙는다
                        부모가 항상 하나라 사이클이 안 생긴다 = 정의상 트리
connectToAll 을 켜면 →  놓인 것 전부와 잇는다 = 그물(그래프)
```

`EffectModule.Apply` 는 적중 1회씩 불리는데, 그게 오히려 트리가 자라는 방식과 맞아떨어진다. 발동 단위 상태는 `ctx.GetState<T>` + `ctx.FiringId` 로 잡는다 — 표식 해제와 같은 패턴.

**간선은 표식과 수명이 독립적이다.**

```
표식   다시 발동하면 지난 것이 풀린다
간선   다음 탐색이 시작돼도 끊기지 않는다.  화면에 계속 남는다
```

그래서 누적 방어가 따로 필요하다 — 노드당 간선 상한은 **8개**고, 넘치면 오래된 것부터 밀려난다. `duration` 을 0으로 두면 노드가 죽을 때까지 남으니 화면이 선으로 뒤덮일 수 있다.

**전이는 `DamagePipeline` 7단계다.**

```
LinkHops     간선을 타고 몇 번 더.  전이마다 하나씩 준다
LinkVisited  이미 거쳐간 노드.      되짚기 차단
```

전이된 피해도 파이프라인을 **다시 통과**하므로 표식 보정과 분류 색이 그대로 붙는다. `visited` 가 있어 총 전이 횟수는 연결된 노드 수를 넘지 않는다 — 지수로 안 터진다.

> 확장 자리 — 간선을 타고 **상태이상을 공유**하려면 `Link` 에 필드를 하나 더하고
> `StatusHolder.Apply` 에서 이웃에게 넘기면 된다. `Link` 가 모듈을 참조하지 않고
> 값을 복사해 들고 있는 것이 이 때문이다.

---

### 지속 효과 — 탐색과 따로 산다

```
Status (추상)          지속시간 · 세기 · 시각 · 갱신 규칙
 ├ DamageOverTime      주기마다 피해
 └ Slow                이동속도 감소

StatusHolder           대상 1개가 지닌 상태 목록. 자동으로 붙는다
```

`Status` 모듈 하나를 끼우는 구조라 **새 상태이상은 클래스 하나만 추가**하면 된다.

```
Effect: Status
  Duration   0 × 1      ← 시트 지속시간
  Magnitude  0 × 1      ← 시트 효과 피해
  status: [ Damage Over Time ▾ ]   Interval 0.5
```

**탐색 표식은 여기 속하지 않는다.** 전역 탐색풀에 등록되고 다른 증강이 그 목록을 조회하는, 성격이 다른 물건이라 `Search` 로 따로 있다.

시각 자리도 갈린다.

```
탐색 표식    머리 위 MarkAnchor      "내가 표시한 것"
상태이상     대상 몸에 직접           "적에게 걸린 것"
```

| | |
|---|---|
| 지속 피해 | `DamagePipeline` 을 그대로 통과한다 — **탐색 표식 보정도 틱마다 얹힌다.** 지속피해 + 탐색 조합은 생각보다 훨씬 세니 밸런싱 때 주의 |
| 둔화 | `Enemy` 가 `SpeedMultiplier` 를 조회한다. 여러 개면 곱해지되 0에는 안 닿는다 |
| 개체별 값 | `Status` 는 에셋에 하나뿐인 공유 객체다. 남은 시간·타이머는 `StatusHolder.Active` 가 든다 |
| 갱신 vs 중첩 | `refreshInsteadOfStack` 으로 고른다 |

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
| `SearchTrigger` | 쿨타임이 찬 뒤 **다음 탐색이 일어나면** |

베이스에 두 가지가 있어 모든 Trigger가 공유한다.

| 필드 | 뜻 |
|---|---|
| `castFx` | 시전 연출 |
| `noTargetPolicy` | 대상을 못 찾았을 때 쿨타임을 유지(`Hold`)할지 버릴지(`Consume`) |

> `noTargetPolicy` 는 원래 `AugmentData` 루트에 있었다. "발동 조건을 소비할 것인가"는 명백히 Trigger 의 일이라 여기로 옮겼다 — 나중에 발동 조건마다 다른 정책을 줄 수도 있다.

### Targeting — 겹치지 않게 영역이 나뉜다

**무엇을 고르나 × 몇을 고르나**, 두 축으로 읽으면 헷갈리지 않는다.

| 모듈 | 무엇 | 몇 | 개수 필드 |
|---|---|---|---|
| `Nearest` | 적 | **1체** (가장 가까운) | 없음 |
| `Random` | 적 | **N체** (무작위) | `targetCount` |
| `AllInRange` | 적 | **전부** (항상 무제한) | `targetLimit` (직접 입력할 때만) |
| `RandomPoint` | 좌표 | **N곳** | `pointCount` |
| `OwnerPoint` | 좌표 | **1곳** (원점 고정) | 없음 |
| `DirectionPoint` | 좌표 | **1곳** (향한 방향 앞) | 없음 |
| `SearchPool` | 적 | **표식 붙은 것만** | `targetLimit` |

```
적을 고른다   → 주변에 적이 없으면 발동 자체가 안 된다
좌표를 찍는다 → 적이 없어도 무조건 발동한다
```

`AllInRange` 만 **`halfAngle`** 을 추가로 갖는다. 180이면 전방위, 좁히면 진행 방향 부채꼴만 — DFS 전파용이다.

전부 **`rangeOverride`**(`Scalable`) 를 갖는다. 비워두면 시트의 **사거리(`range`)**, 하위 파이프라인 안에서는 **효과 범위(`effectRange`)**. 이 값이 곧 `ctx.EffectiveRange` 가 되어 뒤따르는 전달 모듈의 비행 거리까지 결정한다.

> **모든 타겟팅의 공통 사거리**다. 탐색(BFS·DFS) 증강 전용이 아니라 `Damage` 든 뭐든 다 쓴다.

### Delivery

| 모듈 | 설명 | 거리 기준 |
|---|---|---|
| `Projectile` | 타겟을 **겨눠** 투사체 | `travelRange` ← 사거리 |
| `Radial` | **각도로** 방사 (타겟 위치 무시) | `travelRange` ← 사거리 |
| `Instant` | 타겟을 **그대로** 즉시 적중 | — |
| `Area` | 타겟 지점 **주변까지** 원형·부채꼴 (+본체) | `blastRadius` ← 효과 범위 |
| `Line` | 원점에서 직선 관통 | `length` ← 사거리 |

거리 기준은 전부 **`Scalable`** 이다. 비워두면 시트값, 배수만 주면 비례한다.

> ⚠ **`Area` · `FanArea` 는 시트 `효과범위` 가 0이면 아무것도 안 한다.** 폴백 없이 콘솔에 증강 이름을 찍고 끝난다. 폭발형 증강은 시트 컬럼을 반드시 채울 것.

### 본체와 연출은 다르다

```
본체   Area.bodyPrefab · Line.beamPrefab   판정 그 자체의 그림.  항상 반경에 맞춰진다
연출   FxGroup 의 Vfx                      분위기.             따라 커질지 고른다
```

**본체는 크기를 고를 수 없다.** 판정 반경과 다르게 그리면 플레이어를 속이는 것이기 때문. 근접 휘두르기가 레벨업으로 커지면 낫 그림도 반드시 같이 커져야 한다.

반대로 `FxGroup` 의 **`Scale With Range`** 는 꺼둘 수 있다. 불꽃이나 타격 섬광처럼 크기에 의미가 없는 연출은 범위가 커져도 그대로 두는 게 낫다.

> `FanArea` 는 `Area` 에 흡수됐다. 판정 코드가 같았고 차이는 본체 프리팹뿐이었다.
> 이제 `halfAngle` 하나로 원(180) ↔ 낫(45) 이 연속으로 조절되고, 원형 장판에도 본체를 붙일 수 있다.

**근접 휘두르기 조립**

```
Targeting   OwnerPoint            ← 내 몸이 중심.  DirectionPoint 를 쓰면 중심이 앞으로 밀린다
Delivery    Area
   halfAngle          45
   bodyPrefab         SwingArc 프리팹
   attachBodyToOwner  ✔          ← 걸으면서 쳐도 안 처진다
Effect      Damage · Knockback
```

양손 휘두르기는 `Area` 를 두 개 넣고 `directionOffset` 에 0 과 180 을 준다.

### 부채꼴을 스프라이트로 그리지 말 것

```
❌  45도 부채꼴 스프라이트          halfAngle 을 60으로 바꿔도 그림은 45  →  거짓말
✅  칼날 스프라이트 + SwingArc      칼날이 훑는 범위가 곧 halfAngle      →  항상 일치
```

스프라이트에 각도를 그려 넣으면 **코드가 그 각도를 알 수 없어 검증도 경고도 불가능**하다. 레벨업으로 각도가 자라는 증강을 만들면 그때부터 계속 어긋난다.

`SwingArc` 는 칼날을 호를 따라 이동시키고 `SweepFan` 이 지나간 자리를 채운다. **각도 정보가 스프라이트에 없으므로 어긋날 것 자체가 없다.** 그림은 여전히 자유롭게 그리면 된다.

| 필드 | |
|---|---|
| `duration` | 0.15~0.25. 길면 "칼이 안 닿았는데 죽었다"가 된다 |
| `easing` | 2면 처음이 빠르고 끝이 느려진다 — 칼을 뿌리는 느낌 |
| `clockwise` | 양손으로 만들 때 하나만 뒤집는다 |

> 판정은 한 프레임에 끝나고 휘두르기는 0.2초 걸린다. 히트박스는 즉발, 그림은 시간축 — 액션 게임의 표준 구성이다. 레이더와 같은 구조다.

**잔상에 그림 입히기** — `SweepFan` 의 **`Sprite`** 슬롯에 그림을 드래그하면 된다. 부채꼴이 그 그림의 **해당 각도 조각만** 잘라 보여준다.

```
그림 규격   정사각 안에 꽉 차는 원판.  가운데가 부채꼴 꼭짓점
UV          평면 매핑 — 정점 위치가 곧 그림 좌표
색          nearColor · leadColor · tailColor 가 그림에 곱해진다
            원본 색감 그대로 쓰려면 셋 다 흰색으로 두고 알파만 조절
```

`artFollowsSwing` 을 켜면 그림이 휘두르는 방향을 따라 돌아서 **베는 자국이 항상 같은 모양**으로 나온다. 끄면 그림이 월드 기준으로 고정되고 부채꼴이 그 위를 훑는다.

> **머티리얼의 텍스처 칸에는 넣지 말 것.** 유니티에서 스프라이트와 텍스처는 다른 에셋이라
> 인스펙터에서 드래그가 거부당하고, 시트에서 잘라 쓴 그림이면 UV 도 어긋난다.
> `Sprite` 슬롯은 프로퍼티 블록으로 물리므로 머티리얼이 복제되지도 않고 시트 조각도 알아서 맞춘다.

---

### 유도는 전달이 아니라 투사체 성질이다

```
ProjectileDeliveryBase
 ├ Projectile   타겟을 겨눠 발사
 └ Radial       각도로 발사
      ↓  둘 다 AugmentProjectile 을 낳는다
         유도는 여기 붙으므로 두 전달이 같이 얻는다
```

**유도용 전달 모듈은 없다.** `[Fold("유도")]` 안의 `turnSpeed` 를 올리면 그만이다.

```
Turn Speed         0     0이면 직진. 90이 자연스럽고 360이면 즉시 꺾인다
Seek Radius        0     0이면 발사 때 정한 대상만 쫓는다
Retarget Interval  0.15  목표를 다시 고르는 주기(초)
```

| | |
|---|---|
| `Projectile` | 겨냥한 대상을 그대로 쫓는다. `seekRadius` 없어도 유도됨 |
| `Radial` | 발사 대상이 없다 — **`seekRadius` 를 줘야 유도가 걸린다** |
| `Line` · `Area` · `Instant` | 비행 시간이 없어 유도 개념이 성립하지 않는다 |

방금 뚫은 적은 목표에서 빠진다. 안 그러면 그 자리를 맴돈다.

> **빔을 휘게 하려면** `Line` 을 고치는 게 아니라 "지속 빔" 모듈이 따로 필요하다. `Line` 은 한 프레임에 직사각형 판정을 끝내므로 유도할 시간 자체가 없다.

**`Instant` 와 `Area` 는 대상이 늘어나느냐로 갈린다.**

```
Nearest(1명) + Instant  →  그 1명만
Nearest(1명) + Area     →  그 1명 + 주변 전부      ← 늘어남
```

`Area` 는 타겟을 **폭발 중심점으로만** 쓰고 피해 대상은 새로 검색한다. 그래서 `AllInRange(5명) + Area` 는 폭발이 5번 나고 겹친 적은 여러 번 맞는다.

좌표 타겟일 때는 둘 다 그 자리를 훑어서 동작이 비슷해진다. 이때는 **의도로 구분**한다 — `Instant.pointSearchRadius` 는 "밟은 놈" 판정용이라 작게 고정, `Area.blastRadius` 는 폭발 범위라 레벨 따라 자란다.

`Area` 의 **`halfAngle`** 을 좁히면 부채꼴이 된다. 휘두르기 판정용.

**본체 프리팹이 필요한 모듈** — `Projectile`·`Radial` 은 `projectilePrefab`(＊), `Line` 은 `beamPrefab`(＊). 비우면 판정만 나가고 아무것도 안 보인다. `Instant`·`Area` 는 본체가 없어 연출 묶음만 있다.

### Effect

| 모듈 | 설명 |
|---|---|
| `Damage` | 피해 |
| `Knockback` | 밀거나 당김 |
| `Link` | 적중 대상들을 간선으로 이음 |
| `Search` | 탐색 표식 (딜 증폭) |
| `Status` | **지속 효과.** 지속피해 · 둔화 |
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
  적 전부 — 반경 안 전원   ·   각도를 좁히면 진행 방향 부채꼴만
```

새 모듈을 만들면 클래스에 `[ModuleInfo("무엇을 한다", "이웃 모듈과 뭐가 다르다")]` 를 붙일 것.

### 연출은 한 줄로 접혀 있다

```
▶ 발사 연출        발사 원점  ·  비어 있음
▶ 폭발 연출        폭발 중심  ·  이펙트 · 효과음
```

**오른쪽에 부착 위치와 채움 여부**가 보인다. 펼치면 이펙트 · 크기 · 효과음 · 음량 4칸.

### 연출이 방향을 쓰려면 — `IDirectionalVisual`

스포너는 **방향만 건네고 처리는 프리팹이 정한다.** 모듈에는 회전 옵션 같은 게 없다.

```csharp
public interface IDirectionalVisual { void Aim(Vector2 direction); }
```

```
원형 충격파   → 인터페이스 미구현. 방향을 무시한다
휘두르기 도트  → DirectionalSprite 로 8방향 클립 선택
검기          → transform.up = dir 로 회전
```

**`DirectionalSprite` 가 기본 제공된다.** `Animator` 와 같이 붙이고 클립을 `Swing_E` · `Swing_NE` … 로 이름만 맞추면 된다. State 만 만들어두면 전이는 안 그려도 된다 — `Animator.Play` 가 이름으로 바로 점프한다.

### 연출이 대상을 따라다니려면 — 붙이기 토글

연출 묶음마다 **붙이기** 체크가 있다. 켜면 이펙트가 대상에 붙어 따라간다.

```
따라와야 함    레이더 · 근접 휘두르기 · 오라 · 화상 같은 지속 연출
따라오면 안 됨  폭발 · 착탄 · 피격      ← 그 자리에서 터진 것
```

붙는 대상은 연출이 난 자리가 정한다 — 시전·발사는 시전자, 적중·틱은 맞은 적.

`FanArea` 는 본체 프리팹에도 따로 **`Attach To Owner`** 가 있고 **기본이 켜짐**이다. 걸으면서 휘두를 때 칼이 뒤로 처지지 않게.

> 붙일 때는 반드시 `VfxSpawner.Attach`(= `SetParent(parent, true)`)를 쓴다.
> `Instantiate` 의 parent 인자를 쓰면 부모 스케일이 곱해져 크기가 통째로 어긋난다.

---

### 레이더 스윕 — `RadarSweep` + `SweepFan`

탐색 증강의 시전 연출. 범위만큼 원을 그리고 스캔 라인이 돌면서 지나간 자리를 밝힌다.

```
RadarSweep      회전 · 페이드 · 크기.  ISizedVisual 이라 사거리를 자동으로 받는다
SweepFan        지나간 자리를 채우는 부채꼴. 메시를 직접 그린다
```

**판정과 무관하다.** 표식은 레이더가 돌기 전에 이미 다 붙어 있다 — 시전 연출은 파이프라인 뒤에 나오기 때문. 레이더는 "방금 이 범위를 훑었다"를 보여줄 뿐이다.

`SweepFan` 이 메시인 이유 — `SpriteRenderer` 에는 Radial Fill 이 없다. UI `Image` 를 쓰면 Canvas 가 매 프레임 리빌드돼 오히려 비싸고 스프라이트와 정렬이 따로 논다. 메시는 드로우콜 1회에 정점 49개고, 배열을 재사용해서 GC 할당이 0이다.

| 필드 | |
|---|---|
| `trailAngle` | 0이면 원이 다 채워지고, 90이면 꼬리만 따라다닌다 |
| `opacity` | 링·라인·잔광 전체 투명도 |
| `duration` | 한 바퀴 시간. **쿨타임보다 짧게** — 겹치면 레이더가 두 개 뜬다 |

---

### 연출이 판정 크기를 쓰려면 — `ISizedVisual`

방향과 같은 방식이다. 스포너는 **반경만 건네고 처리는 프리팹이 정한다.**

```csharp
public interface ISizedVisual { void Resize(float radius); }
```

```
원형 폭발 스프라이트  → localScale = radius
파티클                → Shape Radius 조절
고정 크기 연출         → 인터페이스 미구현. 반경을 무시한다
```

`Area` · `FanArea` · `Line` 이 판정 크기를 넘긴다. **범위가 커지면 그림도 같이 커진다.**

> 회전은 도트를 뭉갠다. **8방향 클립이 픽셀아트에는 정답**이고, 회전은 원형·대칭 연출에만 쓸 것.

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
pierce  몇 명    투사체가 뚫는 수 · 레이저 최대 적중
depth   몇 번    연쇄가 더 번지는 횟수     ← 횟수만. 거리가 아니다
```

**섞지 말 것.** `ChainEffect`는 `depth`를 쓴다. `count`를 넣으면 연쇄가 안 돈다.

**`depth` 는 "최초 적중 뒤 몇 번 더 번지나"다.**

```
depth 0  →  안 번짐. 최초 적중만
depth 1  →  1번 번짐   (선형 연쇄 기준 대상 2개)
depth 3  →  3번 번짐   (선형 연쇄 기준 대상 4개)
```

> **탐색 증강의 "깊이"는 `depth` 가 아니라 `effectRange` 다.**
> 기획상 탐색의 깊이는 *전파 반경*이지 횟수가 아니다. 반경 안이면 몇 마리든 전부 표식이 붙는다(상한 없음).
> 시트의 BFS·DFS 성장 항목도 `깊이` → `효과 범위` 로 읽는다.

### `range` 와 `effectRange` — 거리는 둘이다

```
range        발동 사거리   대상을 찾고 거기까지 도달하는 거리
effectRange  효과 범위     닿은 뒤 퍼지는 크기
```

| 무엇 | 어느 스탯 |
|---|---|
| 타겟팅 탐색 반경 | `range` |
| 투사체 비행 거리 | `range` → `travelRange` |
| 레이저 길이 | `range` → `length` |
| **폭발 반경** | **`effectRange`** |
| **하위 파이프라인 반경** | **`effectRange`** |

> **하위 파이프라인은 자동으로 `effectRange` 로 갈아탄다.** `SubPipeline` · `Chain` 안의 타겟팅은 `rangeOverride = 0` 이면 `effectRange` 를 본다. BFS 전파 반경이 레벨 따라 자라는 게 이 규칙 덕분이다.
> `effectRange` 가 0이면 `range` 로 물러난다.

### 방향 — `ctx.Heading`

**이 단계가 향하는 방향**이다. 어디서 오느냐에 따라 출처가 다르다.

```
최초 발동      시전자가 바라보는 쪽   ← IFacingProvider
하위 파이프라인  여기까지 날아온 쪽     ← HitInfo.Direction
```

`Player` 가 `IFacingProvider` 를 구현해서 마지막 이동 방향을 알려준다. 손을 떼도 유지된다.

전달 모듈이 적중할 때마다 방향을 실어준다.

```
투사체   비행 방향
레이저   레이저가 뻗은 방향
폭발     중심에서 대상 쪽 (바깥으로)
즉발     원점에서 대상 쪽
```

쓰는 곳은 세 군데.

| 쓰는 곳 | 무엇 |
|---|---|
| `DirectionPoint` | 그 방향 앞에 좌표를 찍는다 |
| `AllInRange.halfAngle` | 그 방향 부채꼴 안의 적만 고른다 |
| `Area.halfAngle` | 그 방향 부채꼴로만 터진다 (휘두르기) |
| `Radial.aimBasis = Incoming` | 그 방향을 중심으로 방사한다 |

**시전자가 방향을 안 알려주면 zero.** 이때는 각도를 무시하고 전방위로 물러난다.

### `ctx.EffectiveRange`

**타겟팅이 실제로 쓴 반경**을 기록해두는 자리다. Delivery가 이걸 읽는다.

```
Targeting.rangeOverride = 3   →  ctx.EffectiveRange = 3
Projectile 비행 거리          =  travelRange.Of(3)
Line 레이저 길이              =  length.Of(3)
```

폭발 반경은 여기 안 딸린다. **`Area` 는 `effectRange` 를 직접 본다** — 도달 거리가 아니라 퍼지는 크기니까.

타겟팅에서 반경을 좁히면 뒤따르는 전달도 같이 짧아진다. 따로 맞출 필요 없다.

> **새 Targeting 모듈을 만들면 반드시 `ctx.EffectiveRange` 를 채울 것.** 안 채우면 전달이 엉뚱한 거리를 쓴다.

### 전역 보정 — `PlayerStats`

하드웨어 업그레이드·캐릭터 능력·한시적 버프가 여기 모여서 **보유 증강 전부에 한꺼번에** 적용된다.

```
최종 수치 = (시트 레벨 수치 + 가산) × (1 + 승산)
쿨타임만  = (시트 쿨타임 + 가산) ÷ (1 + 승산)     ← 짧아지는 방향
```

통로는 `AugmentInstance.Stat` 하나뿐이다. 모든 모듈이 여기를 거치므로 증강이 몇 개든 자동으로 따라온다.

```csharp
PlayerStats.Current.Add(new StatModifier(StatKind.Damage, cpuUpgrade, percent: 0.2f));
PlayerStats.Current.AddTimed(new StatModifier(StatKind.Cooldown, item, percent: 0.5f), 5f);
PlayerStats.Current.Remove(cpuUpgrade);
```

| | |
|---|---|
| **쿨타임은 나눗셈** | 아무리 쌓아도 0에 닿지 않는다. 곱셈이면 100%에서 매 프레임 발동이 된다 |
| **가산 → 승산 순** | 반대로 하면 같은 20%가 순서에 따라 다른 결과를 내서 밸런싱이 안 잡힌다 |
| **정수는 가산만** | `count` · `pierce` · `depth`. 투사체 1.5개가 없으므로 퍼센트를 안 받는다 |
| `Source` 로 해제 | 같은 출처가 건 보정을 한 번에 뗀다 |

`AugmentInstance.BaseStat` 은 보정을 뺀 시트 원본이다. 설명문에 "기본 수치"를 보여줄 때 쓴다.

> 하드웨어가 붙기 전까지는 `PlayerStats` 인스펙터의 **시작 보정** 목록으로 시험해 볼 수 있다.

### `Scalable` — 고정값 × 배수

시트 수치를 어떻게 바꿔 쓸지 한 줄로 적는다.

```
Blast Radius     [ 0 ]  ×  [ 0.5 ]
```

| 고정값 | 배수 | 결과 |
|---|---|---|
| 0 | 0 | **시트값 그대로** (기본) |
| 0 | 0.5 | 시트값의 절반 — **레벨업하면 같이 자란다** |
| 3 | 0 | 고정 3 |
| 3 | 2 | 고정 6 |

**효과 범위 하나를 여러 곳이 나눠 쓸 때 이걸 쓴다.** 폭발 ×1, 전파 ×0.5, 표식 ×0.3 으로 두면 전부 레벨을 따라 같이 자란다. 고정값으로 박으면 레벨업해도 안 커진다.

쓰는 곳 — 타겟팅 `rangeOverride` · `Area`/`FanArea` `blastRadius` · `Line` `length` · 투사체 `travelRange` · `Search` `bonus`·`duration`

### 방향 오프셋

모든 전달이 **`directionOffset`** 을 갖는다. 자기가 구한 방향을 이 각도만큼 돌린다.

```
Area  halfAngle 90,  directionOffset   0     앞쪽 휘두르기
Area  halfAngle 90,  directionOffset 180     뒤쪽 휘두르기
                                             → 둘 다 넣으면 양쪽
```

`Instant` 는 위치가 이미 정해져 있어 영향이 없다.

### 0 = 시트 수치 규칙

모듈 필드가 **0이면 시트 수치를 쓰고, 0보다 크면 그 값을 쓴다.** 예외 없다.

| 모듈 필드 | ← 시트 |
|---|---|
| `rangeOverride` | 사거리 / 효과 범위 |
| `projectileCount` · `shotsPerTarget` | 수량 |
| `speed` | 속도 |
| `pierce` · `maxHits` | 관통력 |
| `blastRadius` | 효과 범위 |
| `bonus` | 효과 피해 |
| `duration` | 지속시간 |
| `maxDepthOverride` | 깊이 |

**`Scalable` 인 것** — `rangeOverride` · `blastRadius` · `travelRange` · `length` · `bonus` · `duration`

**단순 배수** — `damageScale`(피해량) · `damageMultiplier`(피해량)

**시트와 무관한 고정값** — `lifetime` · `width` · `spreadAngle` · `angleJitter` · `scatterRadius` · `minDistanceRatio` · `pointSearchRadius` · 넉백 `distance` · `amplifyPerDepth`

### 시트 `수량` 은 전달 축 전용이다

**타겟 수를 정하는 필드는 시트를 보지 않는다.** `targetCount` · `pointCount` · `targetLimit` 셋 다 모듈에 적힌 수가 전부다.

```
시트 수량(count)  →  발사체 수만        projectileCount · shotsPerTarget
타겟 수          →  타겟팅 모듈이 직접   targetCount · pointCount · targetLimit
```

**이유는 곱셈이다.** 두 축이 같은 컬럼을 보면 `타겟 수 × 발 수` 가 된다.

```
Random(count 5) + Projectile(count 5발)  →  25발
```

레벨업으로 늘어나야 하는 건 대개 발사체 쪽이라 시트를 그쪽에 줬다.

> **대가** — "레벨업하면 정렬 대상이 늘어난다" 같은 증강은 지금 구조로 표현할 수 없다.
> 필요해지면 시트에 `타겟수` 컬럼을 따로 파는 게 맞다. 컬럼 하나가 두 축에 걸치는 것만은 피할 것.

모든 툴팁에 **한글 시트 이름과 코드 이름을 같이** 적어두었다.

> ⚠ **거리·발사체 계열 모듈 필드 기본값은 0이다.** 예전에는 `speed 12` · `pierce 1` 처럼 0이 아닌 기본값이 있어서 시트를 채워도 안 먹었다. 지금은 새로 만든 모듈이 곧바로 시트를 따른다 — 그러니 **시트 컬럼을 비워두면 아무것도 안 나간다.**

### `damageScale`

추가 피해가 아니라 **배율**이다.

```
최종 피해 = 시트 피해량(damage) × damageScale × 연쇄 증폭
```

Tree의 *"자식에게 50% 전이"* 처럼 한 증강 안에서 강약을 줄 때만 쓴다. 보통은 1로 둔다.

---

## 5-1. BFS / DFS 조립

각도 하나로 갈린다.

```
Trigger    Cooldown
Targeting  Nearest
Delivery   Projectile   pierce 크게(관통), 보이는 프리팹

Effect  ① Search
        ② SubPipeline
             targeting   AllInRange
                           rangeOverride 0        ← effectRange 자동 사용
                           halfAngle   BFS 180 / DFS 60
             deliveries  Radial
                           aimBasis    BFS Random / DFS Incoming
                           spreadAngle BFS 360    / DFS 120
                           projectilePrefab  투명
             effects     Search
        ③ Damage
```

```
BFS   적중 지점에서 사방으로        전방위 파동
DFS   맞은 방향 그대로 앞쪽으로만   한 방향으로 뻗는 탐색
```

전파 반경은 `SubPipeline` 안이라 **시트의 효과 범위(effectRange)** 를 따라 레벨마다 자란다.

---

## 6. 증강 만들기 — 코드 없이

### 완성한 증강은 잠근다

인스펙터 맨 위에 잠금 버튼 두 개가 있다.

```
[ 작동 방식 잠금 ]   [ 수치 잠금 ]
```

| | 잠기는 것 |
|---|---|
| 작동 방식 | `trigger` · `targeting` · `deliveries` · `effects` |
| 수치 | 레벨별 수치 표 (레벨 추가·삭제 포함) |

**따로 있는 이유** — 작동 방식은 확정됐는데 밸런싱은 계속하는 경우가 대부분이다. 그때 작동 방식만 잠가두면 실수로 모듈을 갈아끼우는 일이 없다.

잠긴 칸은 회색으로 비활성화되고 드롭다운도 안 열린다. 풀려면 같은 버튼을 다시 누른다.

---

1. `Data/Augments/{분류}/` 우클릭 → **Create → CoD → Augment**
2. 파일명은 **시트의 `id`와 동일하게**
3. `id` / `displayName` / `category` / `icon` 입력
4. **레벨별 수치 표**에 시트 값 입력 — 안 쓰는 칸은 0으로 비워둘 것
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

> **본체와 연출을 섞지 말 것.**
> ```
> 모듈이 반드시 만들어내는 것   →  개별 필드 + [Required]
> 있으면 좋은 것                →  [Fx] 묶음
> ```
> `Line` 은 레이저 프리팹을 `[Fx]` 안에 넣었다가 "비어 있음"이 정상처럼 보이는 문제가 있었다.
> `Projectile` 처럼 **`○○Prefab`(＊) + `○○Fx`(선택)** 두 칸으로 나눌 것.
>
> `[Fx]` 와 `[Tooltip]` 을 같이 쓰면 **툴팁이 조용히 사라진다.** 설명은 `FxGroup` 안쪽 필드에 달 것.
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
| 사거리 밖 적을 때린다 | `travelRange` 배수가 1보다 큼 |
| 모듈이 조용히 아무것도 안 한다 | ＊ 필드가 비었음. 붉은 칸을 찾을 것 |
| 적이 스폰 직후 이상하게 잡힌다 | 물리 좌표 미동기화. `Spawner`가 활성화 후 이동 중 |
| 이펙트가 씬에 쌓인다 | 파티클에 `Stop Action = Destroy` 없음 |
| 빌드가 안 된다 | 런타임 스크립트의 `using UnityEditor;` |

---

## 10. 미해결 — 알고 쓰는 것들

감사에서 나온 항목. **고치기 전까지는 피해서 조립할 것.**

### ✅ 1. `deliveries` 다중 사용 — 해결됨

예전 `Excluded` 시절에는 두 번째 전달이 약해졌지만, `ChainVisited` 로 정리하면서 **전달 모듈은 이 목록을 아예 안 본다.** 타겟팅만 참조한다.

> **`deliveries` 를 여러 개 넣어도 된다.** 각자 온전히 판정한다.
> `directionOffset` 을 달리 주면 같은 전달로 양쪽 휘두르기·십자 방사를 만들 수 있다.

### ✅ 2. 탐지 사거리와 비행 거리가 어긋난다 — 해결됨

`ctx.EffectiveRange` 도입. Targeting이 자기가 쓴 반경을 기록하고 Delivery가 그걸 읽는다.

> Targeting 없이 Delivery만 도는 경우는 없으므로 항상 채워져 있다.
> 새 Targeting 모듈을 만들 때는 **반드시 `ctx.EffectiveRange` 를 채울 것.**

### ✅ 3. `OwnerPoint` + `Projectile` = 무동작 — 대체됨

원점과 목표가 같아서 방향이 0이 되고, 가드에 걸려 아무것도 안 나간다. 이 조합 자체는 여전히 무동작이다.

> **앞으로 쏘려면 `DirectionPoint` 를 쓸 것.** `OwnerPoint` 는 제자리에서 터지는 `Area` · `Radial` 전용.

### 🟡 4. 연쇄 분기 폭발 — 에디터가 경고한다

**연쇄는 선이 아니라 나무다.** 한 단계에서 b갈래로 갈라지면 총 실행 수가 `b^깊이` 가 된다.

| 분기 b | 깊이 3 | 깊이 8 |
|---|---|---|
| 1 (Nearest 1체) | 4 | 9 |
| 3 (관통 3) | 40 | 9,841 |
| 8 (Radial 8발) | 585 | **1,600만** |

깊이 상한 8은 **분기 없는 선형 연쇄**를 가정한 숫자다. `Chain` 하위에 관통·방사·다중 타겟을 물리면 한 프레임에 프리즈한다.

`ChainAudit` 이 에셋을 열 때 조합을 계산해서 인스펙터 맨 위에 띄운다.

```
⚠ 연쇄가 지수로 불어난다 — 분기 8 × 깊이 3 = 최대 512회 실행.
   하위 타겟 수나 관통·발사 수를 줄이거나 깊이를 낮출 것.
```

`AllInRange`(상한 없음)나 `Area` 처럼 **미리 셀 수 없는** 조합은 숫자 대신 "적이 몰리면 수천 번"으로 경고한다. 중첩된 `SubPipeline` 안에 숨은 `Chain` 도 찾아낸다.

> 경고는 어디까지나 경고다. **런타임 차단은 없으니 뜨면 실제로 고칠 것.**

### 🔴 5. `speed = 0` 투사체가 제자리에 머문다

`travelRemain` 이 안 줄어서 `lifetime` 이 다할 때까지 떠 있다.

> **`speed` 는 반드시 0보다 크게.**

### 🟡 판단이 필요한 것

**`Radial` 은 타겟을 안 쓴다.** 그런데 타겟이 0이면 발동 자체가 막힌다.
→ **`OwnerPoint` + `Radial`** 이 정답 조합. `Nearest + Radial` 은 적이 있어야만 방사한다.

**`AllInRange` + `Area`** — 폭발 5번이 서로를 갉아먹는다(`Excluded`). 겹치는 폭발이 더 아파야 하는지는 밸런스 판단.

**Effect 순서가 결과를 바꾼다.** `Knockback → Chain` 은 밀려난 위치에서, `Chain → Knockback` 은 원래 위치에서 연쇄한다.

**넉백은 `hit.Direction` 을 따른다.** 투사체는 비행 방향, 레이저는 발사 방향, 폭발은 중심에서 바깥. `pull` 을 켜면 그 반대라 폭발은 중심으로 빨아들이고 투사체는 시전자 쪽으로 끌어온다.

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
| 사거리 일관성 (`ctx.EffectiveRange` · `ctx.BaseRange`) | ✅ |
| 스탯 10종 + 레벨 수치 표 드로어 | ✅ |
| 모듈 설명 (`[ModuleInfo]`) · 접이 묶음 (`[Fold]`) | ✅ |
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
