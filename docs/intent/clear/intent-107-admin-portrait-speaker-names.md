---
id: intent-107
title: 관리자 초상화 추가 및 화자 이름 한글화(관리자·비트·코리)
part: UIandCinematic
status: resolved   # open | resolved
created: 2026-10-31
resolved: 2026-10-31
---

## 문제 (Problem)
- 파일 맵 2·3라운드 컷씬에서 관리자가 `ADMIN` 이름으로 말하지만 초상화가 없어 빈 얼굴로 나온다.
- 화자 이름이 `PET`, `PLAYER`, `ADMIN` 영문 임시 이름이다.

## 기대 결과 (Proposed outcome)
- 플레이어·펫처럼 관리자 얼굴을 모델에서 촬영한 512×512 투명 배경 스프라이트를 만들고, 컷씬 화자 기본 초상화에 등록해 관리자 줄에 자동으로 뜬다.
- 화자 이름을 `ADMIN → 관리자`, `PET → 비트`, `PLAYER → 코리`로 바꾼다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 컷씬·대화·독백을 보는 플레이어
- 어떤 시스템/모듈이 건드려지는가:
  - `Art/UI/AdminPortraits/` (새 스프라이트)
  - `Canvas_InGame.prefab` 의 `CutscenePresenter`·`MonologuePresenter` 기본 초상화 목록, `CutsceneDirector` 화자 이름, `PetGuidePresenter._petName`
  - `ScriptableObjects/UIandCinematic/` 의 Cutscene·Dialogue·Monologue SO 의 `SpeakerName`
  - 위 세 스크립트의 기본값·툴팁 예시

## 제약 (Constraints)
- 관리자 모델은 사용자 지정 `ThirdParty/Spook Ghost Cute Series/Prefabs/Spook.prefab`. 원본은 Git 미포함이지만 촬영한 PNG 는 `_Project` 안에 두므로 공유된다.
- 씬 파일은 열지 않는다(프리팹·SO 만 수정).
- `CutsceneDirector` 는 화자 이름 문자열로 카메라를 고르므로, 대사 SO 와 같은 값으로 함께 바꿔야 한다.

## 열린 질문 (Open questions)
- (해결) 관리자 모델: Spook. 이름 표기: 한글 `관리자`/`비트`/`코리`.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 관리자 초상화 `Art/UI/AdminPortraits/Admin_00_Default.png`(Spook 정면, 조명 포함 렌더 — 게임처럼 하늘색 몸통·흰 해골)를 `CutscenePresenter` 기본 초상화에 `관리자`로 등록. SO·프리팹 화자 이름 ADMIN→관리자(6), PET→비트(155+필드 2), PLAYER→코리(94+필드 1), 스크립트 기본값·툴팁 예시 갱신.
- 관련 커밋/PR: (커밋 전)
