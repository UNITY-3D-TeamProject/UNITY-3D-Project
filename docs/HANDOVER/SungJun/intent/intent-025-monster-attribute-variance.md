---
id: intent-025
title: 같은 종류 몬스터라도 개체마다 MoveSpeed에 비율 오차 주기
part: Spawner (Core)
status: open   # open | resolved
created: 2026-10-15
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- 같은 종류 몬스터는 같은 능력치 데이터·스포너 Effect를 받아 MoveSpeed가 모두 같다.
- 여러 마리가 플레이어를 쫓으면 같은 속도로 줄지어 따라와 단조롭다.

## 기대 결과 (Proposed outcome)
- `MonsterSpawner`에 `_moveSpeedVariance`(비율 오차 0~1) 칸을 둔다. 소환 직후 Effect 적용 다음에 MoveSpeed를 `값 × Random(1 - 오차, 1 + 오차)`로 덮어쓴다.
- 사용자 결정으로 MoveSpeed만 다룬다. 체력·공격력 오차는 필요해질 때 따로 정한다.
- SO를 개체 수만큼 만들 필요가 없다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 맵 담당(스포너 배치), 몬스터 이동
- 어떤 시스템/모듈이 건드려지는가: `Scripts/Core/MonsterSpawner.cs`, `Prefabs/System/MonsterSpawner.prefab`(MoveSpeed 항목 기본값)
- 몬스터 이동 속도는 MovePrefab의 MoveMediator가 `MoveSpeed` 변경 콜백으로 CharacterMotor.Speed에 넘긴다 (NavMeshAgent는 경로만).

## 제약 (Constraints)
- `Scripts/Core/` 코드만 수정한다.
- 오차가 0이면 기존과 똑같이 동작한다.
- 풀 재사용 시 같은 어트리뷰트를 Set하는 Effect가 먼저 기본값을 넣어야 배율이 누적되지 않는다 (현재 MoveSpeed Set Effect 있음).

## 열린 질문 (Open questions)
- 없음 (나중에 체력 오차가 필요하면 MaxHp·CurrentHp 연동을 새로 정한다)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
