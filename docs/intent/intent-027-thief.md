---
id: intent-027
title: 미로 라운드 3의 도망치는 적 도둑(Thief)
part: AI / Enemy
status: open   # open | resolved
created: 2026-10-16
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

> 번호 메모: grilling에서는 024로 적었지만, 김성준 님 폴더(`docs/HANDOVER/SungJun/intent/`)에서 024·025를 이미 쓰고 있어 026으로 잡았다. 이후 develop에 `intent-026-enemy-hit-stun`이 먼저 들어와 027로 바꿨다(2026-10-16).

## 문제 (Problem)
미로 맵(`Map_3_File`)의 3번째 미로(라운드 3)에는 할 일이 없다. 기획은 이 라운드를 "미로 안에서 도망 다니는 도둑 20마리를 제한 시간 안에 전부 잡아 음악 자료를 회수하는 라운드"로 만들고 싶어 한다. 지금 적은 쫓아오는 적(근접·원거리·GC)뿐이고, 플레이어를 보면 **달아나는** 적이 없다. 기존 원거리 적의 후퇴 계산(`RetreatPointFinder`)은 "도착 후 플레이어가 보여야 함" 조건과 직선 후보 때문에 미로에서 도망치는 데 쓸 수 없다.

## 기대 결과 (Proposed outcome)
- 라운드 3 시작점에 닿으면 3번째 미로 안에 도둑 20마리가 웨이포인트 중 무작위로 나타난다.
  - 시작점에서 25m 안에는 나타나지 않는다.
  - 한 웨이포인트에 최대 3마리까지 나타난다.
- 도둑은 웨이포인트를 순찰한다. 플레이어를 보면 웨이포인트를 기준으로 도망치고, 놓치면 순찰로 돌아간다. 공격은 하지 않는다.
- 도망칠 곳은 도둑 주변 NavMesh 위에서 무작위로 뽑은 지점 중 다음 조건을 모두 통과한 곳에서 고른다. 고를 때는 플레이어에게서 먼 상위 절반 중 무작위로 고른다.
  - 처음에는 웨이포인트 중에서 골랐으나, 미로에는 웨이포인트를 많이 놓을 수 없어 웨이포인트끼리 경로가 최소 33m라 25m 안 후보가 늘 0개였다(도둑이 멈춰 있음). 그래서 NavMesh 샘플링으로 바꿨다(2026-10-17 결정).
  - 경로 25m 이내
  - 경로가 플레이어 근처를 지나지 않음
  - 지금보다 멀어짐 (이 조건을 통과한 후보가 없으면 이 조건만 빼고 다시 고른다)
- 도망치는 동안 0.25초마다 남은 경로가 플레이어 근처를 지나는지 확인하고, 지나게 되면 우회할 곳을 다시 고른다. 고른 뒤 플레이어가 경로 쪽으로 움직여 도둑이 플레이어에게 달려들던 문제 때문이다(2026-10-17 결정).
- 도망칠 곳이 없으면 멈추고 1초 뒤 다시 찾는다.
- 플레이어 총에 맞아 HP가 0이 되면 잡힌다. 스포너가 잡힌 수를 세고, 잡힌 위치·집계·전부 잡힘을 이벤트로 알린다. 음악 자료 아이템, 문 열기, UI는 다른 담당이 이 이벤트에 붙인다.
- 라운드 3에서 플레이어가 죽거나 시간이 초과되어 재시작하면 도둑과 집계가 처음으로 돌아간다. 단 전부 잡은 뒤에는 돌아가지 않는다.
- GC 스포너와 도둑 스포너는 라운드 감지·사망 리셋 같은 공통 부분을 추상 부모 `RoundSpawnerBase`로 공유한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(미로 라운드 3), 기획(마리 수·도망 거리·스폰 규칙 조절), 맵·스테이지·UI 담당(이벤트 구독)
- 어떤 시스템/모듈이 건드려지는가:
  - 신규
    - `AI/RoundSpawning/RoundSpawnerBase`
    - `AI/FleePointFinder`
    - `AI/BT/AIFleeAction`
    - `AI/Thief/ThiefSpawner`
  - 변경
    - `AI/GarbageCollector/GarbageCollectorSpawner`: 공통부를 부모로 옮긴다. 동작은 같다.
    - `AI/AIController`: 도망 API를 추가한다.
    - ~~`AI/WaypointPatrolRoute`: 목록 조회 API를 추가한다.~~ → 도망 후보를 NavMesh 샘플링으로 바꾸면서 쓰는 곳이 없어 되돌렸다(2026-10-17).
  - 읽기만: `StageManager`(`CurrentRoundIndex`, `IsRunning`, `GetSpawnPoint()`, `OnPlayerRespawnRequested`), `GameManager.OnPlayerSpawned`, `CharacterMediator.OnDeath`/`OnDeathEnded`
  - 에셋(`[USER]`): `Thief.prefab`, 도둑 어트리뷰트·초기값 에셋, BT 그래프 `NPC_Thief`, 테스트 씬, `Map_3_File`에 스포너 배치

## 제약 (Constraints)
- 예산/기한: 구현(Phase 3)은 AI 애니메이션 코드가 푸시된 뒤 시작한다. → 2026-10-16 develop(PR #83)에 들어와 조건이 풀렸다.
- 지켜야 할 정책
  - `character-architecture.md`의 중재자 구조를 따른다.
  - BT 노드는 `AIController`의 공개 API에만 묻는다.
  - 다른 팀원 코드(`StageManager`, `Map/Maze/*`, `MediatorBase`)는 고치지 않는다.
  - 웨이포인트 생성, 3번째 미로 NavMesh, 출구 문 판정, 타이머·실패 처리는 **담당자에게 요청**한다.
- 쓰지 말아야 할 것
  - `.unity` / `.prefab` / `.asset` / `.controller` 직접 편집(AI는 하지 않는다)
  - 풀, DontDestroyOnLoad
  - 도둑끼리 공유하는 예약 상태(몰림이 심하면 Play 검증 후 다시 검토)

## 열린 질문 (Open questions)
- ~~도둑 초기값 적용 방식~~ → **몬스터 DB(`SOMonsterData_Thief`)**. 스포너가 생성 직후 `SetValue`로 넣는다(2026-10-16 결정).
- ~~Phase 2.5 설계 항목 V1~V3~~ → V1 조건 완화(멈춤이 심하면 웨이포인트 증설 요청), V2 제외 반경 25m, V3 재선택 없이 관찰(2026-10-16 결정).
- ~~애니메이션 연결 방식~~ → develop의 `EnemyAnimator` + `EnemyAnimationMediator`를 그대로 쓴다. 사망 끝 알림은 Animator의 `Death` 상태가 끝나면 나온다. 잡힌 도둑은 `ThiefSpawner`가 `OnDeathEnded` 뒤 `Destroy`하고, 프리팹에서 `EnemyPoolReturner`는 뺀다(2026-10-16 결정).
- 맵 담당이 정할 웨이포인트 형식: 정해지면 스포너의 임시 `Transform[]` 입력만 교체한다.
- 근거 기록: `docs/planning/thief.md` (개인 기획 폴더, Round 1~3)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
