# PlayerSpawner 로비 복귀 위치 선택 코드 안내

- 작업 요약: 현재 PlayerSpawner와 GameManager를 다시 읽고 로비에서만 위치를 선택하는 공용 스포너 예시를 제공했다. 런타임 코드는 변경하지 않았다.
- 방법/접근: 스포너에 로비 여부와 직렬화 가능한 스테이지 타입/Transform 쌍 배열을 둔다. 기본 위치는 현재 스포너의 Transform이다. 로비용이며 보관값이 있을 때만 일치하는 유효한 Transform을 선택하고, 기존 Instantiate 및 RegisterPlayer 흐름을 유지한다.
- 제안 사항: GameManager의 SpawnStage를 EStageType?으로 바꾸고 public ClearSpawnStage()를 제공한다. 로비 스포너만 생성·등록 후 이 메서드를 호출하고 스테이지 스포너는 기록을 유지한다. 배열이 비어 있거나 대응 위치가 없으면 기본 위치로 생성하는 예시다.
- 현재 상태 및 이슈: 현재 실제 코드의 SpawnStage는 일반 EStageType이고 HandleBeforeSceneChange의 인자 및 이벤트 연결도 아직 미적용이다. 안내한 예시는 nullable 프로퍼티와 앞서 논의한 전환 전 보관 흐름을 전제로 한다. 현재 PlayerSpawner의 전역 네임스페이스는 유지하는 예시로 안내한다. 코드 예시는 실행·컴파일 검증하지 않았다.
- 다음 할 일: 사용자가 예시를 적용한 후 Inspector에서 로비 여부와 타입별 위치를 연결하고, 최초 로비 진입·앱 진입·로비 복귀 시 단일 생성 및 기록 소비를 확인한다.
