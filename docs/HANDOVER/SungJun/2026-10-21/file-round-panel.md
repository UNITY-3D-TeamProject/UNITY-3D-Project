# 파일 앱(미로) 라운드 패널 표시 정리

## 작업 요약 및 방법

### 1차: 패널에 지금 라운드 표시
- 사용자 요청: 미로 1라운드부터 지금 몇 라운드인지 보여 준다. 2라운드는 먹은 아이템, 3라운드는 남은 도둑 수를 보여 준다. 3줄 형식(목표 줄)은 하지 않는다.
- 먼저 확인한 것:
  - 1라운드 사망 시 씬을 재시작하지 않는다. `StageManager._restartSceneOnFirstRoundDeath`가 `Map_3_File`에서 꺼져 있다(프리팹에 값 없음 → 기본 false, 씬 오버라이드 없음). 켜진 곳은 `Map_2_Feed`뿐이다.
  - 체크포인트 → 적 생성, 사망 → 적 재생성은 적 파트 `RoundSpawnerBase`가 이미 처리한다(GC_Spawner 2라운드, Thief_Spawner 3라운드).
  - 라운드 로직 베이스는 기존 `StageRound`(일반 클래스)를 그대로 쓴다.

### 2차: StageBase 공통화 + 3라운드 남은 시간 + TestScene 공간 (intent-056)
- `StageBase`(CP949 유지): `CurrentRoundIndex`(StageManager 값을 따라감, 진행 전 -1), `OnProgressChanged` 이벤트, `protected virtual Update`(번호가 바뀌면 `OnRoundChanged()` → `NotifyProgressChanged()`), `NotifyProgressChanged()`. 주석으로 남아 있던 Update 자리를 이걸로 바꿨다.
- `FileApp`: 자기 이벤트·번호 비교를 지웠다. `OnRoundChanged`에서 라운드 로직을 켜고 끄고, `Update`에서는 `base.Update()` 후 진행 중인 라운드만 Tick한다.
- `FeedApp`(CP949 유지): 자기 `OnProgressChanged`를 지우고 `NotifyProgressChanged()`를 쓴다. Update는 `protected override` + `base.Update()`이고, 주석으로 남아 있던 override 자리를 지웠다. 동작은 같다.
- `InfoPanelPresenter`: 제목 `N라운드`. 본문은 2라운드 `숫자 5 6 _ · 연산 - _`, 3라운드 `남은 도둑 4명 · 남은 시간 7초`, 로직 없는 라운드는 빈칸이다.
- `ThiefChaseRound` 버그 수정: 시간 초과 때 체력을 0으로 한 번만 시도하고 타이머를 껐다. 그래서 피격 무적 중(`CombatMediator.IsDamageBlocked`)이면 죽지 않고 0초에서 멈췄다. 이제 실제로 0이 될 때까지 매 프레임 다시 시도한다(`TryKillPlayer`).
- TestScene(개인 씬, 저장함):
  - `FileRoundTest` 루트를 새로 만들었다.
    - `Checkpoint_R2_Puzzle`: 퍼즐 바닥 입구에 x -35.5, 폭 26 m인 띠 모양. 복귀 지점은 (-38, 44)이고 퍼즐 판 쪽을 본다.
    - `Checkpoint_R3_Thief`: 몬스터 바닥 입구에 x 40.3, 폭 17 m인 띠 모양. 복귀 지점은 (38, 11)이다.
    - `R3_Bridge`: 시작 바닥과 몬스터 바닥을 잇는 바닥.
    - 표지판 2개.
  - StageManager: 체크포인트 13개, 라운드 시작점 `[0, 11, 12]`. 점프맵의 `_Round` 체크포인트(3·6·9)는 이제 일반 체크포인트다.
  - FileApp 숫자 퍼즐 라운드 번호 0 → 1. 실제 맵과 같다.
  - Thief_Spawner 시작점 제외 반경 25 → 5. 3라운드 시작점이 웨이포인트와 가깝기 때문이다.
  - 경로: 시작 바닥 → 펫 안내 바닥 → 컷씬 바닥 → 퍼즐 바닥(2라운드). 3라운드는 시작 바닥 동쪽 끝 → 연결 바닥 → 몬스터 바닥이다.

### 3차: 3라운드 공간을 숫자 퍼즐 옆으로 이동
- 사용자 요청으로 3라운드 구역을 퍼즐 바닥(x -58~-34, z 31~57) 바로 북쪽으로 옮겼다.
  - `FileRoundTest/R3_Area`: 24×24 바닥 `R3_Floor`(z 57~81). `NavMeshSurface`(자식만 수집)를 달고 구워서 `TestScene/NavMesh-R3_Area.asset`으로 저장했다.
  - `Checkpoint_R3_Thief`: 경계 z 58.5에 가로 24 m 띠. 복귀 지점은 (-46, 60.5)이고 +Z를 본다.
  - 표지판을 퍼즐 바닥 북쪽 끝(-38, 55.5)으로 옮겼다.
  - 도둑 웨이포인트 9개를 새 바닥 3×3 격자(x -52/-46/-40, z 66/72/78)로 옮겼다.
  - 동쪽 연결 바닥 `R3_Bridge`는 지웠다.
- Play 확인: 2라운드 → 3라운드 띠 진입 시 도둑 5명이 새 바닥 NavMesh 위에 생성되고, 패널에 `남은 도둑 5명 · 남은 시간 16초`가 나온다. 몬스터 바닥 적과는 이제 떨어져 있다.

### 4차: StageRound 제거 (사용자 결정)
- `StageRound.cs`(+meta)를 지웠다. 커밋 전이라 git에서는 삭제로만 보인다.
- `StageBase`(CP949 유지): `public virtual bool IsRoundCleared(int roundIndex)`를 추가했다. 기본은 true(깰 조건 없음)다.
- `FileApp`: 공개 상수 `NUMBER_PUZZLE_ROUND_INDEX = 1`, `THIEF_CHASE_ROUND_INDEX = 2`를 두었다.
  - 라운드 목록·`ActiveRound` 대신 `NumberPuzzleRound`·`ThiefChaseRound` 프로퍼티로 꺼낸다.
  - `OnRoundChanged`: 두 라운드에 `SetActive(번호 일치)`를 부른다.
  - `Update`: `base.Update()` 다음 `_thiefChaseRound.Tick`을 부른다. 타이머는 3라운드에서만 돈다.
  - 사망: 지금 번호가 퍼즐이면 `ResetPuzzle`, 도둑이면 `RestartTimer`를 부른다.
  - 출구 문: `LockMazeGate`로 2·3라운드 문을 막아 두었다가 각 `OnCleared`에서 연다.
  - `IsRoundCleared`를 덮어쓴다.
- `NumberPuzzleRound`·`ThiefChaseRound`: 부모 없는 `[Serializable]` 일반 클래스다.
  - 자기 `OnProgressChanged`·`OnCleared`, `IsCleared`, `SetActive`를 가진다.
  - 도둑 라운드는 `RestartTimer`를 공개했다.
  - 인스펙터 값(`_minSolutionSetCount`·`_maxTargetValue`·`_timeLimitSeconds`)은 이름이 같아 그대로 쓰인다. 라운드별 `_roundIndex` 값은 없어졌다.
- `InfoPanelPresenter`: `switch (CurrentRoundIndex)`로 두 라운드 값을 읽는다.
- 주의: 리컴파일 직후 열려 있던 TestScene의 FileApp 라운드 값이 메모리에서 기본값(제한 시간 60초)으로 바뀌고 씬이 수정됨 상태가 됐다. 저장하지 않고 디스크에서 다시 열어 20초로 돌아온 것을 확인했다. 다른 사람이 이 씬을 열어 둔 채 리컴파일하면 같은 일이 생길 수 있다.
- Play 확인(순간이동, 실제 트리거):
  - 1라운드: `1라운드` / 빈칸
  - 2라운드: 노드 3개 → `숫자 8 3 _ · 연산 - _`, `IsRoundCleared(1)` false, `IsRoundCleared(0)` true. 사망 → (-38, 44) 재스폰, 패널 리셋, 목표 17 → 12
  - 3라운드: 도둑 5명, 1명 잡음 → `4명`, 시간 초과 사망 → (-46, 60.5) 재스폰, 도둑 5명·타이머 다시 시작
- Play 중 다시 바뀐 폰트 아틀라스는 되돌렸다. `SOMazeChainSettings_File.asset`에 빈 Placements 항목이 추가된 변경이 있는데, 이 세션에서 건드리지 않은 파일이라 그대로 두었다.

## 결정 사항

- 라운드 번호 추적·진행 알림은 모든 앱에 공통이라 `StageBase`에 둔다. 앱별 처리는 `OnRoundChanged` 훅으로 한다.
- 라운드 번호 변화 감지는 매 프레임 비교(적 스포너와 같은 방식)로 하고, StageManager에 이벤트는 추가하지 않았다.
- 패널은 2줄(제목·본문) 그대로다. View·Canvas_InGame 프리팹은 수정하지 않았다.
- 1라운드용 StageRound를 만들지 않는다. 만들면 FileApp이 1라운드 출구 문을 잠가 버린다.

## 현재 상태 및 이슈

- 컴파일 에러 0, 새 경고 없음.
- TestScene Play(Unity 창이 백그라운드라 Play 중에만 `Application.runInBackground = true`로 게임 루프를 돌림, 끝난 뒤 PlayerSettings 원복·파일 값 0 확인)에서 플레이어를 체크포인트로 순간이동시켜 실제 트리거로 확인:
  - 1라운드: `1라운드` / 빈 본문
  - 2라운드 진입: `2라운드` / 노드 3개 먹음 → `숫자 5 6 _ · 연산 - _`
  - 2라운드 사망: 새 플레이어가 (-38, 44)에서 재스폰, HP 100, 패널 `숫자 _ _ _ · 연산 _ _`, 목표 4 → 46 재선정
  - 3라운드 진입: 도둑 5명 생성, `남은 도둑 5명 · 남은 시간 18초`, 1명 잡음 → `4명`
  - 시간 초과 + 피격 무적: 첫 시도에서 체력 감소가 막히고 타이머가 유지됨 → 무적(1초)이 끝난 뒤 사망 → (38, 11)에서 재스폰, 도둑 5명·타이머 20초부터 다시 시작
- 실제 걷기·`Map_3_File` 플레이는 미확인이다. 커밋하지 않았다.
- `Pretendard-Bold SDF.asset`은 Play 중 새 글자가 동적 아틀라스에 추가돼 크게 바뀌었다. 사용자 요청으로 되돌렸다(git checkout + 재임포트).
- TestScene 몬스터 바닥의 `MonsterSpawner_Melee/Ranged` 적이 3라운드 중 플레이어를 공격한다. 시험할 때 방해될 수 있다(씬 그대로 둠).
- `Map_3_File`에는 숫자 퍼즐 노드·패널이 아직 없다. 지금 맵에서는 2라운드 퍼즐이 시작되지 않고, 2라운드 출구 문도 열리지 않는다(맵 담당 배치 필요).

## 다음 할 일

- Unity를 앞에 두고 TestScene을 직접 걸어서 1→2→3라운드 확인.
- `Map_3_File` 실제 플레이로 확인(퍼즐 배치 후).
