---
id: intent-048
title: UIManager의 입력 처리를 UIInputHandler로 분리
part: UI / Input
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-20 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
UI 쪽 입력이 두 갈래로 나뉘어 있다.
- 설정창 열기/닫기와 Player·UI 맵 전환은 `PlayerFacade`의 이벤트와 메서드로 처리한다.
- Dialogue 맵(Space 넘기기, ESC 설정창)은 `UIManager`가 Facade를 건너뛰고 플레이어의 `PlayerInput`을 직접 잡아서 처리한다(intent-044에서 PlayerFacade를 바꾸지 않으려고 택한 방식).

그 결과 UIManager가 HUD·설정창·대화 흐름에 더해 입력 배선(맵 이름 상수, 액션 찾기·구독·해제)까지 떠안고 있고, 같은 ESC가 맵에 따라 다른 경로로 들어온다.

## 기대 결과 (Proposed outcome)
- UI 전용 입력 클래스 `UIInputHandler`(일반 C# 클래스)가 Facade 이벤트 중계, Dialogue 맵 액션 구독, 맵 전환 3종(Player/UI/Dialogue)을 모두 맡는다.
- UIManager는 `UIInputHandler`의 이벤트(설정 열기·닫기, 다음 대사)와 메서드만 사용한다. 두 경로로 들어오던 ESC는 `OnOpenSettingsRequested` 하나로 합친다.
- "설정창을 닫거나 플레이어가 바뀌었을 때 어느 맵으로 되돌릴지"는 UI 상태를 아는 UIManager에 남긴다.
- 동작은 지금과 같다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 설정창, 대화(`StartDialogue`/`PlayDialogue`)
- 어떤 시스템/모듈이 건드려지는가:
  - `UI/UIInputHandler.cs`: 새 파일
  - `UI/UIManager.cs`: 입력 코드 제거, 핸들러 사용

## 제약 (Constraints)
- `PlayerFacade`는 다른 파트(이세훈) 담당이고 플레이어 조작 창구라서 수정하지 않는다.
- 일반 C# 클래스로 만들어 프리팹(Canvas_InGame·Canvas_Legacy)은 수정하지 않는다.
- `UI.Input` 네임스페이스는 `UnityEngine.Input`과 충돌하므로 만들지 않는다(`UI` 루트에 둔다).

## 열린 질문 (Open questions)
- 없음 (형태: 일반 C# 클래스로 사용자 확정)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 기대 결과대로 구현(2026-10-20). `UI/UIInputHandler.cs`(일반 C# 클래스: `BindPlayer`/`UnbindPlayer`, `SwitchToPlayerInput`/`SwitchToUIInput`/`SwitchToDialogueInput`, 이벤트 `OnOpenSettingsRequested`·`OnCloseSettingsRequested`·`OnNextLineRequested`, `HasPlayer`). UIManager는 `_input` 필드 하나로 사용하고 이벤트는 OnEnable/OnDisable에서 한 번만 구독. 플레이어 교체 시 맵 복원 판단은 `UIManager.HandlePlayerSpawned`에 남김. PlayerFacade·프리팹 미수정.
- 확인: 컴파일 0에러. TestScene Play에서 Facade 설정창 열기 → UI 맵·Pause, 닫기 → Player 맵·Playing, `PlayDialogue(2)` → Dialogue 맵, 대화 중 ESC → UI 맵, 닫기 → Dialogue 맵, 다음 줄 → 줄 번호 증가, 마지막 줄 후 Player 맵·상호작용 안내 차단 해제. 에디터가 백그라운드라 실제 키 입력 대신 PlayerFacade·UIInputHandler의 입력 콜백을 직접 호출해 확인(simulate_key 미전달).
- 관련 커밋/PR: (커밋 전)
