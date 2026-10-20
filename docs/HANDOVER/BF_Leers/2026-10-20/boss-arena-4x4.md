# 보스 아레나 3×3 → 5×5 확장 (4×4 시도 후 변경)

관련 intent: [intent-046](../../../intent/intent-046-boss-arena-4x4.md) (파일명은 4×4 로 만들어 그대로 둠)

## 1. 작업 요약 및 방법
- 사용자 결정: 전체 60×60 m 유지, C# 코드도 같이 수정. 처음 4×4(칸 15 m)로 만들었다가 사용자 요청으로 5×5(칸 12 m)로 다시 바꿨다.
- 코드: `ArenaGrid.SIZE` 3 → 5 (`COUNT` 25 자동), `ArenaGrid`·`BossArena`의 3×3 주석/툴팁을 5×5로 갱신.
- 보스 A 패턴 일반화(사용자 "5*5로 바꿔줘" 요청, 세부 규칙은 임시 결정): `ArenaGrid.CENTER`(=12) 추가 → `BossAPatternSelector.CENTER`·`BossArenaFallZone.CENTER_TILE`·`BossAGrab` 이 사용. `IsCorner` 는 `SIZE` 기준 네 모서리. A2/A6 의 손 칸은 `tiles[SIZE - 1]`, `BossAClap` 의 가운데 칸은 `Tiles[SIZE / 2]`. A5 `PlusTiles`(가운데 가로줄+세로줄)·`CrossTiles`(두 대각선)는 `BuildPlusTiles`/`BuildCrossTiles` 로 생성, 손 칸은 +자 = 가운데 줄 양 끝, X자 = 윗줄 양 끝. 주석 `BossALineSlam`·`BossAChessSlam` 갱신.
- 확인: 리컴파일 통과, eval 로 CENTER=12, plus=[2,7,10,11,12,13,14,17,22], cross=[0,4,6,8,12,16,18,20,24], corners=[0,4,20,24] 확인.
- 씬(`Assets/Test/Boss/Map_4_Security_Boss_T.unity`, Git 제외): Unity MCP `eval`로 `BossFloor_0`을 복제해 25개(`BossFloor_0~24`, 인덱스 = row × 5 + col)로 늘리고 재배치했다.
  - 배치: 아레나 원점 (294, −20), 회전 y 270 → row 는 월드 −X, col 은 월드 +Z. 칸 pivot 은 최소 모서리라 `pos = (294 − 12·(row+1), 0, −20 + 12·col)`.
  - 크기: 부모(`05_Boss`) 월드 스케일이 2 이므로 칸 로컬 스케일 `(0.6, 1, 0.6)` → 월드 12 m, 두께는 그대로.
  - `BossArena._tiles` 25개 연결, `_tileSize` 12.
  - `05_Boss`의 NavMeshSurface 를 `NavMeshAssetManager.StartBakingSurfaces`로 재베이크(`NavMesh-05_Boss 1.asset` 갱신).
- 작업 중 에디터가 Play 모드여서 Play를 중지한 뒤 편집했다.

## 2. 결정 사항
- 낙사 존(x 234~294, z −20~40)·체크포인트는 전체 크기가 같아 그대로 둔다.
- 새 칸 9~24는 프리팹 연결이 풀린 복제 오브젝트다(컴포넌트·자식 `Telegraph`는 동일).

## 3. 현재 상태 및 이슈
- 검증됨: `gate.cjs check recompile` 통과, 타일 25개 렌더러/콜라이더 12 m·합산 범위 x 234~294 · z −20~40, 칸 중심 25곳 NavMesh 샘플 성공, 씬 저장 완료.
- **미검증**: Play 동작(붕괴·예고·낙사), 게이트 루프, 육안 확인. 커밋 전.
- 보스 A 패턴 규칙은 임시 결정이다(intent-046 열린 질문): A5 가 9칸(판의 36%)을 무너뜨리는 것, 손 배정 등 사용자 확인 필요. Play 에서 동작 미확인.
- 칸이 12 m 로 줄어 보스의 미터 단위 값(`BossHand` 호버 거리, B 의 `_shockwaveTiles` 범위, 낙하 거리 등)이 상대적으로 커졌다.
- 보스 어셈블리 테스트 5개는 비활성 상태라 `SIZE` 변경에 따른 테스트 갱신은 없다.

## 4. 다음 할 일 (Next Steps)
1. 임시로 정한 A 패턴 규칙(A5 모양·손 칸 등)이 의도에 맞는지 사용자 확인.
2. Play 로 붕괴·예고·낙사와 B 돌진/도약(`_shockwaveTiles` 범위, 칸 크기 변화) 확인.
3. 게이트 루프 실행 후 커밋.
