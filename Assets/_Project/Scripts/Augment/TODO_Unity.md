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
   neighborsPerNode 1   ← 1=트리, 2~3=그물, 0=전부
   linkRange       비워둠 (시트 효과범위)  ← 반드시 채울 것
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

## 2-4. Link 필드 교체 — 에셋 두 개 다시 설정

`connectToAll`(불리언)이 `neighborsPerNode`(숫자)로 바뀌었다. 그 칸이 초기화되므로 다시 넣을 것.

```
004 Graph
   Neighbors Per Node   2~3      ← 무리 모양을 따라 둥글게
   Link Range           비워둠   (시트 효과범위)

005 Tree
   Neighbors Per Node   1        ← 가장 가까운 하나에만
   Link Range           비워둠
```

**`Link Range` 가 핵심이다.** 시트 `효과범위` 가 0이면 거리 제한이 없어져서
화면 반대편끼리 이어진다. Graph·Tree 의 시트 `효과범위` 를 꼭 채울 것 (5~7 정도).

---

## 2-3. 풀링 — 연출 프리팹 규율

연출이 전부 풀을 거치게 바뀌었다. **프리팹이 재사용 가능해야 한다.**

| 어디로 | 무엇 |
|---|---|
| `bulletParent` | 투사체 · 빔 |
| `effectParent` | VFX · 간선 선 · Area 본체 |
| 풀 안 씀 | 표식 · 상태이상 시각 (대상에 매달림) · 소환물 (오래 삶) |

### 프리팹이 신경 쓸 것은 하나뿐

**"어떻게 사라지는가"** 만 정하면 된다. 되감아 트는 것은 코드가 알아서 한다.

| 연출 종류 | 사라지는 방법 |
|---|---|
| 파티클 | `Particle System` 메인 블록 → **`Stop Action` 을 `Disable`** 로 |
| 스프라이트 | `FxAutoDespawn` 붙이기 |
| 직접 만든 것 | `Destroy(gameObject)` 대신 `PooledSpawner.Despawn(gameObject)` |

**`Stop Action = Disable` 이 곧 풀 반납이다.** 별도 컴포넌트가 필요 없다.

`Stop Action` 위치 — 프리팹 선택 → Inspector → `Particle System` 컴포넌트 **맨 위 블록**
(컴포넌트 이름 바로 아래 큰 박스) → 아래로 스크롤, `Ring Buffer Mode` 다음.

> ⚠ **`Looping` 이 켜져 있으면 `Stop Action` 이 영원히 안 불린다.** 일회성 연출은 끌 것.
> ⚠ **`Destroy` 로 두면** 풀에 죽은 참조가 남는다. 코드가 걸러내긴 하지만 매번 새로 만들게 되어 풀링 이득이 사라진다.

파티클 되감기(`Clear` + `Play`)는 `PooledSpawner` 가 `PooledParticles` 를 자동으로 붙여 처리한다.
**프리팹에 아무것도 안 붙여도 된다.**

### 빔 프리팹

`BeamVisual` 이 없으면 수명을 관리할 주체가 없어 콘솔에 경고가 뜬다.
`BeamVisual` 이나 `FxAutoDespawn` 중 하나는 붙일 것.

---

## 2-5. Scalable 전환 — 에셋 재입력

정수 칸들이 `Scalable`(고정값 × 배수)로 바뀌었다. 저장값이 초기화되므로 다시 넣을 것.

| 모듈 | 칸 | 기본으로 두면 |
|---|---|---|
| `Radial` | Projectile Count | `0 × 1` = 시트 수량 그대로 |
| `Projectile` | Multi Shot → Shots Per Target | 〃 |
| 투사체 공통 | Pierce | `0 × 1` = 시트 관통력 그대로 |
| `Line` | Max Hits | 〃 |
| `SearchPool` | Target Limit | `0 × 1` = 시트 수량 |
| `TreeFrontier` | Max Depth | `0 × 1` = 시트 깊이 |

**대부분 `0 × 1` 로 두면 예전과 똑같이 동작한다.** 배수를 주는 건 전달을 여러 개 쓰면서
서로 다른 값을 원할 때만.

> 예) 전달 A `Pierce 0 × 1`, 전달 B `Pierce 0 × 3` → B만 세 배 관통하면서 둘 다 레벨업을 따라간다.

---

## 3. 파일 정리

- `_Project/Editor/AugmentEditor/FxGroupDrawer.cs` **삭제**
  `FoldDrawer.cs` 로 합쳐서 주석만 남은 빈 파일이다. 유니티에서 지워야 `.meta` 도 같이 정리된다.
- `Scripts/Effect.cs` **삭제** — 단, 먼저 `explosion.prefab` 의 `Effect` 컴포넌트를
  `FxAutoDespawn` 으로 교체할 것. 순서를 지키지 않으면 프리팹 참조가 깨진다.
- `Scripts/Augment/ProjectileSpawner.cs` **삭제** — `PooledSpawner` 로 대체됐고 아무도 안 쓴다.

- `.meta` 가 없는 새 파일 넷 — 유니티를 한 번 켜면 생성되니 **커밋할 때 같이 넣을 것.**
  `ISizedVisual.cs` · `ScalableDrawer.cs` · `ChainAudit.cs` · `ModuleWarning.cs`

- `Summon.Clear()` 를 런 재시작 지점에 물릴 것.
  증강별 생존 소환물 목록이 static 이라, 안 비우면 지난 런의 죽은 참조가 상한을 잡아먹는다.

- `ModuleWarning.Reset()` 을 런 재시작 지점에 물릴 것.
  안 물리면 에디터를 껐다 켜기 전까지 같은 경고가 다시 안 뜬다. (`GameManager` 의 재시작 자리)

---

## 4. 확인만 해보고 알려줄 것

**증강 잠금 — 폴드아웃이 눌리는지**

인스펙터 맨 위 `작동 방식 잠금` 을 켠 뒤, 잠긴 모듈 칸의 ▶ 를 눌러 펼쳐지는지 본다.
안 눌려도 되도록 잠글 때 자동으로 펼쳐두게 해뒀지만, 실제 동작을 한 번 봐야 한다.

---

## 5. 나중에 — 지금은 안 해도 됨

- 대기(지연) 이펙트 — 예고 장판 · 시간차 폭발. 필요한 증강이 시트에 나올 때 만든다.
- 지속 빔(레이저를 계속 쏘는 형태) 모듈. `Line` 은 한 프레임짜리라 대체 불가.
- `Chain` 하위에 `Chain` 을 넣는 분기 연쇄. 지금은 에디터 경고만 뜨고 런타임 차단은 없다.

---

## 6. 런 흐름 — 새로 붙은 것 (오늘 작업)

### 씬에 얹을 것

| 할 일 | 어디에 |
|---|---|
| `RunDirector` 컴포넌트 붙이기 | `GameManager` 와 같은 오브젝트 |
| `RunResultPanel` 붙일 빈 오브젝트 만들기 | `StageResult` 씬 |
| `EventSystem` 있는지 확인 | `StageResult` 씬 (없으면 버튼이 안 눌린다) |

`RunResultPanel` 은 아트 없이 코드로 그린다. 글꼴 칸을 비우면 TMP 기본 글꼴을 쓴다.

### RunDirector 인스펙터

```
보스 일정
   Display Name     Segfault
   At Seconds       300        ← 이 시각(초)에 등장
   Prefab           보스 프리팹
   Spawn Distance   8          ← 플레이어로부터 이만큼 떨어진 곳
   Ends Run         ✔          ← 켠 항목을 잡으면 런 클리어

정산
   Kill Value       1          ← 처치 1체당 재화
   Time Value       0.5        ← 버틴 1초당 재화
   Clear Bonus      100

마무리
   End Delay        1.2        ← 결과 씬으로 넘어가기 전 뜸
   Result Scene     StageResult
```

**중간보스는 `Ends Run` 을 안 켠 항목이다.** 여러 개 넣으면 그대로 중간보스 구조가 된다.
시각이 뒤죽박죽이어도 시작할 때 정렬하므로 순서는 신경 안 써도 된다.

> `Ends Run` 이 켜진 보스가 하나도 없으면 **이길 방법이 없는 런**이 된다. 콘솔에 경고가 뜬다.

### 보스 프리팹

일반 적 프리팹을 복제해서 만들면 된다. 필요한 것:

- `Enemy` — 체력·속도를 프리팹에서 직접 잡는다 (`Init` 을 안 거치므로 SpawnData 를 안 본다)
- `BossMarker` — 없으면 `RunDirector` 가 자동으로 붙이지만, 프리팹에 넣어두는 편이 명확하다

> ⚠ **보스는 풀에 넣지 말 것.** 풀 반납도 `OnDisable` 이라 처치와 구별되지 않는다.
> `RunDirector` 가 `Instantiate` 로 직접 만든다.

### 시간 표시가 뒤집혔다

`gameTime` 상한을 걷어내서 **남은 시간 → 버틴 시간**이 됐다. HUD 타이머가 0부터 올라간다.
`GameManager.maxGameTime` 은 이제 코드에서 안 쓴다 — 지울지는 팀과 정할 것.

### 저장

`PlayerProgress` 가 PlayerPrefs 한 칸(`CoD.Progress`)에 JSON 으로 넣는다.
런이 끝나면 보상이 자동으로 쌓이고 저장된다.

- 상점을 만들려면 `Create → CoD → Hardware Table` 로 표를 만들고 부품별 값을 채울 것
- **능력치 주입은 아직 배선하지 않았다.** 저장·구매 틀만 있는 상태다
- 테스트 중 초기화하려면 `PlayerProgress.Wipe()`

### 증강별 피해 표

결과 패널에 증강마다 총 피해·비중·타격수·평균이 나온다. `DamagePipeline` 을 지나는
모든 피해가 자동 집계되므로 증강을 새로 만들어도 따로 할 일이 없다.

> 막타 기여(누가 죽였나)는 아직 없다. `Enemy.Dead()` 가 가해자를 모르기 때문 —
> 필요해지면 그때 뚫는다.

---

## 7. UI 테마 (오늘 작업)

색·글꼴·여백이 화면마다 따로 적혀 있던 것을 `UiTheme` 에셋 하나로 모았다.
값 하나를 바꾸면 증강 선택·HUD·결과 패널이 **같이 움직인다.**

### 반드시 할 것 — 에셋 만들기

1. `Assets/Resources` 폴더가 없으면 만든다 (**이름 정확히**, 코드가 여기서 찾는다)
2. `Create → CoD → UI Theme`
3. 파일 이름을 **`UiTheme`** 로 (다른 이름이면 못 찾는다)
4. `Resources` 폴더 안에 둔다

> 안 만들어도 게임은 돈다 — 기본값으로 굴러가고 콘솔에 경고만 뜬다.
> 다만 인스펙터로 색을 조절할 수 없으니 결국 만들어야 한다.

### 고정폭 글꼴 넣기

터미널 느낌의 8할이 글꼴에서 온다. 무료 한글 고정폭이면 **D2Coding** 이 무난하다.

1. `.ttf` 를 `Assets` 아래 아무 데나 넣는다
2. `Window → TextMeshPro → Font Asset Creator`
3. Source Font File 에 그 `.ttf`
4. `Character Set` → **Custom Characters** 로 두고 쓸 글자를 넣거나,
   한글은 글자 수가 많으니 `Atlas Population Mode` 를 **Dynamic** 으로 두는 게 편하다
5. `Generate Font Atlas` → `Save`
6. 만들어진 Font Asset 을 `UiTheme` 의 **Mono** 칸에 물린다

각 화면의 `Font` 칸은 **비워두면** 테마 글꼴을 쓴다.
이미 물려둔 게 있으면 그게 이긴다 — 한 화면만 다른 글꼴을 쓰고 싶을 때를 위해 남겼다.

### 팔레트

기본값은 어두운 터미널 톤이다. 인스펙터에서 바로 조절할 수 있다.

| 칸 | 쓰이는 곳 |
|---|---|
| `Background` | 화면 바탕 · 오버레이 어둠 |
| `Surface` | 패널·카드 바닥 |
| `Surface Dim` | 한 단 들어간 칸(아이콘 자리·리롤 버튼) |
| `Line` | 테두리·구분선 |
| `Text` / `Dim` | 본문 / 설명 |
| `Accent` | 고른 것·비중 막대 |
| `Good` / `Warn` | 클리어·회복 / 실패·피해 |
| `Categories` | 증강 분류별 색 |

`Unit`(기본 8)은 여백의 기본 단위다. 간격을 전부 이 배수로 쓰면 리듬이 맞는다.

### 아직 밝은 채로 남은 두 칸

`GameHud` 인스펙터의 **`Exp Fill Color` · `Exp Back Color`** 는 씬에 저장된 값이라
코드에서 못 바꾼다. 지금 밝은 회색이라 어두운 팔레트와 부딪친다.

> 씬에서 `Exp Back Color` 를 테마 `Surface` 쯤으로, `Exp Fill Color` 를 `Accent` 쯤으로
> 직접 낮출 것.

---

## 8. 구조 정리 (오늘 작업)

### 폴더가 바뀌었다

`Scripts/` 최상위에 널려 있던 18개 파일을 담당별로 나눴다. **`.meta` 를 같이 옮겼으므로
씬·프리팹 참조는 그대로다** — 유니티를 켜면 조용히 재임포트만 한다.

```
Core/      GameManager · LevelSystem · ExpManager · PoolManager
Player/    Player · PlayerHealth · Scanner · ExpMove
Stage/     StageSetup · Spawner · Reposition · GridRender
Enemy/     Enemy
UI/        + LogManager · MenuManager · UIPanel
Combat/    + DamageText · DamageTextManager
Legacy/    Bullet · Weapon · Item · ItemData   ← 증강이 대체하면 통째로 삭제
```

`Legacy/` 는 "곧 사라질 것"을 폴더 이름으로 알리려는 것이다. 증강만으로 한 판이 돌면
이 폴더째로 지우면 된다.

### GameManager 가 얇아졌다

시간·처치 수·게임오버가 **`RunDirector` 한 곳으로** 모였다. 예전에는 "런이 끝났는가"를
`GameManager.isGameOver` 와 `RunDirector.State` 두 군데서 물어봐야 했다.

| 예전 | 지금 |
|---|---|
| `GameManager.instance.gameTime` | `RunDirector.RunTime` |
| `GameManager.instance.kills` | `RunDirector.KillCount` |
| `!GameManager.instance.isGameOver` | `RunDirector.IsPlaying` |
| `GameManager.instance.GameOver()` | 없음 — `PlayerHealth.Died` 를 디렉터가 듣는다 |

`GameManager` 에 남은 것은 **공용 참조(player·poolManager·expManager)와 LevelSystem** 뿐이다.
`LevelSystem` 과 `RunDirector` 는 없으면 스스로 붙으므로 씬 세팅을 빠뜨려도 돌아간다.

> `GameManager` 인스펙터에서 `Game Time` · `Max Game Time` · `Background Prefab` 칸이 사라진다.
> **배경 프리팹은 아래 `StageSetup` 에 다시 물려야 한다.**

### 씬에 붙일 것 — `StageSetup`

배경 깔기와 부팅 로그가 `GameManager` 에서 빠져나왔다.

1. `GameManager` 오브젝트에 **`StageSetup`** 컴포넌트 추가
2. `Background Prefab` 에 예전 배경 프리팹을 물린다
3. `Boot Lines` 는 기본값이 예전 로그 그대로다. 스테이지마다 바꾸면 된다

**안 붙이면 배경이 안 나온다.**

### 증강 풀이 에셋이 됐다

`AugmentSelectUI` 안의 `Augment Pool` 리스트가 **`AugmentPool` 에셋**으로 빠졌다.
상자(.zip) 드랍이나 캐릭터 스타트 증강도 같은 목록을 봐야 하기 때문이다.

1. `Create → CoD → Augment Pool`
2. 예전 `AugmentSelectUI` 의 `Augment Pool` 목록에 있던 증강들을 여기에 다시 넣는다
3. 만든 에셋을 `AugmentSelectUI` 의 **`Pool`** 칸에 물린다

> 예전 리스트는 필드째로 사라졌으므로 **내용이 날아간다.** 유니티를 켜기 전에
> 지금 목록을 스크린샷 찍어두거나, 켠 뒤 `Data/Augments` 폴더에서 다시 골라 담을 것.
> 비어 있으면 콘솔에 에러가 뜬다.

### 증강 카드가 독립 컴포넌트가 됐다

`AugmentCardView` — 카드 한 장의 조립·내용 채우기·리롤 버튼을 전부 맡는다.
`AugmentSelectUI` 는 663줄 → 300줄 아래로 내려갔고, 하는 일이 셋만 남았다:
화면 열고 닫기 · 카드 배치 · 클릭 받기.

**할 일 없음.** 코드가 알아서 만든다. 나중에 카드 아트가 나오면 이 파일만 프리팹 방식으로
바꾸면 되고 `AugmentSelectUI` 는 안 건드려도 된다.

### 뽑기 규칙도 분리됐다

`AugmentDraft` — 만렙 제외 · 내부증강 해금 · 회복 아이템 확률이 여기 모였다.
MonoBehaviour 가 아니라서 화면 없이도 결과를 확인할 수 있다.

상자 드랍을 만들 때는 이걸 쓰면 된다:

```csharp
var draft = new AugmentDraft(pool, augmentManager);
AugmentData one = draft.PickOne(null);
```

---

## 9. 매니저 정리 · 시간 담당 (오늘 작업)

### 새로 생긴 두 담당자

| | 하는 일 |
|---|---|
| `TimeControl` | **`Time.timeScale` 을 대입하는 유일한 곳** |
| `UIManager` | 어느 화면이 떠 있는지(스택). 멈출지는 TimeControl 에 맡긴다 |

`TimeControl` 은 "멈춰라"가 아니라 **"내 이유로 붙잡는다"** 로 동작한다.

```csharp
TimeControl.Hold(this);      // 내가 붙잡는다
TimeControl.Release(this);   // 내 볼일 끝 (남이 붙잡고 있으면 여전히 멈춤)
TimeControl.ReleaseAll();    // 씬이 바뀔 때
```

**고쳐진 버그** — 예전에는 5개 파일이 각자 timeScale 을 만졌다. 레벨업 카드가 떠 있을 때
일시정지를 눌렀다 풀면 **카드가 떠 있는 채로 게임이 뒤에서 돌아갔다.**
이제 카드가 아직 붙잡고 있어서 구조적으로 불가능하다.

### 사라진 매니저 둘

**`ExpManager` → `LogManager` 로 흡수**

하는 일이 "경험치 로그를 묶어서 내기" 하나뿐이었고, 그건 로그 창의 일이다.
`LogManager.ExpGained(int)` 로 들어갔다.

**`DamageTextManager` → `DamageTextSpawner` (static)**

팔레트 보관 + 풀에서 꺼내기뿐이라 씬 오브젝트가 필요 없었다.
팔레트는 `UiTheme` 과 같은 방식으로 `Resources` 에서 찾는다.

### ⚠ 유니티에서 할 일

**1. 씬 오브젝트 두 개 삭제**

`stage 1` · `stage 2` 씬에서 아래 오브젝트를 지운다. 그대로 두면 스크립트를 지운 뒤
`Missing (Mono Script)` 로 남는다.

```
[ExpManager]          삭제
[DamageTextManager]   삭제  ← 먼저 팔레트 에셋을 아래 3번으로 옮길 것
```

**2. 스크립트 파일 두 개 삭제** (Project 창에서 — 탐색기로 지우면 `.meta` 가 남는다)

```
Assets/_Project/Scripts/Core/ExpManager.cs
Assets/_Project/Scripts/Combat/DamageTextManager.cs
```

**3. 팔레트를 Resources 로**

`DamageTextManager` 인스펙터에 물려 있던 `DamageTextPalette` 에셋을
`Assets/Resources/` 로 옮기고 이름을 **`DamageTextPalette`** 로 맞춘다.
없으면 숫자가 전부 흰색으로 나오고 콘솔에 경고가 뜬다.

**4. `GameManager` 에서 없어진 칸**

- `Exp Manager` 칸이 사라졌다 (흡수됨)
- **`Level` 칸이 새로 생겼다** — `LevelSystem` 컴포넌트를 물려야 한다.
  비워두면 씬에서 찾아보고, 그래도 없으면 콘솔에 에러가 뜬다.

> `Ensure<T>`(없으면 자동으로 붙이기)를 걷어냈다. 편하라고 넣었는데
> 오히려 GameManager 오브젝트에 컴포넌트가 다 몰리는 이유가 됐다.
> 이제 **씬에서 명시적으로 붙여야 한다.**

**5. Hierarchy 를 역할별로 나누기**

빈 오브젝트를 만들고 컴포넌트를 옮긴다. 컴포넌트 우클릭 → `Copy Component` →
새 오브젝트에서 `Paste Component As New` → 원본 삭제.

```
[Game]     GameManager · LevelSystem
[Run]      RunDirector
[Stage]    StageSetup · Spawner · PoolManager
[Augment]  AugmentManager
[UI]       UIManager · GameHud · AugmentSelectUI · MenuManager · LogManager
```

**`UIManager` 는 새 컴포넌트다. 반드시 붙일 것** — 없으면 레벨업 카드가 떠도
게임이 안 멈춘다.

> 옮긴 뒤 `GameManager` 의 `Player` · `Pool Manager` · `Level` 칸이 비지 않았는지 확인할 것.
> 오브젝트를 지웠다 만들면 참조가 끊긴다.

### 화면을 새로 만들 때

```csharp
UIManager.Current.Open(UIManager.Screen.Pause);    // 스택에 쌓고, 멈춰야 하면 멈춤
UIManager.Current.Close(UIManager.Screen.Pause);   // 스택에서 빼고, 남은 게 없으면 품
```

멈춰야 하는 화면은 `UIManager` 의 `Freezing` 목록에 이름을 추가하면 된다.
**`Time.timeScale` 을 직접 만지지 말 것.**

---

## 10. 스테이지를 에셋으로 (오늘 작업)

씬을 복제해서 인스펙터 세 곳을 손으로 고치던 것을 **에셋 하나**로 옮겼다.
**씬은 하나면 된다** — 스테이지를 바꾸는 것은 `StageData` 를 갈아끼우는 일이다.

### 새 에셋 두 종류

```
EnemyData  (Create → CoD → Enemy Data)     "이 적은 무엇인가"
   프리팹 · 이름 · 등급 · 기본 체력/속도/접촉피해 · 경험치 · 비트

StageData  (Create → CoD → Stage Data)     "이 스테이지는 어떻게 흐르는가"
   배경 · 부팅 로그
   웨이브[]   시작 시각 · 지속 · 어떤 적 · 간격 · 한 번에 몇 · 상한 · 배율 4종
   보스[]     시각 · 프리팹 · endsRun
   증강 풀 · 시작 리롤
   정산       killValue · timeValue · clearBonus
```

**적 스탯은 EnemyData 에만 있다.** 스테이지 웨이브는 배율만 얹는다 —
그래야 적 밸런스를 고칠 때 스테이지를 전부 뒤지지 않아도 된다.

### ⚠ 할 일

**1. `EnemyData` 만들기**

지금 쓰는 적 프리팹마다 하나씩. 예전 `Spawner.spawnData` 값이 그대로 들어간다.

```
Display Name     Bug
Prefab           적 프리팹
Rank             Minion
Health           10
Speed            2
Contact Damage   10
Exp              1        ← 예전에는 Exp 프리팹에 1로 박혀 있었다
Bits             0
```

> `Enemy` 의 `Anim Con` 배열이 사라졌다. 프리팹 하나를 여러 적이 돌려쓰던 흔적이다.
> 이제 적마다 프리팹이 따로 있으니 **각 프리팹의 Animator 에 컨트롤러를 직접 물릴 것.**
> 프리팹을 공유하고 싶으면 `EnemyData` 의 `Animator Override` 를 채우면 된다.

**2. `StageData` 만들기**

```
Display Name       Stage 01
Stage Id           1
Background Prefab  Background1        ← StageSetup 에 있던 것을 옮김
Boot Lines         (기본값 있음)

Waves
  [0] Label        잡몹
      Start At     0
      Duration     0       (끝까지)
      Enemy        방금 만든 EnemyData
      Interval     0.4     ← 예전 spawnTime
      Burst        1
      Max Spawns   0       (제한 없음)
      Health Scale 1
      Speed Scale  1
      Damage Scale 1
      Exp Scale    1

Bosses             (비워두면 죽어야만 끝난다 — 콘솔에 경고)
Augment Pool       기존 AugmentPool 에셋
Starting Rerolls   2
Kill Value         1
Time Value         0.5
Clear Bonus        100
```

**난이도 상승은 이제 웨이브 줄을 늘려서 만든다.** 예전 "10초마다 한 칸"은 사라졌다.

```
[0] 잡몹 1단계   Start 0     Health x1
[1] 잡몹 2단계   Start 60    Health x1.6   Interval 0.3
[2] 엘리트       Start 120   Health x4     Interval 6   Burst 1
```

**3. `StageSetup` 인스펙터**

`Background Prefab` · `Boot Lines` 칸이 사라지고 **`Default Stage`** 하나만 남는다.
방금 만든 `StageData` 를 여기에 물린다.

> 스테이지 선택 화면에서 넘어온 것이 있으면 그쪽이 이긴다.
> 이 칸은 씬을 바로 재생했을 때를 위한 예비다.

**4. `RunDirector` 인스펙터**

`Bosses` · `Starting Rerolls` · `Kill Value` · `Time Value` · `Clear Bonus` 칸이 전부 사라졌다.
남은 것은 `End Delay` 와 `Result Scene` 둘뿐이다. **StageData 로 옮겨 적을 것.**

**5. `Spawner` 인스펙터**

`Spawn Data` 배열과 `Prefab Id` 가 사라졌다. `Spawn Points` 는 비워두면
**직계 자식만** 자동으로 모은다 (예전에는 재귀라서 Point 밑에 뭘 넣으면 그것도 스폰 지점이 됐다).

**6. `AugmentSelectUI` 인스펙터**

`Pool` 이 `Fallback Pool` 로 바뀌었다. 보통은 `StageData` 가 정하므로 **비워둬도 된다.**

### 스테이지를 새로 만들 때

`StageData` 를 복제(Ctrl+D)하고 값만 바꾼다. **씬은 안 건드린다.**

스테이지 선택 화면에서는 이렇게 넘긴다:

```csharp
StageContext.Choose(선택한StageData);
SceneManager.LoadScene("stage");
```

### 10-1. Enemy 정리 · 보스 통합 (이어서)

**`Enemy.cs` 의 역할이 분명해졌다**

`EnemyData` 가 설계도, `Enemy` 가 실물이다 — 증강의 `AugmentData` / `AugmentInstance` 와 같다.
`Enemy` 에 남은 public 필드(`speed` · `health` · `contactDamage`)는 **지금 이 개체의 상태**다.

정리한 것:

- `expPrefab` 삭제 — 아무도 안 쓰던 죽은 필드. **프리팹 인스펙터에서 칸이 사라진다**
- `OnTriggerEnter2D` 옛 Bullet 경로 삭제 — `DamagePipeline` 을 우회하던 유일한 구멍이 막혔다
- `Init` 이 EnemyData 를 못 받으면 콘솔에 경고 (조용히 이전 개체 수치로 돌아다니던 것)

> ⚠ `Weapon` · `Bullet` 프리팹을 아직 쓰고 있다면 **그 피해가 이제 안 들어간다.**
> 증강만으로 도는 중이면 문제 없다.

**적 크기 · 좌우 반전이 데이터로**

```
EnemyData
   Scale          1     ← 몸집. 2면 두 배. 엘리트는 이것만 올려도 위협이 읽힌다
   Flip To Face   ✔     ← 끄면 플레이어 쪽을 봐도 안 뒤집힌다
```

**글자 모양 적은 `Flip To Face` 를 끌 것.** 뒤집히면 글자를 못 읽는다.

웨이브에도 `Size Scale` 이 생겨서 같은 적을 스테이지 후반에 더 크게 낼 수 있다.

**🔴 표식이 적 크기를 안 따라가던 버그 — 고쳤다**

`MarkAnchor.size` 를 월드 절대값으로 읽고 있어서, 적을 두 배로 키워도 테두리만
원래 크기로 남아 몸을 못 감쌌다. 이제 **부모 스케일을 곱한다.**

- `size` 의 뜻이 "월드 유닛" → **"프리팹에 그려진 크기 기준"** 으로 바뀌었다
- 적 스케일이 1이면 값이 같으므로 **기존 프리팹은 다시 안 잡아도 된다**
- 씬 뷰 기즈모도 실제 그려질 크기로 나온다

**보스도 `EnemyData` 를 쓴다**

`BossSpawn` 이 프리팹 대신 `EnemyData` 를 참조한다. 예전에는 보스만 프리팹을 열어야
체력이 보였는데, 이제 잡몹과 같은 자리에서 밸런싱한다.

```
Bosses
  [0] Enemy          보스용 EnemyData      ← 프리팹 칸이 사라졌다
      Name Override  (비우면 EnemyData 이름)
      At Seconds     300
      Spawn Distance 8
      Ends Run       ✔
      Health Scale   1     ← 이 스테이지에서만 더 세게
      Speed Scale    1
      Damage Scale   1
      Size Scale     1
```

**보스용 `EnemyData` 를 따로 만들 것.** `Rank` 를 `Boss` 로 두고, 프리팹에는
`Enemy` 와 `BossMarker` 가 붙어 있어야 한다.

> 보스 프리팹의 `Enemy` 컴포넌트에 손으로 적어둔 체력은 **이제 무시된다.**
> `EnemyData` 로 옮겨 적을 것.

---

## 11. 시트 → 유니티 연동 (오늘 작업)

구글 시트 `1_설계` 탭을 CSV 로 내려받아 증강 레벨 표를 통째로 채운다.

```
Unity 메뉴 → CoD → 증강 시트 가져오기
```

### 성장이 곱셈에서 덧셈으로 바뀌었다

```
실수 스탯   Lv(n) = 1레벨값 + 증가량 × (n-1)
정수 스탯   Lv(n) = 1레벨값 + 내림(증가량 × (n-1))    ← 0.5 면 2레벨마다 +1
```

**시트에 이미 채워둔 값이 대부분 그대로 맞는다.** 곱셈이던 시절의 계산은
`효과피해 0.03` 을 "0.03배씩"으로 읽어 2레벨에 0이 됐고, `깊이 계단 0.5` 를
"레벨당 +2"로 읽어 폭주했다.

> **`2_Augment_Levels` 탭은 이제 안 쓴다.** 임포터가 `1_설계` 를 직접 읽어
> 곡선을 계산하므로, 그 탭의 수식이 깨져 있어도 게임에는 영향이 없다.
> 눈으로 확인하는 용도로만 남는다.

### 시트에서 고칠 것

**1. 쿨타임 열** — `0.95`(곱셈 배수)로 적혀 있다. 선형이므로 **`-0.2`** 처럼
레벨당 줄어들 양으로 다시 적을 것.

**2. 열 이름** (권장) — `성장` · `계단` → **`레벨당 증가`** 로 바꾸면 헷갈릴 일이 없다.
임포터는 네 이름(`레벨당 증가` · `증가` · `성장` · `계단`)을 전부 받으므로
안 바꿔도 동작한다.

**3. 비어 있는 `효과범위`** — `TREE` · `GRAPH` · `BRUTE_FORCE` 가 0이다.
0이면 **`Area` 가 안 터지고 간선이 화면 반대편까지 이어진다.** 5~7 정도로 채울 것.

**4. 비어 있는 `속도`** — `BRUTE_FORCE` 가 0이다. 투사체를 쓴다면 발사가 안 된다.

가져오기가 끝나면 이 두 가지를 **경고로 알려준다.**

### id 를 시트에 맞춰 바꿨다

`001` · `002` 같은 번호가 세 곳에서 겹쳐 있었다. 겹치면 어느 에셋에 넣을지
코드가 정할 수 없으므로 시트 id 로 통일했다.

| 에셋 | 새 id |
|---|---|
| 001 Bash | `BASH` |
| 002 DFS | `DFS` |
| 003 BFS | `BFS` |
| 004 Graph | `GRAPH` |
| 005 Tree | `TREE` |
| 007 BruteForce | `BRUTE_FORCE` |

시트에 없는 것들도 겹치지 않게 이름을 줬다 — `TEST` · `ITEM_0XCAFE` · `ITEM_0XBEEF` ·
`WIP_BEAM_SEARCH` · `WIP_CHAIN_ATTACK` · `WIP_CIRCLE_ATTACK`.

> **id 는 시트와 에셋을 잇는 유일한 키다.** 한번 정했으면 바꾸지 말 것.
> 새 증강을 만들 때 id 를 비워두면 임포터가 그 에셋을 못 찾는다.

### 잠금이 지켜진다

`AugmentData` 의 **`Lock Stats`** 를 켠 증강은 임포터가 건너뛴다.
손으로 맞춰둔 값을 시트가 덮어쓰지 않게 하는 자물쇠다.

지금 **`BASH` 가 잠겨 있어서** 시트 값이 안 들어갔다. 시트로 관리하려면 잠금을 풀 것.

### 이미 채워진 값

임포터를 돌리기 전에도 `DFS` · `BFS` · `GRAPH` · `TREE` · `BRUTE_FORCE` 는
시트 내용대로 8레벨이 채워져 있다. 시트를 고친 뒤 가져오기를 돌리면 갱신된다.

---

## 12. 하드웨어 업그레이드 배선 (오늘 작업)

재화가 쌓이기만 하고 쓸 데가 없던 구멍을 메웠다. **메타 루프가 닫혔다.**

```
런 종료 → PlayerProgress.AddBits      (이미 있던 것)
            ↓
로비 UpgradeShop 에서 구매            ← 새로 만듦
            ↓
PlayerProgress 에 레벨 저장           (이미 있던 것)
            ↓
런 시작 HardwareLoader 가 주입        ← 새로 만듦
            ↓
PlayerStats · LevelSystem · Scanner · Player
```

### ⚠ 할 일 — 순서대로

**1. 하드웨어 표 만들기**

```
Unity 메뉴 → CoD → 하드웨어 표 만들기
```

`Assets/_Project/Data/HardwareTable.asset` 에 부품 9종이 채워진 표가 생긴다.
값은 **초안**이므로 에셋에서 바로 고치면 된다.

**2. 런 씬(`augmentTest`)에 `HardwareLoader` 붙이기**

`[Run]` 오브젝트에 컴포넌트를 추가하고 방금 만든 표를 물린다.

```
Table        HardwareTable
Log Result   밸런싱 중에만 ✔ — 무엇이 얼마나 걸렸는지 콘솔에 찍힌다
```

**안 붙이면 상점에서 아무리 사도 런이 똑같다.** 이 컴포넌트가 메타 진행이
게임에 닿는 유일한 지점이다.

> `Player` 오브젝트에 `PlayerStats` 가 붙어 있어야 증강 수치 보정이 들어간다.
> 없으면 콘솔에 경고가 뜨고 그 부분만 통째로 빠진다.

**3. `MainB` 의 업그레이드 패널에 `UpgradeShop` 붙이기**

`MenuManager` 의 `Upgarde Panel` 칸에 물려 있는 오브젝트에 컴포넌트를 추가한다.

```
Table   비워두면 씬의 HardwareLoader 에서 찾는다.
        MainB 에는 HardwareLoader 가 없으므로 여기서는 직접 물릴 것
```

패널 밑에 `ShopBody` 를 만들어 그 안에만 그리므로 패널에 이미 있는 것은 안 건드린다.
줄을 누르면 바로 구매되고 저장까지 끝난다.

> 부품이 늘어 화면을 넘치면 스크롤을 붙여야 한다. 지금은 9줄이라 그냥 놓았다.

**4. 테스트**

재화가 없으면 전부 회색이라 확인이 안 된다. 잠깐 넣어 보려면:

```csharp
PlayerProgress.AddBits(5000);
PlayerProgress.Save();
```

초기화는 `PlayerProgress.Wipe()`.

### 표 읽는 법

부품 한 줄이 **효과 여러 개**를 가진다. GPU 가 사거리와 효과범위를 같이 올리는 식이다.

```
Entry
  Kind          Gpu
  Display Name  GPU
  Max Level     10        ← 0이면 상점에 LOCKED 로 뜬다
  Base Cost     70
  Cost Growth   1.55      ← 레벨마다 이만큼 비싸진다
  Effects
    [0] Target  Stat  /  Stat Kind  Range        /  Mode Percent  /  Per Level 0.04
    [1] Target  Stat  /  Stat Kind  EffectRange  /  Mode Percent  /  Per Level 0.04
```

| Target | 어디로 가나 |
|---|---|
| `Stat` | `PlayerStats` → 보유 증강 전부 |
| `Exp` | `LevelSystem.ExpMultiplier` |
| `Vision` | `Scanner.RangeMultiplier` |
| `MoveSpeed` | `Player.HardwareSpeed` |
| `Critical` | 🔴 **판정기 없음** — 사도 아무 일도 안 일어난다 |
| `StartingAugments` | 🔴 **캐릭터 시스템 없음** — 마찬가지 |

`Critical`(마우스)과 `StartingAugments`(메인보드)는 `Max Level 0` 으로 잠가 뒀다.
받아 줄 시스템을 만들면 그때 레벨을 올리면 된다.

> 배율은 **합으로 쌓인다.** 부품 둘이 각각 +5% 면 +10% 지 +10.25% 가 아니다.
> 표에 적힌 숫자와 실제가 어긋나지 않게 하려는 것.

### 🔴 기획에서 정할 것 — CPU 와 SSD 가 겹친다

기획서는 CPU 를 **공격속도**, SSD 를 **쿨타임 감소**로 적어 뒀는데,
이 게임에는 그 둘이 나뉘어 있지 않다. 증강은 전부 쿨타임으로 발동하므로
**두 부품이 같은 수치를 올린다.**

지금은 둘 다 `Cooldown` 으로 채워 뒀지만, 부품 하나를 다른 역할로 바꾸는 편이 낫다.
후보 — 상자 드랍률 · 증강 선택지 수 · 지속시간 · 리롤 획득.

### 경험치 배율의 소수 이월

`ExpMultiplier` 를 곱하면 소수가 남는다. 매번 버리면 **RAM 을 사도 아무 일도 안 일어난다** —
경험치 1짜리 적을 1.05 로 받아도 내림하면 계속 1이기 때문이다.
`LevelSystem` 이 남은 소수를 다음 획득으로 넘긴다.

### 배율 칸을 따로 둔 이유

`Scanner.scanRange` · `Player.speed` 를 직접 곱하지 않고 배율 칸을 새로 만들었다.
인스펙터 값이 곧 원본이어야 두 번 주입하거나 씬을 다시 열었을 때 값이 누적되지 않는다.

특히 `Player` 는 한시적 이동속도 버프(`0xCAFE`)가 끝날 때 배율을 1로 되돌리는데,
칸을 같이 썼다면 **버프가 끝날 때마다 키보드 업그레이드도 사라졌을 것이다.**

---

## 13. 드랍 시스템 — 🔴 되돌림 (읽지 말 것)

> **이 장의 내용은 전부 취소됐다.** 컴파일이 깨져 코드를 걷어냈다.
> 경험치는 다시 `Enemy.Dead()` 가 오브를 직접 꺼내는 원래 방식이다.
> 아래는 나중에 다시 시도할 때를 위한 기록으로만 남긴다.

<details>
<summary>(취소된 내용 펼치기)</summary>


경험치가 적마다 무조건 오브 하나였던 것을 **확률로 등급을 뽑는 계단식**으로 바꿨고,
아이템을 필드에 떨굴 통로를 만들었다.

```
적 사망
  ├─ DropSpawner.Resolve()
  │    ├─ BitOrbTable 이 랭크별 확률로 등급을 뽑는다 → 오브 하나 (또는 빈손)
  │    ├─ EnemyData.drops[]      이 적만 떨구는 것
  │    └─ StageData.commonDrops[] 스테이지 전체에 얹히는 것
  └─ 비트(재화) 정산
```

### ⚠ 할 일 — 순서대로

**1. 비트 오브 표 만들기**

```
Unity 메뉴 → CoD → 비트 오브 표 만들기
```

`Assets/Resources/BitOrbTable.asset` 에 값과 확률이 채워진 표가 생긴다.
**Resources 안에 있어야 코드가 찾는다.** 표가 없으면 예전처럼(항상 1개, `EnemyData.exp` 값) 굴러간다.

**프리팹은 안 만들어도 된다 — 하나를 돌려쓴다**

`Exp01.prefab` 하나에 등급별 그림과 크기만 갈아끼운다. 표를 만들면 `Art/Sprite/exp_*`
여섯 장이 자동으로 물린다.

**애니메이션과 안 싸우는 이유** — 클립이 소유한 속성만 확인하면 된다.

```
Exp_idle.anim
  m_PPtrCurves:   []                ← 스프라이트에 키가 없다 → 코드가 바꿔도 된다
  m_ScaleCurves:  []                ← 크기에도 키가 없다   → 바꿔도 된다
  m_FloatCurves:  m_Color.r/g/b/a   ← 색만 애니메이션한다
```

**애니메이션은 자기가 키를 찍은 속성만 매 프레임 덮어쓴다.** 키가 없는 속성은 건드리지
않으므로 코드가 정한 값이 그대로 남는다.

> 🔴 **색은 클립이 이긴다.** 등급을 색으로 구분하고 싶으면 `SpriteRenderer.color` 가 아니라
> **스프라이트 자체를 다른 색으로** 준비할 것. `exp_*` 여섯 장이 이미 그렇게 돼 있다.

특별한 연출이 필요한 등급(예: `0xFF` 보스 오브)만 표의 **Prefab** 칸에 따로 물리면 된다.
비워두면 Exp 풀 0번에 그림·크기만 얹어 쓴다.

**2. `0xBEEF` 프리팹의 스크립트 교체**

지금 `Prefabs/Item_Prefabs/0xBEEF.prefab` 에 **`ExpMove` 가 붙어 있다.** 그대로 두면
주웠을 때 경험치가 들어간다.

```
ExpMove 컴포넌트 우클릭 → Remove Component
Add Component → FieldItem
   Item        0xBEEF.asset  (Data/Augment_Data)
   Base Speed  1.2
   Accel       5
   Fx G        효과음/이펙트
```

`0xCAFE` 는 프리팹이 없으므로 `0xBEEF` 를 복제해서 스프라이트(`cafe_1024_512`)와
`Item` 만 바꾸면 된다.

**3. 드랍 확률 채우기**

```
StageData → Common Drops
  [0] Prefab  0xBEEF.prefab   Chance 0.015   Count 1   Spread 0.6
  [1] Prefab  0xCAFE.prefab   Chance 0.01    Count 1   Spread 0.6
```

`0xBEEF` 는 회복이라 확률이 낮아도 체감이 크다. 0.015 면 **70마리에 한 번**쯤.

**4. 엘리트용 `EnemyData` 만들기 — 안 하면 등급표가 안 먹는다**

지금 `Stage 01` 의 엘리트 웨이브는 잡몹 `EnemyData` 를 배율만 올려 재사용한다.
그런데 등급 확률은 **`EnemyData.rank`** 로 갈리므로, 엘리트 웨이브의 적도 여전히
`Minion` 확률표를 탄다 — 큰 오브가 안 나온다.

`001 No_Image` 를 복제(Ctrl+D)해서:

```
Display Name  memory leak
Rank          Elite        ← 이것 때문에 만드는 것
Kill Messages 엘리트다운 문구로
```

만든 뒤 `Stage 01` 의 엘리트 웨이브 두 줄에서 `Enemy` 를 새 에셋으로 바꾼다.

### 등급표 읽는 법

```
Tiers  (값이 커지는 순서로 둘 것)
  Label 0x01   Value 1     Sprite exp_01   Scale 0.6   Minion 70   Elite  0   Boss  0
  Label 0x0A   Value 10    Sprite exp_0A   Scale 0.9   Minion  8   Elite 40   Boss  0
  Label 0x0F   Value 15    Sprite exp_0F   Scale 1.1   Minion  2   Elite 40   Boss  0
  Label 0x11   Value 17    Sprite exp_11   Scale 1.3   Minion  0   Elite 15   Boss  0
  Label 0xAA   Value 170   Sprite exp_AA   Scale 1.8   Minion  0   Elite  5   Boss 30
  Label 0xFF   Value 255   Sprite exp_FF   Scale 2.4   Minion  0   Elite  0   Boss 70

No Drop Minion  20      ← 잡몹의 20%는 빈손
Prefab          비워둘 것 — 특별한 연출이 필요한 등급만 채운다
```

가중치는 **비율이 아니라 저울**이다. 합이 100일 필요가 없고, 한 줄을 지워도 나머지가
알아서 다시 나뉜다. 잡몹 기대값은 지금 **한 마리당 약 1.8**.

**표시 이름과 값이 어긋나면 안 된다.** `0x0A` 가 10을 안 주면 16진수로 적은 의미가 없다.

### 🔴 웨이브의 `Exp Scale` 은 지금 전부 1이다

의미가 바뀌었다 — **값에 곱하는 배율이 아니라 등급을 몇 칸 위로 미는 값**이다.
값에 곱하면 그림은 `0x0A` 인데 실제로 30을 주게 되어 표기가 거짓말이 된다.

그런데 등급 간격이 `1 → 10` 처럼 10배씩이라 **잡몹에 한 칸만 밀어도 경험치가 폭주한다.**
그래서 `Stage 01` 은 전부 1로 두었고, 후반 성장은 **적 밀도**(초당 1 → 7.8마리)가 맡는다.

> 굳이 쓰려면 `2` 부터 한 칸씩 오른다. 쓰기 전에 반드시 한 판 돌려 레벨 수를 확인할 것.

### 계산해둔 경제

```
총 스폰 2,314마리 × 처치율 60% ≈ 1,390킬 × 1.8  ≈ 2,500 exp
엘리트 26마리 × 21                                ≈   550 exp
                                                  ─────────
                                                   3,050 exp
```

레벨 곡선 `base 10 / growth 10` 기준 **Lv 24** 근처에서 끝난다. 어제 목표(Lv 23)와 맞다.

### 줍는 로직이 하나로 합쳐졌다

`ExpMove` 안에만 있던 자석 코드를 `Pickup` 으로 뺐다. `ExpMove` · `FieldItem` 이 상속한다.
**프리팹 값은 그대로 유지된다** — 유니티는 필드를 이름으로 찾으므로 부모 클래스로 올라가도 안 깨진다.

같이 고친 것:

- **매 프레임 `PLAYER NULL` 로그** — 플레이어를 `Start` 에서 한 번만 잡아서, 풀에서 꺼낸
  두 번째 런부터 죽은 참조를 붙잡고 있었다. 이제 필요할 때 다시 찾는다
- **`baseSpeed` 가 아무 일도 안 하고 있었다** — 프리팹에 1.2 로 맞춰뒀는데 코드가 0에서
  출발했다. 이제 붙잡히는 순간 이 속도로 시작한다. 예전 느낌이 좋으면 **0으로 두면 된다**
- **이펙트가 월드 원점에서 터지던 것** — `PlayAt(Vector2.zero)` 였다. 이제 오브 자리에서 난다

### 즉시 효과가 UI 밖으로 나왔다

`AugmentSelectUI.ApplyInstantEffect` → **`InstantItem.Apply(data)`**.

카드로 먹든 필드에서 줍든 같은 코드를 탄다. 회복량을 고치려면 `AugmentData` 에셋 한 곳만
고치면 되고, 새 아이템을 만들 때도 `FieldItem` 에 에셋만 물리면 된다.

</details>
