# 카메라 연출·컷씬·독백 SO 나누기 (intent-066)

## 1. 작업 요약 및 방법
- 테스트 폴더 `UnityAssets/Dialogue`를 `_Project`와 같은 4개 폴더(Dialogue·DialogueTable·MonologueTable·PetGuideTable)로 나눴다. Unity에서 옮겨 GUID를 유지했다.
- 연출 데이터를 종류별 SO로 나누는 설계를 [intent-066](../../../intent/intent-066-cinematic-so-split.md)에 정리하고 사용자 답변으로 결정했다.
- 구현 내용은 intent-066 "진행 기록"에 있다. 요약:
  - 카메라 연출 `SOCameraShot`, 컷씬 `SOCutscene`, 표 2개, 씬 카메라 키 `CinematicCamera`
  - `CinematicManager.PlayCutscene/PlayCameraShot`
  - 컷씬 대사 표시는 `IDialogueController` 4개 멤버(UIManager)
  - 독백 전용 `SOMonologue`
- 에셋 폴더를 `ScriptableObjects/UIandCinematic` 하나로 합쳤다(이전 `Cinematic`·`UI`의 하위 폴더 9개). 테스트 폴더도 `UnityAssets/UIandCinematic`으로 합쳤다. Unity에서 옮겨 GUID와 프리팹 연결을 유지했다.
- SO 생성 메뉴를 `Create > UIandCinematic` 하나로 모았다.
- 스크립트를 `Scripts/UIandCinematic`으로 옮겼다.
  - 이전 `Core/Cinematic`·`UI/Dialogue`·`UI/PetGuide`
  - 종류별 폴더: CameraShot·Cutscene·Dialogue·Monologue·PetGuide·Common
  - 네임스페이스는 `UIandCinematic` 하나(사용자 결정)
- TestScene `CutsceneTest` 발판 4개를 컷씬(대화만)·컷씬(대화+카메라)·카메라 샷·독백 예시로 바꾸고, 팻말에 기반 SO 이름을 적었다. 시작 연출의 한 번만 재생도 껐다.

## 2. 결정 사항
- SO는 씬 카메라를 키 문자열로 가리킨다. 씬 카메라에 `CinematicCamera(Key)`를 붙인다.
- 컷씬 단계는 [대사 + 카메라 키(비우면 말하는 사람 카메라)] 또는 [카메라 연출 SO 참조] 두 종류다. 컷씬 안의 카메라 연출은 컷씬의 Id·한 번만 재생·끝 블렌드를 쓴다.
- 표는 `CinematicManager`가 들고, 카메라 표와 컷씬 표는 따로 둔다. 재생 함수는 void(UnityEvent 연결용).
- 독백 한 줄 시간은 독백 SO가 가진다(표는 번호 → SO만).
- 기존 `CinematicSequence`·`SODialogue`·`SODialogueTable`·`CameraIndex`는 당분간 그대로 둔다.
- `CinematicManager.Stop`은 `StopAllCoroutines`로 단계 코루틴까지 멈춘다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. TestScene Play(eval)로 확인한 것:
  - 컷씬 전체 흐름, 카메라 연출 단독, 중간 정지 복구, 독백
  - 기존 연출·대화 회귀
- `EditorApplication.Step`으로 프레임을 넘기는 테스트에서, Stop하거나 끝난 직후 새 연출을 시작하면 첫 샷 코루틴이 멈추는 경우가 있었다.
  - 실제 시간(`Application.runInBackground = true`)으로는 같은 순서가 재현되지 않았다. 테스트 방식 문제로 판단했다.
  - 실제 키 조작 확인이 남았다.
- `_Project`의 새 표 2개는 비어 있고, 맵 씬 `CinematicManager`에는 연결하지 않았다.
- 작업 시작 때 열려 있던 `Map_1_Lobby` 대신 TestScene을 열어 두었다. 로비 RenderTexture 4개가 에디터에서 수정됨으로 잡혀 있다(이번 작업에서 건드리지 않음).
- 커밋: `392d83d9`(스크립트), `02dedcf1`(SO 에셋), `09f065a8`(문서).
- 커밋 뒤 수정: 끝 블렌드(플레이어 카메라로 돌아오는 동안)에도 시간 정지·조작 막기를 유지하고, 블렌드가 끝난 뒤 푼다. 카메라가 돌아오기 전에 움직이거나 탈것이 앞으로 나가 떨어지던 문제. TestScene 확인. 커밋 `ebf39deb`.

## 4. 다음 할 일
- 실제 키 조작(E)으로 TestScene 컷씬 1번·카메라 연출 1번 확인.
- 맵 담당과 맵 씬 `CinematicManager`에 표 연결, 씬 카메라에 키 부착 협의.
- 기존 `CinematicSequence`·`SODialogue` 컷씬을 새 SO로 옮길지(그리고 지울지) 결정.
