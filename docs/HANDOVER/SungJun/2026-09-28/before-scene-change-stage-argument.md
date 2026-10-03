# HandleBeforeSceneChange의 스테이지 인자 예시

- 작업 요약: 현재 GameManager에 사용자가 추가한 SpawnStage 프로퍼티를 확인하고, 전환 직전 메서드의 인자와 보관 코드를 제안했다. 런타임 코드는 변경하지 않았다.
- 방법/접근: SpawnStage를 EStageType?으로 선언하고 HandleBeforeSceneChange(EStageType? destinationStage)에서 능력치를 저장한 뒤 인자가 있을 때만 SpawnStage를 갱신하는 예시다.
- 제안 사항: 전환 담당자가 스테이지 진입에는 목적지 EStageType을, 로비행 전환에는 null을 전달한다. null 인자를 받았을 때 기존 SpawnStage는 유지한다. 이벤트 방식으로 연결한다면 이벤트 인자 형식도 EStageType?으로 일치시켜야 한다.
- 현재 상태 및 이슈: 현재 코드의 SpawnStage는 일반 EStageType이며 초기값이 Tutorial이다. 예시의 nullable 프로퍼티는 최초 로비 진입의 값 없음과 Tutorial 복귀를 구분한다. 실제 전환 담당 이벤트는 아직 연결되지 않았고 예시는 미적용이다.
- 다음 할 일: 실제 전환 이벤트에 연결하고 로비 스폰 완료 후 초기화 메서드를 통해 보관값을 비우는 흐름을 구현한다.
