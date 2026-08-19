# 유니티에서 손으로 해야 하는 것

코드로는 못 하는 작업 목록. 다 끝나면 이 파일은 지워도 된다.

---

## 1. 지금 당장 — 안 하면 증강이 안 돈다

### 시트 컬럼 추가

`AugmentLevelData` 는 이미 10칸인데 시트가 못 따라오고 있다.

| 컬럼 | 코드 이름 | 없으면 |
|---|---|---|
| 효과범위 | `effectRange` | **`Area` · `FanArea` 가 아예 안 터진다** (콘솔에 경고) |
| 관통력 | `pierce` | 투사체가 항상 1명만 뚫는다 |
| 속도 | `speed` | 투사체가 발사되지 않는다 (콘솔에 경고) |

> 모듈 필드 기본값을 전부 0으로 바꿨다. 이제 **시트가 비면 진짜로 안 나간다.**
> 예전에는 `speed 12` 같은 기본값이 가려주고 있었다.

### 기존 증강 에셋 점검

- **`noTargetPolicy` 재설정** — `AugmentData` 루트에서 `Trigger` 안으로 옮겼다.
  `Consume` 으로 바꿔둔 증강이 있으면 트리거를 열어 다시 체크할 것. (`Hold` 는 기본값이라 무손실)
- **`depth` 값 확인** — 의미가 "몇 단계"에서 "몇 번 더 번지나"로 바뀌었다.
  숫자는 그대로인데 **한 번씩 더 번진다.** 밸런싱 다시 볼 것.
- **모듈 필드에 남은 옛 기본값** — 이미 만든 에셋에는 `speed 12` · `pierce 1` 등이 저장돼 있다.
  시트를 따르게 하려면 손으로 0으로 바꿔야 한다.
- **`Random` · `RandomPoint` 의 타겟 수** — 이제 시트를 안 본다. 원하는 수를 모듈에 직접 적을 것.
  (`0` 으로 두면 1체다)

---

## 2. 씬 세팅

| 할 일 | 어디에 |
|---|---|
| `PlayerStats` 컴포넌트 붙이기 | Player 오브젝트 |
| `DamageTextPalette` 에셋 만들기 | `Create → CoD → Damage Text Palette` |
| 만든 팔레트 물리기 | `DamageTextManager` 인스펙터 |
| `MarkAnchor` 빈 오브젝트 추가 | 적 프리팹들 (머리 위, `Size` 를 씬 뷰에서 눈으로 맞춤) |

`DamageTextPalette` 를 안 물려도 동작은 한다 — 전부 흰색으로 나올 뿐.

---

## 2-1. FanArea → Area 통합 (지금 해야 함)

`FanAreaDelivery` 는 `AreaDelivery` 에 흡수됐다. **순서를 지킬 것.**

1. `Scripts/Augment/Module/Delivery/FanAreaDelivery.cs` **삭제**
   (코드 참조는 이미 다 걷어냈으니 지워도 컴파일된다)

2. `007 BruteForce.asset` 의 Delivery 를 `Area` 로 다시 조립

   ```
   Half Angle            45
   Body Prefab           melee_attack.prefab
   Attach Body To Owner  ✔
   Blast Radius          비워둘 것 (시트 효과범위)
   ```

   타겟팅은 이미 `OwnerPoint` 라 그대로 두면 된다.

3. `melee_attack.prefab` 을 `SwingArc` 구조로 교체 (아래 참고)

### melee_attack 프리팹 재구성

지금은 **45도 부채꼴이 스프라이트에 그려져 있다.** `halfAngle` 을 바꾸면 판정만 넓어지고
그림은 45도로 남아 조용히 어긋난다. 코드는 스프라이트가 몇 도인지 알 수 없어 경고도 못 한다.

```
melee_attack              ← SwingArc
 ├ Fan                    ← SweepFan (+ 머티리얼)   지나간 자리 잔상
 └ Blade                  ← 칼날 스프라이트.  피벗 Left, 길이 1유닛
```

피벗이 `Left` 면 이 오브젝트를 그대로 돌려도 원점을 축으로 돈다. 별도 Pivot 오브젝트가 필요 없다.

**칼날 스프라이트는 부채꼴이 아니라 칼날 한 장이어야 한다.** 각도 정보가 그림에 없어야
`halfAngle` 이 유일한 진실이 된다. 지금 스프라이트를 쓰고 싶으면 부채꼴을 잘라내고 날만 남기면 된다.

`SwingArc` 의 `Blade` · `Fan` 슬롯에 각각 연결. `Fan` 은 비워도 칼날만으로 동작한다.

> `[SerializeReference]` 는 클래스 이름을 문자열로 저장한다. 클래스를 지우면 그 슬롯이
> 통째로 비므로 위 값을 손으로 다시 넣어야 한다.

---

## 2-2. Tree 증강 조립 (간선 버전)

`004 Tree.asset` 은 지금 투사체 버전이 조립돼 있다. 통째로 다시 짤 것.

```
Trigger     SearchTrigger        쿨타임이 찬 뒤 다음 탐색에 얹혀 발동
Targeting   SearchPool           표식 붙은 적을 노드로
   rangeOverride   사거리
   targetLimit     0  (시트 수량)
   nearestFirst    ✔
Delivery    Instant
Effect      Link
   transfer        비워둠 (시트 효과피해)
   isPercent       ✔   0.4 면 원래 피해의 40%가 이웃에게
   hopsOverride    0   (시트 깊이)
   duration        비워둠 (시트 지속시간)
   connectToAll    ✖   ← 끄면 트리, 켜면 그물(그래프)
   linePrefab      선 프리팹 (LineRenderer)
```

**선 프리팹** — 빈 오브젝트에 `LineRenderer` 하나. `Positions` 는 코드가 매 프레임 채우니
비워둬도 된다. `Width` 와 `Material`(Sprites/Default) 만 잡아줄 것.

### 시트 수치를 다시 채울 것

| 컬럼 | 간선 버전에서의 뜻 | 지금 |
|---|---|---|
| `cooldown` | 발동 주기 | 1.6 — 그대로 쓸 만함 |
| `range` | 노드로 삼을 표식 적 검색 반경 | 6 — 그대로 |
| `count` | 연결할 노드 수 | 3 — 그대로 |
| `effectDamage` | **전이 피해** (비율이면 0.4 등) | **0 → 채울 것** |
| `depth` | **몇 홉까지 번지나** | **0 → 1~3** |
| `duration` | **간선 유지 시간(초)** | **0 → 채울 것** |
| `damage` · `speed` · `pierce` | 안 씀 | 비워도 됨 |

> `depth` 는 2~3 을 넘기지 말 것. 간선은 안 풀리고 쌓이는 구조라 홉이 늘면 화면 전체가 한 번에 녹는다.

**혼자서는 발동하지 않는다.** 테스트할 때 BFS 나 DFS 를 같이 지급할 것.

---

## 3. 파일 정리

- `_Project/Editor/AugmentEditor/FxGroupDrawer.cs` **삭제**
  `FoldDrawer.cs` 로 합쳐서 주석만 남은 빈 파일이다. 유니티에서 지워야 `.meta` 도 같이 정리된다.
- `Scripts/Effect.cs` **삭제** — 단, 먼저 `explosion.prefab` 의 `Effect` 컴포넌트를
  `FxAutoDespawn` 으로 교체할 것. 순서를 지키지 않으면 프리팹 참조가 깨진다.
- `.meta` 가 없는 새 파일 넷 — 유니티를 한 번 켜면 생성되니 **커밋할 때 같이 넣을 것.**
  `ISizedVisual.cs` · `ScalableDrawer.cs` · `ChainAudit.cs` · `ModuleWarning.cs`

- `ModuleWarning.Reset()` 을 런 재시작 지점에 물릴 것.
  안 물리면 에디터를 껐다 켜기 전까지 같은 경고가 다시 안 뜬다. (`GameManager` 의 재시작 자리)

---

## 4. 확인만 해보고 알려줄 것

**증강 잠금 — 폴드아웃이 눌리는지**

인스펙터 맨 위 `작동 방식 잠금` 을 켠 뒤, 잠긴 모듈 칸의 ▶ 를 눌러 펼쳐지는지 본다.
안 눌려도 되도록 잠글 때 자동으로 펼쳐두게 해뒀지만, 실제 동작을 한 번 봐야 한다.

---

## 5. 나중에 — 지금은 안 해도 됨

- `Line` · `FanArea` 본체가 매번 `Instantiate` 된다. 연사형 증강이 늘면 풀링 필요.
- `FanAreaDelivery` 와 `Area.halfAngle` 의 역할이 겹친다. 하나로 합칠지 판단 보류 중.
- 지속 빔(레이저를 계속 쏘는 형태) 모듈. `Line` 은 한 프레임짜리라 대체 불가.
- `Chain` 하위에 `Chain` 을 넣는 분기 연쇄. 지금은 에디터 경고만 뜨고 런타임 차단은 없다.
