---
id: intent-036
title: 메인 메뉴 씬을 팀 공용으로 옮기고 메뉴 캔버스를 하나로 합치기
part: UI / MainMenu
status: resolved   # open | resolved
created: 2026-10-17
resolved: 2026-10-17
---

## 문제 (Problem)
- 메인 메뉴 씬 `MainScene`과 그 UI 프리팹(`Canvas_MainMenu`·`Canvas_Settings`·`Canvas_Splash`)이 모두 Git 제외 대상인 `Assets/Test/`에 있어 팀원에게 공유되지 않는다.
- 씬에 캔버스가 4개(`Canvas_MainMenu`·`Canvas_Settings`·`Canvas_Splash`·`Canvas_AnyPress`)와 로직 오브젝트 `MainMenu`가 따로 있다. `Canvas_AnyPress`는 `Canvas_Splash` 프리팹을 복제한 것이고, 실제로는 `Canvas_AnyPress`만 `MainMenuView._splashRoot`로 쓰인다. `Canvas_Splash`는 아무 데서도 참조하지 않는다.

## 기대 결과 (Proposed outcome)
- `Assets/_Project/Scenes/MainScene.unity`에 카메라·조명·EventSystem과 `Canvas_MainMenu` 하나만 있다.
- `Canvas_MainMenu`는 `Assets/_Project/Prefabs/UI/Canvas_MainMenu.prefab` 인스턴스이다. Canvas 하나 아래에 `Menu`·`Settings`·`AnyPress` 패널이 있고, 자식 순서(메뉴 → 설정 → AnyPress)가 기존 sortingOrder 0/10/20과 같은 순서로 그린다.
- `MainMenuView`·`MainMenuPresenter`는 `Canvas_MainMenu` 루트에 붙는다. `_menuRoot` = `Menu`, `_splashRoot` = `AnyPress`.
- `Canvas_Splash`는 새 씬에서 뺀다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 팀원 전체(메인 메뉴 씬을 열어 보는 사람)
- 어떤 시스템/모듈이 건드려지는가: 새 씬·새 프리팹만 추가. 스크립트 변경 없음. Test 폴더의 기존 `MainScene`과 프리팹은 그대로 둔다.

## 제약 (Constraints)
- 지켜야 할 정책: 공유 작업물은 `Assets/_Project/`에만 둔다. Test 폴더 에셋을 참조하면 안 된다.
- 쓰지 말아야 할 것: Build Settings에는 넣지 않는다. `SceneLoader`가 빌드 번호를 `ESceneType`(Tutorial = 0 …)으로 쓰므로, 앞에 끼우면 번호가 밀린다.
- AnyPress 배경은 ThirdParty Shift 컴포넌트(`UIManagerBackground`·`UIManagerParticle`)를 쓴다. ThirdParty는 Git 제외라 팀원도 Shift 에셋이 있어야 한다(RealCanvas와 같은 조건).

## 열린 질문 (Open questions)
- (해결) 프리팹으로 만들지 → 프리팹으로 만든다.
- (해결) 캔버스 구조 → Canvas 하나로 합친다.
- 메인 메뉴 씬을 Build Settings·`ESceneType`에 언제 어떻게 넣을지는 이번 범위 밖이다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 열린 씬을 `Assets/_Project/Scenes/MainScene.unity`로 다른 이름 저장한 뒤, `Canvas_MainMenu`·`Canvas_Settings`·`Canvas_AnyPress`를 완전히 언팩하고 `Canvas_Splash`를 지웠다. 메뉴 내용은 새 `Menu` 패널로 묶었다. 설정창·AnyPress는 Canvas·CanvasScaler·GraphicRaycaster를 떼어 `Settings`·`AnyPress` 패널로 바꿨다(모두 화면 꽉 채움, 순서 Menu → Settings → AnyPress). `MainMenuView`·`MainMenuPresenter`는 루트로 옮기고 `MainMenu` 오브젝트는 지웠다. 이 결과를 `Assets/_Project/Prefabs/UI/Canvas_MainMenu.prefab`으로 저장하고 씬 인스턴스와 연결했다.
- 처음 상태: `Settings` 켜짐, `SettingsPanel`·`SettingsPresenter` 꺼짐(열 때 Presenter가 켬).
- Test 의존 제거: AnyPress 로고 `LogoText`의 `Aldrich-Regular SDF Logo Outline.mat`을 `Assets/_Project/Art/UI/`로 복사해 교체했다. Test 원본은 그대로 둔다.
- Play 확인: 시작 시 AnyPress, Space로 메뉴, SETTINGS로 열기, 닫기 버튼·ESC로 닫기, 에러 없음. 화면 모습은 사용자가 에디터에서 확인했다.
- 관련 커밋/PR: feature/System/HudUI 브랜치 커밋 (메인 메뉴 씬 팀 공용 정리)
