---
id: intent-016
title: StageManager 체크포인트 컴포넌트 연결과 도달 처리
part: Stage / Player State
status: open
created: 2026-10-08
resolved: null
---

## 문제 (Problem)
CheckpointTrigger는 존재하지만 StageManager는 SCheckpoint[]를 사용해 도달 이벤트와 연결되지 않는다.

## 기대 결과 (Proposed outcome)
- CheckpointTrigger[]와 RespawnPoint로 최초 스폰 위치를 조회한다.
- 활성화/비활성화에 맞춰 도달 이벤트를 구독/해제하고 현재 플레이어·플레이 상태·생존 여부를 확인한다.
- 일반 진행도는 실제 도달한 최대 인덱스를 유지한다. 다음 라운드 시작점에 실제 도달하면 최초 HP·배터리 저장 성공 후 라운드를 갱신한다.
- 재접촉 시 진행도 역행과 최초 스냅샷 덮어쓰기를 방지한다.

## 영향 범위 (Affected users and systems)
- Core.Stage.StageManager 및 기존 PlayerState 스냅샷 API.
- 자료형 변경 후 Inspector 체크포인트 참조 재연결이 필요하다.

## 제약 (Constraints)
- 이번 구현은 사용자 요청 1~3번이다. Inspector 연결/Play 검증은 다음 단계다.
- 게임 코드 수정은 Core 안에서 수행한다. 이동·사망·낙하 복구는 후속 범위다.
- 일반 진행도보다 낮더라도 다음 라운드 시작점을 밟으면 라운드 판정을 수행한다.
- 기존 사용자 변경 및 StageManager의 CP949 인코딩을 유지한다.

## 열린 질문 (Open questions)
- 이번 범위의 차단 질문 없음. 이동 초기화/부활 API는 후속 단계에서 확인한다.

## 해결 기록 (Resolution)
- 코드 구현 완료: CheckpointTrigger[] 전환, 최초 스폰 null 검사, 이벤트 구독/해제, 현재 CharacterController 및 HP/사망/Playing 상태 확인, 일반 최대 진행도와 다음 라운드 최초 저장 연결.
- dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet: 오류 0, 기존 경고 5. git diff --check 통과.
- 사용자 코드 검토 및 Inspector 재연결/Unity Play 검증 대기 상태이므로 open을 유지한다. 관련 커밋/PR 없음.