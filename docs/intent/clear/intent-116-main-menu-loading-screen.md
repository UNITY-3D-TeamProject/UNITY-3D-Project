---
id: intent-116
title: 메인 메뉴로 돌아갈 때 달리는 플레이어 로딩 화면 표시
part: SceneTransition
status: resolved   # open | resolved
created: 2026-11-01
resolved: 2026-11-01
---

## 문제 (Problem)
메인 메뉴로 돌아갈 때(설정창 "메인 화면으로", 사망 팝업 메인 메뉴, 엔딩 크레딧 끝) `SceneLoader.TryLoadMainMenu`가 메인 씬을 바로 열어 로딩 화면 없이 넘어간다. 다른 이동은 달리는 플레이어 로딩 화면을 거친다.

## 기대 결과 (Proposed outcome)
- 메인 메뉴로 돌아갈 때도 로딩씬을 거쳐 기본 로딩 화면(달리는 플레이어·펫, 진행 바)을 보여 주고, 문구는 "메인 메뉴로 가는 중..."(튜토리얼·로비 문구와 같은 형식).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 메인 메뉴로 돌아가는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `SceneTransition/SceneLoader.cs`, `SceneTransition/LoadingScreen.cs`, `SceneTransition/LoadingStageView.cs`

## 제약 (Constraints)
- 메인 메뉴는 ESceneType에 넣지 않는다(포탈 목적지로 고를 수 없게 한 기존 설계).
- 로딩 컷씬 설정창에서 메인 화면으로 갈 때는 이미 로딩씬 안이라 로딩씬을 또 열지 않고, 떠 있는 로딩 화면이 달리는 화면으로 바뀌어 메인 메뉴를 불러온다(후속 사용자 요청).

## 열린 질문 (Open questions)
- 없음

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `SceneLoader.IsMainMenuPending` 추가. `TryLoadMainMenu`는 저장·세션 초기화 뒤 로딩씬을 Additive로 열고(로딩씬이 이미 열려 있으면 새로 열지 않고 `LoadingScreen.CoLoadMainMenuFromCutscene`이 이어서 불러온다), `LoadPendingSceneAsync`가 메인 메뉴가 기다리면 메인 씬을 불러온다. `LoadingScreen`은 메인 메뉴면 컷씬 없이 `LoadingStageView.ShowMainMenu()`(기본 화면 문구를 `_mainMenuMoveText`로)를 부른다.
- 최소 로딩 시간(`_fillDuration`) 5초 → 4초 (사용자 요청).
- 관련 커밋/PR: 73090190 (1차), 후속은 커밋 전
