# SpawnPlayer 추출 범위 안내

- 작업 요약: 현재 PlayerSpawner를 줄 번호와 함께 확인하고 SpawnPlayer로 옮길 범위를 안내했다. 런타임 코드는 변경하지 않았다.
- 방법/접근: Start 본문의 첫 GameManager 조회부터 마지막 로비 여부 검사 및 ClearSpawnStage 호출 블록까지 전체를 새 public SpawnPlayer에 옮긴다. 확인 시점 기준으로 본문은 38~83행이다.
- 결정 사항: Start는 SpawnPlayer 호출만 남긴다. 초기화 검사·위치 선택·생성·AttributeSet 확인·플레이어 등록·로비 기록 소비를 하나의 스폰 흐름으로 유지한다. GetLobbySpawnPoint는 별도 보조 메서드로 유지한다.
- 현재 상태 및 이슈: 추출 범위 안내만 수행했으며 실제 추출 및 재스폰 호출 연결은 미적용이다. 컴파일·실행 검증은 하지 않았다.
- 다음 할 일: 안내 범위대로 분리하고 실제 사망 처리 경로에서 기존 플레이어 제거 후 SpawnPlayer를 호출한다.
