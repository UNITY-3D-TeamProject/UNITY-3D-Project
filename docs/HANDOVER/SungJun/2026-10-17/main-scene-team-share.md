# 메인 메뉴 씬 팀 공용 정리 (2026-10-17)

관련 intent: [intent-036](../../../intent/clear/intent-036-main-scene-team-share.md)

## 작업 요약 및 방법
- Test 폴더에 있던 `MainScene`을 팀원에게 공유하려고, 열려 있던 씬(저장 안 된 변경 포함)을 `Assets/_Project/Scenes/MainScene.unity`로 다른 이름 저장했다. Test의 `MainScene.unity` 파일은 디스크 상태 그대로 두었다.
- 새 씬에서 캔버스 4개와 `MainMenu` 오브젝트를 `Canvas_MainMenu` 하나로 합쳤다.
  - `Canvas_Splash`는 지웠다. 아무 곳에서도 참조하지 않고, `Canvas_AnyPress`가 이 프리팹을 복제한 실제 사용본이었다.
  - `Canvas_MainMenu`·`Canvas_Settings`·`Canvas_AnyPress`를 완전히 언팩했다.
  - 메뉴 내용(배경·제목·버튼·버전·힌트)은 새 `Menu` 패널로 묶었다.
  - `Canvas_Settings`·`Canvas_AnyPress`에서 Canvas·CanvasScaler·GraphicRaycaster를 떼어 `Settings`·`AnyPress` 패널로 바꿨다.
  - 모든 패널은 화면을 꽉 채운다. 자식 순서는 Menu → Settings → AnyPress로, 기존 sortingOrder 0/10/20과 같은 순서이다.
  - `MainMenuView`·`MainMenuPresenter`는 `Canvas_MainMenu` 루트로 옮겼다(값 복사, `_view`는 새 View, `_menuRoot` = `Menu`, `_splashRoot` = `AnyPress`).
- 결과를 `Assets/_Project/Prefabs/UI/Canvas_MainMenu.prefab`으로 저장하고 씬 인스턴스와 연결했다(오버라이드 없음).
- AnyPress 로고 `LogoText`가 Test 머티리얼(`Aldrich-Regular SDF Logo Outline.mat`)을 써서, 이를 `Assets/_Project/Art/UI/`로 복사해 교체했다. 이제 프리팹·씬에 Test 의존이 없다.

```
MainScene (Assets/_Project/Scenes/)
├ Main Camera / Directional Light / EventSystem
└ Canvas_MainMenu  (Canvas 1개, MainMenuView·MainMenuPresenter)
   ├ Menu
   ├ Settings  (SettingsView/SettingsPanel 꺼짐, SettingsPresenter 꺼짐)
   └ AnyPress
```

## 변경된 파일
- 추가: `Assets/_Project/Scenes/MainScene.unity`, `Assets/_Project/Prefabs/UI/Canvas_MainMenu.prefab`, `Assets/_Project/Art/UI/Aldrich-Regular SDF Logo Outline.mat` (각 `.meta`, `Art/UI` 폴더 메타 포함)
- 문서: intent-036(clear), `INTENT_POINTER.md`, 이 작업 기록, `HANDOFF_POINTER.md`
- 스크립트 변경 없음.

## 결정 사항
- 메인 메뉴 UI는 `Canvas_MainMenu` 프리팹 하나로 관리한다. UI를 고칠 땐 프리팹을 고친다. 그러면 씬 파일이 바뀌지 않아 씬 충돌이 줄어든다.
- 캔버스는 하나로 합치고, 그리는 순서는 자식 순서로 정한다.
- Build Settings에는 넣지 않았다. `SceneLoader`가 빌드 번호를 `ESceneType`(Tutorial = 0 …)으로 쓰므로, 앞에 끼우면 번호가 밀린다.

## 현재 상태 및 이슈
- Play 확인 결과는 다음과 같다. 시작하면 AnyPress가 보인다. Space를 누르면 메뉴로 넘어간다(Menu 켜짐, AnyPress 꺼짐). SETTINGS로 열기, 닫기 버튼으로 닫기, 다시 열기, ESC로 닫기가 모두 된다. 에러는 0건이다(경고 1건은 Test `TpsPlayerSetup` 에디터 스크립트로 무관).
- 메뉴·설정창 화면 모습은 사용자가 에디터에서 확인했다(게임 뷰 캡처는 갱신되지 않아 AI는 오브젝트 활성 상태로만 확인).
- `SettingsPanel`에 `_root`·`_closeButton`이 비어 있는 `SettingsView`가 하나 더 붙어 있다(기존부터 있던 것, 그대로 둠).
- `SettingsPanel`은 중첩 Canvas(override sorting 10)라 같은 Canvas 안의 AnyPress보다 위에 그린다. 두 화면이 동시에 켜지는 흐름은 없어 지금은 문제없다.
- AnyPress 배경은 ThirdParty Shift 컴포넌트를 쓰므로 팀원도 Shift 에셋이 필요하다.
- Test의 `MainScene`·`Canvas_Splash` 등 프리팹은 그대로 남아 있다. 에디터에는 지금 새 `_Project/Scenes/MainScene.unity`가 열려 있다.
- 팀 저장소에 커밋했다(push 안 함).

## 다음 할 일
- 원격에 push한다.
- 메인 메뉴를 빌드 첫 씬으로 넣으려면 `ESceneType`·`SceneLoader` 빌드 번호 규칙과 함께 정한다.
- 팀원 작업을 모두 합친 뒤 다음을 확인한다(사용자 결정).
  - GameManager는 MainScene에 두지 않는다(검토 결론). 이유는 두 가지다. `Awake`에서 커서를 잠그고, `DontDestroyOnLoad`라서 메인으로 돌아오면 이전 판 `playerState`가 남는다. 지금은 `Map_0`~`Map_4`에만 있고 `Map_5_Live`에는 없다.
  - 컷씬은 `Loading.unity`의 `LoadingScreen._cutsceneData`(`ScriptableObjects/SceneTransition/SOLoadingCutsceneData.asset`)가 (출발, 목적지) 쌍으로 고른다. 메인 → 튜토리얼은 `TryStartNewGame`이 출발 씬을 null로 두므로 컷씬을 넣을 수 없다. 오프닝 컷씬이 필요하면 구조 변경이 필요하다.
