---
id: intent-055
title: 파일 앱 라운드별 로직 분리와 3라운드 제한 시간
part: Stage / FileApp / UI
status: open   # open | resolved
created: 2026-10-21
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- FileApp에는 2라운드 숫자 퍼즐만 있다. 1·3라운드 로직은 FileApp 밖에 흩어져 있거나 아예 없다.
- 라운드 클리어 판정을 맡는 곳이 없다. 그래서 3라운드 출구 문은 도둑을 잡지 않아도 열린다(`MazeExitGate.IsRoundCleared()`가 TODO).
- 3라운드에 제한 시간이 없다.
- 2라운드 패널은 계산 순서(왼쪽부터)가 눈에 보이지 않는다. `2 + 3 × 4`는 실제로 `(2 + 3) × 4`로 계산되는데, 화면에서는 그렇게 보이지 않는다.

## 기대 결과 (Proposed outcome)
- 공통 부모 `StageRoundBase`를 둔다. 라운드 컴포넌트가 각자 자기 라운드의 시작·종료, 사망 시 리셋, 클리어 여부를 맡는다.
  - 1라운드: 컴포넌트 없음. 지금처럼 출구에 닿으면 끝이다.
  - 2라운드: `MazeNumberPuzzle`이 상속한다. 2라운드에 들어가면 켜지고, 사망하면 리셋되고, 정답이면 클리어다.
  - 3라운드: `ThiefChaseRound`를 둔다.
    - 인스펙터에서 정한 제한 시간 안에 도둑(`ThiefSpawner`)을 다 잡으면 클리어다.
    - 시간이 다 되면 플레이어가 죽는다. 3라운드 시작점에서 부활하고 타이머는 처음부터 다시 센다.
- FileApp은 맵 연결만 맡는다. 라운드 컴포넌트마다 같은 번호의 미로 출구 문을 막아 두었다가, 그 라운드를 깨면 연다.
- Active Mission 패널에 진행 중인 라운드를 표시한다. 3라운드는 `남은 시간 n초 · 도둑 a/b`다.
- 2라운드 패널: 앞 연산자가 +·−이고 뒤 연산자가 ×·÷이면 `(4 + 9) × 2`처럼 괄호를 보여 준다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 파일 앱 스테이지를 플레이하는 사람
- 어떤 시스템/모듈이 건드려지는가:
  - Core/Stage: 새 파일 `StageRoundBase`, `ThiefChaseRound`. 수정하는 파일 `MazeNumberPuzzle`, `FileApp`
  - UI/InfoPanel: `InfoPanelPresenter`
  - 시험: TestScene (`NumberPuzzleTest`, 3라운드용 `ThiefChaseRound`와 `Thief_Spawner`), Test 폴더 `TestNumberPuzzleZone`

## 제약 (Constraints)
- 코드는 Core 스크립트 안에서만 수정한다. 패널 표시만 예외로 UI `InfoPanelPresenter`를 수정한다(사용자 허락).
- AI(`ThiefSpawner`)와 Map(`MazeExitGate` 등) 스크립트는 읽기만 하고 수정하지 않는다.
- `Map_3_File` 등 맵 씬은 열지도 저장하지도 않는다. 기능 시험은 TestScene에서 한다.
- 시간 초과 사망은 플레이어 체력을 0으로 만들어 기존 사망 흐름(StageManager 재스폰)을 그대로 탄다.

## 열린 질문 (Open questions)
- (없음) 타이머 표시(Active Mission), 3라운드 클리어 기준(도둑 전부 잡기), 사망 후 처리(3라운드 시작점에서 처음부터), TestScene 도둑 스포너 배치, 괄호 표시는 2026-10-21 사용자가 결정했다.
- 이번 범위 밖: 스테이지 완료 보고, 2라운드 제한 시간, 2라운드 가비지 컬렉터 제거, `MazeExitGate` TODO 연결

## 진행 상황 (2026-10-21)
- 구조 변경(사용자 요청): 라운드를 씬 컴포넌트가 아닌 FileApp 안의 일반 클래스(`StageRound`, `NumberPuzzleRound`, `ThiefChaseRound`)로 바꿨다.
  - 맵 담당자는 노드 프리팹(13개 이상)과 패널 프리팹만 놓는다.
  - FileApp이 시작할 때 씬에서 찾아 값을 붙인다.
- 코드 반영 완료, 컴파일 0에러. TestScene(FileApp 켬, FeedApp 끔)에서 확인했다.
  - 노드 값 배정과 리셋 재배정
  - 2R 퍼즐 클리어
  - 3R 타이머, 시간 초과 사망·재스폰, 도둑 클리어
- 남은 것
  - 실제 사격 확인
  - 맵에 노드·패널 배치(맵 담당자)
  - 노드·패널이 없을 때 2R 문을 막을지 결정

## 후속 설계 (2026-10-21)

- 사용자 요청: StageManager와 StageBase의 책임을 명확하게 나누고, StageRound를 사용하는 FileApp·FeedApp 공통 실행 구조를 Markdown으로 설계한다.
- 산출물: [StageManager · StageBase · StageRound 책임 분리 설계](../design/stage-lifecycle-responsibilities.md).
- 설계 제안: Manager는 진행 확정·체크포인트·플레이어 복구, Base는 라운드 공통 실행, Round는 개별 게임 규칙, 각 앱은 구성·전환 조건·맵 연결을 담당한다.
- 현재 라운드 번호는 Manager, 전체 라운드 수는 앱의 Base 구성, 게임 상태는 각 Round를 원본으로 사용한다.
- 미로 체크포인트 전환과 피드 버튼 전환, 사망 후 스폰·능력치 복원 이후 재시작, 최종 클리어와 후속 연출의 차이를 문서에 정의했다.
- 이번 요청 범위는 설계 문서 작성이다. 런타임 코드·씬·프리팹을 수정하지 않았으며 설계는 구현·검증 완료를 의미하지 않는다.
- 기존 구현의 남은 검증과 제약은 유지한다. 피드 버튼·Map 계약 변경은 별도 후속 구현 범위이며, 이 기록만으로 변경 제한을 해제하지 않는다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
