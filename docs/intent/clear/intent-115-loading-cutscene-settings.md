---
id: intent-115
title: 로딩 컷씬(폰 주인 영상) 중 ESC로 설정창 열고 영상 멈춤
part: SceneTransition / UI Settings
status: resolved   # open | resolved
created: 2026-11-01
resolved: 2026-11-01
---

## 문제 (Problem)
라이브 앱의 엔딩 영상(`ScreenVideoZoom`)은 ESC로 설정창이 열리고 영상이 멈추지만, 로딩 화면에서 트는 폰 주인 영상(오프닝 = 메인 메뉴→튜토리얼, 피드→로비, 파일→로비)은 ESC를 눌러도 아무 일도 없다.
로딩씬에는 플레이어(PlayerInput)·UIManager·설정창·EventSystem이 없고, 이전 씬과 함께 AudioManager도 사라진 뒤라 영상 소리(Direct 출력)는 볼륨 설정을 받지 않는다.

## 기대 결과 (Proposed outcome)
- 로딩 컷씬 중 ESC → 영상 멈춤·E 스킵 게이지 초기화 + 설정창(볼륨·감도·메인 화면·게임 종료·닫기). ESC/닫기 → 이어서 재생.
- 볼륨 슬라이더가 로딩 컷씬 소리에도 적용된다.
- 오프닝 영상 중에도 "메인 화면으로"를 쓸 수 있다 (오프닝은 영상이 끝난 뒤 튜토리얼을 불러옴). 오프닝 흐름의 로딩 배경은 검은색.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 로딩 컷씬을 보는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `SceneTransition/LoadingScreen.cs`, `SceneTransition/SceneLoader.cs`(`CancelPendingLoad`), `Scenes/Loading.unity`

## 제약 (Constraints)
- 지켜야 할 정책: `Loading.unity` 수정은 사용자 허락(2026-11-01). 맵 씬은 건드리지 않음.
- 쓰지 말아야 할 것: 다시하기 버튼(로딩 중 의미 없음)

## 열린 질문 (Open questions)
- 없음 (버튼: 메인 화면·게임 종료 포함 / 볼륨: 동작하게 / 오프닝: 처음엔 메인 버튼 숨김 → 후속으로 영상 뒤 불러오기 + 메인 버튼 — 사용자 확정)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 인게임 설정창(`Canvas_InGame`의 SettingsPresenter·SettingsView, 다시하기 버튼 끔)과 EventSystem(꺼 둔 채 저장, 설정창 동안만 켬)을 로딩씬에 복사, BGM 없는 AudioManager 프리팹 배치, 영상 소리를 AudioSource → Bgm 믹서 그룹으로 변경. 메인 화면·게임 종료는 `SceneLoader.CancelPendingLoad()`로 이동을 멈춘 뒤 `GameManager.ReturnToMainMenu`/`QuitGame` 경로를 그대로 탄다.
- 관련 커밋/PR: (커밋 전)
