# 시네마틱·대화 구조 검토

## 1. 작업 요약
- 사용자 요청에 따라 현재 CinematicManager, CinematicSequence, UIManager, UIInputHandler, DialogueDirector, DialoguePresenter, MonologuePresenter, GameManager와 스테이지 시작 연결을 검토했다.
- 구현 변경 없이 상태 충돌과 비활성화 후 복구 누락을 확인했다.

## 2. 방법/접근
- 재생 진입, 샷/대화 전환, 연출 종료 후 복귀 블렌드, 독립 대화 시작, 컴포넌트 비활성화 경로를 정적으로 추적했다.
- 로컬 Cinemachine 패키지의 Brain 코드에서 IgnoreTimeScale과 DefaultBlend의 사용 위치를 확인했다.
- 이번 검토에서는 Unity Play 재현이나 컴파일을 실행하지 않았다. 아래 항목은 코드 흐름에 따른 검토 결과다.

## 3. 변경된 파일
- 이 검토 기록과 HANDOFF_POINTER.md만 변경했다. 기존 구현·씬·에셋 변경은 수정하지 않았다.

## 4. 결정 사항
- 시퀀스(구성), 매니저(진행), UIManager(UI·입력), DialogueDirector(대화 구도)의 역할 분리 방향은 유지할 수 있다.
- UIManager와 CinematicManager를 합칠 필요는 없다. 공유 상태의 제어·복구와 재생 진입 경계를 보완하는 것이 우선이다.
- 독백과 시네마틱 동시 재생 미지원은 현 기능 범위이며 별도 버그로 분류하지 않았다.

## 5. 현재 상태 및 이슈
1. **샷 도중 외부 대화 진입 허용**: UIManager.StartDialogue는 _isCinematicActive를 검사하지 않고 시퀀스 내부 요청과 외부 PlayDialogue 호출을 구분하지 않는다. 샷 중 외부 대화가 시작되면 샷 카메라와 대화 카메라가 동시에 활성화되고 두 실행 흐름이 시간을 제어한다. 마지막 샷이 끝나면 Finish는 진행 중인 외부 대화를 종료하지 않은 채 UI·입력을 복구할 수 있다.
2. **종료 블렌드 중 독립 대화와 Brain 복구 충돌**: Finish는 즉시 EndCinematic을 부르지만 Brain 복구는 CoRestoreBrain으로 지연한다. 그 사이 독립 대화를 시작하면 DialogueDirector가 IgnoreTimeScale=true를 원래 값으로 저장한다. 이후 이전 연출의 RestoreBrain이 false로 덮어쓰고, 대화 종료는 true를 복구하므로 대화 중 카메라 시간 처리가 깨지고 종료 후에도 설정이 남을 수 있다.
3. **비활성 CinematicManager의 Play 허용**: Play에 isActiveAndEnabled 검사가 없고 OnDisable은 Instance를 유지한다. 비활성 매니저에 연결된 Sequence.Play를 호출하면 BeginCinematic으로 입력을 막고 본 기록까지 남기지만 Update가 실행되지 않아 진행·자동 복구가 없다.
4. **UIManager 단독 비활성화 후 입력 복구 누락**: 샷 도중 UIManager를 비활성화하면 OnDisable이 입력 참조를 해제하지만 진행 중인 시네마틱은 계속된다. UIManager가 꺼진 동안 Finish가 EndCinematic을 호출하면 입력 참조가 없어 Player 맵으로 돌아가지 못한다. 재활성화 후 HandlePlayerSpawned도 일반 상태에서 Player 맵을 명시적으로 복구하지 않아 Dialogue 맵이 남을 수 있다. 샷이 유효하고 양의 Duration이며 CinematicManager가 별도 활성 오브젝트에 있는 경우를 기준으로 한 경로다.

## 6. 다음 할 일
- 수정 시 시퀀스 소유 대화 요청과 외부 대화 요청을 구분하고, 외부 대화는 연출 중 시작하지 못하게 한다.
- Brain 설정을 변경하는 주체끼리 제어 인계를 명시해 이전 연출의 지연 복구가 새 대화를 덮지 않게 한다.
- 매니저 활성 상태 검사와 UIManager 비활성화 시 연출 중단·정리 경로를 보완한다.
- 위 네 시나리오를 Play에서 재현하고 입력 맵, timeScale, HUD, 활성 카메라, Brain.IgnoreTimeScale의 최종 상태를 검증한다.
