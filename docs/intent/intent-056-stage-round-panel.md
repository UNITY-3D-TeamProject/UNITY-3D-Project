---
id: intent-056
title: 미로 패널에 지금 라운드 표시와 라운드 번호의 StageBase 공통화
part: Stage / FileApp / UI
status: open   # open | resolved
created: 2026-10-21
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- 미로 1라운드에는 라운드 로직이 없어서 Active Mission 패널이 숨겨진다. 지금 몇 라운드인지 알 수 없다.
- 지금 라운드 번호를 따라가며 바뀌면 화면에 알리는 코드가 FileApp에만 있다. 이건 앱마다 같은 일이다. 진행 알림 이벤트(`OnProgressChanged`)도 FileApp·FeedApp이 각자 따로 갖고 있다.
- TestScene에는 미로처럼 1→2→3라운드를 이어서 해 볼 공간이 없다. 그래서 퍼즐을 1라운드 번호로 바꿔서 시험하고 있다.

## 기대 결과 (Proposed outcome)
- 파일 앱 패널은 1라운드부터 제목에 `N라운드`를 보여 준다. 본문은 다음과 같다.
  - 1라운드: 비움
  - 2라운드: 먹은 숫자·연산자 (`숫자 3 5 _ · 연산 + _`). 목표값은 맵의 퍼즐 패널에 보인다.
  - 3라운드: 남은 도둑 수와 남은 시간 (`남은 도둑 17명 · 남은 시간 45초`)
- `StageBase`가 공통 부분을 맡는다.
  - `CurrentRoundIndex`: StageManager 값을 매 프레임 따라간다. 진행 전에는 -1이다.
  - 라운드가 바뀌면 `OnRoundChanged()` 훅을 부르고 `OnProgressChanged`로 알린다.
  - 자식 앱은 `NotifyProgressChanged()`로 진행을 알린다.
- FileApp은 `OnRoundChanged`에서 라운드 로직을 켜고 끄며, Update에서 진행 중인 라운드를 Tick한다. FeedApp은 자기 이벤트를 버리고 공통 이벤트를 쓴다(동작은 같다).
- TestScene에 1라운드 → 2라운드(숫자 퍼즐) → 3라운드(도둑 잡기)로 이어지는 공간을 대충 만들어, 체크포인트 진행·사망 재스폰·패널 갱신을 확인한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 파일 앱(미로)·피드 앱을 플레이하는 사람, TestScene으로 시험하는 김성준
- 어떤 시스템/모듈이 건드려지는가: Core/Stage `StageBase`·`FileApp`·`FeedApp`, UI `InfoPanelPresenter`, 개인 TestScene

## 제약 (Constraints)
- 지켜야 할 정책:
  - `StageBase`·`FeedApp`은 CP949 인코딩을 그대로 둔다.
  - Map·AI 스크립트, 맵 씬, `Canvas_InGame` 프리팹은 수정하지 않는다.
  - TestScene 외의 씬은 열지 않는다.
- 쓰지 말아야 할 것:
  - 설계 문서의 6단계 라운드 계약, `TryAdvanceRound` 같은 큰 구조
  - StageManager 이벤트 추가. 적 스포너와 같은 매 프레임 비교 방식을 유지한다.

## 열린 질문 (Open questions)
- 없음. 3라운드 남은 시간은 패널에 넣기로 했다(2026-10-21 사용자 요청).

## 진행 기록
- 2026-10-21: StageBase 공통화·패널·TestScene 공간 구현. TestScene Play에서 순간이동으로 확인했다. 확인한 것은 체크포인트 진입, 2라운드 사망 리셋, 3라운드 도둑·시간, 시간 초과 사망이다.
- 시험 중 버그를 발견해 함께 고쳤다. 원래는 시간 초과 때 피격 무적이면 체력 감소가 막혀 0초에서 멈췄다. `ThiefChaseRound.TryKillPlayer`가 이제 실제로 0이 될 때까지 다시 시도한다.
- 2026-10-21: TestScene 3라운드 공간을 퍼즐 바닥 바로 북쪽으로 옮겼다(바닥·NavMesh·체크포인트·도둑 웨이포인트).
- 2026-10-21: 사용자 결정으로 라운드 공통 부모 `StageRound`를 지웠다.
  - `StageBase`: 지금 라운드 번호 추적, 진행 알림, `IsRoundCleared(roundIndex)`(기본 true)를 맡는다.
  - `FileApp`: 라운드 번호 상수 `NUMBER_PUZZLE_ROUND_INDEX`=1, `THIEF_CHASE_ROUND_INDEX`=2를 두고 각 라운드 로직을 직접 부른다.
    - `OnRoundChanged`에서 `SetActive`를 부른다.
    - 사망하면 그 라운드만 `ResetPuzzle`·`RestartTimer`를 부른다.
    - 라운드를 깨면 그 라운드 출구 문을 연다.
    - `IsRoundCleared`를 덮어쓴다.
  - `NumberPuzzleRound`·`ThiefChaseRound`: 부모 없는 일반 클래스다. 자기 `OnProgressChanged`·`OnCleared`를 가진다.
  - 패널: 라운드 번호로 FileApp의 두 라운드 값을 읽는다.
- 남은 일: 실제 걷기 확인, `Map_3_File` 확인(퍼즐 배치 후).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
