# 목적지 씬 정보와 스테이지 판정 책임

- 작업 요약: 씬 전환 담당자가 목적지 씬 정보만 전달하고 GameManager가 스테이지 여부를 판정하는 구조를 검토했다. 런타임 코드는 변경하지 않았다.
- 방법/접근: 현재 GameManager와 EStageType, 씬 관련 참조 및 Build Settings를 확인했다. Unity 공식 GetSceneByName 문서에서 로드된 씬을 조회하는 API임을 확인했다.
- 제안 사항: 전환 담당자는 목적지 씬 식별자(현재 인터페이스에 맞는 씬 이름·경로 또는 공용 씬 enum)를 씬 전환 직전 이벤트로 전달한다. GameManager가 명시적인 씬 식별자→EStageType 매핑으로 스테이지 여부를 판정한다. 목적지가 스테이지면 SpawnStage를 갱신하고 로비면 기존 값을 유지한다. 로비 스폰 후 ClearSpawnStage로 소비한다.
- 결정 사항: 스테이지 판정과 복귀 정보 보관은 GameManager의 책임으로 두는 방향을 추천한다. 전환 전 인자는 Unity Scene 구조체보다 목적지 식별자가 적합하다. 로드 후에는 기존 sceneLoaded의 Scene 정보를 사용한다.
- 현재 상태 및 이슈: 공용 씬 enum과 목적지 씬 매핑은 현재 확인한 코드에 없고, 스테이지별 실제 씬 식별자 및 전환 이벤트 형식도 미확정이다. 구현·컴파일·실행 검증은 하지 않았다.
- 다음 할 일: 전환 담당자와 목적지 식별자 형식 및 이벤트 호출 시점을 맞추고 GameManager에서 전환 전/로드 후에 같은 매핑 규칙을 사용한다.
- 참고: https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/scenemanagement/scenemanager/getscenebyname
