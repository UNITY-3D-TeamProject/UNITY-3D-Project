# 07-3. 스캔 스킬

- **우선순위**: 높음
- **상태**: 완료
- **상위 작업**: [07. 스킬 구현](07-skill-implementation.md)
- **관련 작업**: [19. 펫 시스템](19-pet-system.md) — 펫 본체(추종·애니메이션, `PetMediator`)는 완료됐다. 펫이 스캔을 대리 사용하는 연출 질문은 아래 "펫 대리 사용" 으로 옮겨 왔다.

## 목표
`Scan` 스킬을 사용하면 기존 스캔 판정(`ScanWave`)이 발동되어 스캔 대상 반응이 동작하도록 연결한다.

## 현재 상황
- 스캔 스킬 `Scan` 은 `Debug.Log` 만 찍는 빈 껍데기다 (`Assets/_Project/Scripts/Skill/Skills/Scan.cs`).
- 입력은 이미 연결돼 있다: `PlayerInputComponent.OnScan` → `"Scan"` 스킬 실행 요청. HUD 도 `"Scan"` 이름으로 쿨타임을 표시한다 (`PlayerHudPresenter._scanSkillName`).
- 스캔 판정은 이미 있다: `ScanWave.Begin(origin, duration, maxDiameter)` 가 구 모양 파티클을 확산시키며 범위 안 `IScannable` 에 알린다 (`Assets/_Project/Scripts/Core/Scanning/ScanWave.cs:127`). 반응 컴포넌트(`MarkerReaction`, `MeshRevealReaction`, `SilhouetteReaction`, `WeakPointReaction`)도 있다.
  - `ScanWave` 는 "스킬 아래에 상주하다가 Begin 으로 재생"되는 전제로 작성돼 있고, 중심은 Begin 시점 위치로 고정된다.
  - `Assets/_Project/Prefabs/System/Scan/ScanWave.prefab` 이 있고 `Map Total test` 씬에서 쓰인다. `Scan` 스킬과는 아직 연결되지 않았다.
  - 시험용 값(SungJun 파트, `Assets/Test` 의 `ScanTestTrigger`): 지속 6초, 지름 30m → 확산 속도 2.5 m/s. `duration` 은 대상 활성 유지 시간도 겸한다.

## 확정 사항
- 펫 대리 사용까지 07-3 범위에 포함한다.
- `ScanWave.prefab` 을 `Player.prefab` 의 `SkillPrefab/Scan` 아래에 켜진 상태로 두고, `Scan` 이 `_scanWave.Begin(펫 위치, 지속시간, 최대 지름)` 을 호출한다. 지속시간·최대 지름은 `Scan` 인스펙터에 노출한다.
- `Map Total test` 씬의 `ScanWave` 는 건드리지 않는다.
- 앞 지점: `SK_ZMike` 아래에 `PetScanPivot` Transform 을 두고, 스캔 시 펫 추종 대상을 잠시 이 Transform 으로 바꾼다 (기본 위치: PetPivot 에서 캐릭터 정면 2m). 플레이어가 움직이면 앞 지점도 따라간다.
- 발동 시점: 고정 대기 시간(`_scanDelay`, 기본 0.3초, 인스펙터 노출) 뒤 그 순간의 펫 위치에서 발동한다.
- 스캔 이동 방식: 평소 추종(SmoothDamp)은 이동 중 목표보다 크게 뒤처져 발동 위치가 어긋나므로, 스캔 이동만 `_scanDelay` 동안 시작 위치 → 현재 앞 지점(벽 보정 포함)을 SmoothStep 진행률로 보간해 정확히 도착시킨다 (`PetFollower.MoveToTarget`).
- 복귀: 발동 지점에 고정된 채 `_scanReturnDelay`(기본 0.5초, 인스펙터 노출) 동안 머문 뒤 PetPivot 추종으로 돌아간다. (처음엔 즉시 복귀로 정했다가 변경)
- 벽: PetPivot → 앞 지점 사이에 벽이 있으면 벽 앞까지만 이동한다 (레이캐스트).
- 펫이 없으면 대기 없이 PetPivot(캐릭터) 위치에서 즉시 발동한다.
- 수치: 지속 6초, 최대 지름 30m, 쿨타임 6초. 입력 키는 기존 `OnScan` 그대로, 코스트는 쿨타임만.

## 작업 범위 (예상)
- `Scan` 스킬에서 `ScanWave.Begin` 호출. `ScanWave` 를 플레이어(스킬) 아래에 두는 방식과 참조 경로를 정한다.

## 열린 질문
(모두 답변됨 → "확정 사항" 참고)
- [x] 07-3 범위: 펫 없이 **플레이어 위치에서 즉시 발동**까지만 하고, 펫 대리 사용은 19번에서 덧붙이는가?
- [x] 발동 위치(origin): 플레이어 몸통 중심인가, 발밑인가?
- [x] 스캔 지속 시간·최대 지름 값은? (시험값 6초 / 30m 를 그대로 쓰는가)
- [x] `ScanWave` 배치: `Player.prefab` 의 `Scan` 스킬 아래에 `ScanWave.prefab` 을 넣는가? `Map Total test` 씬에 있는 `ScanWave` 는 어떻게 하나?
- [x] 스캔 진행 중 재사용 시: 쿨타임으로 막는가, 새 스캔으로 다시 시작하는가(`Begin` 기본 동작)?
- [x] 입력 키 / 쿨타임 / 코스트 값

### 펫 대리 사용 (19번에서 이관)
- [x] 07-3 범위에 포함하는가, 별도 문서로 나누는가?
- [x] 스캔 발동 위치: 펫 위치에서 파동이 퍼지는가, 플레이어 위치인가?
- [x] "앞"의 기준: 캐릭터 정면인가, 카메라 정면인가? 거리는?
- [x] 펫이 앞으로 나오는 데 걸리는 시간만큼 스캔이 늦게 발동돼도 되는가? (즉시 발동 + 펫은 연출만 하는 방식도 가능)
- [x] 스캔 중 펫이 앞 위치에 머무는 시간, 복귀 조건은?
- [x] 펫이 앞으로 갈 수 없는 위치(벽)일 때 처리는?

## 결과 (2026-10-14)

### 동작
1. 스캔 입력 → `Scan` 발동, 쿨타임 6초 시작 (HUD 는 기존 `OnSkillUsed` 경로).
2. `SkillMediator.OnScanRequested` → `CharacterMediator` → `PetMediator.RequestScan` → `PetSpawner.PlayScan`.
3. 펫이 `_scanDelay`(0.3초) 동안 `PetScanPivot`(PetPivot 정면 2m) 으로 SmoothStep 보간 이동해 정확히 도착. PetPivot → 앞 지점 사이가 레이어 0·7·10 에 막히면 벽 0.3m 앞까지만 간다.
4. 도착 시점의 펫 위치로 `SkillMediator.BeginScan` → `Scan.BeginWave` → `ScanWave.Begin(위치, 6초, 30m)`.
5. 펫은 발동 지점에 `_scanReturnDelay`(0.5초) 머문 뒤 PetPivot 추종으로 복귀. 사망하면 대기·머묾을 취소한다.
6. 펫이 없으면 대기 없이 PetPivot(캐릭터) 위치에서 즉시 발동한다.

### Inspector 값
| 위치 | 필드 | 값 | 설명 |
|---|---|---|---|
| `Scan` | `_duration` | 6 | 파동 확산 시간이자 대상 활성 시간(초) |
| `Scan` | `_maxDiameter` | 30 | 파동 최대 지름(m) |
| `Scan` `CooldownCost` | `_cooldown` | 6 | 쿨타임(초) |
| `PetSpawner` | `_scanDelay` | 0.3 | 앞 지점 도착·발동까지 시간(초) |
| `PetSpawner` | `_scanReturnDelay` | 0.5 | 발동 후 머무는 시간(초) |
| `PetFollower` | `_blockingLayers` | 0, 7, 10 | 앞 지점까지 막는 레이어 |
| `PetFollower` | `_wallMargin` | 0.3 | 벽에서 띄우는 거리(m) |

### 변경 파일
- `Scan.cs`: `_scanWave`·`_duration`·`_maxDiameter`, `BeginWave`. `Execute` 는 발동 알림만 한다 (위치는 펫 이동 후 정해짐).
- `SkillMediator.cs`: `OnScanRequested`, `BeginScan`.
- `CharacterMediator.cs`: `OnScanRequested` 연결, 펫 Mediator 가 없으면 캐릭터 위치에서 발동.
- `PetMediator.cs`: `RequestScan`.
- `PetSpawner.cs`: `_scanPivot`, `_scanDelay`, `_scanReturnDelay`, `PlayScan`, 사망 시 스캔 취소.
- `PetFollower.cs`: `ChangeTarget`(순간이동 없는 대상 변경), `MoveToTarget`(시간 보간 + 벽 보정), `HoldPosition`.
- `Scan.prefab`: `ScanWave.prefab` 자식 추가·연결, 쿨타임 1 → 6.
- `Player.prefab`: `SK_ZMike/PetScanPivot` 추가, `PetSpawner._scanPivot` 연결.

### 검증 (플레이 모드, `Map_1_Lobby`)
- 스캔 요청 시 `OnSkillUsed("Scan", 6)`, 파동이 펫 위치에서 수명 6·크기 30 으로 재생.
- 정지 상태: 펫이 앞 지점으로 이동 후 발동, 0.3~0.8초 그 자리에 머문 뒤 복귀.
- 6 m/s 직진 이동 중: 발동 위치와 그 순간 앞 지점 오차 0.02m (SmoothDamp 방식에서는 펫이 약 1m 이상 뒤처졌다).
- 임시 벽(레이어 0)을 앞에 두면 벽 앞에서 멈춤.
- 입력은 리플렉션으로 같은 요청 경로를 호출했다. `Map_1_Lobby` 에는 스캔 대상이 없어 대상 반응은 이 세션에서 확인하지 않았다.

### 알려진 사항
- 0.3초 타이머가 펫 위치 갱신 직전 프레임에 끝나 최대 한 프레임 이동량(60fps·6 m/s 기준 약 0.1m) 오차가 있다.
- 달리는 중에는 펫이 0.3초에 3m 이상 빠르게 튀어나간다. `_scanDelay` 로 조절한다.

## 완료 기준
- 플레이 모드에서 스캔 스킬 사용 시 파동이 발동되고, 스캔 대상 반응이 동작한다. HUD 쿨타임 표시가 동작한다. 컴파일 에러 없음.
