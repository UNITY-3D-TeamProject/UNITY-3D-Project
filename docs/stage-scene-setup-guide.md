# 스테이지 씬 세팅 가이드

스테이지 씬(튜토리얼, 인스타, 파일 등)을 만드는 파트가 **플레이어 스폰, 체크포인트, 낙하 복귀, 사망 후 재스폰**을 쓰려면 씬에 아래 오브젝트를 배치해야 한다. 프리팹은 모두 `Assets/_Project/Prefabs/System/`에 있다.

> 관련 구현: [낙하 복귀·사망 후 재스폰 구현 기록](./HANDOVER/SungJun/2026-10-09/fall-death-recovery-implementation.md), 작성자 김성준

## 1. 씬에 필요한 오브젝트 한눈에 보기

| 오브젝트 | 위치 | 개수 | 역할 |
|---|---|---|---|
| `StageManager` | `Prefabs/System/StageManager.prefab` | 1 | 체크포인트·낙하 구역 관리, 사망 시 재스폰 요청 |
| `PlayerSpawner` | `Prefabs/System/PlayerSpawner.prefab` | 1 | 플레이어 생성. `Is Lobby Spawner`는 **꺼둔다** |
| `Checkpoint` | `Prefabs/System/Checkpoint.prefab` | 코스 구간마다 | 도달 기록 + 복귀·스폰 위치(`RespawnPoint` 자식) |
| `FallZone` | `Prefabs/System/FallZone.prefab` | 1개 이상 | 맵 아래 떨어졌을 때 데미지 + 체크포인트 복귀 |
| `StageBase`를 상속한 컴포넌트 | 빈 오브젝트에 붙임 | 1 | 이 씬이 어떤 스테이지인지 알려줌(`Tutorial`, `FeedApp`, `FileApp`, `SecurityApp`, `LiveApp`) |
| `GameManager` | `Prefabs/System/GameManager.prefab` | 1 (프로젝트 전체) | 이미 다른 씬에서 `DontDestroyOnLoad`로 넘어오는지 확인한다. 씬에서 단독 테스트할 때만 직접 배치 |

## 2. 설치 순서

### 2-1. StageBase 상속 스크립트 넣기
1. 빈 GameObject를 만들고(예: `Stage`) 이 씬에 맞는 스크립트를 붙인다. 예: 튜토리얼 씬은 `Tutorial`, 인스타 씬은 `FeedApp`.
2. 씬에 **하나만** 있어야 한다. `GameManager`가 씬 로드 직후 `FindAnyObjectByType<StageBase>()`로 찾아 `StageManager`에 스테이지 종류를 전달한다. 없으면 `현재 씬에 StageBase가 없습니다.` 오류가 나고 스테이지가 시작되지 않는다.

### 2-2. StageManager 설치
`StageManager.prefab`을 씬에 드래그한다. 프리팹에는 Fall Effect(낙하 데미지 에셋)만 연결돼 있고, 나머지 세 배열은 **비어 있어서 씬에서 직접 연결**해야 한다.

### 2-3. 체크포인트 설치
1. `Checkpoint.prefab`을 코스 진행 순서대로 배치한다. 자식 `RespawnPoint`의 위치·회전이 스폰 또는 복귀 위치다. (바닥에 묻히지 않게 약간 위에 둔다.)
2. 프리팹의 Player Layer Mask는 이미 `Player` 레이어로 설정돼 있다. 트리거 크기는 기본 3x2x3이니 코스 폭에 맞게 조정한다.
3. 씬의 `StageManager` → **Checkpoints** 배열에 **진행 순서대로** 넣는다. 배열 순서가 곧 진행도(앞 칸일수록 먼저 지나가는 구간)다. 낙하하면 지금까지 도달한 체크포인트 중 **가장 뒤 번호**로 복귀한다.

### 2-4. 라운드 체크포인트 지정 (첫 체크포인트 = 최초 스폰 위치)
- **Round Checkpoint Indices**는 "라운드 i의 시작이 되는 체크포인트가 Checkpoints 배열의 몇 번인가"를 적는 배열이다.
- **Element 0에 `0`을 넣는다.** 첫 번째 체크포인트(Checkpoints[0])가 라운드 0이 되고, **씬에 처음 들어갈 때의 플레이어 스폰도 이 위치**에서 한다.
- 이후 라운드를 만들려면 뒤에 체크포인트 번호를 이어서 적는다. 예: `[0, 3, 6]` → 라운드 0은 0번, 라운드 1은 3번, 라운드 2는 6번 체크포인트. 라운드 체크포인트에 처음 도달하면 그 시점의 체력·배터리가 저장되고, **사망 시 그 라운드의 체크포인트에서 저장된 체력·배터리로 재스폰**된다.
- 라운드 체크포인트가 아닌 일반 체크포인트는 낙하 복귀 위치로만 쓰인다.

### 2-5. FallZone 설치
1. `FallZone.prefab`을 맵 **아래쪽**에 둔다. 기본 크기는 120x2x120이므로 맵 크기에 맞게 늘린다. Player Layer Mask는 `Player`로 설정돼 있다.
2. `StageManager` → **Fall Zones** 배열에 넣는다. 낙하 구역이 여러 개여도 된다.
3. 닿으면 `StageManager`의 **Fall Effect**(`Map/Effect_Fall_Damage10.asset`, 현재 HP 10 감소)가 적용되고, 생존하면 가장 뒤에 도달한 체크포인트로 이동한다. HP가 0이 되면 이동 대신 사망 처리(재스폰)된다. 데미지를 바꾸려면 에셋의 `Amount`를 수정하거나 다른 Effect SO를 연결한다.

### 2-6. PlayerSpawner 설치
- `PlayerSpawner.prefab`을 씬에 둔다(위치는 무관, 스폰 위치는 `StageManager`가 정한다).
- `Player Prefab`과 `Player Effects`(시작 HP·배터리 등 초기화 Effect)가 연결돼 있는지 확인한다. 초기 HP가 0이면 첫 라운드 스냅샷 저장에 실패해 스테이지가 시작되지 않는다.

## 3. 스테이지가 시작되는 흐름
1. 씬 로드 → `GameManager`가 `StageManager`와 `StageBase`를 찾아 스테이지 종류를 전달
2. `PlayerSpawner.Start` → `StageManager.GetSpawnPoint()`(라운드 0의 체크포인트 `RespawnPoint`)에 플레이어 생성 + 초기 Effect 적용
3. `GameManager`가 첫 라운드 스냅샷 저장 → HUD 연결 → `StageManager.StartStage()` → `StageBase.StartStage()` 호출

## 4. 확인 체크리스트
- [ ] 콘솔에 `현재 씬에 StageBase가 없습니다` / `체크포인트 목록이 없습니다` / `라운드 시작 체크포인트 설정이 없습니다` 오류가 없다.
- [ ] Play하면 Checkpoints[0]의 `RespawnPoint`에 플레이어가 생성된다.
- [ ] 체크포인트를 지나 FallZone으로 떨어지면 HP가 줄고 가장 최근 체크포인트로 돌아온다.
- [ ] HP가 0이 되면 라운드 체크포인트에서 재스폰되고 체력·배터리가 저장값으로 복원된다.

## 5. 주의사항
- 플레이어의 콜라이더가 `Player` 레이어여야 체크포인트와 낙하 구역이 감지한다. 프리팹을 바꿨다면 Layer Mask를 같이 확인한다.
- `Map/Gimmicks`의 옛 `RespawnZone`/`PlayerRespawner`를 같은 씬에 두면 낙하·재스폰이 충돌한다. 같이 쓰지 않는다.
- 겹치는 FallZone에 동시에 닿으면 같은 프레임에 데미지가 두 번 들어갈 수 있다. 낙하 구역은 겹치지 않게 둔다.
- 낙하 이동 직후 `CharacterMotor`에 남은 낙하 속도는 아직 초기화하지 않는다(알려진 이슈).
- `Checkpoints`와 `Fall Zones` 배열에는 **씬에 배치한 인스턴스**를 넣는다. 프리팹 에셋을 넣으면 안 된다.
