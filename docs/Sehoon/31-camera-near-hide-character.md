# 31. 카메라가 가까워지면 캐릭터 숨김 처리

- **우선순위**: 미정
- **상태**: 대기
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
- [ ] 숨김 방식: 렌더러 끄기 / 페이드(투명 머티리얼·디더링) / `ShadowsOnly` 로 그림자만 남김
- [ ] 기준 거리 값과 거리 기준점 (캐릭터 루트 / `CameraPivot` / 머리)
- [ ] 거리에 따라 점점 흐려지는가, 한 번에 숨기는가
- [ ] 대상 범위: 캐릭터만 / 무기·펫·탈것 포함
- [ ] 피격 깜빡임과 겹칠 때 우선순위 (숨김 중에는 깜빡임을 무시하는가)
- [ ] 컴포넌트 위치: `CameraMediator` 쪽 / `Visual/` 에 새 컴포넌트
