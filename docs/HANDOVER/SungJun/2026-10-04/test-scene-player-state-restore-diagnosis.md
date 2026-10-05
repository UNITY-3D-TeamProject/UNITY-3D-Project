# KSJ 테스트 씬 왕복 시 상태 초기화 원인

## 작업 요약 및 방법
- 두 테스트 씬에 배치된 TestSceneTransitionBox의 전환 경로를 GameManager와 PlayerSpawner의 스폰 이유 분기에 대조했다.
- 원인 진단 요청으로 런타임 코드는 수정하지 않았다. Play Mode 검증은 미실행이다.

## 결정 사항
- 테스트 전환 코드의 SaveCurrentAttributes 단독 호출을 GameManager.PrepareSpawn(GameManager.EPlayerSpawnReason.SceneTransition) 호출로 교체하는 최소 수정안을 안내한다.
- 이 호출은 현재값 저장과 스폰 이유 변경을 함께 처리한다.

## 현재 상태 및 이슈
- TestSceneTransitionBox는 SaveCurrentAttributes 후 SceneManager.LoadSceneAsync를 직접 호출한다. SceneLoader.OnBeforeSceneChange 이벤트 경로를 거치지 않는다.
- 새 게임에서 테스트 씬 왕복 시 SpawnReason은 NewGame으로 남는다. 다음 씬 스포너가 초기 Effect를 적용하고 CompletePlayerSpawn이 복원 대신 ClearSavedAttributes를 호출한다.
- 따라서 저장 호출 자체는 존재하지만 복원 분기에 들어가지 않아 저장값이 삭제된다.
- 직전 검토 이후 두 씬의 스폰 Effect가 연결됐고, Effect 대상/값은 CurrentHp=100, CurrentBattery=100, JumpPower=5로 설정돼 있다.

## 다음 할 일
- 테스트 전환 호출 한 줄을 위 최소 수정안으로 변경한다.
- HP/배터리/Heat 변경 후 두 방향 왕복 시 값 유지 여부를 실행 검증한다.

## 변경된 파일
- 이 작업 기록과 HANDOFF_POINTER.md.
