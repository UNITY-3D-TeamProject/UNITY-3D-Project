---
id: intent-098
title: 에디터 상단 메뉴에서 진행 단계를 골라 바로 들어가기
part: Core / Editor / SceneTransition
status: open
created: 2026-10-30
resolved: null
---

## 문제 (Problem)
- 로비 복귀 대화(예: 보안 후 29번)나 특정 단계의 로비 상태를 보려면 앞 스테이지를 다 깨야 한다.
- 이어하기 저장(PlayerPrefs `GameProgress.SequenceStep`)을 손으로 바꾸고 MainScene → 이어하기로 들어가도, `GameManager.SpawnStage`가 비어 있어 로비 기본 위치에서 시작한다. 복귀 대화 발판은 복귀 위치(`Position(Security)` 등)에 3m 상자로 놓여 있어서 실제 흐름과 다르게 밟히지 않는다.

## 기대 결과 (Proposed outcome)
- Unity 상단 메뉴 `Tools > 진행 단계`에 `SceneLoader` 순서 9칸(튜토리얼 → 로비 → 피드 → 로비 → 파일 → 로비 → 보안 → 로비 → 라이브)과 `저장 지우기`가 있다.
- 단계를 누르면 그 단계로 이어하기 저장을 쓰고, MainScene부터 Play를 시작해 자동으로 이어하기(`SceneLoader.TryContinueGame`)한다.
- 로비 단계는 직전 스테이지의 복귀 위치에서 시작한다(로비가 불러와질 때 `GameManager.SpawnStage`를 직전 스테이지로 맞춤).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 에디터에서 테스트하는 팀원.
- 어떤 시스템/모듈이 건드려지는가: 신규 에디터 전용 `Scripts/Core/Editor/ProgressStepMenu.cs` 하나. 런타임 코드·씬은 바꾸지 않는다.

## 제약 (Constraints)
- 코드는 `Scripts/Core` 안에서만 쓴다. `SceneTransition/SceneLoader`(다른 파트)는 고치지 않고 공개 API만 쓴다.
- 에디터에 열린 씬은 바꾸지 않는다(`EditorSceneManager.playModeStartScene`으로 시작 씬만 지정, Play 시작 후 원래대로).
- `GameManager.SpawnStage`는 private set이라 에디터 도구에서만 리플렉션으로 맞춘다(런타임 코드 변경 없음).

## 열린 질문 (Open questions)
- 이어하기로 들어가면 `SceneLoader.PreviousScene`이 비어 있어 로딩 영상(피드·파일 → 로비)과 파일 복귀 전용 BGM은 재현되지 않는다. 필요하면 별도 논의.

## 구현 기록 (2026-10-30)
- `Scripts/Core/Editor/ProgressStepMenu.cs` 추가. 리컴파일 통과, 메뉴 10개 등록, `8. 로비 (보안 후)` 실행 시 Play 진입 → 시작 씬 지정 해제 → 자동 이어하기 → 로딩 씬 진입 확인.
- Unity 창이 비활성이라 Play 루프가 멈춰 로비 도착·복귀 위치 스폰·29번 자동 재생은 미확인.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
