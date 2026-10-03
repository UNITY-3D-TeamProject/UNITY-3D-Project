# 박스 접촉 씬 전환 테스트

## 작업 요약 및 방법
- Assets/_Project/Scripts/Core/TestSceneTransitionBox.cs 및 meta 추가.
- 박스의 BoxCollider를 트리거로, Rigidbody를 중력 없는 키네마틱으로 자동 설정한다.
- GameManager.CurrentPlayerState의 Transform과 접촉 Collider의 부모 관계를 검사한다. Player 프리팹은 Untagged이며, 자식 이동 오브젝트의 Collider도 감지해야 한다.
- KSJ_Home에서는 KSJ_Lobby로, KSJ_Lobby에서는 KSJ_Home으로 자동 전환한다.
- 씬 로드 가능 여부를 확인한 뒤 SaveCurrentAttributes를 호출하고 LoadSceneAsync(Single)로 전환한다. 로딩 중 같은 박스의 중복 요청을 차단한다.

## 결정 사항
- 기존 PlayerSpawner의 등록 흐름을 사용한다. Player 프리팹과 GameManager 등 기존 미커밋 소스는 수정하지 않았다.
- 테스트 컴포넌트만 제공하며 Assets/Test 씬과 Build Settings는 수정하지 않았다.

## 현재 상태 및 이슈
- 새 소스를 임시 MSBuild targets로 명시적으로 포함해 dotnet build 성공: 오류 0개, 기존 StageManager 미사용 이벤트 경고 3개.
- Unity Play Mode 테스트는 미실행.
- 현재 EditorBuildSettings에는 SampleScene만 등록되어 있다.
- 전체 git diff --check에서 기존 GameManager/PlayerHudPresenter의 공백 경고가 확인되었다. 이번 변경 범위 밖이므로 유지했다.

## 다음 할 일
1. Build Profiles의 Scene List에 KSJ_Home과 KSJ_Lobby를 추가하고 활성화한다.
2. 각 씬의 Cube에 TestSceneTransitionBox를 붙이고 스폰 위치와 겹치지 않게 배치한다.
3. GameManager와 PlayerSpawner가 준비된 씬에서 실행해 왕복, HUD 재연결, 능력치 유지를 확인한다.
