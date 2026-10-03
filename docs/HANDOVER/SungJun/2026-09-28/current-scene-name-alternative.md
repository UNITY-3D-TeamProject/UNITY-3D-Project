# 현재 씬 이름 조회를 사용하는 대안

- 작업 요약: 씬 enum 전달 없이 현재 씬 이름을 가져올 수 있는지 설명했다. 런타임 코드는 변경하지 않았다.
- 방법/접근: Unity 공식 GetActiveScene 및 sceneLoaded 문서를 확인했다. 현재 활성 씬 이름은 SceneManager.GetActiveScene().name으로 조회할 수 있다. 로드 후 콜백에서는 전달받은 Scene의 name을 사용할 수 있다.
- 결정 사항: 전환 전 조회는 떠나는 씬이며 목적지 정보와 구분한다. 목적지를 전환 전에 알아야 한다면 전환 담당자가 전달해야 한다. 로드 후 스테이지 판정만 필요하면 기존 HandleSceneLoaded의 scene.name을 사용하여 공용 씬 enum 전달을 생략할 수 있다.
- 제안 사항: 로비 복귀 위치 기록만 필요하다면 스테이지를 떠나기 직전에 현재 씬 이름을 EStageType으로 매핑해 SpawnStage에 저장하는 대안도 가능하다. 어느 방식이든 씬 이름과 스테이지 타입 간 대응 규칙은 필요하다.
- 현재 상태 및 이슈: 활성 씬 조회와 실제 목적지 정보는 서로 다르다. 예시는 현재 단일 씬 전환 논의를 전제로 하며 구현·실행 검증은 하지 않았다.
- 다음 할 일: 전환 전에 목적지 판정이 필요한지, 로드 후 판정 또는 떠나는 스테이지 기록이면 충분한지에 따라 조회 시점을 선택한다.
- 참고: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.SceneManager.GetActiveScene.html
- 참고: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html
