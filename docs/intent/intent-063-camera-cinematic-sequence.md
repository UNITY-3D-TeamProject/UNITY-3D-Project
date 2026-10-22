---
id: intent-063
title: 대화와 따로 쓰는 카메라 연출 (카메라 샷·대화 단계 목록)
part: Camera / Cinematic / UI
status: open   # open | resolved
created: 2026-10-22
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
지금 카메라를 바꿀 수 있는 곳은 컷씬 대화(`UIManager.PlayDialogue(id)`)뿐이다.

- 대사 한 줄(`SDialogueLine`)의 `CameraIndex`가 0 이상이면 `DialogueDirector._fixedCameras` 목록의 카메라, -1이면 말하는 사람 카메라를 켠다.
- `DialogueDirector`는 `Canvas_InGame` 프리팹 안에 있다. 그래서 씬 카메라를 쓰려면 씬마다 프리팹 오버라이드로 `_fixedCameras`를 채워야 한다.
- 대화 중에는 `GameManager.PauseGameTime()`(timeScale 0)이라 화면 속 물체가 멈춘다. 카메라는 바뀌지만 "뒤에서 굴러오는 것"처럼 움직이는 장면은 보여 줄 수 없다.
- 대화 없이 카메라만 쓰는 연출이 없다. 예를 들면 다음과 같은 것들이다.
  - 로비 첫 입장 시 전경을 보여 준 뒤 펫·플레이어 대화 시작
  - 어떤 곳을 밟으면 게임이 멈추고 다른 곳을 카메라로 쓱 보여 주기
  - 미로 스테이지 시작 시 미로 가운데에서 플레이어까지 카메라가 내려오며 시작

## 기대 결과 (Proposed outcome)
씬에 연출 하나당 컴포넌트 하나(`CinematicSequence`)를 두고, 단계 목록으로 연출을 만든다.

| 단계 | 내용 |
|---|---|
| 카메라 샷 | CinemachineCamera, 보여 줄 시간(초), 게임 시간 멈춤 여부, 들어오는 블렌드 시간(초) |
| 대화 | 대화 번호 → `UIManager.PlayDialogue(id)`, 대화가 끝나면 다음 단계 |

- 연출 전체 설정:
  - 연출 Id
  - "한 번만 재생" 여부
  - 끝날 때 플레이어 카메라로 돌아가는 블렌드 시간(초)
- `CinematicManager`(씬에 하나, `Instance`)가 `Play(sequence)`로 단계를 차례로 실행한다. 연출이 시작될 때부터 끝날 때까지 다음을 맡는다.
  - 입력: 기존 `Dialogue` 입력 맵으로 바꾼다. 샷 단계에서는 E에 동작이 없고, 대화 단계에서는 지금처럼 E로 넘긴다. ESC 설정창은 그대로 열린다.
  - 인게임 UI: 연출 내내 숨긴다(UIManager에 요청). 대화창은 대화 단계에서만 보인다.
  - 게임 시간: 샷 단계 설정에 따라 `GameManager.PauseGameTime`/`ResumeGameTime`. 대화 단계는 지금처럼 멈춘다.
  - 카메라 전환: 시간이 멈춰도 블렌드가 돌도록 CinemachineBrain을 게임 시간과 상관없이 돌린다(`IgnoreTimeScale`).
  - 플레이어 무적은 첫 구현에 넣지 않는다. 시간이 흐르는 샷은 적이 없는 곳에서만 쓴다.
  - 끝나면 위 상태를 모두 원래대로 돌린다.
- 카메라가 "쓱" 움직이는 효과는 Cinemachine 블렌드를 쓴다. 샷 카메라를 켜면 그 샷의 블렌드 시간으로 넘어가고, 마지막에 끄면 연출의 끝 블렌드 시간으로 플레이어 카메라에 돌아간다.
- 의존은 한 방향이다: `CinematicManager` → `UIManager`.
  - 대화만 띄울 때: 지금처럼 `UIManager.PlayDialogue`
  - 카메라가 끼는 연출: `CinematicManager.Play`
- 예시에 대입하면 다음과 같다.

| 예시 | 단계 | 시작하는 곳 |
|---|---|---|
| 로비 첫 입장 | [전경 샷 3초] → [대화 N], 한 번만 재생 | `StageManager.PlayIntroDialogue` 자리 |
| 발판 밟으면 다른 곳 보여주기 | [먼 곳 샷 2초, 시간 멈춤] | 발판 `TriggerPlate` UnityEvent |
| 미로 시작 | [미로 가운데 위 샷 1초], 끝 블렌드 2초로 플레이어 카메라 | 스테이지 시작 |
| 튜토리얼 뒤에서 굴러오는 것 | [대화 3] → [굴러오는 쪽 샷 2초, 시간 흐름] → [대화 4] | 튜토리얼 진행 |

## 결정 사항 (2026-10-22 사용자 답변)
1. 시간이 흐르는 샷 동안 적 공격: **첫 구현에서는 무적을 넣지 않고, 시간이 흐르는 샷은 적이 없는 곳에서만 쓴다.** 처음에는 "연출 동안 무적"으로 답했으나, `PlayerFacade`에 원하는 동안 무적을 켜는 공개 함수가 없어 바꿨다. 무적이 필요해지면 Facade·Mediator 담당과 함수 추가를 따로 협의한다.
2. 연출 중 입력: **기존 `Dialogue` 입력 맵을 같이 쓴다.**
3. 건너뛰기: **일단 없음.** 필요하면 나중에 추가한다.
4. 한 번만 보기: **본 기록을 저장한다.** 연출 Id + PlayerPrefs로 저장하고, "한 번만 재생"을 켠 연출만 해당한다.
5. 둘 자리: **맵 씬마다 씬 오브젝트.** 폴더·네임스페이스는 **`Scripts/Core/Cinematic` → `Core.Cinematic`**.
6. [대화] → [샷] 이음새: **연출 안에서 연 대화는 끝나도 시간·입력·UI를 돌려주지 않는 옵션을 UIManager에 둔다.** 연출이 끝날 때 `CinematicManager`가 한 번에 돌려준다.
7. 블렌드 시간: **샷마다 따로** 정하고, 끝 블렌드(플레이어 카메라로 복귀)는 연출 설정에 둔다.
8. 기존 `CameraIndex` 고정 카메라: **일단 유지.** 씬 카메라 샷은 앞으로 연출 단계에 넣도록 권장한다.
9. 연출 중 펫: **그대로 따라온다.** 샷 단계에서는 펫을 건드리지 않고, 대화 단계는 지금처럼 `DialogueDirector`가 펫을 앞에 세운다.
   - 9-1. 인게임 UI: **연출 내내 항상 숨긴다.**

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 맵·스테이지 담당(씬에 연출 카메라·`CinematicManager`·`CinematicSequence` 배치, 발판 연결), 기획(단계 구성·시간).
- 어떤 시스템/모듈이 건드려지는가:
  - 새 파일 (`Assets/_Project/Scripts/Core/Cinematic/`, 네임스페이스 `Core.Cinematic`):
    - `CinematicManager`: 씬 오브젝트, `Instance`, `Play(sequence)`, 진행 중 여부
    - `CinematicSequence`: 단계 목록 + 연출 설정
    - 단계 구조체(`S...` 접두사)
  - 수정(예상):
    - `UI/UIManager.cs`
      - 연출용 공개 함수: 인게임 UI 숨김·복구, `Dialogue` 입력 맵 전환·복구
      - "끝나도 돌려주지 않는" 대화 시작 옵션
      - 대화 끝 알림 이벤트. 지금은 밖에서 대화가 끝난 것을 `IsDialoguePlaying` 값으로만 알 수 있다.
    - `UI/UIInputHandler.cs`: 연출 중 `Dialogue` 맵 유지에 필요한 경우만.
    - `Core/Stage/StageManager.cs`: 시작 대화 자리에서 그 씬의 시작 연출이 있으면 연출을 부른다.
  - 건드리지 않음: `PlayerFacade`·Mediator. 무적은 첫 구현 범위 밖이다.
  - 그대로 둠:
    - `DialogueDirector`: 말하는 사람 카메라·`CameraIndex` 고정 카메라 유지
    - `GameManager.PauseGameTime`/`ResumeGameTime`: 그대로 사용
    - 독백·펫 안내 동작
  - 씬: 연출 카메라와 `CinematicManager`는 각 씬 오브젝트다. 첫 구현·확인은 개인 TestScene에서 한다.
- 관련 intent: [intent-044 스테이지 첫 입장 대화](./intent-044-stage-intro-dialogue.md), [intent-046 컷씬 테이블](./intent-046-cutscene-table.md)

## 제약 (Constraints)
- 예산/기한: 없음. 첫 단계는 카메라 샷·대화 두 종류만 만든다.
- 지켜야 할 정책:
  - UIManager는 UI 담당으로 둔다. 연출 실행 흐름(단계 진행·시간·카메라)은 UIManager에 넣지 않는다.
  - 기존 대화 흐름(`PlayDialogue`·`DialogueDirector`·`CameraIndex`)과 독백·펫 안내의 지금 동작은 바꾸지 않는다. 새 옵션은 기본값이 지금 동작이어야 한다.
  - 연출이 끝나거나 중간에 끊기면(씬 이동·비활성화) 시간·입력·UI·CinemachineBrain을 반드시 원래대로 돌린다.
  - 시간이 흐르는 샷(`게임 시간 멈춤` 끔)은 적이 없는 곳에서만 쓴다. 연출 동안 플레이어는 무적이 아니다.
  - 작업 씬은 개인 TestScene만 쓴다. 맵 씬 연결은 맵 담당 몫이다.
- 쓰지 말아야 할 것: 첫 단계에서는 Timeline(`PlayableDirector`)을 쓰지 않는다. 경로 이동 등 정교한 연출이 필요해지면 "Timeline 재생" 단계 추가를 따로 검토한다.

## 열린 질문 (Open questions)
- 없음 (2026-10-22 모두 결정). 구현 중 새로 생기면 여기에 적는다.

## 진행 기록 (2026-10-22 1차 구현)
- 새 파일 `Scripts/Core/Cinematic/`
  - `CinematicSequence`: `ECinematicStepType`(Shot·Dialogue), `SCinematicStep`, Id·한 번만 재생·끝 블렌드. Awake에서 샷 카메라를 끄고, `Play()`는 UnityEvent용이다.
  - `CinematicManager`: `Instance`, `Play`·`Stop`·`PlayIntro`, 본 기록 키 `Cinematic.Seen.{Id}`.
- `UIManager`
  - `BeginCinematic()`(bool)·`EndCinematic()` 추가.
  - 연출 중이면 대화가 끝나도 시간·입력·UI를 돌려주지 않는다.
  - 연출 중에는 펫 안내·독백을 막는다.
  - 설정창 닫기와 플레이어 교체 때는 Dialogue 맵을 유지한다.
- `StageManager.PlayIntroDialogue`: 시작 연출이 있으면 연출을 먼저 재생한다.
- 설계와 달라진 점
  - 대화 끝 알림 이벤트는 만들지 않았다. 연출이 `UIManager.IsDialoguePlaying`을 매 프레임 확인해 다음 단계로 넘어간다.
  - 발판 트리거(물리 콜백) 안에서 코루틴을 바로 시작하면, 게임 시간을 멈춘 뒤 단계가 진행되지 않았다. 그래서 `Play`는 상태만 바꾸고 코루틴은 다음 `Update`에서 시작한다.
  - 설정창이 열린 동안에는 샷 시간을 세지 않는다. 닫은 뒤 멈추는 샷이면 시간을 다시 멈춘다.
- TestScene 확인(EditorApplication.Step 프레임 진행)
  - 시작 연출: 1.5초 전경 → 2초 블렌드로 플레이어 카메라 복귀. 본 기록 뒤 다음 Play에서는 생략된다.
  - 발판 연출: [퍼즐 전경 2.5초, 시간 멈춤] → [대화 1번 4줄] → [몬스터 바닥 3초, 시간 흐름(적 이동 확인)] 순서로 진행된다. 대화와 샷 사이에 입력·UI가 새지 않았고, 끝나면 Player 맵·UI·Brain 블렌드 0.2초로 복구된다.
  - 샷 중 설정창: 시간을 세지 않고, 닫으면 Dialogue 맵·시간 정지로 돌아온다. 대화 중 `Stop()`을 부르면 모두 복구된다.
- 남은 것: 실제 키보드 조작 확인, 맵 씬 배치(맵 담당).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
