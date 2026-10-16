# 31. 카메라가 가까워지면 캐릭터 숨김 처리

- **우선순위**: 미정
- **상태**: 완료
- **관련 작업**: [20. 카메라 가림 처리](done/20-camera-occlusion.md) — 가림 회피로 카메라가 당겨질 때 주로 발생한다. [10. 피격 시 깜빡임 + 무적](done/10-hit-blink-invincibility.md) — 같은 렌더러를 켜고 끈다.

## 목표
카메라와 캐릭터 사이 거리가 일정 이하가 되면 캐릭터를 숨기거나 흐리게 해, 화면이 캐릭터 메시로 가려지지 않게 한다.

## 현재 상황
- 20 완료로 `BaseCamera`·`AimCamera` 의 `CinemachineThirdPersonFollow.AvoidObstacles` 가 켜져 있다 (Obstacle + CameraObstacle). 벽 쪽에서는 카메라가 기본 3.0m → 약 1.3m 까지 당겨지며, 더 좁은 곳에서는 더 가까워질 수 있다.
- 카메라 관련 코드: `CameraControl/PlayerBaseCamera.cs`, `Mediator/SubMediators/CameraMediator.cs`. 카메라-캐릭터 거리를 계산하거나 렌더러를 다루는 코드는 없다.
- `Visual/RendererBlinker` 가 피격 깜빡임 때 자식 `Renderer.enabled` 를 켜고 끈다. 숨김도 `enabled` 를 쓰면 깜빡임 종료 시 다시 켜져 충돌한다.
- 펫·탈것(블루투스)도 카메라 가까이 올 수 있다.

## 작업 범위 (예상)
- 카메라 ~ 캐릭터(또는 `CameraPivot`) 거리 측정, 기준 거리 이하에서 숨김.
- 기준 거리 Inspector 노출.

## 열린 질문
- [x] 숨김 방식: 렌더러 끄기 / 페이드(투명 머티리얼·디더링) / `ShadowsOnly` 로 그림자만 남김 → `Renderer.shadowCastingMode = ShadowsOnly`. 그림자는 남고, `RendererBlinker` 의 `enabled` 토글과 충돌하지 않는다.
- [x] 기준 거리 값과 거리 기준점 (캐릭터 루트 / `CameraPivot` / 머리) → `CameraPivot` 기준, 1.0m 로 시작해 플레이 모드에서 조정. Inspector 노출.
- [x] 거리에 따라 점점 흐려지는가, 한 번에 숨기는가 → 한 번에.
- [x] 대상 범위: 캐릭터만 / 무기·펫·탈것 포함 → 캐릭터 + 무기 (`Player` 하위 렌더러). 펫·탈것은 제외, 문제가 되면 별도 작업.
- [x] 피격 깜빡임과 겹칠 때 우선순위 (숨김 중에는 깜빡임을 무시하는가) → 별도 처리 없음. 숨김 중 피격되면 그림자만 깜빡인다.
- [x] 컴포넌트 위치: `CameraMediator` 쪽 / `Visual/` 에 새 컴포넌트 → `Visual/` 에 새 컴포넌트.

## 배경 (2026-10-16)
- Map_3_File 에서 벽에 붙은 채 카메라를 돌리면 `AvoidObstacles` 가 카메라를 어깨 지점 쪽으로 당겨, 카메라가 캐릭터 메시 안으로 들어가 화면 대부분이 가려진다. 이 경우에 대한 예외 처리다.
- 현재 수치: Base `CameraDistance 4` / `ShoulderOffset (0.85, 0.2)`, Aim `1.5` / `(1.0, 0.1)`, `NearClipPlane 0.3`.

## 결과 (2026-10-16)
- `Visual/CameraNearHider.cs` 추가: 렌더 직전(`RenderPipelineManager.beginCameraRendering`, `Camera.main` 만)에 카메라 ~ `_pivot` 거리가 `_hideDistance` 이하면 `_renderers` 의 `shadowCastingMode` 를 `ShadowsOnly` 로, 아니면 원래 값으로 되돌린다. 비활성화 시 원래 값 복원. Cinemachine Brain 이 `LateUpdate` 에서 카메라를 옮기므로 `LateUpdate` 판정의 한 프레임 지연을 피하려고 렌더 직전에 판정한다.
- `Player.prefab`:
  - `Player/AnimationPrefab` (`RendererBlinker` 와 같은 오브젝트) 에 `CameraNearHider` 추가. `_renderers` = `MikeZ`, `RadicalBuster (4)`, `RadicalBuster (3)`, `PowerPod (6)` (깜빡임과 동일), `_pivot` = `CameraPivot`, `_hideDistance` 1.0.
  - (범위 확장) `BaseCamera`·`AimCamera` `Lens.NearClipPlane` 0.3 → 0.1. 숨김 후에도 벽에 붙으면 벽면이 잘려 회색 기둥처럼 보였는데, 근평면 모서리 거리(0.3 기준 Base 약 0.46m / Aim 약 0.39m)가 `CameraRadius 0.2` 보다 커서 근평면이 벽을 뚫었기 때문이다. 0.1 이면 모서리 거리가 약 0.15 / 0.13m 라 반경 0.2 안에 들어온다. `CameraRadius` 를 키우는 안은 좁은 곳에서 카메라가 더 당겨져 기각.

검증 (플레이 모드, Map_3_File — 맵이 플레이마다 새로 생성되므로 플레이어를 벽 앞으로 옮겨 재현):

| 상황 | 카메라-피벗 거리 | 결과 |
|---|---|---|
| 기본 카메라, 벽 쪽 회전 | 0.25~0.44m | 4개 렌더러 `ShadowsOnly`, 근평면 0.1 적용 후 벽면 잘림 없음 |
| 조준 카메라, 벽 쪽 | 0.22~0.39m | 동일 |
| 평소 거리 (기본 / 조준) | 2.37m / 1.87m | 숨김 없음 |
| 숨김 중 `RendererBlinker` Play → Stop | - | `enabled` 만 바뀌고 숨김 유지, 간섭 없음 |
| 24m 복도 원거리 | - | z-fighting 없음 (Direct3D12, reversed-Z) |

## 알려진 문제
- 수백 m 원거리 z-fighting 은 확인하지 못했다 (미로 안 최대 시야 24m). 넓은 맵에서 문제가 보이면 `NearClipPlane 0.15` + `CameraRadius 0.25` 로 조정하는 안이 있다.
- 펫·탈것은 숨김 대상이 아니다. 카메라에 가까이 와서 문제가 되면 별도 작업.
