---
id: intent-092
title: 인게임 설정창에서 현재 스테이지 다시하기
part: UI / Settings / Core / Stage
status: open
created: 2026-10-30
resolved: null
---

## 문제 (Problem)
- 인게임 설정창에는 현재 스테이지의 씬을 처음부터 다시 시작하는 버튼이 없다.

## 기대 결과 (Proposed outcome)
- ESC 설정창을 열면 스크롤하지 않아도 하단에 `RETRY`(다시하기) 버튼이 보인다.
- 누르면 현재 씬을 다시 불러와 스테이지 진행·체크포인트를 처음부터 시작한다. 기존 `StageRestart` 스폰 규칙에 따라 씬 입장 시 능력치를 복원한다.

## 영향 범위 (Affected users and systems)
- 플레이어가 사용하는 모든 인게임 스테이지 설정창.
- `Canvas_InGame` 프리팹, Settings View/Presenter, UIManager, StageManager.

## 제약 (Constraints)
- 기존 `MAIN MENU`·`QUIT GAME` 버튼은 계속 하단에 표시한다.
- SceneLoader의 캠페인 진행 순번과 영구 저장을 바꾸지 않는다.
- 기존 스테이지 사망 시 `StageRestart` 동작을 재사용한다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 진행(2026-10-30): `Canvas_InGame` 고정 하단 줄에 `RETRY` 버튼을 추가하고 SettingsView → SettingsPresenter → UIManager → StageManager.RestartStage 흐름을 연결했다. 첫 라운드 사망 시 씬 재시작도 같은 메서드를 사용한다. 프리팹 참조·계층 정적 검증 완료. Unity Editor Play 확인은 남음.
- 관련 커밋/PR: 이번 작업 커밋.
