# 보스A 박수·그랩 화살표 예고 (intent-096, AI_TODO2 보스-19·23)

## 작업 요약 및 방법
- 박수(A6)·그랩(A4) 예고를 `WarningSignals_Arrows.prefab` 화살표로 교체. 다른 패턴은 기존 타일 채움 그대로.
- 신규 `BossArrowTelegraph`(자식 화살표를 꼬리→머리 순으로 하나씩 켜는 깜빡임), `BossArena` 에 화살표 풀·`SetArrowTelegraph`(칸 상태는 유지, 채움만 생략)·`ShowArrowLine`/`HideArrowLine`(직선에 토막을 이어 붙임), `BossTile._warningSignals` 토글.
- `BossAPatternBase` 에 `IsArrowTelegraph`/`GetArrowDirection` 훅, `BossAClap` 이 켬(양 끝·사이 칸 모두 가운데를 향함, 가운데 칸은 줄 방향), `BossAGrab` 은 경로·놓일 칸을 화살표로.
- 씬(`Map_4_Security`): `BossArena._arrowPrefab` 연결, 보스 타일 25개의 `_warningSignals` 연결(MCP eval).

## 결정 사항
- 맵 WarningSignals 는 화살표가 뜬 칸만, 예고 동안만 끈다(발판 5개 제외). 끄기 전 상태를 기억해 되돌린다.
- 기존 타일 채움·직선 예고는 박수·그랩에서 완전 대체. 박수의 상자 예고(`_wallPrefab`)는 유지.

## 현재 상태 및 이슈
- 리컴파일 통과, Play 에서 칸 화살표 1토막·직선 6토막 표시/해제, WarningSignals 토글, 칸 예고 상태 유지 확인.
- 보스전 직접 플레이·게이트 루프(EditMode/PlayMode 테스트) 미실행, 커밋 전. 신규 단위 테스트 없음.
- 플레이 중 확인했을 때 `BossFloor_Tile_R03_C03` 의 WarningSignals 가 이미 비활성이었음(원인 미확인). 그래서 되돌릴 때 원래 상태로 복원하도록 함.
- 이제 안 쓰는 `IBossArena.ShowLine`/`HideLine`·직선 풀은 삭제하지 않고 남김.
- 화살표 크기(1.2)·토막 길이(7.6)·깜빡임 간격(0.15s)은 임시값. 화살표 폭이 그랩 `_hookRadius*2` 와 일치하는지 확인 필요.

## 다음 할 일
1. 보스전을 직접 플레이해 박수 1·2타, 그랩 직선·놓일 칸 화살표의 방향/가독성 확인.
2. 게이트 루프 실행 후 커밋(intent-096 resolved 처리 + `docs/intent/clear/` 이동).
