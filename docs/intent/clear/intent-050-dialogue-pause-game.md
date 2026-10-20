---
id: intent-050
title: 대화 중에는 플레이어·펫 대기 애니메이션과 BGM만 남기고 게임을 멈춤
part: UI / Dialogue / Core
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-20 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
대화가 떠 있는 동안 입력만 Dialogue 맵으로 막혀 있고, 적·투사체·물리·이펙트·3D 효과음은 그대로 진행된다.
대화를 읽는 사이에 적이 다가오거나 공격할 수 있다.

## 기대 결과 (Proposed outcome)
- 대화가 시작되면 게임 시간(`Time.timeScale`)이 0이 되어 적·투사체·물리·파티클·코루틴이 멈춘다.
- 플레이어와 펫은 대기 애니메이션이 계속 움직인다 (두 캐릭터의 Animator만 `UnscaledTime`).
- 줄마다 말하는 사람 카메라 전환은 그대로 된다 (대화 동안만 CinemachineBrain `IgnoreTimeScale` 켬).
- 소리는 BGM(과 UI 소리)만 들리고 3D 효과음은 멈춘다 (`AudioListener.pause`, 설정창과 같은 방식).
- 대화 중에 설정창을 열고 닫아도 대화가 끝날 때까지 멈춘 상태가 유지된다.
- 대화가 끝나면(끝까지 / `StopDialogue` / UIManager 꺼짐) 시간·Animator·Brain이 원래대로 돌아온다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 대화(`UIManager.PlayDialogue`)를 재생하는 모든 곳 (발판, 스테이지 시작 대화)
- 어떤 시스템/모듈이 건드려지는가:
  - `GameManager`: `PauseGameForDialogue`·`ResumeGameForDialogue` 추가. 기존 `PauseGame`·`ResumeGame`은 그대로 둔다.
  - `UIManager`: 대화 시작·끝에 위 메서드 호출, 대화 중 설정창을 닫으면 다시 대화 멈춤
  - `DialogueDirector`: 플레이어·펫 Animator `updateMode`, CinemachineBrain `IgnoreTimeScale`을 대화 동안만 바꾸고 되돌림. 펫은 시간이 멈추면 `PetFollower`가 따라가지 않으므로 설 자리로 바로 옮기고 이동 속도 애니메이션을 0으로
- 다른 사람 파일은 고치지 않는다. 런타임에 값만 잠깐 바꾼다: 플레이어·펫 Animator(이세훈), Main Camera CinemachineBrain(양준범). `PetAnimator.SetMoveSpeed`(public)만 호출.

## 제약 (Constraints)
- 예산/기한: 없음
- 지켜야 할 정책: 펫 가이드는 이번 범위에서 뺀다. GameState는 대화 중에도 `Playing`(설정창을 열 수 있어야 함), 커서는 잠근 그대로.
- 쓰지 말아야 할 것: 기존 `ResumeGame`에 대화 분기 추가, 적·기믹마다 멈춤 처리, 다른 사람 스크립트·프리팹 수정

## 열린 질문 (Open questions)
- (없음) 3D 효과음은 멈추고 BGM만 → 사용자 확정. 플레이어·펫 대기 애니메이션은 계속 → 사용자 확정.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 기대 결과대로 구현. GameManager에 대화 전용 Pause/Resume 추가(기존 ResumeGame 미수정, ResumeGameForDialogue는 설정창 Pause 중이면 무시), UIManager가 대화 시작·끝·설정창 닫기에서 호출, DialogueDirector가 대화 동안 플레이어·펫 Animator UnscaledTime·Brain IgnoreTimeScale을 바꾸고 펫을 설 자리로 바로 옮김. 다른 사람 파일 미수정. TestScene Play 확인(Space는 리플렉션 호출).
- 관련 커밋/PR: (커밋 전)
