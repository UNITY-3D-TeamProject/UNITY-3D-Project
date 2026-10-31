# 2026-10-31 — 미로 벽 차폐 효과음 설계 (intent-106)

## 1. 작업 요약 및 방법
- 요청: 미로씬에서 벽에 막혀 안 보이는 소리(특히 도둑 발소리)가 들리지 않게.
- 코드는 수정하지 않고 설계만 했다: [intent-106](../../../intent/intent-106-audio-wall-occlusion.md)
- 확인한 사실
  - 도둑 발소리 경로: `EnemyAnimator.OnFootstep` → `SoundController.PlayFootstep` → `AudioManager.PlaySfx` (단발음)
  - 켜진 AudioListener는 `Player.prefab`에 있다 (`Main Camera.prefab` 쪽은 꺼짐).
  - 미로 벽 레이어: `MazeWall_A*` = CameraObstacle(10), `MazeWall_B*` = Obstacle(7)
  - `ThiefSpawner._count` 기본 20

## 2. 결정 사항
- 처음 낸 "레이 1개로 막히면 무음" 안은 옆이 뚫린 벽도 밀폐된 벽처럼 처리해서 버렸다. **경로 거리 방식으로 결정**(사용자 승인).
- `AudioManager.PlaySfx`에서 리스너 → 소리 위치 `Physics.Linecast` 한 번. 안 막히면 그대로 재생한다.
- 막혔고 직선거리가 `MaxDistance` 안이면 `NavMesh.CalculatePath`로 돌아오는 경로 길이 L을 구한다. 경로가 불완전하거나 L > `MaxDistance`면 생략하고, 아니면 리스너 앞 코너 방향으로 L만큼 떨어진 위치에서 볼륨 × `_diffractedVolumeScale`로 재생한다.
- 레이 양 끝을 `_occlusionHeight`만큼 올리고, 끝을 `_occlusionEndMargin`만큼 리스너 쪽으로 당긴다. NavMesh를 못 찾으면 원래 위치에서 배율만 적용한다.
- 반복음(`PlaySfxLoop`)·UI·BGM은 판정하지 않는다.
- `_occlusionMask` 기본 Nothing → 마스크를 넣은 씬에서만 동작.

- 켜는 범위는 (B) 씬 이름 상수 `MAZE_SCENE_NAME = "Map_3_File"`. 벽 레이어는 코드의 레이어 이름(`Obstacle`, `CameraObstacle`)으로 만든다. 씬 수정 없음.

## 3. 현재 상태 및 이슈
- `AudioManager.cs`에 구현함: `TryResolveOcclusion`, `IsInDirectSight`, `GetListener`, 인스펙터 칸 4개(`_diffractedVolumeScale` 0.6, `_occlusionHeight` 1, `_occlusionEndMargin` 0.5, `_navMeshSampleRadius` 2).
- 리컴파일 통과. **미로에서 청취·프로파일러 확인 전**(Map_3_File은 열지 않기로 해서 직접 Play하지 않음). 커밋 안 함.
- 열린 질문 1개(벽 무시가 필요한 소리) 답변 대기. 지금은 미로 씬의 모든 3D 단발음에 적용된다.

## 4. 다음 할 일 (Next Steps)
- `Map_3_File`에서 Play → 벽 너머 도둑 소리가 줄거나 입구 방향에서 들리는지, 밀폐된 곳은 안 들리는지 확인.
- 프로파일러에서 `AudioManager.PlaySfx` 비용 확인. 무거우면 경로 결과 캐시 검토.
- 꼭 들려야 하는 소리가 나오면 SO에 "벽 무시" 표시 추가.
