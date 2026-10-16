# MainScene → UITestScene 이름 변경, 새 메인 메뉴 씬 (2026-10-16)

## 작업 요약 및 방법
- 사용자 요청: 지금 씬(Test 저장소 `MainScene`)을 `UITestScene`으로 바꾸고, 새 메인 씬에 Canvas 하나로 메인 메뉴를 대충 배치, 기존 것 중 가져올 게 있으면 별도 Canvas로 가져오기.
- `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Scene/MainScene.unity` → `UITestScene.unity` 이름 변경(GUID 유지). 코드에서 씬 이름으로 참조하는 곳은 없었다.
- 같은 폴더에 새 `MainScene.unity` 생성(카메라·라이트·EventSystem).
- `Canvas_MainMenu`: Shift 에셋(Cut Frame Filled/Glow, Gradient Background, Rajdhani 글꼴)으로 제목 + 버튼 4개(START·CONTINUE·SETTINGS·EXIT) + 버전/안내 문구 배치. 에디터 빌더 스크립트(스크래치, 프로젝트에 남기지 않음)로 생성.
- `Canvas_Settings`: 기존 HUD Canvas의 설정창(`SettingsView`·`SettingsPresenter`)을 그대로 복사. SETTINGS 버튼 → `SettingsPresenter.Open`, 닫기 버튼 → `SettingsPresenter.Close`를 지속 리스너로 연결.
- 두 Canvas를 Test `UnityAssets/Prefabs/UI/MainMenu/`에 프리팹으로 저장. 자세한 수치는 Test 저장소 `Docs/WorkLog/2026-10-16.md` 작업 25.
- 추가 요청: 참고 이미지(Shift 데모 시작 화면)대로 `Canvas_Splash`(sort 20) 추가. 데모 씬 `Main Menu (Desktop)`에서 배경(정지 이미지 + Glow Motes 파티클)과 로고·PRESS [SPACE] TO CONTINUE를 복사, 데모 매니저 참조(Hotkeys)는 제거. Test WorkLog 작업 26.
- 추가 요청: 스플래시 로고 이미지를 게임 이름 "FID" 텍스트로 교체. 프로젝트 TMP 글꼴 중 원본 로고와 가장 비슷한 Aldrich-Regular에 속 빈 외곽선 + 네온 번짐 머티리얼(`UnityAssets/Material/Aldrich-Regular SDF Logo Outline.mat`)을 입힘. 원래 `Logo` 이미지는 꺼 둠. Test WorkLog 작업 27.

## 결정 사항
- 새 메인 씬은 기존 씬과 같은 Test 폴더에 둔다(공유 저장소 Build Settings는 건드리지 않음).
- 버튼 연결은 새 코드 없이 Inspector 이벤트로만 한다.

## 현재 상태 및 이슈
- Play 모드 캡처로 메뉴·설정창 열기/닫기 확인, 에러 0. MainScene·프리팹 저장, 커밋 안 함(Test 저장소 포함).
- START·CONTINUE·EXIT는 로직 미연결. 제목은 `GAME TITLE` 자리표시.
- SETTINGS 버튼의 Canvas_Settings 연결은 프리팹 간 참조라 씬에만 저장됨.
- Test 저장소 Git에는 `MainScene.unity`가 내용 변경 + `UITestScene.unity` 새 파일로 보인다(이름 변경이 같은 경로에 새 씬이 생긴 형태).
- `Canvas_Splash`가 sort 20으로 메뉴를 덮고 있다. SPACE 입력으로 스플래시를 끄고 메뉴를 보이는 로직은 없음(버튼 로직과 같이 나중에).
- 스플래시 배경은 영상(`Advanced Background`) 대신 정지 이미지 사용. 영상 재생은 확인 못 함(에디터 비포커스로 프레임 정지).
- 로고는 "FID" 텍스트(Aldrich). 원본 로고의 스텐실처럼 끊긴 획은 프로젝트에 맞는 글꼴이 없어 재현하지 않음.

## 다음 할 일
- SPACE → 메뉴 전환 연결. 더 비슷한 스텐실 글꼴이 필요하면 외부 글꼴 추가 검토.
- START → 로딩/로비 씬 이동, EXIT → 종료 등 버튼 로직 연결 여부 결정.
- 메인 메뉴를 `Assets/_Project/Scenes/`로 옮겨 Build Settings 0번에 둘지 결정.
