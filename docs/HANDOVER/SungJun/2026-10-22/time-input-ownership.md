# 게임 시간·입력 맵·Brain 주인 정리 (intent-064)

## 1. 작업 요약 및 방법
- 카메라 연출 구조 검토 두 건을 코드와 대조했다. 원인은 게임 시간·입력 맵·CinemachineBrain 설정을 맡는 곳이 없어 여러 클래스가 각자 바꾸고 되돌린다는 점 하나로 모인다. 버그 5건(종료 블렌드 중 Brain 복구 충돌, 연출 중 외부 대화, 비활성 매니저 재생, UIManager 껐다 켜면 입력 복구 누락, 설정창 닫을 때 한 프레임 흐름)을 정리했다.
- [intent-064](../../../intent/intent-064-time-input-ownership.md) 작성, 열린 질문 모두 결정.
- 1단계로 게임 시간을 GameManager 요청 방식으로 바꿨다. 상세는 intent-064 "진행 기록 1".
  - `GameManager.PauseGameTime(requester)`/`ResumeGameTime(requester)` + `ApplyGameTime()`(설정창 Pause 또는 요청 하나라도 → 0).
  - UIManager·CinematicManager 호출을 요청 방식으로, 서로 다시 멈추던 땜질 두 곳 삭제.
- 이전 변경(StageManager 시작 연출 호출 제거, 문서) 커밋: `d770cb40`, `3e59ad21`.

## 2. 결정 사항
- 시간 = GameManager(싱글톤, 설정창 Pause와 같은 곳). 씬 로드 때 파괴된 요청자만 지운다.
- 입력 맵 = 씬 안 새 Core 컴포넌트(`Canvas_InGame` 프리팹), Instance 접근. 우선순위 설정창(UI) > 조작 막기(Dialogue) > Player. `UIInputHandler`는 ESC·E 이벤트만.
- 연출 중 외부 `PlayDialogue`는 거절. `CinematicManager.Play`는 꺼져 있으면 false.
- Brain `IgnoreTimeScale`은 대화·연출 시작 때 true로만 두고 되돌리지 않는다.
- 인게임 UI 숨기기는 CinematicManager 시작·끝 이벤트를 UIManager가 구독하는 방식.
- 작업 범위: `Scripts/Core`, `Scripts/UI`만. GameManager는 본인 작성이라 수정 가능.

## 3. 현재 상태 및 이슈
- 시간 단계: 컴파일 0에러, TestScene Play(eval 직접 호출)로 확인. 씬 전환 때 요청 정리·실제 키 입력은 미확인. 커밋 안 함.
- 에디터가 백그라운드면 Play 프레임이 멈춘다. Play 중 eval로 `Application.runInBackground = true`를 주면 돈다(Play 종료 시 원래대로).
- 아직 남은 버그: 2(외부 대화), 3(비활성 매니저), 4(UIManager 비활성화), 1(Brain).

## 4. 다음 할 일
- 입력 맵 컴포넌트 구현 + `Canvas_InGame` 배치, UIInputHandler·UIManager 정리.
- UIManager 연출 전용 코드 제거, 외부 대화 거절, `Play` 활성 검사.
- Brain 저장·복구 제거, UI 숨기기 이벤트화.
- 끝나면 TestScene에서 버그 5건 재현 확인 후 intent-064 해결 처리.
