# 전수 점검 — 코드 · 씬 · 에셋

점검 범위: 스크립트 179개(16,310줄) · 씬 5개 · 프리팹 30개 · 데이터 에셋 16개

> **먼저 밝힐 것** — 나는 컴파일을 못 돌린다. 아래 "구문" 판정은 괄호 짝·타입 참조·호출부 일치까지만
> 본 것이고, 실제로 빌드가 되는지는 유니티에서 확인해야 한다.

---

## 0. 한 장 요약

```
구조        ★★★★☆   역할분리·데이터화가 제대로 됐다. 이 규모 개인 프로젝트에서 보기 드문 수준
런 루프     ★★★★☆   시작→웨이브→보스→정산→로비 가 전부 이어져 있다
콘텐츠      ★★☆☆☆   증강 6종 · 적 2종. 10분을 버티기엔 얇다
연출        ★★☆☆☆   무음이다. BGM·효과음 배경이 하나도 없다
정리        ★★★☆☆   죽은 파일 9개 + 튜토리얼 사본 19개가 남아 있다
```

**데모까지 1주일 기준, 순서대로 세 가지만 하면 된다.**

1. 적 회수 충돌 제거 (오늘 넣은 배치 제한이 지금 작동을 안 한다)
2. BGM 붙이기 (코드 20줄. 무음 시연은 인상이 반토막 난다)
3. 정렬 증강 2종 추가 (신규 코드 0. 게임의 정체성이 화면에 처음 보인다)

---

## 1. 🔴 지금 막혀 있는 것

### 1-1. 적 회수 시스템이 두 개, 서로 방해한다

`Enemy001` · `Enemy002` 프리팹에 **`Reposition` 컴포넌트**가 붙어 있다. 이게 예전 회수 방식이다.

```
Reposition (적 프리팹)          거리 > 50 → 플레이어 기준 같은 방향 40 유닛으로 당긴다
Spawner.Recycle (오늘 작업)     거리 > 50 → 앞쪽 스폰 지점으로 옮긴다
```

**두 개가 같은 거리에서 걸린다.** `Reposition` 이 `OnTriggerExit2D` 로 먼저 반응해 40 유닛으로 당겨버리면,
`Spawner` 가 1초마다 훑을 때는 이미 50 안쪽이라 회수 대상이 아니다.

결과:
- 꼬리가 **여전히 뒤에 늘어선다** — 방향이 안 바뀌고 거리만 줄어들기 때문
- 오늘 넣은 `recycleBatch`(한 번에 3마리) 가 **사실상 한 번도 안 돈다**
- 씬의 `recycleDistance` 는 50, `Reposition` 의 기준도 50 — 정확히 겹친다

**해야 할 것** — 둘 중 하나만 남긴다. `Spawner` 쪽이 낫다(스폰 지점으로 보내니 다시 전투에 들어온다).

```
Enemy001.prefab · Enemy002.prefab
  → Reposition 컴포넌트 제거
```

`Reposition` 자체는 지우면 안 된다. `Background1.prefab` 이 무한 배경에 쓰고 있다.

> 이건 내가 회수 기능을 만들 때 기존 것을 확인 안 하고 얹은 결과다. 같은 일을 하는 것이 이미 있었다.

### 1-2. 소리가 하나도 안 난다

```
Scripts 안 BGM 재생 코드      0줄
씬 5개의 AudioSource          0개
보유 오디오 에셋              698개  (back_001~004.ogg 가 배경음 후보)
```

효과음은 증강 모듈(`SfxEffect` · `FxGroup`)이 재생하지만 **배경음이 없다.** 시연에서 무음은
"미완성" 신호로 읽힌다. 오디오는 이미 다 사 놨거나 받아놨는데 안 쓰고 있는 상태다.

가장 싼 해법: `[Audio]` 빈 오브젝트 + `AudioSource`(Loop ✔, Play On Awake ✔) + `back_002.ogg`.
컴포넌트 하나로 끝난다. 씬 전환마다 끊기는 게 싫으면 그때 `DontDestroyOnLoad` 를 붙이면 된다.

### 1-3. 보스가 경험치를 안 떨군다

오늘 바꾼 구조에서 경험치는 웨이브가 정한다. 보스는 `waveIndex = -1` 이라 `ReportKill` 이 그냥 돌아간다.

9분에 나오는 보스를 잡아도 아무것도 안 나온다. 클리어 직전이라 게임 진행에는 영향이 없지만,
**보스를 잡았는데 화면에 아무 일도 안 일어나는 건 시연에서 김이 샌다.**

`BossSpawn` 에 `expOrb` · `expCount` 칸을 주면 된다 (carry 가 필요 없으니 웨이브보다 단순하다).

### 1-4. 정렬(CC) 증강이 0종 — 게임의 정체성이 안 보인다

기획서상 이 게임의 핵심 루프는 이거다:

```
정렬로 모으고  →  탐색으로 표식  →  자료구조로 전파
   ✗ 없음          ✔ DFS·BFS        ✔ Graph·Tree
```

첫 단계가 없어서 시연자에게는 **"자동 공격 게임"** 으로 보인다. 그리고 적이 뭉치는 문제를
게임 안에서 풀 수단도 없다 — 원래 정렬 증강이 담당할 몫인데 물리로만 해결하려 했다.

**재료는 이미 다 있다.** 아무도 안 쓰고 있을 뿐이다.

| 미사용 모듈 | 상태 |
|---|---|
| `StatusEffect` | 구현 완료 · 어떤 증강도 안 씀 |
| `SlowStatus` | 구현 완료 · 안 씀 |
| `DamageOverTimeStatus` | 구현 완료 · 안 씀 |
| `KnockbackEffect.pull` | 끌어당김 지원 · `BFS` 만 쓰는데 `distance: 0` 이라 사실상 미사용 |
| `SummonEffect` | 구현 완료 · 안 씀 |

**신규 코드 0줄로 만들 수 있는 것:**

```
Selection Sort   Cooldown → RandomPoint → Area → Knockback(pull ✔) + Status(Slow)
                 한 점으로 빨아들인다

Bubble Sort      Cooldown → OwnerPoint → Area → Knockback + Status(Slow)
                 주변을 밀어내며 둔화
```

Selection Sort 하나만 넣어도 **적이 빨려들고 → 표식이 겹치고 → Graph 간선이 폭발하는**
3초짜리 장면이 만들어진다. 그게 말로 하는 설명보다 빠르다.

---

## 2. 잘 돼 있는 곳

솔직히 이 부분이 더 길다.

### 2-1. 역할 분리가 실제로 지켜지고 있다

`Run` 씬 계층이 설계 의도 그대로다.

```
GameSystem
  Game     GameManager · LevelSystem
  Run      RunDirector
  Stage    PoolManager · StageSetup
  UI       UIManager · GameHud · AugmentSelectUI · MenuManager · LogManager
Object
  Player   Player · Scanner · PlayerHealth · PlayerStats · AnimatorDriver
    AugmentManager
    Spawner  (+ Point ×16)
```

- **씬 전체에 `Missing (Mono Script)` 가 0개다.** 5개 씬 전부.
- `GameManager` 는 44줄, 들고 있는 건 참조 3개뿐
- `AugmentManager` · `Spawner` 가 Player 밑에 있어 플레이어를 따라다닌다 — 의도대로

### 2-2. 데이터화가 끝까지 갔다

```
StageData     스테이지 하나 = 에셋 하나. 씬을 안 건드리고 갈아끼운다
EnemyData     적의 정체. 웨이브가 배율만 얹는다
AugmentData   [SerializeReference] 모듈 조립. 코드 없이 새 증강을 만든다
AugmentPool   증강 목록도 에셋
UiTheme       색·글꼴·여백 한곳
HardwareTable 부품 효과까지 에셋에 (오늘 작업)
```

증강 6종이 **전부 기존 모듈 조합만으로** 조립돼 있다. 새 증강에 새 코드가 필요 없는 구조가
말로만이 아니라 실제로 성립한다. 이게 이 프로젝트에서 제일 큰 자산이다.

### 2-3. 단일 소유 원칙이 지켜진다

| 무엇 | 유일한 소유자 |
|---|---|
| `Time.timeScale` | `TimeControl` |
| 화면 스택 | `UIManager` |
| 런 상태·시간·처치수 | `RunDirector` |
| 런 사이 static 청소 | `RunLifecycle` |
| 증강 문구 토큰 | `AugmentText` |
| UI 색·글꼴 | `UiTheme` |

예전에 5개 파일이 각자 `timeScale` 을 만지던 버그가 구조적으로 재발 불가능해졌다.

### 2-4. 방어적 폴백이 곳곳에 있다

에셋을 안 만들어도 게임이 멈추지 않는다. 이게 팀 작업에서 크다.

```csharp
UiTheme.Current        에셋 없으면 기본값 인스턴스 + 경고
PlayerHealth.Awake     enemyLayer 비었으면 "Enemy" 자동
Player.Awake           scanner 비었으면 GetComponent
Spawner.Awake          spawnPoints 비었으면 자식 수집
AugmentSelectUI        fallbackPool 비었으면 StageData 것
```

실제로 씬을 뜯어보니 `Player.scanner` 와 `Spawner.spawnPoints` 가 **비어 있는데도 돌고 있다.**
폴백이 없었으면 조용히 안 돌았을 자리다.

### 2-5. 오늘 넣은 경험치 구조

```
Spawner.Release()      → Enemy.Init(data, scale, waveIndex)
Enemy.Dead()           → Spawner.Current.ReportKill(waveIndex, 위치)
Spawner.ReportKill()   → wave.TakeOrbCount() → DropOrbs()
```

- 적은 `int` 하나만 든다. `StageWave` 객체를 몰라 스테이지 구조에 안 묶인다
- 이월(carry)이 웨이브에 있어 **총량이 정확하다** — 100마리 × 0.4 = 정확히 40개
- 오브 값은 프리팹이 소유 → **표기와 실제가 어긋날 수 없다**
- 오브를 한 줄로 나열 → 몇 개인지 눈으로 세인다

호출부·정의부가 전부 일치하는 것까지 확인했다.

---

## 3. 🟡 위험하지만 급하지 않은 것

### 3-1. `Editor/lecture/` — 튜토리얼 사본 19개

`Enemy` · `Player` · `GameManager` · `PoolManager` · `Spawner` · `Scanner` · `Bullet` · `Item` ·
`ItemData` · `Weapon` · `Reposition` — **12개 이름이 실제 코드와 겹친다.**

`Editor` 폴더라 별도 어셈블리로 컴파일돼서 지금은 에러가 안 난다. 하지만:

- 에디터 스크립트를 새로 짤 때 `Enemy` 라고 쓰면 **어느 쪽인지 모호해진다**
- MonoBehaviour 인데 에디터 어셈블리라 **씬에 붙일 수 없다** — 있어도 못 쓴다
- 컴파일 시간과 검색 결과를 계속 오염시킨다

**폴더째 지우는 걸 권한다.** 참고용이면 `Assets` 밖으로 빼거나 `.txt` 로 바꾸면 된다.

### 3-2. 모듈에 수치가 하드코딩돼 시트가 무력하다

```
Bash.ProjectileDelivery.speed   9      ← 시트 speed 는 0인데 여기 박혀 있다
BFS.RadialDelivery.speed        14
Tree.RadialDelivery.speed       14 / 10
Graph.LinkEffect.transfer.scale 0.1    ← 시트 effectDamage 0.2 × 0.1 = 2% 전이
```

`Scalable` 은 `value > 0` 이면 시트를 무시한다. 지금 상태로는 시트에서 speed 를 아무리 고쳐도
게임이 안 바뀐다. 시트로 관리하려면 모듈 값을 0으로 비워야 한다 — TODO 2-5 에 적혀 있던 그대로다.

**데모 전에 굳이 안 해도 된다.** 다만 "시트로 밸런싱한다"는 말은 지금 사실이 아니라는 걸
알고 있어야 한다. 밸런싱은 에셋을 직접 열어서 해야 한다.

### 3-3. 증강 6종의 실질 딜 구조

```
        damage  effectDamage      역할
Bash      3→11.4    0.15→0.29     직접 딜
BruteForce 6→14.4   0             직접 딜
DFS       0         0.2→0.41      표식 (추가 피해)
BFS       0         0.2→0.41      표식
Graph     0         0.2→0.41      전이 (×0.1 → 2%)
Tree      0         0.5           전이 50%
```

**직접 피해가 Bash 와 BruteForce 둘뿐이다.** 나머지 넷은 그 둘이 때릴 때 얹히는 증폭기다.
그래서 시연자가 Bash·BruteForce 를 안 뽑으면 **딜이 거의 안 나온다.**

`AugmentManager.startingAugments` 가 `BruteForce` 로 고정돼 있어서 지금은 괜찮다.
근데 이건 우연히 막혀 있는 것이고, 증강을 늘리면 다시 문제가 된다.

### 3-4. HUD 색이 테마와 안 맞는다

```
GameHud.expBackColor   (0.88, 0.88, 0.88)   밝은 회색
UiTheme.background     (0.04, 0.06, 0.08)   어두운 터미널
```

경험치 바 배경만 밝아서 튄다. 씬 값이라 코드가 못 고친다. 인스펙터에서 어둡게 내리면 된다.

### 3-5. 청소 안 되는 static 하나

```csharp
LinkHolder.warned   // HashSet<GameObject>
```

파괴된 GameObject 참조가 계속 쌓인다. 경고 중복 방지용이라 게임에는 영향이 없지만,
`RunLifecycle` 에 한 줄 넣는 게 맞다. 나머지 static 은 전부 관리되고 있다.

### 3-6. 씬 두 개가 거의 같다

`Run.unity` 와 `augmentTest.unity` 가 레이어 값 몇 개 빼고 동일하다.
`MenuManager` 는 `"Run"` 을 로드하므로 `Run` 이 본판이다. `augmentTest` 는 지워도 된다.

---

## 4. 정리하면 좋을 것 (지워도 아무 일도 안 남)

```
■ 오늘 되돌리면서 빈 껍데기만 남은 것
   Scripts/Drop/BitOrbTable.cs
   Scripts/Drop/DropSpawner.cs
   Scripts/Player/Pickup.cs
   Scripts/Player/FieldItem.cs
   Scripts/Augment/InstantItem.cs
   Editor/BitOrbTableCreator.cs
   → Project 창에서 지울 것. Scripts/Drop 폴더가 통째로 없어진다

■ 아무도 안 쓰는 것
   Scripts/Combat/DummyTarget.cs
   Editor/lecture/  (19개 전부)

■ TODO 3장에 적어둔 채 아직 안 지운 것
   Editor/AugmentEditor/FxGroupDrawer.cs   ← 이미 지운 듯 (지금 없음)
   Scripts/Augment/ProjectileSpawner.cs    ← 이미 지운 듯
   Scripts/Effect.cs                       ← 이미 지운 듯
```

한 가지 이상한 점: `Scripts/Drop/DropEntry.cs` 만 유니티가 컴파일 목록에서 빠뜨렸던 사건은
파일에는 아무 문제가 없었다(인코딩·BOM·문법 전부 정상). **`Library` 캐시 손상이 유력하고,
같은 뿌리로 Animator 창의 `Graph.OnEnable` NRE 도 설명된다.** `Assets → Reimport All` 을 한 번
돌려두는 게 좋다 — 시연 준비 중에 같은 일이 또 나면 곤란하다.

---

## 5. 안 쓰고 있는 자산 — 여기가 기회다

```
Art/icons/          개발툴·언어 아이콘 500+장    증강 아이콘 비용이 0이다
Art/Audio/          효과음·배경음 698개          BGM 이 그중 4개면 된다
StatusEffect        슬로우·도트 구현 완료        정렬 증강을 코드 0으로
SummonEffect        소환물 구현 완료             나중에 "프로세스 포크" 같은 증강
Homing              유도 투사체 구현 완료        어떤 증강도 안 씀
MultiShot           다중 발사 구현 완료          안 씀
LineDelivery        관통 레이저                  DFS 만 씀
exp_* 스프라이트 6장  경험치 오브 등급            프리팹 하나뿐
```

**증강을 6 → 10종으로 늘리는 데 새 코드가 한 줄도 필요 없다.** 조립과 아이콘 선택만 하면 된다.

---

## 6. 데모 시연 시나리오 점검

```
MainA → [Connect] → MainB(로비) → [stage 1] → Run → 10분 → StageResult → [로비]
  ✔        ✔           ✔             ✔          ?          ✔            ✔
```

- 로비 UI 존재 확인 (`UICanvas` 에 `StagePanel` · `UpgradePanel` · `CharacterPanel` + 버튼들)
- `UpgradePanel` 이 이미 있으므로 `UpgradeShop` 을 붙일 자리가 준비돼 있다
- `StageResult.retryScene` 이 `"stage 1"`(없는 씬)이었던 버그는 오늘 `"Run"` 으로 고쳤다
- `RunDirector.endDelay 3` · `resultScene StageResult` ✔

**10분 구간이 실측 안 됐다.** 특히:

| 확인할 것 | 왜 |
|---|---|
| 보스 체력 3,000 | 순전히 추정치다. 10초에 죽거나 2분 넘게 안 죽을 수 있다 |
| 레벨업 횟수 | 목표 Lv 23. `expPerKill` 이 전부 1이라 지금은 킬당 1개 |
| 후반 오브 수 | 초당 7.8마리 처치 × 1개 = 오브가 쌓인다. `expPerKill` 을 0.3~0.5 로 내려야 할 수 있다 |

**첫 30초가 제일 중요하다.** 시연자가 "이 게임 뭐지"를 판단하는 구간이다. 지금 부팅 로그
5줄이 목표를 말해주고, 도입 웨이브가 초당 1마리로 여유롭다. 여기에 BGM 과 첫 레벨업만
확실히 붙으면 된다.

---

## 7. 확장 가능성 — 지금 구조가 감당하는 것

이미 뚫려 있어서 **코드를 안 고치고** 되는 것들:

| 하고 싶은 것 | 지금 상태 |
|---|---|
| 스테이지 2·3 | `StageData` 복제. 씬은 안 건드린다 |
| 중간보스 | `BossSpawn` 에 `endsRun` 끈 항목을 추가 |
| 레이어(L1/L2/L3) | 보스 목록을 레이어별로 쪼개면 된다 |
| 새 증강 | 모듈 조립 + 아이콘 |
| 내부 증강 | `rootAugment` · `requiredRootLevel` 구현 완료 · **데이터가 0개** |
| 상자(.zip) 드랍 | `AugmentDraft.PickOne()` 이 이미 UI 밖에 있다 |
| 하드웨어 상점 | 오늘 틀 완성. 표 만들고 컴포넌트 두 개 붙이면 끝 |

코드가 필요한 것:

| | 필요한 것 |
|---|---|
| 캐릭터 7종 | 스타트 증강 + 고유 메카닉. 선택 화면도 |
| 크리티컬 | `DamageContext.IsCritical` 필드와 색만 있고 **판정기가 없다** |
| 에러율 시스템 | 통째로 미구현 (기획서상 초기 제외) |

> **내부 증강이 구현돼 있는데 데이터가 0개**인 건 아까운 부분이다. `Bash` 5레벨에서
> `Bash -c` 가 풀리는 식으로 두세 개만 만들어도 "가지가 뻗는다"는 감각이 생긴다.
> 증강 종류를 늘리는 것보다 싸다.

---

## 8. 1주일 우선순위

```
Day 1   ■ Reposition 제거 → 회수 동작 확인          30분   🔴 오늘 작업이 무력화돼 있다
        ■ BGM 붙이기                                20분   🔴 무음 시연 방지
        ■ Reimport All                              10분   🔴 Library 손상 정리
        ■ 빈 껍데기 6개 + Editor/lecture 삭제        10분

Day 2   ■ 10분 완주 실측 (레벨업 횟수·보스 시간)     1시간  🔴 모든 밸런싱의 기준선
        ■ expPerKill 웨이브별 조정                   1시간
        ■ 보스 체력 보정

Day 3   ■ 정렬 증강 2종 조립 (코드 0)                2시간  🔴 게임 정체성이 화면에 보인다
        ■ 아이콘은 Art/icons 에서 고른다

Day 4   ■ 적 스프라이트 2종 (`;` 작고 빠른 / `NULL` 느리고 단단)
        ■ EnemyData 만들고 웨이브에 배치

Day 5   ■ 보스 전용 아트 (큰 글자 1장 + 글리치 2장이면 충분)
        ■ 보스 경험치 드랍 (BossSpawn 에 칸 추가)

Day 6   ■ 하드웨어 상점 배선 (표 만들기 + 컴포넌트 2개)
        ■ HUD 색 정리

Day 7   ■ 통합 리허설 · 예비일
```

**Day 1 은 전부 "이미 만든 것이 작동하게 만드는" 일이다.** 새로 만드는 건 Day 3 부터다.

---

## 9. 확인 못 한 것

내 접근 범위가 `Assets/_Project` 까지라 아래는 못 봤다.

```
ProjectSettings/         레이어 충돌 행렬 (Default ↔ Enemy 체크 해제 여부)
Assets/ 최상위           _Project 밖의 스크립트·에셋
Library/                 임포트 캐시
Packages/                Cinemachine · Input System 등
```

특히 **플레이어가 적을 통과하는지**(레이어 충돌 행렬)는 확인이 안 됐다. 파고들 때 벽에
막히는 느낌이 남아 있으면 여기부터 볼 것.

컴파일 통과 여부도 마찬가지다. 오늘 바꾼 파일은:

```
StageWave.cs · EnemyScale.cs · Enemy.cs · EnemyData.cs · Spawner.cs · BossSpawn.cs
Stage 01.asset   (expScale → expOrb · expPerKill 로 10줄 교체)
```
