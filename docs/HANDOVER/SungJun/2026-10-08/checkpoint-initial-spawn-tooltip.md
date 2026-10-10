# 체크포인트 최초 스폰 툴팁 및 플레이어 캐싱 확인

- 작업 요약 및 방법: 현재 StageManager.GetInitialSpawnPoint와 PlayerSpawner 호출을 확인하고 _checkpoints에 최초 스폰 선택 기준을 설명하는 Tooltip을 추가했다.
- 결정 사항: 최초 스폰은 _checkpoints[_roundCheckpointIndices[0]].RespawnPoint다. 배열 0번을 사용하려면 라운드 매핑 첫 값을 0으로 설정해야 한다. 기존 선택 로직은 유지한다.
- 현재 상태: HandlePlayerSpawned의 _playerAttributeSet 저장 및 부모 CharacterController 캐싱 코드는 이미 들어 있다. 중복 추가할 필요가 없다.
- 검증: 소스 확인 및 git diff --check. 툴팁만 추가했으며 빌드/Unity Play 검증은 재실행하지 않았다.
- 다음 할 일: Inspector 컴포넌트 재연결과 라운드 첫 값 0 설정 후 최초 스폰/도달/재접촉 검증.