---
id: intent-031
title: Test 저장소 MainScene을 UITestScene으로 바꾸고 새 메인 메뉴 씬 만들기
part: UI / Test Scene
status: resolved   # open | resolved
created: 2026-10-16
resolved: 2026-10-16
---

## 문제 (Problem)
Test 저장소의 `MainScene`은 HUD 시안 Canvas를 모아 둔 UI 실험용 씬인데 이름이 `MainScene`이라 실제 메인 메뉴 씬과 헷갈린다. 메인 메뉴 화면은 아직 없다.

## 기대 결과 (Proposed outcome)
- 기존 `MainScene.unity`를 `UITestScene.unity`로 이름만 바꾼다(GUID 유지, 내용 그대로).
- 같은 폴더에 새 `MainScene.unity`를 만들고, 메인 메뉴 Canvas(`Canvas_MainMenu`)에 제목·버튼(시작/이어하기/설정/종료)을 대략 배치한다.
- 기존 HUD Canvas의 설정창(`SettingsView`·`SettingsPresenter`)은 별도 Canvas(`Canvas_Settings`)로 가져와 메뉴의 설정 버튼으로 열고 닫기 버튼으로 닫는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: UI 담당(김성준)
- 어떤 시스템/모듈이 건드려지는가: `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Scene/` 씬 파일, `Prefabs/UI/MainMenu/` 새 프리팹

## 제약 (Constraints)
- 지켜야 할 정책: 새 코드는 쓰지 않는다. 버튼 연결은 Inspector 이벤트(지속 리스너)로만 한다.
- 쓰지 말아야 할 것: 공유 저장소 Build Settings는 건드리지 않는다(Test 폴더 씬이므로).

## 열린 질문 (Open questions)
- 시작/이어하기/종료 버튼이 실제로 어느 씬을 여는지(로직 연결)는 정하지 않았다. 지금은 배치만.
- 메인 메뉴 씬을 나중에 `Assets/_Project/Scenes/`로 옮겨 Build Settings 0번에 둘지.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 이름 변경(GUID 유지) + 같은 Test 폴더에 새 MainScene, `Canvas_MainMenu`·`Canvas_Settings` 프리팹(`Prefabs/UI/MainMenu/`). 버튼 로직(시작·이어하기·종료)과 `_Project` 이동은 열린 질문으로 남김.
- 관련 커밋/PR: 없음(커밋 안 함). 작업 기록: `docs/HANDOVER/SungJun/2026-10-16/main-menu-scene.md`
