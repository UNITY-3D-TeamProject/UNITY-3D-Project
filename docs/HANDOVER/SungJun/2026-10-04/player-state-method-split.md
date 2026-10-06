# 플레이어 참조 연결과 저장값 복원/삭제 분리

## 작업 요약 및 방법
- 사용자 승인에 따라 PlayerState.InitializePlayerState를 SetCurrentPlayer, RestoreSavedAttributes, ClearSavedAttributes로 분리했다.
- GameManager.CompletePlayerSpawn에서 참조 연결 후 씬 이동이면 복원, 새 게임/부활이면 저장값만 삭제하고 OnPlayerSpawned를 알린다.
- 스크립트 수정은 Core/GameManager.cs와 Core/PlayerState.cs 두 파일로 제한했다.

## 결정 사항
- PlayerState는 스폰 이유나 shouldRestore 인자를 받지 않는다. 복원/삭제 결정은 GameManager가 담당한다.
- ClearSavedAttributes는 보관한 딕셔너리만 비워 스포너의 Effect 적용값을 유지한다.
- SaveCurrentAttributes와 PlayerSpawner의 동작은 유지한다.

## 현재 상태 및 이슈
- git diff --check 통과. 이전 InitializePlayerState 참조가 코드/씬/프리팹/에셋에 남지 않았음을 확인했다.
- 연결 → 복원 또는 저장값 삭제 → 스폰 알림 순서를 정적으로 확인했다.
- Unity 컴파일 및 실행 검증은 수행하지 않았다.

## 다음 할 일
- 기존 intent-012의 Inspector 배선 및 스폰 분기 실행 검증을 이어간다.

## 후속 확인 — CompletePlayerSpawn 이름 및 분기 유지
- 사용자가 CompletePlayerSpawn 이름이 적절하면 현재 방식으로 진행하도록 요청했다.
- 현재 Core 세 파일을 재확인한 결과 새 게임/부활의 Effect 적용, 씬 이동의 저장값 복원, 최종 스폰 이벤트 순서가 이미 반영돼 있어 런타임 코드는 추가 수정하지 않았다.
- git diff --check 통과. Unity 컴파일/실행 검증과 실제 사망 후 재생성 연결은 여전히 미완료다.
