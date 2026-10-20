---
id: intent-051
title: 펫 가이드가 뜨면 게임을 멈추고 Space로 닫기
part: UI / PetGuide / Core
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-20 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
펫 가이드(`UIManager.ShowPetGuide(id)`)는 문구만 띄우고 게임은 그대로 진행된다. 닫는 입력도 없다.
안내를 읽는 동안에도 적·투사체가 움직이고, 플레이어가 안내를 확인했는지 알 수 없다.

## 기대 결과 (Proposed outcome)
- 모든 펫 가이드가 뜨면 게임 시간(`Time.timeScale`)이 0이 된다. 플레이어·펫 애니메이션도 함께 멈춘다.
- 소리는 BGM(과 UI 소리)만 들리고 3D 효과음은 멈춘다 (대화와 같은 방식).
- 패널 아래에 "Space ▶ 닫기"가 보이고, Space를 누르면 패널이 닫히며 게임과 조작이 돌아온다.
- 펫 가이드 중 ESC로 설정창을 열 수 있고, 닫으면 다시 펫 가이드 상태(멈춤)로 돌아온다.
- 대화 중에는 펫 가이드를 무시하고, 펫 가이드 중에는 대화를 무시한다 (같은 패널을 써서 동시에 띄우지 않음).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: `ShowPetGuide`를 부르는 곳 (현재 TestScene 발판 9곳, `HidePetGuide`를 부르는 곳은 없음)
- 어떤 시스템/모듈이 건드려지는가:
  - `GameManager`: `PauseGameForDialogue`·`ResumeGameForDialogue` → `PauseGameTime`·`ResumeGameTime` 이름 변경 (대화·펫 가이드 공용)
  - `UIManager`: 펫 가이드 열림 상태, 열 때 멈춤·Dialogue 입력 맵, Space로 닫기, 설정창·플레이어 교체·비활성화 시 처리
  - `PetGuidePresenter`: 성공 여부 반환, "Space ▶ 닫기" 안내 표시
  - `DialogueDirector`: 주석의 메서드 이름만 변경
- 다른 사람 파일은 고치지 않는다.

## 제약 (Constraints)
- 예산/기한: 없음
- 지켜야 할 정책: 입력은 기존 Dialogue 맵(Space 넘기기·ESC 설정창)을 재사용한다. 입력 에셋 수정 없음.
- 쓰지 말아야 할 것: 펫 가이드용 Pause 메서드를 따로 복제, 펫 가이드마다 멈춤 여부 옵션

## 열린 질문 (Open questions)
- (없음) 대상 패널 = 펫 가이드, 모든 안내 멈춤 + Space 닫기, BGM 유지, 대화와 겹치면 무시, 이름 `PauseGameTime` → 사용자 확정.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 기대 결과대로 구현. `GameManager.PauseGameTime`·`ResumeGameTime`(이름 변경, 대화·펫 가이드 공용). UIManager가 `_isPetGuideOpen`으로 펫 가이드 상태를 들고, `ShowPetGuide`에서 멈춤·Dialogue 맵, Space(`HandleNextLine`)·`HidePetGuide`로 닫기, 설정창 닫기·플레이어 교체·OnDisable 처리. `PetGuidePresenter.Show`는 bool 반환 + "Space ▶ 닫기" 표시. TestScene Play 확인(Space는 UIInputHandler 콜백 리플렉션 호출).
- 관련 커밋/PR: (커밋 전)
