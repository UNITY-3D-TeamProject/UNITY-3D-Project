# StageManager 중심의 남은 작업 범위 확인

- 작업 요약: 현재 StageManager, GameManager, PlayerSpawner와 최근 로비 복귀 논의를 읽고 StageManager만 수정하면 되는지 확인했다. 런타임 코드는 수정하지 않았다.
- 방법/접근: 실제 코드의 스테이지 시작·종료 호출 경로와 복귀 위치 저장·사용 경로를 확인했다.
- 결정 사항: 다음 주요 구현 대상은 StageManager이지만 현재 로비 복귀 흐름까지 완료하려면 GameManager와 PlayerSpawner의 남은 연결도 필요하다.
- 현재 상태 및 이슈: StageManager의 StartStage/EndStage는 호출 경로가 없고 완료·실패 이벤트와 진행도 갱신도 미구현이다. GameManager의 전환 전 이벤트 구독, 목적지 스테이지 저장, 로드 후 TakeCurrentStage 호출은 아직 주석 또는 미연결 상태다. PlayerSpawner는 spawnPoint를 선택하지만 Instantiate에 transform.position/rotation을 사용하여 선택 위치가 적용되지 않는다. SpawnStage nullable 프로퍼티와 ClearSpawnStage는 적용되어 있다.
- 다음 할 일: PlayerSpawner의 Instantiate에 선택한 spawnPoint를 적용하고, 실제 씬 전환 인터페이스가 확정되면 GameManager 이벤트를 연결한다. StageManager의 시작·완료·실패 및 진행도 관리 흐름을 구현한다. 이번 검토는 정적 코드 확인이며 Unity 실행 검증은 하지 않았다.
