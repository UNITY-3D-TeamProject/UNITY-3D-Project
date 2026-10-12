---
id: intent-021
title: 미로 2번째 단계의 시간 제한 추격자 가비지 컬렉터(GC)
part: AI / Enemy
status: open   # open | resolved
created: 2026-10-12
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

> 번호 메모: `develop`에는 016까지 있지만 다른 브랜치에서 017(군중)·018~019(김성준)·020(약점 치명타)을 쓰고 있어 021로 잡았다.

## 문제 (Problem)
미로 맵(`Map_3_File`)의 2번째 미로(라운드 2)에는 플레이어를 압박하는 요소가 없어 시간을 끌어도 손해가 없다. 기획은 "가비지 컬렉션"이 등장해 시간 타이머 역할을 하길 원한다. 처음엔 느리게 돌아다니다가 시간이 지날수록 빨라지고, 플레이어를 발견하면 공격한다. 기획상 3번 맞으면 HP가 0이 되며, 이 횟수는 n으로 바꿀 수 있어야 한다.

## 기대 결과 (Proposed outcome)
- 라운드 2 시작점에 닿으면 2번째 미로 안에 무적 GC 1마리가 생긴다. 라운드 3에 들어가면 사라지고, 라운드 2에서 플레이어가 죽으면 처음 상태로 다시 만들어진다.
- GC는 웨이포인트를 순찰하고, 플레이어를 보면 추격한다. 사거리에 들어오면 한 번 때리고 잠깐 멈춘다. 놓치면 마지막으로 본 곳을 수색한 뒤 순찰로 돌아간다.
- 이동 속도는 생긴 순간부터 시간에 따라 `AnimationCurve`대로 오른다(시작 2~3 → 60초 뒤 6~8에서 고정, 플레이어 5보다 빠름). 순찰할 때와 추격할 때 속도가 같다.
- 한 번에 플레이어 **스폰 시작 HP(50) ÷ n**(올림)만큼 피해를 준다. n=3이면 `AttackDamage`가 17이다. 처음엔 최대 HP ÷ n으로 정했으나, Phase 2.5에서 스폰 HP가 50이라는 것이 확인되어 바꿨다.
- 라운드 2 중 살아서 낙하 복귀하면 GC를 리셋하지 않는다.
- (2026-10-12 추가, 테스트 피드백) 무작위 순찰만으로는 플레이어가 GC를 한 번도 못 만나고 지나갈 수 있다. 그래서 **압박 가지**를 BT에 넣는다. 플레이어를 N초(기본 20) 동안 못 보면 플레이어에게 가장 가까운 웨이포인트로 이동하고, 그 뒤 `Cooldown`(기본 20초) 동안은 일반 순찰을 한다. 판단은 BT 그래프가 하고, 코드는 조회·이동 API만 제공한다. 참고한 사례는 Alien: Isolation의 Director로, 정확한 위치가 아니라 근처 구역만 알려 주는 방식이다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(미로 라운드 2), 기획(곡선·n·멈춤 시간 조절), 맵 담당(웨이포인트 제공)
- 어떤 시스템/모듈이 건드려지는가:
  - 신규: `AI/WaypointPatrolRoute`, `AI/GarbageCollector/SpeedOverTime`, `AI/GarbageCollector/GarbageCollectorSpawner`, `AI/BT/AIAttackOnceAction`, `AI/BT/AITimeSinceTargetSeenCondition`, `AI/BT/AIMoveNearTargetAction` (범용 노드)
  - 변경: `AI/AIController`
    - 순찰 지점 공급 컴포넌트가 붙어 있으면 그걸 쓰는 분기 1개를 추가했다.
    - 목적지 제한 시간 상수를 Inspector 값으로 바꿨다(기본 10 유지).
    - 압박용으로 `TimeSinceTargetSeen`과 `MoveToPatrolPointNear`를 추가했다.
  - 변경: `WaypointPatrolRoute.TryGetNearestWaypoint`, 스포너가 블랙보드 `Player` 변수를 주입한다(생성 시와 `GameManager.OnPlayerSpawned` 시점).
  - 읽기만: `StageManager` (`CurrentRoundIndex`, `GetSpawnPoint()`, `OnPlayerRespawnRequested`)
  - 에셋(`[USER]`): `GarbageCollector.prefab`, 어트리뷰트 SO, BT 그래프 `GarbageCollectorAI`, 테스트 씬 2개, `Map_3_File`에 스포너 배치

## 제약 (Constraints)
- `character-architecture.md`의 중재자 구조를 따른다. 속도 변경은 `CharacterMediator.SetAttribute`로 한다. 레인 속도를 이렇게 넣은 군중의 선례(`PlayerFacade`)와 같은 예외다.
- 다른 팀원 코드(`StageManager`, `Map/Maze/*`)는 고치지 않는다. 웨이포인트 생성·NavMesh 런타임 베이크·다리 Not Walkable·벽 레이어 변경은 **맵 담당에게 요청**한다.
- 웨이포인트의 정식 계약은 `IReadOnlyList<Vector3>`이고, 형식은 맵 담당이 정한다. 그 전까지는 스포너와 `WaypointPatrolRoute`가 임시로 `Transform[]`을 받는다.
- GC 전용 BT 노드를 만들지 않는다. 새 노드는 어느 AI든 쓸 수 있는 `AIAttackOnceAction` 하나뿐이고, 멈춤은 기본 `Wait` 노드로 조립한다.
- `.unity` / `.prefab` / `.asset`은 AI가 편집하지 않는다(`[USER]`). 풀·DontDestroyOnLoad·순간이동 리셋은 쓰지 않는다.
- 범위 밖: 애니메이션·공격 연출·피격 반응, 마리 수 증가, 웨이포인트 생성, EditMode 자동 테스트

## 열린 질문 (Open questions)
- (없음 — Phase 1 grilling에서 모두 결정. 근거는 `docs/planning/garbage-collector.md`, 개인 기획 폴더)
- 맵 담당이 정할 웨이포인트 형식 → 정해지면 스포너의 임시 입력만 교체
- **시스템 팀원 확인 필요**: GC 스포너는 AI 파트가 만든 GC 전용 스포너다.
  - 시스템 팀의 `SpawnerBase`(`Core/SpawnerBase.cs`)를 상속하지 않은 이유는 두 가지다. 생성 위치가 Transform이 아니라 계산한 웨이포인트 좌표이고, 라운드에 따른 생성·제거와 재생성이 필요하다.
  - 지금 구조로 유지한다. 팀원이 `SpawnerBase`에 맞추길 원하면 그때 바꾼다.
- 후속 작업: 존재 알림(GC 이동음·근접 경고음, 라운드 2 진입 연출). 압박만으로는 플레이어가 GC가 있다는 것을 미리 알기 어렵다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
