# 35. 이동 시 속도감

- **우선순위**: 미정
- **상태**: 완료
- **관련 작업**: [34. 총알 타격감](34-bullet-hit-feel.md) — 카메라 연출 구조를 함께 잡는 편이 낫다.

## 목표
빠르게 움직일 때 속도감이 느껴지도록 카메라 연출을 넣는다.

1. **블루투스 탑승 시**: FOV 수정, 모션 블러 등 적용
2. **대쉬(Roll) 사용 시**: 카메라 댐핑 증가, FOV 수정 등

## 현재 상황
- 카메라는 Cinemachine 3 이다. `Player.prefab` 의 `BaseCamera` FOV 60, `AimCamera` FOV 45. 두 카메라의 Follow `Damping` 은 모두 `(0, 0, 0)` 이다.
- 프로젝트 Volume Profile 에 Motion Blur 오버라이드가 없다. Volume 은 씬(`Map_0_Tutorial`, `Map_1_Lobby` 등)에 배치돼 있다.
- 연결 지점 후보 (이미 이벤트가 있음): 구르기 `SkillMediator.OnRollRequested`, 탈것 요시 점프·호버 `OnHoverChanged`, 탈것 탑승 점프 `OnMountJumpRequested`, 스킬 사용 `OnSkillUsed`.

## 작업 시작 전
- 속도감 피드백 기법(예: FOV 변화, 모션 블러, 카메라 댐핑·지연, 스피드 라인·파티클, 카메라 흔들림, 비네팅·색수차 등)을 정리하고 각각의 효과·구현 비용·의존성을 블루투스·대쉬별로 비교해 추천안과 함께 사용자에게 제시한다.
- 사용자가 고른 기법만 작업 범위에 넣고, 고른 결과를 아래 "열린 질문"에 기록한다. 고르기 전에는 구현을 시작하지 않는다.

## 작업 범위 (예상)
- 블루투스 탑승 중 FOV 변경, 모션 블러 적용 / 하차 시 원복.
- 구르기 중 카메라 댐핑 증가, FOV 변경 / 종료 시 원복.

## 열린 질문
- [x] 적용할 속도감 기법 선택 (블루투스·대쉬별, "작업 시작 전"의 정리·추천을 보고 사용자가 고른다)
  - 답: 블루투스 → FOV 증가 + 화면 가장자리 방사형 블러 + 스피드 라인. Roll → 카메라 추종 Damping + 스피드 라인 (Roll FOV 변경은 하지 않음)
- [x] 문서 분리: 블루투스·대쉬를 하위 문서(35-1, 35-2)로 나누는가
  - 답: 나누지 않고 이 문서에서 함께 처리
- [x] 연출 구현 방식: 새 컴포넌트(예: 카메라 연출 담당)가 이벤트 구독 / `PlayerBaseCamera` 확장 / 상태별 Cinemachine 카메라 추가
  - 답: `PlayerBaseCamera` 확장 (기존 `CharacterMediator` → `CameraMediator` 경로). 블러·라인은 새 `SpeedEdgeBlurRendererFeature`
- [x] 수치: 탑승·구르기 시 FOV, 댐핑 값, 전환(블렌드) 시간
  - 답: 기본값 Roll Damping 시간 0.1초·복귀 0.15초, 보드 FOV +10·전환 0.2초. 스킬 Inspector(`Roll.prefab`, `SpawnVehicle.prefab`)에서 조절하며, `Player.prefab` 에서 Roll 0.5초·보드 FOV +30 으로 오버라이드
- [x] 모션 블러: 씬마다 있는 Volume 을 수정하는가 / 플레이어 쪽에 로컬 Volume 을 두는가 (씬 수정 시 다른 팀원 씬 충돌 가능성)
  - 답: 둘 다 아님. URP Motion Blur 는 화면 중앙까지 흐려져 제외하고, `PC_Renderer` 에 중앙을 보존하는 방사형 블러 렌더러 피처를 추가 (씬 수정 없음)
- [x] 블루투스 FOV 를 속도에 비례해 바꾸는가 / 탑승 여부로만 바꾸는가
  - 답: 탑승 여부(`SkillMediator.OnRidingChanged`, 강제 하차 포함)로만
- [x] 조준(`AimCamera`) 중에도 적용하는가
  - 답: `AimCamera` FOV(45)는 바꾸지 않음

## 결과 (2026-10-19, `feature/PlayerFeedback` 브랜치, PR #95로 develop 머지)
- `PlayerBaseCamera`: Roll 중 `BaseCamera` ThirdPersonFollow Damping `(t, 0, t)` 적용 후 복귀, 보드 탑승 중 FOV 증가·블러 전환. Roll·탑승 상태를 따로 관리하고 사망·비활성화·재사용 때 원래 FOV/Damping 으로 복구.
- `SpeedEdgeBlurRendererFeature` + `SpeedEdgeBlur.shader`/`M_SpeedEdgeBlur.mat` 추가, `PC_Renderer` 에 연결. 보드 탑승 중 가장자리 방사형 블러, Roll 또는 탑승 중 `FS_Speed.mat` 스피드 라인 표시.
- 설정 위치: `Roll` → Camera Follow Damping Time·Camera Damping Recover Time, `SpawnVehicle` → FOV Offset·전환 시간·블러 강도/반경/Feather/샘플/중심.
- 검증: 컴파일 에러 없음, 로비 씬 Play 모드에서 Roll·탑승·강제 하차·탑승 중 Roll·조준 전환·복구 확인.
- 남은 점: `FS_Speed.mat` 은 `ThirdParty`(Git 제외) 에셋이라 없는 환경에서는 라인만 생략된다. Mobile Quality 미연결, 성능 측정 안 함.
- 상세: [속도감 구현·검증](../discussions/2026-10-19-170115-player-speed-feedback-implementation.md), [스킬 Inspector 이전](../discussions/2026-10-19-171437-speed-feedback-skill-inspectors.md), [Roll Damping 단일 시간](../discussions/2026-10-19-203913-roll-damping-time.md), [스피드 라인](../discussions/2026-10-19-210235-fullscreen-speed-lines.md)
