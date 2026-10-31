---
id: intent-106
title: 미로에서 벽에 가려진 3D 효과음 처리 (도둑 발소리 과다)
part: Core/Audio
status: open   # open | resolved
created: 2026-10-31
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- 파일 앱(`Map_3_File`) 미로에서 도둑(`ThiefSpawner`, 기본 20마리)의 발소리가 너무 많이 들린다.
- 지금 3D 효과음은 직선거리(`SOAudioData.MaxDistance`, 기본 30m)만 보고 감쇠한다. 벽 너머 다른 통로에 있는 도둑도 직선 30m 안이면 그대로 들린다.
- 미로는 통로 폭이 좁아서, 직선거리로는 가깝지만 실제로 걸어가면 먼 도둑이 많다. 보이지 않는 도둑 소리가 겹쳐 시끄럽고, 소리로 도둑 위치를 찾을 수도 없다.
- 다만 벽 뒤라고 무조건 꺼 버리면 안 된다. 보이지는 않아도 옆 통로가 뚫려 있으면 실제로는 어느 정도 들려야 한다.

## 기대 결과 (Proposed outcome)
- 직접 보이는 소리는 지금과 똑같이 들린다.
- 벽에 가려진 소리는 **돌아서 오는 길이**만큼 줄어서 들리고, 벽 너머가 아니라 **뚫린 입구 쪽 방향**에서 들린다.
- 밀폐되었거나 돌아오는 길이 너무 긴 소리는 들리지 않는다.
- 다른 씬의 소리는 지금과 똑같다.

## 설계 (Design)

### 원리: 레이로 먼저 보고, 막혔을 때만 NavMesh 경로
업계에서 말하는 차단(obstruction, 직선은 막혔지만 돌아서 들림)과 차폐(occlusion, 밀폐되어 거의 안 들림)를 함께 다룬다.
Wwise Spatial Audio의 회절처럼 "소리를 돌아오는 입구 위치로 옮기는" 방식을 NavMesh로 단순하게 구현한다.

```
플레이어 ●                       도둑 ●
          █████████████████       │
          █               █       │
          █     ◆─────────────────┘   ← NavMesh 경로 (돌아서 오는 길, 길이 L)
          █     │ (입구)  █
          ▼     │
     소리를 ◆ 방향, 플레이어에서 L만큼 떨어진 위치에서 재생
```

`AudioManager.PlaySfx`가 소리를 내기 직전에 재생 위치를 정한다.

1. **레이 1개**: 리스너에서 소리 위치로 `Physics.Linecast`. 벽 레이어에 안 맞으면 원래 위치에서 그대로 재생한다. (대부분의 소리는 여기서 끝난다)
2. **직선거리 확인**: 막혔는데 직선거리가 이미 SO의 `MaxDistance`보다 멀면 어차피 안 들리므로 그대로 둔다. (경로 계산 생략)
3. **경로 계산**: `NavMesh.SamplePosition`으로 소리 위치와 리스너 위치를 NavMesh 위로 맞춘 뒤, `NavMesh.CalculatePath(소리 → 리스너)`를 부른다.
   - 경로가 완전하지 않으면(`PathPartial`, `PathInvalid`) → 밀폐로 보고 **생략**한다.
   - 경로 길이 L(코너 사이 거리 합)이 `MaxDistance`보다 길면 → **생략**한다.
4. **가상 위치로 재생**: 경로 코너 중 리스너 바로 앞 코너(= 리스너가 보는 입구) 방향으로, 리스너에서 L만큼 떨어진 위치에 소리를 놓는다.
   - Unity의 거리 감쇠(SO의 rolloff·min/max)가 L 기준으로 그대로 적용되어 볼륨을 따로 계산하지 않는다.
   - 소리가 입구 방향에서 들려 소리로 도둑을 쫓아갈 수 있다.
5. **돌아온 소리 배율**: 4번으로 재생할 때 볼륨에 `_diffractedVolumeScale`을 곱해 살짝 먹먹하게 한다. (1이면 거리 감쇠만 적용)

- 모든 효과음이 `AudioManager.PlaySfx`를 거치므로(도둑 발소리: `EnemyAnimator.OnFootstep` → `SoundController.PlayFootstep` → `PlaySfx`) 도둑 쪽 코드는 건드리지 않는다. 수정은 `Core/Audio/AudioManager.cs` 한 파일이다.
- 발소리는 짧은 단발음이라 재생 순간 한 번만 판정해도 충분하다.

### 세부 규칙
1. **판정 높이**: 켜진 리스너는 Player에 있다(`Player.prefab`의 AudioListener가 켜짐, Main Camera 쪽은 꺼짐). 레이는 리스너·소리 위치를 `_occlusionHeight`(기본 1m)만큼 올려서 쏜다. 발소리 위치가 발밑이라 그대로 쏘면 바닥을 스치기 때문이다.
2. **벽 레이어**: `_occlusionMask` 인스펙터 칸. 미로 벽 프리팹이 `Obstacle`(MazeWall_B 계열)과 `CameraObstacle`(MazeWall_A 계열) 두 레이어를 쓰므로 둘 다 넣는다. Player·Enemy 레이어는 넣지 않는다.
3. **벽 표면에서 난 소리**: 총알이 벽에 맞은 소리처럼 벽 표면에서 나는 소리는 레이가 그 벽 자체에 맞을 수 있다. 레이 끝을 리스너 쪽으로 `_occlusionEndMargin`(기본 0.5m) 당겨서 쏜다.
4. **NavMesh에서 떨어진 경우**: 점프 중이거나 NavMesh 밖이라 `SamplePosition`(반경 `_navMeshSampleRadius`, 기본 2m)이 실패하면, 경로를 알 수 없으므로 원래 위치에서 볼륨 × `_diffractedVolumeScale`로 재생한다. (갑자기 끊기는 것보다 자연스럽다)
5. **반복음(`PlaySfxLoop`)은 판정하지 않는다**: 반복음은 대상을 따라 움직여서, 시작할 때 생략·이동되면 나중에 보여도 맞지 않는다. 도둑 발소리는 단발음이므로 1차 범위에서 뺀다.
6. **UI 소리·BGM은 대상이 아니다**: 3D 효과음만 판정한다.
7. **켜는 범위**: 아래 열린 질문 1 참고.

### 켜는 범위 (열린 질문 1 → (B)로 결정)
- `MAZE_SCENE_NAME = "Map_3_File"` 상수. Awake에서 매니저가 놓인 씬 이름이 이것이면 켠다. 다른 씬은 지금과 똑같다.
- 벽 레이어는 코드의 `OcclusionLayerNames = { "Obstacle", "CameraObstacle" }`로 `LayerMask.GetMask`해서 만든다. 씬은 열지 않는다.

### 추가한 인스펙터 칸 (AudioManager, [Header("Occlusion (Map_3_File only)")])
| 칸 | 기본값 | 뜻 |
|---|---|---|
| `_diffractedVolumeScale` | 0.6 | 벽을 돌아서 온 소리의 볼륨 배율. 1이면 거리 감쇠만 적용 |
| `_occlusionHeight` | 1 | 레이를 쏠 때 리스너·소리 위치를 올리는 높이(m) |
| `_occlusionEndMargin` | 0.5 | 레이 끝을 리스너 쪽으로 당기는 거리(m) |
| `_navMeshSampleRadius` | 2 | 소리·리스너 위치에서 가까운 NavMesh를 찾는 반경(m) |

- 리스너는 켜진 `AudioListener`를 찾아 캐시한다. 꺼지거나 파괴되면 다시 찾는다. (Main Camera의 꺼진 리스너는 고르지 않는다)
- 경로 코너는 크기 64 버퍼에 `GetCornersNonAlloc`으로 받는다. 꽉 차면 경로가 너무 길다고 보고 생략한다.

### 비용
- 레이: 도둑 20마리 × 초당 약 2걸음 = 초당 40번 정도. 무시할 수준이다.
- 경로 계산: 레이가 막혔고 직선거리가 `MaxDistance` 안인 소리만. 미로 크기면 한 번에 짧을 것으로 보지만 **실제 비용은 프로파일러로 확인한다**. 무거우면 그때 같은 위치 근처의 결과를 잠깐 재사용하는 캐시를 검토한다. (처음부터 넣지 않는다)
- `NavMeshPath`는 필드 하나를 재사용해 GC를 만들지 않는다.

### 한계
- NavMesh는 걸어갈 수 있는 길이다. 벽 위쪽 창문이나 아래쪽 틈처럼 걸어서 못 지나가는 구멍으로 새는 소리는 반영되지 않는다. 미로는 통로로만 이어져 있어 실제 영향은 작을 것으로 본다.
- 벽 내려가기(`MazeInnerWallLowering`)로 안쪽 벽이 꺼지면 레이가 통과하므로 1단계에서 바로 들린다. NavMesh가 갱신되지 않아도 문제없다.

### 쓰지 않기로 한 방법
- **레이 1개로 무조건 무음** (처음 설계): 옆이 뚫린 벽도 밀폐된 벽처럼 처리한다.
- **레이 여러 개(부채꼴)**: 리스너 바로 옆의 틈만 잡고, 모퉁이를 두 번 이상 도는 소리는 못 잡는다.
- **미리 계산한 음향(Steam Audio, Project Acoustics)**: 반사·잔향까지 되지만 셋업과 패키지 도입 비용이 크다.
- **로우패스 필터로 먹먹하게**: 소스마다 `AudioLowPassFilter`를 붙이고 관리해야 한다. 우선 볼륨 배율로 대신하고, 부족하면 그때 검토한다.
- **매 프레임 재판정**: 단발음에는 필요 없다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 파일 앱 미로를 플레이하는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Core/Audio/AudioManager.cs` (`PlaySfx`에 재생 위치 결정 추가, 인스펙터 칸 5개). `UnityEngine.AI`의 NavMesh API를 호출만 한다.
- 건드리지 않는 것: `CharacterSound/SoundController.cs`, `AI/Thief/*`, `Map/Maze/*` (다른 파트 코드)

## 제약 (Constraints)
- 코드 수정은 `Scripts/Core` 안에서만 한다.
- 씬은 개인 TestScene만 연다. `Map_3_File` 씬을 수정해야 하면 먼저 확인받는다.
- 오버엔지니어링 금지: 소리마다 다른 차폐 규칙, 벽 두께별 감쇠, 반사·잔향 같은 것은 넣지 않는다.

## 열린 질문 (Open questions)
1. ~~어느 씬에서 켤까?~~ → (B) 씬 이름 상수로 결정, 구현함 (2026-10-31)
2. **도둑 발소리 말고 같이 처리되어도 되는가?** (답 전까지는 미로 씬의 모든 3D 단발음에 적용한 상태) 판정은 모든 3D 단발음에 걸리므로, 벽 너머에서 난 폭발·문 열림 소리도 경로 거리로 줄거나 생략된다. 꼭 그대로 들려야 하는 소리가 있으면 그 SO에 "벽 무시" 표시를 하나 더 둬야 한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
