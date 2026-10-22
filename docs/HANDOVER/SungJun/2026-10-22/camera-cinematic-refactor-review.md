# 시네마틱 리팩토링 재검토

## 1. 작업 요약 및 방법
- intent-064/065 이후 현재 시네마틱·대화·시간·입력 구조를 재검토했다.
- GameManager, InputMapController, UIManager, UIInputHandler, CinematicManager, CinematicSequence, DialogueDirector, IDialogueController, DialogueController 및 StageManager 연결을 추적했다.
- DialogueDirector 이동 전후 메타 GUID와 Canvas_InGame 프리팹 참조가 같은 것을 확인했다. Core의 using UI와 이전 대화 타입 참조도 검색했다.
- Unity Play 및 전체 프로젝트 컴파일은 실행하지 않았다. 기존 작업 기록의 Play/컴파일 결과는 이번 검토에서 새로 실행한 결과와 구분한다.

## 2. 결정 사항
- 시간은 GameManager, 입력 맵은 InputMapController가 요청을 모아 결정하는 현재 방향이 적절하다.
- Core가 IDialogueController를 통해 대화를 요청하고 UIManager가 등록·구현하는 방식, DialogueDirector를 Core.Cinematic으로 옮긴 방식도 현재 범위에서 타당하다.
- 이전 Brain.IgnoreTimeScale 복구 충돌은 복구 제거로 사라졌다. 항상 true로 두는 정책을 유지하는 설계다.
- 외부 대화 거절과 비활성 CinematicManager의 Play 차단도 현재 코드에 반영되어 있다.
- UIManager 컴포넌트만 껐다 켜는 경우에는 CinematicManager의 입력 요청이 독립적으로 유지된다.

## 3. 현재 상태 및 이슈
- **P2: InputMapController 자체를 껐다 켜면 진행 중인 연출의 입력 요청이 유실된다.**
  - InputMapController.OnDisable이 모든 요청을 지우고 Player 맵을 적용한다.
  - OnEnable은 현재 플레이어를 다시 연결하지만, 활성 연출의 Dialogue 요청을 복원하지 않는다.
  - CinematicManager는 Play 시 한 번만 RequestMap을 호출하므로 연출이 계속되는 동안에도 Player 맵이 남는다.
  - 공용 Canvas_InGame 프리팹에서는 UIManager와 InputMapController가 같은 오브젝트에 있으므로 UIManager 오브젝트나 상위 Canvas를 비활성화하면 이 경로가 생긴다. UIManager 컴포넌트만 비활성화하는 경우와 구분해야 한다.
  - 실제 InputMapController.cs를 읽어 최소 Unity/GameManager/PlayerFacade 모의 타입과 함께 메모리에서 컴파일하고, Awake/OnEnable → Dialogue 요청 → OnDisable → OnEnable을 실행했다. 소유자가 요청을 해제하지 않았는데 입력 맵이 Dialogue → Player → Player로 남는 것을 확인했다. Unity Play 재현은 하지 않았다.

## 4. 변경된 파일
- 이 검토 문서와 HANDOFF_POINTER.md만 변경했다. 구현·씬·프리팹은 수정하지 않았다.

## 5. 다음 할 일
- 입력 컨트롤러의 비활성화 정책을 정한다. 활성 요청을 보관했다가 재적용하거나, 요청 소유자가 재활성화 때 재등록하도록 보완할 수 있다. 완전 종료라면 연출도 함께 중단하는 정책이 필요하다.
- TestScene에서 시간이 흐르는 긴 샷 도중 UIManager 오브젝트를 껐다 켜고, 연출 중 Dialogue 맵이 유지되는지 확인한다.
- 맵 씬·씬 전환 검증은 기존 미확인 상태를 유지한다.
