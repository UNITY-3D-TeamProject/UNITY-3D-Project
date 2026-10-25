# 34. 총알 타격감

- **우선순위**: 미정
- **상태**: 완료
- **관련 작업**: [29. 총알 메시 + FX](../29-bullet-mesh-fx.md), [30. 사운드 + VFX](../30-sound-vfx.md)

## 목표
사격의 타격감을 높인다. 우선 차지샷 사용 시 카메라 쉐이크를 넣고, 추가 사항은 작업할 때 정해 이 문서에 덧붙인다.

## 현재 상황
- 카메라는 Cinemachine 3 이다. `Player.prefab` 에 `BaseCamera`(FOV 60) · `AimCamera`(FOV 45) 두 `CinemachineCamera` 가 있다.
- 프로젝트 스크립트·`Player.prefab` 에 카메라 쉐이크(Impulse 등) 사용처가 없다.
- 차지샷 발사는 `SkillMediator.OnFireRequested(position, isCharged)` 로 알려진다 (`isCharged` 로 차지샷 구분 가능).

## 작업 시작 전
- 타격감 피드백 기법(예: 카메라 쉐이크, 히트스톱, 반동, 피격 대상 반응, 히트 마커 등)을 정리하고 각각의 효과·구현 비용·의존성을 비교해 추천안과 함께 사용자에게 제시한다.
- 사용자가 고른 기법만 작업 범위에 넣고, 고른 결과를 아래 "열린 질문"에 기록한다. 고르기 전에는 구현을 시작하지 않는다.

## 작업 범위 (예상)
- 차지샷 발사 시 카메라 쉐이크.
- 추가 타격감 요소 (작업 시 추가).

## 열린 질문
- [x] 적용할 타격감 기법 선택 → 카메라 쉐이크(발사 시) + 카메라 반동. 히트스톱·피격 대상 반응·히트 마커·명중 시 쉐이크는 제외 (명중을 발사자에게 알리는 경로가 없어 별도 설계가 필요)
- [x] 쉐이크 방식 → Cinemachine Impulse(`CinemachineImpulseSource` + 카메라 `CinemachineImpulseListener`)
- [x] 쉐이크 시점 → 발사 순간
- [x] 쉐이크 세기·시간 → 차지샷 기준 세기 1.0, 약 0.2초에서 플레이하며 조정. 조준 중에는 0.5배
- [x] 비차지샷에도 약한 쉐이크를 넣는가 → 넣는다 (차지샷의 0.2배)
- [x] 카메라 반동 방식 → 카메라 피벗 피치를 위로 튀기고 자동 복귀. 차지샷 3도, 복귀 20도/초, 배율은 쉐이크와 같게 (일반 0.2배, 조준 중 0.5배)

## 결과 (2026-10-18, `feature/PlayerFeedback` 브랜치, PR #95로 develop 머지)
- `PlayerBaseCamera.PlayFireFeedback(isCharged)` 추가: Cinemachine Impulse 쉐이크 + 카메라 피벗 피치 반동, 자동 복귀(20도/초). 일반 사격 0.2배, 조준 중 0.5배 (Inspector 노출).
- `CameraMediator.NotifyFire` 추가, `CharacterMediator` 가 `SkillMediator.OnFireRequested` 를 구독해 연결.
- `LookPrefab` 에 `CinemachineImpulseSource`, `Player.prefab` 의 `BaseCamera`·`AimCamera` 에 `CinemachineImpulseListener` 추가.
- 플레이 비교 후 최종값 (`Player.prefab` LookPrefab 오버라이드): 쉐이크 세기(`DefaultVelocity.y`) 0.2, 지속시간 0.12초, 차지 반동 1.5도. 열린 질문의 초기값(세기 1.0·0.2초·3도)에서 낮췄다.
- 검증: 컴파일 에러 없음, 로비 씬 Play 모드에서 일반/차지 × 비조준/조준 4조합 확인. 상세: [사격 카메라 피드백 조정](../discussions/2026-10-18-220431-fire-camera-tuning.md)
