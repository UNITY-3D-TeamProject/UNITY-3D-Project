---
id: intent-025
title: 몬스터 종류별 DB SO로 초기값 적용 + 개체마다 MoveSpeed 비율 오차
part: Spawner (Core)
status: open   # open | resolved
created: 2026-10-15
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- 같은 종류 몬스터는 같은 능력치 데이터·스포너 Effect를 받아 MoveSpeed가 모두 같다.
- 여러 마리가 플레이어를 쫓으면 같은 속도로 줄지어 따라와 단조롭다.
- 몬스터 종류(Melee, Ranged, 특수몹)별로 다른 초기값을 줄 곳이 없다. `SOAttributeData_Enemy`를 같이 쓰고 CurrentHp가 0이라 스포너 Effect가 대신 채운다.
- `MediatorBase`의 `InitValue()`가 주석 처리돼 SO 초기값은 모터로 전달되지 않는다(`SetValue`로 바뀔 때만 전달).

## 기대 결과 (Proposed outcome)
- `MonsterSpawner`에 `_moveSpeedVariance`(비율 오차 0~1) 칸을 둔다. 소환 직후 Effect 적용 다음에 MoveSpeed를 `값 × Random(1 - 오차, 1 + 오차)`로 덮어쓴다.
- 사용자 결정으로 MoveSpeed만 다룬다. 체력·공격력 오차는 필요해질 때 따로 정한다.
- SO를 개체 수만큼 만들 필요가 없다.
- 몬스터 종류별 DB SO(`SOMonsterData`: 몬스터 프리팹 + 어트리뷰트 이름·값 목록) 3개를 두고, 스포너는 소환한 프리팹으로 DB를 찾아 `SetValue`로 값을 넣는다. 순서: DB 값 → 스포너 추가 Effect → MoveSpeed 오차.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 맵 담당(스포너 배치), 몬스터 이동
- 어떤 시스템/모듈이 건드려지는가: `Scripts/Core/MonsterSpawner.cs`, `Scripts/Core/SOMonsterData.cs`(신규), `ScriptableObjects/MonsterData/`(신규 에셋 3개), `Prefabs/System/MonsterSpawner.prefab`
- 몬스터 이동 속도는 MovePrefab의 MoveMediator가 `MoveSpeed` 변경 콜백으로 CharacterMotor.Speed에 넘긴다 (NavMeshAgent는 경로만).

## 제약 (Constraints)
- `Scripts/Core/` 코드만 수정한다.
- 오차가 0이면 기존과 똑같이 동작한다.
- 기존 Monster Effects 칸은 스포너별 추가 Effect용으로 유지한다.
- DB 값을 매 소환마다 Set으로 넣으므로 풀 재사용 시에도 배율이 누적되지 않는다.

## 열린 질문 (Open questions)
- 없음 (나중에 체력 오차가 필요하면 MaxHp·CurrentHp 연동을 새로 정한다)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
