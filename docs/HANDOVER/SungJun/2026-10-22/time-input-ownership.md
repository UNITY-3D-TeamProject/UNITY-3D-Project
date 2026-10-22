# 게임 시간·입력 맵·Brain 주인 정리 (intent-064)

## 1. 작업 요약 및 방법
- 카메라 연출 구조 검토 두 건을 코드와 대조했다. 원인은 게임 시간·입력 맵·CinemachineBrain 설정을 맡는 곳이 없어 여러 클래스가 각자 바꾸고 되돌린다는 점 하나로 모인다. 버그 5건(종료 블렌드 중 Brain 복구 충돌, 연출 중 외부 대화, 비활성 매니저 재생, UIManager 껐다 켜면 입력 복구 누락, 설정창 닫을 때 한 프레임 흐름)을 정리했다.
- [intent-064](../../../intent/intent-064-time-input-ownership.md) 작성, 열린 질문 모두 결정.
- 1단계로 게임 시간을 GameManager 요청 방식으로 바꿨다. 상세는 intent-064 "진행 기록 1".
  - `GameManager.PauseGameTime(requester)`/`ResumeGameTime(requester)` + `ApplyGameTime()`(설정창 Pause 또는 요청 하나라도 → 0).
  - UIManager·CinematicManager 호출을 요청 방식으로, 서로 다시 멈추던 땜질 두 곳 삭제.
- 이전 변경(StageManager 시작 연출 호출 제거, 문서) 커밋: `d770cb40`, `3e59ad21`. 시간 단계 커밋: `29605df6`, `93c3c87f`.
- 2단계로 입력 맵 담당 `Core.InputMap.InputMapController`를 만들어 `Canvas_InGame` 프리팹 `UIManager` 오브젝트에 붙였다. 상세는 intent-064 "진행 기록 2".
  - `RequestMap(requester, EInputMapType)`/`ReleaseMap(requester)`, 우선순위 UI > Dialogue > Player, 플레이어 스폰 때 다시 적용, 꺼지면 Player.
  - UIInputHandler는 입력 이벤트만, UIManager는 Presenter별로 요청, CinematicManager는 자기 이름으로 Dialogue 요청.
  - 커밋: `5feeed10`, `7c1f816a`, `42671b58`.
- 3단계로 UIManager의 연출 전용 코드(`_isCinematicActive`·`BeginCinematic`·`EndCinematic`)를 지웠다. 상세는 intent-064 "진행 기록 3".
  - CinematicManager: `Play` 활성 검사·시작 조건 직접 확인, static `OnPlayStarted/OnPlayFinished`, 대화 단계는 `PlayCinematicDialogue`.
  - UIManager: 연출 중 `PlayDialogue` 거절, 인게임 UI 숨기기를 요청 방식(`_inGameUIHiders`)으로, 연출 이벤트 구독.
  - 커밋: `e7585e40`, `3229234d`.
- 4단계로 Brain `IgnoreTimeScale` 저장·복구를 DialogueDirector·CinematicManager에서 지웠다(켜기만 함). CinematicManager는 기본 블렌드만 저장·복구. 상세는 intent-064 "진행 기록 4".

## 2. 결정 사항
- 시간 = GameManager(싱글톤, 설정창 Pause와 같은 곳). 씬 로드 때 파괴된 요청자만 지운다.
- 입력 맵 = 씬 안 새 Core 컴포넌트(`Canvas_InGame` 프리팹), Instance 접근. 우선순위 설정창(UI) > 조작 막기(Dialogue) > Player. `UIInputHandler`는 ESC·E 이벤트만.
- 연출 중 외부 `PlayDialogue`는 거절. `CinematicManager.Play`는 꺼져 있으면 false.
- Brain `IgnoreTimeScale`은 대화·연출 시작 때 true로만 두고 되돌리지 않는다.
- 인게임 UI 숨기기는 CinematicManager 시작·끝 이벤트를 UIManager가 구독하는 방식.
- 작업 범위: `Scripts/Core`, `Scripts/UI`만. GameManager는 본인 작성이라 수정 가능.

## 3. 현재 상태 및 이슈
- 시간 단계: 컴파일 0에러, TestScene Play(eval 직접 호출)로 확인. 씬 전환 때 요청 정리·실제 키 입력은 미확인.
- 입력 맵 단계: 컴파일 0에러, TestScene Play로 대화·펫 안내·설정창·연출 맵 전환과 버그 4(UIManager 껐다 켜기) 해결 확인. 사망 재스폰 때 다시 적용·실제 키 입력은 미확인. 커밋 안 함.
- 에디터가 백그라운드면 Play 프레임이 멈춘다. Play 중 eval로 `Application.runInBackground = true`를 주면 돈다(Play 종료 시 원래대로).
- TestScene에 UIManager가 3개 잡힌다(`FindObjectsByType` 기준, 비활성 포함 여부 미확인). 이번 작업과 무관하게 그대로 둠.
- 3단계: 컴파일 0에러, TestScene Play로 버그 2·3 해결과 UI 숨김 유지·복구 확인.
- 4단계: 컴파일 0에러, TestScene Play로 버그 1(끝 블렌드 중 새 대화) 해결과 연출 전체 흐름 확인. 커밋 안 함.
- 버그 5건 모두 TestScene에서 해결 확인. 4단계 커밋: `1f690a5d`, `29de6a96`.
- 키 입력(E·ESC, `simulate_key`)으로 대화·설정창·연출, 사망 재스폰 때 새 플레이어에 Dialogue 맵 다시 적용까지 TestScene에서 확인(intent-064 진행 기록 5). 코드 변경 없음.
- intent-064는 맵 씬·씬 전환 요청 정리가 미확인이라 열어 둠.

- 후속 [intent-065](../../../intent/intent-065-dialogue-director-core.md): 남은 구조 문제 2개 정리.
  - `DialogueDirector`를 `Core/Cinematic`으로 이동(GUID 유지, `ShowLine(speakerName, cameraIndex)`).
  - Core가 UIManager를 직접 부르지 않게 `IDialogueController` + `DialogueController.Current`(UIManager가 OnEnable 등록·OnDisable 해제). CinematicManager·StageManager의 `using UI` 삭제.
  - 컴파일 0에러, 프리팹 연결 유지, TestScene Play로 대화 카메라 교대·연출 흐름·외부 대화 거절·UIManager 껐다 켜기 확인. 커밋 안 함.

- intent-065 커밋: `6d4677f1`, `a87d110e`, `b4581cb2`. 이름은 `IDialogueController`/`DialogueController`.
- 재검토 P2 수정: `InputMapController.OnDisable`이 요청을 지우지 않고 Player 맵으로만 돌려 두고, `OnEnable`에서 남은 요청을 다시 적용. TestScene에서 연출 중 UIManager 오브젝트 껐다 켜기 → Dialogue 복원 확인(intent-064 진행 기록 6). 커밋 안 함.

## 4. 다음 할 일
- 맵 씬(맵 담당 확인 또는 허락 후)에서 대화·연출·설정창 확인, 씬 전환 뒤 시간이 멈춘 채 시작하지 않는지 확인.
- 확인되면 intent-064 해결 처리(`clear/`로 이동, INTENT_POINTER 해결됨으로).
- (선택) 2단계 구조 정리: DialogueDirector를 Cinematic 쪽으로 옮길지 판단.
- 끝나면 TestScene에서 버그 5건 재현 확인 후 intent-064 해결 처리.
