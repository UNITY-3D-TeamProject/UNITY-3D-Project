# 파일 앱 2라운드 미로 숫자 퍼즐 (intent-054)

## 1. 작업 요약 및 방법
- 파일 앱 2라운드(Maze_1)용 숫자 퍼즐을 만들었다. Map 스크립트와 맵 씬은 수정하지 않았다.
- 퍼즐 흐름
  - 숫자 1~9와 연산자 + - × ÷, 모두 13개의 아이템이 후보 위치 중 무작위 자리에 놓인다.
  - 숫자는 3개, 연산자는 2개까지만 먹을 수 있다. 꽉 차면 아이템은 그 자리에 남는다.
  - 패널의 5칸([숫자][연산][숫자][연산][숫자])은 쏠 때마다 가진 값이 순서대로 바뀐다.
  - 가진 숫자 3개와 연산자 2개를 한 번씩 써서 목표값이 되면 정답이다.
  - 리셋 장치를 쏘면 가진 아이템과 칸이 비워진다. 아이템은 새 무작위 자리에 다시 놓이고, 목표값은 그대로다.
- 구조: 퍼즐 로직은 미로와 무관한 컴포넌트로 분리했다.
  - `Core/Stage/MazeNumberPuzzle.cs`: 퍼즐 로직 전부
    - 시작: `Begin(후보 위치, 아이템 부모, 패널)`
    - 동작: `TryCollect`, `CycleSlot`, `ResetPuzzle`
    - 상태: `IsActive`(먹을 수 있는지, 쓰는 쪽이 켬)
    - 이벤트: `OnProgressChanged`, `OnSolved`
  - `Core/Stage/FileApp.cs`: Maze_1 칸 중앙을 후보 위치로 넘기고, 패널을 출구 문 앞에 만든다.
    - 2라운드일 때 `IsActive`를 켠다.
    - 정답이면 문 `Open()`을 호출하고 패널을 치운다.
    - 2라운드에서 죽으면 리셋한다.
  - 같은 폴더의 `MazePuzzleItem`, `MazePuzzleBoard`, `SMazeToken`, `EMazeOperator`: 아이템, 패널, 값, 연산자 종류
  - `UI/InfoPanel/InfoPanelPresenter.cs`: 퍼즐이 켜져 있으면 `2라운드 / 목표 n · 숫자 3 5 _ · 연산 + _`를 보여 준다. 아니면 기존 피드 앱 표시를 보여 주고, 둘 다 없으면 숨긴다.
  - 프리팹 `Prefabs/Map/Maze/Puzzle/`: `MazePuzzleItem`, `MazePuzzleBoard`(칸 5개와 리셋은 `ShootTarget` 중첩)
- TestScene의 `NumberPuzzleTest` 구역에서 테스트했다. 위치는 컷씬 시험 구역 서쪽(x -58~-34, z 31~57)이다.
  - 바닥, 표지판, 패널 뒤 벽, 숫자 바뀌는 패널, 아이템 노드 20개, `Puzzle`(MazeNumberPuzzle), `Zone`(트리거와 `TestNumberPuzzleZone`)
  - 패널의 5칸 위에 노란 글자로 `목표 n`을 보여 준다. `MazePuzzleBoard.SetTargetText`를 쓰고, `Begin`에서 설정한다.
  - TestScene `PoolTestSpawner` 컴포넌트를 껐다. 왼쪽 위 OnGUI "Object Pool Test" 패널을 없애기 위해서다.
  - `TestNumberPuzzleZone`(Test 폴더, git 제외)은 시작할 때 노드 위치로 `Begin`을 호출한다. 플레이어가 구역 안에 있는 동안 퍼즐을 켜고, 정답이면 패널을 치운다.

## 2. 결정 사항
- 계산은 왼쪽부터 차례로 한다. ÷는 나누어떨어질 때만 계산한다.
- 목표값은 서로 다른 숫자 3개와 연산자 2개로 만들 수 있는 식을 모두 계산해서 고른다. 그 값을 만드는 아이템 조합이 3개 이상이고 값이 50 이하여야 한다.
- 맵 씬(`Map_3_File`)은 열지도 고치지도 않는다. 한 번 FileApp 필드를 연결했다가 사용자 지시로 git에서 되돌렸고, 지금은 변경 내역이 없다.
- 출구 문 막기: `MazeExitGate.IsRoundCleared()`는 TODO라 항상 true다. 그래서 FileApp이 Maze_1 문의 트리거를 꺼 두고 정답 때 `Open()`을 호출한다.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0개, 새 경고 없음.
- TestScene Play에서 확인한 것. 트리거와 버튼은 직접 호출해서 확인했다.
  - 구역에 들어가면 패널이 `2라운드 / 목표 34 …`로 바뀐다. 첫 진입은 실제 물리 트리거로 동작했다.
  - 숫자 4·9·2와 ×·-는 먹고, 7과 +는 거절된다.
  - 0번 칸을 쏘면 4→9→2→4로 바뀐다.
  - 같은 숫자를 두 번 쓴 식(4 - 4 × 2)은 정답이 아니다.
  - 리셋하면 가진 값 0/0, 아이템 13개가 다시 켜지고 위치가 바뀌며, 목표값은 그대로다.
  - 4 × 9 - 2 = 34에서 정답 처리되고 패널이 치워지며, 패널에 정답 표시가 뜬다.
  - 구역에서 나가면 패널이 피드 앱 표시로 돌아간다.
  - 임시 카메라로 구역 모습을 확인했다.
- 확인하지 못한 것
  - 실제로 걸어서 먹기와 실제 사격. Unity 창이 백그라운드라 게임 프레임이 1~3에서 멈췄다. auto-tick과 포커스를 써도 진행되지 않았다.
  - 실제 사망 재스폰 흐름. FileApp에만 있다.
  - `Map_3_File`에서 FileApp 연결. 맵 씬은 손대지 않기로 했다.
- `Map_3_File`의 FileApp에 `_puzzle`·`_mazeSettings`·`_boardPrefab`이 비어 있다. 그대로면 경고 한 줄을 남기고 퍼즐 없이 진행한다(기존과 같다). 퍼즐 컴포넌트를 씬에 두고 연결하는 일은 맵 담당자 몫이다.
- 커밋하지 않았다. 이번 작업과 무관하게 바뀐 파일은 커밋에서 뺀다.
  - `Pretendard-Bold SDF.asset`: 동적 아틀라스
  - 로비 SkySign RenderTexture 4개
  - 원래 있던 `StagePhaseBase` 삭제와 `EditorBuildSettings`

## 4. 다음 할 일 (Next Steps)
- Unity 창을 앞에 둔 상태로 TestScene `NumberPuzzleTest` 구역에서 직접 걸어서 먹고, 실제로 쏴서 확인한다.
- 맵 담당자(양준범 님)와 `Map_3_File` 연결을 상의한다.
  - FileApp 오브젝트에 MazeNumberPuzzle(아이템 프리팹)을 붙이고, FileApp에 퍼즐·`SOMazeChainSettings_File`·패널 프리팹을 연결한다.
  - `MazeExitGate.IsRoundCleared()` TODO와 연결할지도 함께 정한다.
- 숫자와 연산자 아이템의 색 구분, 패널 크기와 오프셋을 조정한다.
- 실제 플레이 확인 뒤 intent-054를 해결 처리한다.
