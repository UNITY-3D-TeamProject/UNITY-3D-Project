---
id: intent-054
title: 파일 앱 2라운드 미로 숫자 퍼즐
part: Stage / FileApp / UI
status: open   # open | resolved
created: 2026-10-21
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`Map_3_File`의 2라운드(두 번째 미로 Maze_1)에는 퍼즐이 없다. 출구에 들어가기만 하면 문이 열린다.
`FileApp`은 비어 있다. `MazeExitGate.IsRoundCleared()`는 TODO라 항상 true를 돌려준다.

## 기대 결과 (Proposed outcome)
- Maze_1에 숫자 1~9와 연산자 + − × ÷, 모두 13개의 아이템이 무작위 칸에 놓인다.
- 숫자는 3개, 연산자는 2개까지만 먹을 수 있다.
- 출구 문 앞에 [숫자][연산][숫자][연산][숫자] 5칸이 있다. 칸을 쏠 때마다 가진 값이 순서대로 바뀐다.
- 가진 숫자 3개와 연산자 2개를 각각 한 번씩 써서 목표값을 만들면 Maze_1의 출구 문이 열린다.
- 계산은 왼쪽부터 차례로 한다. 나누어떨어지지 않는 나눗셈은 정답이 아니다.
- 목표값은 2라운드를 시작할 때 미리 계산한다. 만들 수 있는 아이템 조합이 여러 개인 값만 고른다.
- 리셋 장치를 쏘거나 2라운드에서 죽으면 가진 아이템과 칸 값이 비워진다. 아이템은 새 무작위 칸에 다시 놓이고, 목표값은 그대로다.
- Active Mission 패널에 `2라운드 / 목표 n · 숫자 … · 연산 …`이 표시된다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 파일 앱 스테이지를 플레이하는 사람
- 어떤 시스템/모듈이 건드려지는가:
  - 새 스크립트 `Core/Stage/MazeNumberPuzzle.cs`: 퍼즐 로직 (미로와 무관)
  - `Core/Stage/FileApp.cs`: Maze_1 위치를 넘기고, 문을 막았다가 열고, 2라운드일 때 퍼즐을 켠다
  - 새 스크립트 `Core/Stage/`: `EMazeOperator`, `SMazeToken`, `MazePuzzleItem`, `MazePuzzleBoard`
  - `UI/InfoPanel/InfoPanelPresenter.cs`: 퍼즐 표시 추가
  - 새 프리팹 `Prefabs/Map/Maze/Puzzle/`: 아이템, 퍼즐 패널
  - 테스트: TestScene `NumberPuzzleTest` 구역 (맵 씬은 수정하지 않는다)

## 제약 (Constraints)
- 지켜야 할 정책: Map 스크립트는 수정하지 않는다. `DamageButton`, `MazeExitGate.Open()`, `MapGeneratorBase.GeneratedRoot`, `SOMazeChainSettings`는 가져다 쓰기만 한다.
  - Maze_1 문은 Core에서 트리거 콜라이더를 꺼서 막아 두고, 정답 때 `Open()`을 호출한다.
  - 아이템 위치는 설정 에셋의 `Size`·`TileSize`로 Maze_1 칸 중앙을 계산한다. 입구 줄과 출구 줄은 뺀다.
- 쓰지 말아야 할 것: 기획보드의 "NPC 대결" 안은 이번 범위가 아니다.

- 맵 씬(`Map_3_File`)은 수정 내역이 있으면 안 된다 (2026-10-21 사용자 지시). 씬 연결은 맵 담당자 몫이다.

## 변경 (2026-10-21, 사용자 요청)
- 리셋(리셋 장치, 사망)하면 목표값도 새로 고른다. 처음 결정했던 "목표값 유지"는 바꿨다.
- 아이템은 닿으면 먹는 대신 상호작용(E)으로 먹는다. 노드 프리팹은 Interact 레이어다.
- 퍼즐 로직은 FileApp 안 `NumberPuzzleRound`로 옮겼다(intent-055). 맵에는 노드·패널 프리팹만 놓는다.
- 괄호 표시는 없앴다.
- 숫자 칸·연산 칸 모두 같은 종류끼리 값이 겹치지 않는다. 쏘면 다음 값으로 바뀌고, 그 값을 다른 칸이 쓰고 있으면 서로 자리를 바꾼다(`+ ×` → `× +`, `3 2 4` → `4 2 3`).

## 변경 (2026-10-21, 아이템 위치)
- 아이템 위치는 미로 생성기(`MazeChainGenerator`)의 웨이포인트로 정한다. `Map_3_File`에 `PuzzleWaypoints/PuzzleWaypoint_0~12`(자식으로 `MazePuzzleItem`)를 두고 `_waypointGroups`에 `MazeIndex: 1` 그룹으로 넣는다.
- 위치 13곳은 맵을 만들 때 Maze_1 빈 칸 가운데로 한 번 정해지고 그 판 동안 바뀌지 않는다. 리셋(리셋 장치, 사망) 때는 `NumberPuzzleRound`가 13곳에 값을 다시 섞어 붙인다. 코드 변경 없음.
- 이번 작업에 한해 사용자가 `Map_3_File` 수정을 허락했다(아이템·웨이포인트 연결만).

## 변경 (2026-10-21, 패널 위치)
- 출구 칸은 생성할 때마다 바뀐다. 그래서 패널(`MazePuzzleBoard`)은 맵에 하나만 놓아 두고, 스테이지를 시작할 때 `FileApp`이 2라운드 출구 문 앞으로 옮긴다.
  - 위치는 문 기준 로컬 (0, 0, `_puzzleBoardOffset`)이다(기본 1.5 = 출구 칸 가운데에서 문 쪽으로 1.5 m). 회전은 문과 같다.
  - 미로가 없는 씬(TestScene)에서는 옮기지 않는다.
- 사용자가 `Map_3_File`에 패널 하나를 놓는 것을 허락했다.

## 열린 질문 (Open questions)
- (없음) 계산 순서, ÷의 의미, 순환 규칙, 리셋, 사망 시 초기화, 목표값 고르는 방식, 정답 시 문 열림은 2026-10-21 사용자가 결정했다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
