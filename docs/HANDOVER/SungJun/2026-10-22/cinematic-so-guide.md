# 컷씬·카메라 샷·독백 SO 제작 가이드 작성 (2026-10-22)

## 1. 작업 요약 및 방법
- 다른 파트가 SO만으로 연출을 만들 수 있도록 [docs/cinematic-so-guide.md](../../../cinematic-so-guide.md)를 작성했다.
- 다루는 종류: 컷씬(대화만) `SOCutscene`, 컷씬(대화+카메라) `SOCutscene` + `CinematicCamera` 키 + `SOCameraShot`, 카메라 샷 `SOCameraShot`, 독백 `SOMonologue`.
- 내용: 씬 공통 준비(Canvas_InGame, CinematicManager 직접 배치·표 연결, CinemachineBrain), CinematicCamera 키·Priority 20 이상·GameObject 켜 두기, 각 SO 항목 설명과 예시, 표 등록, 발판(TriggerPlate) UnityEvent 연결과 코드 호출(`Instance` null 확인), `OnPlayFinished` 이벤트, 재생되지 않는 조건, 문제 해결 표.
- 근거는 `Scripts/UIandCinematic/`의 현재 코드(CinematicManager·CinematicCamera·SO 4종·MonologuePresenter)와 `UI/UIManager.cs`를 읽고 정리했다. 코드·에셋 변경 없음.

## 2. 결정 사항
- 가이드 파일은 기존 `stage-scene-setup-guide.md`처럼 `docs/` 바로 아래에 둔다.
- 표 번호는 맵별 번호대(예: 튜토리얼 100번대)를 권장으로만 적었다(강제 규칙 아님).

## 3. 현재 상태 및 이슈
- `CinematicManager`는 프리팹이 없어 씬마다 직접 만들어야 한다. 실제 맵 씬에는 아직 배치되지 않았다(TestScene에만 있음).
- 공용 `SOCutsceneTable`·`SOCameraShotTable`은 비어 있고, `Cutscene/SOCutscene.asset`은 빈 기본 에셋이다.

## 4. 다음 할 일 (Next Steps)
- 다른 파트에 가이드 공유 후 질문을 받아 보완.
- 필요하면 `CinematicManager` 프리팹(표 연결된 상태)을 `Prefabs/System/`에 만들어 배치를 단순화할지 결정.
