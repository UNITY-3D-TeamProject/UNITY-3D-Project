---
id: intent-097
title: 로딩 화면 가운데에 달리는 플레이어·펫 표시
part: SceneTransition / Loading / UI
status: open
created: 2026-10-30
resolved: null
---

## 문제 (Problem)
로딩 화면에는 배경·진행 바·"로딩 중" 문구만 있어 화면 가운데가 비어 있다.

## 기대 결과 (Proposed outcome)
- 로딩 화면 가운데에 플레이어와 펫이 제자리에서 달리는 모습이 나온다.
- 컷씬 영상이 나올 때는 진행 바와 함께 숨겨진다.

### 설계
- `Loading.unity`에 `LoadingRunners`(y=-2000, 다른 씬과 겹치지 않는 먼 곳)를 두고 그 아래에:
  - `PlayerRunner`·`PetRunner`: `Player.prefab`·`Pet.prefab`을 꺼내 스크립트·충돌체·카메라·소리·이펙트를 모두 지운 **모습 전용 복사본**. 애니메이터는 달리기 클립 하나만 반복하는 새 컨트롤러(`Animations/Loading/`), 게임 시간과 상관없이 돌게 함.
  - `RunnerCamera`: `RT_LoadingRunners`에만 그리는 카메라(투명 배경, 촬영 거리 짧게).
  - `RunnerLight`: 가까운 범위만 비추는 조명.
- UI: `ProgressGroup/Runners`(RawImage, 화면 가운데)에 RenderTexture를 띄운다. 진행 바 그룹 안이라 컷씬 영상 중에는 함께 숨겨진다.
- 코드 변경 없음(`LoadingScreen` 그대로).

## 영향 범위 (Affected users and systems)
- 씬 전환 때마다 로딩 화면을 보는 플레이어.
- `Loading.unity`, 새 애니메이터 컨트롤러 2개, 새 RenderTexture.

## 제약 (Constraints)
- 팀 공용 프로젝트 설정(새 레이어 등)은 바꾸지 않는다.
- 실제 Player·Pet 프리팹은 수정하지 않는다.

## 열린 질문 (Open questions)
- 없음(배치·크기는 기본값으로 두고 사용자가 확인 후 조정).

## 구현 기록 (2026-10-30)
- 애니메이터: `Animations/Loading/LoadingRunner_Player.controller`(Tacko3D `Anim_ZMIKE_Run` 반복), `LoadingRunner_Pet.controller`(DrollRobots `R11_Run` 반복). 둘 다 상태 하나뿐이라 코드 없이 바로 달린다.
- `Art/Loading/RT_LoadingRunners.renderTexture` 2048×1024 ARGB32.
- `Loading.unity` 루트 `LoadingRunners`(0, -2000, 0):
  - `PlayerRunner`: `Player.prefab` 완전 언팩 후 Transform·Animator·렌더러·메시 외 컴포넌트 79개 제거. 오른쪽으로 달리는 옆모습. Animator `UnscaledTime`·`AlwaysAnimate`·루트 모션 끔.
  - `PetRunner`: `Pet.prefab` 같은 방식(2개 제거). 게임처럼 원래 크기로 플레이어 어깨 뒤(높이 1.2m)에 떠서 따라옴.
  - `RunnerCamera`: RT 전용, 투명 배경, FOV 30, 촬영 거리 1~20m. `RunnerLight`: 범위 15m 점광원.
- UI: `LoadingScreen/ProgressGroup/Runners`(RawImage 2400×1200, 위치 (-74, -59). 처음 960×480에서 사용자 요청으로 2.5배, RT도 2048×1024로 올림. 위치는 달리기 8순간을 렌더해 잰 캐릭터 영역 중심이 화면 정중앙(오차 0.5px 이내)에 오도록 맞춤, 캐릭터 아래 끝은 화면 아래에서 276px). 진행 바 그룹 안이라 컷씬 영상 중에는 함께 숨겨진다.
- 확인: 카메라 렌더 미리보기(달리기 자세 샘플)로 구도 확인. 실제 씬 전환 중 화면은 미확인(사용자 확인).
- 참고: `LoadingRunners`는 `LoadingScreen` 밖이라 목적지 씬을 켜며 로딩 씬이 정리되면 사라지고, 마지막 0.3초 페이드아웃 동안은 마지막 프레임이 멈춘 채 보인다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
