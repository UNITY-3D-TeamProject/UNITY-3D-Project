# 컷씬 대사 부품·입력 맵 이름 Dialogue → Cutscene (2026-10-22, intent-067)

## 1. 작업 요약 및 방법
- [intent-067](../../../intent/clear/intent-067-dialogue-to-cutscene-naming.md) 작성 후 이름만 바꿨다. 동작 변경 없음.
- 스크립트 (`git mv`로 `.cs`·`.meta` 같이 이동, GUID 유지)
  - `Common/IDialogueController` → `Common/ICutsceneController`
  - `Common/DialogueController` → `Common/CutsceneController`
  - `Dialogue/DialoguePresenter` → `Cutscene/CutscenePresenter`
  - `Dialogue/DialogueDirector` → `Cutscene/CutsceneDirector`
- 참조 코드: `UIManager`(필드 `_cutscenePresenter`·`_cutsceneDirector`), `CinematicManager`(지역 변수 `cutsceneController`), `StageManager`(등록 클래스 이름), `SOCutscene`·`SODialogue` 툴팁.
- 입력 맵: `Assets/FID_InputSystem.inputactions`의 맵 이름 `Dialogue` → `Cutscene`(액션·id 그대로). `EInputMapType.Cutscene`, `InputMapController.ACTION_MAP_CUTSCENE`, `UIInputHandler`의 `Cutscene/NextLine`·`Cutscene/OpenSettings`.
- 가이드 `docs/cinematic-so-guide.md`를 Cutscene 이름 기준으로 갱신: `Canvas_InGame` 안 부품 표(UIManager·InputMapController·CutscenePresenter·CutsceneDirector·MonologuePresenter와 바꿀 수 있는 칸), 조작 막기 = `Cutscene` 입력 맵, 10장에 옛 대화 방식은 새로 만들지 않고 삭제 예정·이전/지금 이름 대응.
- 프리팹: `Canvas_InGame`의 UIManager 필드 키 이름과 `DialogueDirector` 오브젝트 이름을 텍스트로 바꿔 연결 유지. 씬은 이 필드를 덮어쓴 곳이 없어 수정 없음.

## 2. 결정 사항
- 옛 대화 시스템은 `ScriptableObjects/UIandCinematic/Dialogue`·`DialogueTable` 폴더를 지울 때까지 이름·기능 그대로 둔다(사용자 결정: 폴더는 지금 지우지 않음).
- 펫 안내(PetGuide)는 컷씬과 다른 기능(게임만 멈추고 펫·카메라는 그대로, 한 줄)이라 유지한다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. `Canvas_InGame`·TestScene 빠진 스크립트 0, 필드 연결 유지.
- TestScene Play 확인: 컷씬 중 `Cutscene` 맵·E 진행·끝 블렌드 뒤 `Player` 복구, 펫 안내 `Cutscene` 맵·E 닫기, 카메라 샷 중 ESC 설정창 열고 닫기, `Stop` 복구, 독백 중 `Player` 맵 유지.
- 맵 씬(`Map_0_Tutorial` 등)은 열지 않았다. 튜토리얼 발판 4개는 여전히 `UIManager.PlayDialogue`를 부르며 지금도 동작한다(옛 코드 유지).
- Play 중 TMP 동적 폰트(`Pretendard-Bold SDF.asset`)가 Unity에 의해 바뀌어 있다. 이번 작업과 무관해 커밋하지 않는다.
- 커밋 `440d8861`(이름 변경). 문서·가이드는 별도 커밋.

## 4. 다음 할 일 (Next Steps)
- 튜토리얼 담당: 대사 3개를 `SOCutscene`으로 옮기고 발판 4곳을 `CinematicManager.PlayCutscene`으로 변경.
- 그 뒤 옛 대화 폴더(공용·Test)와 옛 대화 코드 일괄 삭제(목록은 intent-067 해결 기록).
