---
id: intent-112
title: 라이브씬 스킬 잠금·설정창 일시정지(영상 포함)·사망 시 게임 멈춤
part: Core / UI / UIandCinematic
status: open   # open | resolved
created: 2026-11-01
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
1. 라이브씬(`Map_5_Live`)은 엔딩 연출만 있는 맵인데 구르기·블루투스(탈것)·스캔 스킬을 쓸 수 있다.
2. 라이브씬 엔딩 중 ESC로 설정창을 열면 엔딩 크레딧까지 가지 않고, 캐릭터가 움직일 수 있게 되며 TV 밖 대화도 나오지 않는다.
   - 원인: 스크린 VideoPlayer가 `UnscaledGameTime`·Direct 오디오라 설정창(Pause)에도 영상이 계속 돌고, `ScreenVideoZoom`의 대기·페이드도 실제 시간으로 돈다. 설정창이 열린 채 TV 밖 대화(35번)로 넘어갈 때가 되면 `CinematicManager.PlayCutscene`이 Pause 상태라 거절되고, 앞의 스크린 컷씬은 이미 `Stop`해서 조작만 풀린다.
3. 설정창을 열어도 TV 영상·전체화면 영상이 멈추지 않는다.
4. 플레이어가 죽어 사망 팝업이 떠 있는 동안 게임이 계속 진행돼 몬스터가 계속 때린다.

## 기대 결과 (Proposed outcome)
1. 라이브씬에 들어오면(재스폰 포함) `Roll`·`SpawnVehicle`·`Scan` 세 스킬이 꺼진다 (HUD 스킬 칸 3개).
2·3. 설정창이 열린 동안 스크린 영상(TV·전체화면 같은 VideoPlayer)과 영상 소리, `ScreenVideoZoom`의 대기·페이드·E 스킵 게이지가 멈추고, 닫으면 그 자리부터 이어서 TV 밖 대화 → 크레딧으로 간다.
4. 사망 팝업이 뜨면 BGM만 남기고 게임 시간을 멈춘다(`PauseGameTime`). 다시하기·메인 메뉴를 고르면 푼다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 모든 플레이어 (4번은 모든 스테이지)
- 어떤 시스템/모듈이 건드려지는가: `Core/Stage/LiveApp.cs`, `UIandCinematic/CameraShot/ScreenVideoZoom.cs`, `UI/UIManager.cs`(사망 팝업), `Core/GameManager.cs`(주석)

## 제약 (Constraints)
- 지켜야 할 정책: 코드 수정은 Core/UI/UIandCinematic 안에서만 (`Map/Live/LiveEndingScreen.cs`는 건드리지 않음). 씬 수정 없음.
- 쓰지 말아야 할 것: 전역 카메라 블렌드 정지 같은 다른 씬까지 바뀌는 변경

## 열린 질문 (Open questions)
- 막을 스킬 3개: HUD에 칸이 있는 구르기·블루투스·스캔으로 판단 (공격·점프·상호작용은 그대로).
- 사망 시 멈추는 시점: 사망 애니메이션이 끝나 팝업이 뜰 때 (죽는 순간 멈추면 사망 애니메이션도 멈춤).
- 이미 시작된 카메라 블렌드(TV 안으로 1.5초, 밖으로 3초)는 설정창 중에도 끝까지 간다 (Brain이 게임 시간과 상관없이 돎).

- (후속) 엔딩 크레딧 중 설정창이 크레딧 캔버스(Sort Order 100)에 가려짐 → SettingsPanel Canvas Sort Order 200.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
