---
id: intent-046
title: 보스 B 근접공격 빈도와 피격 회전 복구
part: AI / Boss
status: resolved
created: 2026-10-19
resolved: 2026-10-19
---

## 문제 (Problem)
보스 B가 근접 콤보를 거의 쓰지 않고 돌진을 반복한다. 360도 감지로 플레이어가 보이는 동안 AIController가 피격 돌아보기를 생략한다. SmallBoss 작성 그래프에는 돌진/도약 뒤 쿨타임 설정 연결이 있으나 저장된 런타임 시퀀스에는 후속 노드가 포함되지 않는다.

## 기대 결과 (Proposed outcome)
근접 범위에서는 기존 3타 콤보를 사용하고, 돌진/도약 사이에는 설정된 쿨타임 동안 추격한다. 스킬로 방향이 고정된 동안을 제외하면 피격 시 플레이어 쪽을 바라본다.

## 영향 범위 (Affected users and systems)
- 보스 B AIController 설정 및 피격 회전
- SmallBoss BT 작성/런타임 그래프

## 제약 (Constraints)
- 기존 미커밋 작업을 보존한다.
- 돌진 중 방향 고정과 스킬 Hold를 유지한다.
- 프리팹/BT는 Unity Editor API로 수정한다.

## 열린 질문 (Open questions)
- 없음. 기존 공격 거리 4m와 돌진 6초/도약 8초 쿨타임을 복구하고 실행 검증한다.

## 해결 기록 (Resolution)
- Attack.None을 기존 0.5초 추격 가지에 연결하고 실행 그래프를 재생성해 쿨타임 노드를 복구했다.
- 보스 B에만 전투 중 피격 회전을 켰고 Hold 방향은 유지한다.
- 렌더 메시 베이크로 단절됐던 테스트 씬의 보스 NavMesh를 PhysicsColliders로 다시 만들었다. 데이터는 사용자 지시에 따라 `Assets/Test/Boss/Map_4_Security_Boss_T/NavMesh-05_Boss-PhysicsColliders.asset`에 저장했다(저장 위치 정정).
- 컴파일·기존 공격 선택 테스트 11/11·타일 중심 경로 81/81 통과. Play에서 추격 3초/11.62m, 콤보 3타, 뒤쪽 180도 피격 회전 확인.
- 관련 커밋/PR: 없음. [검증 및 로컬 테스트 씬 적용 사항](../../HANDOVER/BF_Leers/2026-10-19/boss-b-melee-hit-facing.md).
