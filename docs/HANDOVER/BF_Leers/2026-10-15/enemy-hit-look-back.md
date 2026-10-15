# 적 피격 시 맞은 쪽 돌아보기

## 1. 작업 요약
플레이어 총알에 맞은 적(순찰·대기 중)이 잠깐 멈추고 맞은 쪽으로 몸을 돌리게 했다. 시야(90° 부채꼴)에 플레이어가 들어오면 기존 Sensor가 그대로 발견한다.

## 2. 방법/접근
- `CharacterMediator.OnHitCallback`의 생존 블록에는 플레이어 방향별 피격 애니메이션이 쓰는 `hitSource`(`GetHitSource`: HitPoint → 없으면 Cursor 위치)가 이미 계산되어 있다. 같은 자리에서 이 위치를 새 이벤트 `OnHitFrom(Vector3)`로 알린다.
- `AIController`가 `OnHitFrom`을 구독한다. 대상이 보이지 않을 때만 `_hitLookTimer`를 켜고, 그동안 `Update`에서 목적지를 유지한 채 이동은 0으로, 회전은 맞은 쪽으로 계속 요청한다. 정체 판정은 이 동안 건너뛴다. 타이머가 끝나면 원래 이동을 이어간다.
- 회전 요청을 한 번만 보내면 이동 중 매 프레임 이동 방향 회전 요청에 덮어써지므로, 짧게 멈추는 방식을 택했다.

## 3. 변경된 파일
- `Assets/_Project/Scripts/Mediator/CharacterMediator.cs`: `OnHitFrom` 이벤트 추가와 호출 1줄 (이세훈 담당 영역, 추가만 함)
- `Assets/_Project/Scripts/AI/AIController.cs`: `_hitLookDuration`(Hit Reaction, 기본 1.0초), `OnEnable` 구독, `OnDisable` 해지와 타이머 초기화, `Update` 멈춤 분기, `OnHitFromCallback`
- `docs/intent/intent-016-hit-context.md`, `docs/intent/INTENT_POINTER.md`

## 4. 결정 사항
- 방향 기준은 HitPoint로 정했다. 적에게도 방향별 피격 애니메이션을 넣을 예정이라 애니메이션과 같은 기준을 쓴다. `-Direction`은 쓰지 않는다.
- 반응은 "돌아보기만(잠깐 멈춤)"이다. BT와 Sensor는 바꾸지 않았다. intent-016 2차의 `IHitController` + 수색 계획은 이 방식으로 대체했다.
- 메디에이터가 직접 몸을 돌리지 않는다. 메디에이터는 위치만 알리고 판단은 조종부가 한다. 플레이어에게 영향이 가고, `LockLookDirection`이 구르기·BT 조준과 충돌하기 때문이다.
- 대상이 보이는 중(전투 중)의 피격은 무시한다. 추격과 사격이 끊기지 않게 하기 위해서다.
- 기본 1.0초: 적 프리팹의 `CharacterRotator._rotateSpeed`가 180°/s라서 0.5초로는 90°밖에 돌지 못한다(Play에서 확인).

## 5. 현재 상태 및 이슈
- 컴파일 에러 0건.
- Play 검증(`Map_4_Security_T`, Melee, BT를 끄고 eval로 피격 시뮬레이션):
  - 이동 중 뒤에서 피격: 1.0초 동안 위치가 고정되고 +z에서 약 -z까지 돌아선 뒤 목적지로 이동을 재개했다.
  - 대상이 보일 때 피격: 타이머가 켜지지 않았다.
  - 실제 총알 발사, BT를 켠 순찰 중 피격, 사망 피격, 풀 재사용은 직접 확인하지 않았다(코드상 사망 피격은 생존 블록 밖이라 발생하지 않고, `OnDisable`에서 타이머를 0으로 둔다).
- 새 직렬화 필드를 추가한 직후에는 에디터 메모리의 프리팹이 이전 기본값(0.5)을 들고 있었다. 프리팹 3개를 강제 재임포트해서 1.0으로 맞췄다(파일 변경 없음).
- 알려진 한계: HitPoint는 트리거에 처음 겹친 순간의 총알 중심이라(`BulletController` TODO, 최대 약 0.33m) 드물게 방향이 뒤집힐 수 있다. 비스듬히 스친 총알은 옆쪽을 볼 수 있다.

## 6. 다음 할 일
- 실제 플레이로 순찰 중인 적을 등 뒤에서 쏴서 돌아본 뒤 전투에 들어가는지 확인
- `CharacterMediator.OnHitFrom` 추가를 이세훈에게 공유
- 적 피격 애니메이션을 붙일 때 같은 `hitSource`를 쓰면 회전과 방향이 맞는다
- 필요하면 HitPoint 정밀화(표면 진입점 계산)
