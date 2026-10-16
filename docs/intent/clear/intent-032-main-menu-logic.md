---
id: intent-032
title: 메인 메뉴 로직 연결 (아무 키로 스플래시 → 메뉴, 설정창, 종료)
part: UI / Main Menu
status: resolved   # open | resolved
created: 2026-10-16
resolved: 2026-10-16
---

## 문제 (Problem)
Test 저장소 새 `MainScene`에 `Canvas_Splash`·`Canvas_MainMenu`·`Canvas_Settings`가 배치만 돼 있고 로직이 없다. 스플래시가 메뉴를 덮은 채 넘어가지 않고, 버튼은 SETTINGS 열기/닫기만 Inspector 지속 리스너로 연결돼 있다.

## 기대 결과 (Proposed outcome)
- 같은 씬 안에서 아무 키(키보드·마우스·게임패드 버튼)를 누르면 스플래시가 꺼지고 메뉴가 보인다. 씬 전환 없음.
- SETTINGS로 설정창을 열고, 닫기 버튼과 ESC로 닫는다.
- EXIT는 게임 종료(에디터에선 플레이 모드 종료).
- CONTINUE는 저장 기능이 없어 비활성(회색).
- 기존 UI와 같은 View/Presenter(MVP) 구조: `MainMenuView`, `MainMenuPresenter`. 기존 `SettingsPresenter`/`SettingsView` 재사용.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: UI 담당(김성준), 게임 시작 시 모든 플레이어
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/UI/` 새 스크립트 2개, Test 저장소 `MainScene.unity`·`Prefabs/UI/MainMenu/` 프리팹

## 제약 (Constraints)
- 지켜야 할 정책: 아무 키 입력은 `InputSystem.onAnyButtonPress`로 받아 inputactions 에셋을 수정하지 않는다. ESC는 기존 `FID_InputSystem` `UI/CloseSettings`를 재사용한다.
- 쓰지 말아야 할 것: `UIManager`(GameManager·플레이어 전제), `GameManager`·`SceneLoader`·공유 Build Settings는 건드리지 않는다.

## 열린 질문 (Open questions)
- START → 튜토리얼 이동 방식: `ESceneType` 값이 Build 번호와 같고 메뉴 씬이 `SceneLoader` 순서 목록에 없어 `TryLoad`가 실패한다. → 해결: `SceneLoader.TryStartNewGame()` 추가.
- 메뉴 씬에서의 `GameManager` 배치·`GameState.Title`·커서 처리 담당.
- 메인 메뉴 씬을 `Assets/_Project/Scenes/`로 옮겨 Build Settings에 넣을지.

## 진행 상황 (2026-10-16)
- START를 뺀 나머지 완료: `MainMenuView`·`MainMenuPresenter` 추가, Test `MainScene`에 `MainMenu` 오브젝트로 연결. 아무 키 → 메뉴, SETTINGS 열기, 닫기 버튼·ESC 닫기, CONTINUE 비활성, EXIT 종료를 Play 모드에서 확인.
- START: `SceneLoader.TryStartNewGame()` 추가 후 연결, Play 모드로 튜토리얼 진입·플레이어 스폰 확인.
- 작업 기록: `docs/HANDOVER/SungJun/2026-10-16/main-menu-logic.md`

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `MainMenuView`·`MainMenuPresenter`(MVP)로 메뉴 로직 연결. 아무 키는 `InputSystem.onAnyButtonPress`, ESC는 기존 `UI/CloseSettings` 재사용(액션 맵 추가 없음). START는 `SceneLoader.TryStartNewGame()`(순서를 0칸=튜토리얼로 맞추고 로딩씬 경유, `OnBeforeSceneChange` 미발행) 호출. 튜토리얼 씬의 GameManager가 NewGame으로 플레이어를 스폰한다.
- 후속(별도 작업): 실제 빌드에서 메뉴를 첫 씬으로 두려면 `ESceneType` = Build 번호 규칙 때문에 enum·Build Settings를 함께 바꿔야 함(메뉴 씬 `_Project/Scenes` 이동 시). 게임 중 메뉴로 돌아가기는 GameManager·플레이어 상태 초기화가 필요.
- 관련 커밋/PR: 없음(커밋 안 함). 작업 기록: `docs/HANDOVER/SungJun/2026-10-16/main-menu-logic.md`
