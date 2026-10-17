# MainScene Canvas 프리팹 정리 (2026-10-16)

## 작업 요약 및 방법
- 로비의 Canvas_ReferenceHud를 Test MainScene으로 이동했다. 기존 로비 HUD를 가렸던 CanvasGroup은 제거하고 CrossHair 활성 상태는 원본 프리팹 값으로 복원했다.
- MainScene의 Canvas 9개를 Test `UnityAssets/Prefabs/UI/MainScene/`에 개별 프리팹으로 저장하고 씬 오브젝트와 연결했다. 기존 팀 프리팹 인스턴스 3개는 풀어서 독립 Test 프리팹으로 만들었다.

## 변경된 파일
- `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Scene/MainScene.unity`
- `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Prefabs/UI/MainScene/`의 프리팹 9개, 각 `.meta`, 폴더 `.meta`
- `Assets/Test/UNITY-3D-Project-for-KSJ-Test/Docs/WorkLog/2026-10-16.md`
- intent-027 및 이전 HUD intent·작업 기록의 후속 변경 안내

## 결정 사항
- 모든 Canvas를 독립 Test 프리팹으로 두고, MainScene의 활성 상태와 구성은 유지한다.

## 현재 상태 및 이슈
- MainScene을 저장한 뒤 활성 씬으로 남겼다. Canvas 9개 연결, 외부 씬 참조 0건, Unity 컴파일·Console 에러 0건을 확인했다.
- 참고 HUD는 Git 제외된 로컬 ThirdParty UI 에셋에 의존한다.
- 기존 ProjectSettings/EditorBuildSettings.asset의 사용자 변경은 건드리지 않았다. 커밋·Push 하지 않았다.

## 다음 할 일
- 다른 환경에서 사용할 경우 필요한 ThirdParty 에셋을 준비하고 프리팹 표시를 확인한다.
