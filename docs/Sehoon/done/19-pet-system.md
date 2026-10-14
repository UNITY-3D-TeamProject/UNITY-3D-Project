# 19. 펫 시스템 (따라다니기 + 스캔 대리 사용)

- **우선순위**: 미정
- **상태**: 완료
- **관련 작업**: [07. 스킬 구현](../07-skill-implementation.md) — 스킬 구조를 공유한다. 스캔 대리 사용은 [07-3. 스캔 스킬](../07-3-scan-skill.md) 로 이관했다.

## 목표
플레이어를 따라다니는 펫을 만들고, 플레이어가 스캔을 사용하면 **펫이 앞으로 나와서 스캔을 사용**하게 한다.

## 현재 상황
- 펫 관련 코드·프리팹은 없다.
- 스캔 스킬 `Scan` 은 `Debug.Log` 만 찍는 빈 껍데기다 (`Assets/_Project/Scripts/Skill/Skills/Scan.cs`).
- 스캔 판정은 이미 있다: `ScanWave.Begin(origin, duration, maxDiameter)` 가 구 모양 파티클을 확산시키며 범위 안 `IScannable` 에 알린다 (`Assets/_Project/Scripts/Core/Scanning/ScanWave.cs:127`). 반응 컴포넌트(`MarkerReaction`, `MeshRevealReaction`, `SilhouetteReaction`, `WeakPointReaction`)도 있다.
  - `ScanWave` 는 "스킬 아래에 상주하다가 Begin 으로 재생"되는 전제로 작성돼 있고, 중심은 Begin 시점 위치로 고정된다.
  - `Assets/_Project/Prefabs/Skills/ScanWave.prefab` 이 있고 `Map Total test` 씬에서 쓰인다. `Scan` 스킬과는 아직 연결되지 않았다.

## 작업 범위 (예상)
- 펫 추종: 플레이어 뒤·옆의 지정 위치를 부드럽게 따라간다.
- 스캔 연출: 스캔 사용 → 펫이 플레이어 앞으로 이동 → 스캔 발동(`ScanWave.Begin`) → 원래 추종 위치로 복귀.
- `Scan` 스킬에서 펫으로 요청을 전달하는 경로는 기존 Mediator 연결 방식을 따른다.

## 확정 사항
- 이 문서는 **펫 추종·애니메이션 등 펫 자체 작업**까지만 한다. 스캔 대리 사용(아래 "스캔" 질문)과 07-3 은 별도로 진행한다.
- 모델: `Assets/ThirdParty/GrigoriyArx/DrollRobots` 의 `Rob11`. 원본 `Rob11.prefab` 의 `Rob11Ctrl`·`RobotLift` 는 구 `Input` API 를 써서 새 Input System 전용 설정(`activeInputHandler: 1`)에서 예외가 나므로 쓰지 않는다.
- 펫 프리팹은 `Assets/_Project/Prefabs/Pet/` 에 별도로 만든다.
- Animator Controller 는 `Rob11.controller`(데모용, ThirdParty 수정 불가) 대신 `Assets/_Project/Animations/Pet/PetAnimator.controller` 를 새로 만든다.
- 펫으로 가는 신호는 `CharacterMediator` → `PetMediator`(SubMediator) → `PetSpawner` → `PetAnimator` 경로로 전달한다. `PlayerFacade` 는 구독하지 않는다.
- `PetMediator` 는 다른 SubMediator 처럼 Action 프리팹(`Prefabs/Actions/PetPrefab.prefab` = `PetSpawner` + `PetMediator`)으로 두고 `Player.prefab` 자식으로 넣는다.
- 이동 애니메이션 배속 상한 3배. 펫 크기는 원본(키 약 0.3m) 그대로.
- 플레이어 애니메이션 동기화 (펫 클립 길이를 플레이어 재생 시간에 맞춘다)

  | 플레이어 | 펫 | 펫 배속 |
  |---|---|---|
  | 점프 시작 → 공중 → 착지 | `R11_Jump` 한 클립을 구간별 배속으로 재생: 최고점까지 0.45초, 체공 중 최고점에서 정지, 남은 구간 0.5초 | 가변 |
  | 특수 대기 0 (`Idle_Breaker` 5.97초) | `R11_EmoLookingFor` | 0.56 |
  | 특수 대기 1 (`Wave_Loop` ×2 = 3.27초) | `R11_EmoHello` | 0.71 |
  | 피격 앞·뒤 (0.57초) | `R11_getHitfront` | 2.35 |
  | 피격 왼쪽 / 오른쪽 | `R11_getHitLeft` / `R11_getHitRight` | 2.35 |
  | 사망 | `R11_Death` 후 정지 | 1 |

  - 이동 우선은 특수 대기에만 적용한다 (펫이 움직이면 즉시 이동 블렌드로). 점프·피격은 이동 중에도 재생한다.
  - 플레이어가 이동 없이(사격·조준 등) 특수 대기를 끊어도 펫은 끝까지 재생한다.
  - 피격 방향은 플레이어가 계산한 방향(`GetHitDirection`)을 그대로 쓴다.
  - 구르기·사격·탑승은 매핑하지 않는다 (이동 블렌드 유지).

## 열린 질문
### 펫 기본
- [x] 펫 모델·애니메이션 리소스가 있는가? → `GrigoriyArx/DrollRobots/Rob11` 사용. Animator Controller 구성은 계획 단계에서 확정.
- [x] 이동 방식 → `Player.prefab` 에 펫 위치용 pivot 을 두고 그 위치를 따라간다.
- [x] 추종 위치와 속도 → 플레이어 뒤 1.5m·오른쪽 1m, SmoothDamp 0.3초. 이동 속도 값은 애니메이션(걷기/달리기 블렌드)에 필요한 만큼만 둔다.
- [x] 순간이동 → 플레이어와 10m 이상 멀어지면 pivot 위치로 즉시 이동.
- [x] 피격·충돌 판정 → 없음 (콜라이더 없음).
- [x] 플레이어 사망 시 → `R11_Death` 재생 후 멈춤.
- [x] 펫 배치 → 플레이어가 스폰되면 펫을 직접 스폰한다.
- [x] 블루투스 탈것 탑승 중 → 똑같이 따라간다.

### 스캔
- 이 문서 범위 밖. 질문은 [07-3. 스캔 스킬](../07-3-scan-skill.md) 의 "펫 대리 사용" 항목으로 옮겼다.

## 결과
### 구조
- `PetSpawner` (`Prefabs/Actions/PetPrefab`, `Player` 자식): `Start` 에서 `Pet.prefab` 을 월드 오브젝트로 생성하고 `PetPivot` 을 따라가게 한다. 파괴될 때 펫도 파괴한다 (리스폰 시 새 플레이어가 새 펫 생성).
- `PetPivot`: `SK_ZMike`(회전하는 몸) 아래. `SK_ZMike` 에 FBX 기본 회전(X -90°)이 있어 local 값은 월드 기준으로 환산해 넣는다. 위치는 사용자가 조정.
- `PetFollower` (`Pet.prefab` 루트): `LateUpdate` 에서 `SmoothDamp` 추종, 10m 이상이면 순간이동. 이동 중엔 이동 방향, 멈추면 pivot 정면을 본다. 실제 수평 속도를 `PetAnimator` 에 넘긴다.
- `PetAnimator` (`Pet.prefab` 루트): `Speed`·`MoveAnimSpeed`(3배 상한), 점프 구간 배속(`JumpAnimSpeed`), 피격·특수 대기·사망 트리거.
- 플레이어 쪽 추가 인터페이스: `CharacterAnimator.OnSpecialIdleStarted(int)`, `CharacterAnimator.GetHitDirection(Vector3?)`, `AnimationMediator` 의 같은 이름 이벤트·메서드.

### Inspector 값
| 컴포넌트 | 필드 | 값 |
|---|---|---|
| `PetFollower` | `_smoothTime` / `_teleportDistance` / `_rotateSpeed` | 0.3 / 10 / 720 |
| `PetAnimator` | `_runAnimSpeed` / `_maxMoveAnimSpeed` | 0.8 / 3 |
| `PetAnimator` | `_jumpStartDuration` / `_jumpLandDuration` / `_jumpApexNormalizedTime` | 0.45 / 0.5 / 0.5 |

### 변경 파일
- 신규: `Scripts/Pet/PetFollower.cs`, `PetSpawner.cs`, `PetAnimator.cs`, `Scripts/Mediator/SubMediators/PetMediator.cs`, `Animations/Pet/PetAnimator.controller`, `Prefabs/Pet/Pet.prefab`, `Prefabs/Actions/PetPrefab.prefab`
- 수정: `CharacterAnimator.cs`, `AnimationMediator.cs`, `CharacterMediator.cs`, `Player.prefab` (`PetPivot`, `PetPrefab` 인스턴스, `_petMediator` 연결)

### 검증 (플레이 모드, `Map_1_Lobby`)
- 추종·회전·걷기/달리기 블렌드, 10m 순간이동 확인.
- 점프: 최고점까지 0.56초(전환 포함) → 체공 중 정지 → 착지 구간 0.49초 후 Locomotion.
- 특수 대기 0·1 이 플레이어와 함께 재생되고 각각 5.96초·약 3.26초 유지. 재생 중 이동하면 0.25초 안에 Locomotion.
- 피격 앞(실제 데미지 경로)·왼쪽·오른쪽(`PetMediator` 직접 호출) 0.57초 재생. 사망 시 `Death` 에서 정지.

### 알려진 사항 / 미확인
- 탑승 중 추종, 리스폰 시 펫 정리는 플레이 모드로 확인하지 않았다.
- 탑승 점프에서도 `OnJumpStarted` 가 발생하면 펫이 점프 모션을 재생할 수 있다 (미확인).
- pivot 이 몸 기준이라 뒷걸음질 시 펫이 플레이어 앞쪽에서 따라오고, 탑승 중에는 몸 높이(0.3m)만큼 떠서 따라간다.
- 펫이 이미 같은 특수 대기를 재생 중일 때 다시 트리거되면 끝난 뒤 한 번 더 재생된다 (실제 플레이에선 대기 시간이 더 길어 겹치지 않음).

## 완료 기준
- 플레이 모드에서 펫이 플레이어를 자연스럽게 따라다닌다.
- 점프·특수 대기·피격 시 펫이 플레이어와 같은 길이로 대응 모션을 재생한다.
- 플레이어 사망 시 펫이 `R11_Death` 를 재생하고 멈춘다. 컴파일 에러 없음.
