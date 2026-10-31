---
id: intent-108
title: 미로 관리자 대화 카메라 추가(펫은 제자리)
part: UIandCinematic
status: resolved   # open | resolved
created: 2026-10-31
resolved: 2026-10-31
---

## 문제 (Problem)
- 미로 2라운드·3라운드 시작 컷씬에서 관리자와 대화하지만, 컷씬 대사가 시작되면 `CutsceneDirector.BeginStaging()`이 펫을 플레이어 앞 1.8m에 세워 플레이어를 보게 한다. 관리자는 플레이어 앞 3m에 생성되므로 펫이 둘 사이를 가리고, 플레이어-펫 대화처럼 보인다.
- 대사 카메라는 모든 줄이 `FileMazeStaging`(플레이어 등 뒤 고정) 하나라 말하는 사람이 바뀌어도 화면이 그대로다. 말하는 사람 카메라는 플레이어·펫용 두 개뿐이다.

## 기대 결과 (Proposed outcome)
- 관리자와 대화하는 줄에서는 펫을 세우지 않고 평소 자리에 가만히 둔다.
- 관리자 카메라(`SpeakerCam_Partner`)를 추가해, 관리자가 말하면 플레이어 어깨 너머로 관리자를, 코리·비트가 말하면 관리자 어깨 너머로 플레이어를 비춘다.
- 3라운드는 관리자가 있는 앞 두 줄만 적용하고, 관리자가 떠난 뒤 비트 대사·도둑 연출은 지금처럼(펫을 세우고 `FileMazeStaging`).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 미로 스테이지 컷씬을 보는 플레이어, 컷씬 SO를 만드는 기획
- 어떤 시스템/모듈이 건드려지는가:
  - `SOCutscene.SCutsceneStep` 에 줄 단위 `IsPartnerTalk` 추가
  - `ICutsceneController.ShowCutsceneLine` / `CinematicManager` / `UIManager` 전달
  - `CutsceneDirector` 관리자 대화 배치·카메라 선택
  - `Canvas_InGame.prefab` 에 `SpeakerCam_Partner` 추가
  - `SOCutscene_File_Round2Start`(7줄), `SOCutscene_File_Round3Start`(앞 2줄)

## 제약 (Constraints)
- 관리자 생성 코드 `Map/Maze/FileMazeCutsceneStaging.cs` 는 다른 파트(양준범) 파일이라 수정하지 않는다. 관리자 위치는 같은 규칙(플레이어 정면 3m, 위 1.4m)에 맞춘 오프셋으로 계산한다. 생성 규칙이 바뀌면 `CutsceneDirector._partnerFaceOffset` 도 맞춰야 한다.
- `Map_3_File` 씬은 열지 않는다.
- 미로 통로 벽에 카메라가 묻히지 않도록 배치 시 벽 거리만큼 당긴다.

## 열린 질문 (Open questions)
- (해결) 비트 대사: 유지, 펫만 제자리. 카메라: 말하는 사람 따라 전환. 범위: UIandCinematic·UI 안에서만. 3라운드: 앞부분 관리자 대화에 같이 적용.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 줄 단위 `SCutsceneStep.IsPartnerTalk`. 켜진 줄은 `CutsceneDirector.BeginPartnerTalk`(펫 그대로, 관리자 카메라·플레이어 역샷 배치, 벽 당김)와 `ShowPartnerTalkLine`(관리자→`SpeakerCam_Partner`, 코리·비트→`SpeakerCam_Player`)으로 처리하고, 이후 일반 줄이 오면 그때 펫을 세운다. 2라운드 7줄·3라운드 앞 2줄 지정, 카메라 키 비움. 같이 이름 칸 폰트를 Rajdhani(영문 전용) → Pretendard-Bold로 바꿔 한글 화자 이름 □□ 깨짐 수정.
- 관련 커밋/PR: (커밋 전)
