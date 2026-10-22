# 시간을 멈추지 않는 독백 (intent-062)

## 1. 작업 요약 및 방법
- `UIManager.Instance.PlayMonologue(id)` / `StopMonologue()` 추가. 게임 시간·입력·카메라를 건드리지 않고 펫 안내 패널에 대사를 띄운다.
  - 테이블에 없는 번호면 경고, 컷씬·펫 안내 중이면 무시, 독백 중이면 새 독백으로 교체.
  - `ShowPetGuide`·`StartDialogue`(컷씬 시작)·`OnDisable`에서 `StopMonologue()` 호출 (같은 패널을 쓰므로).
- 새 스크립트 `Assets/_Project/Scripts/UI/Dialogue/`
  - `MonologuePresenter` — `Play(SODialogue, lineDuration)`: 줄마다 `ShowLine` 후 `lineDuration` 동안 게이지를 1 → 0으로 줄이고(게임 시간 기준), 마지막 줄 뒤 게이지·패널 숨김. `lineDuration`이 0 이하면 `_defaultLineDuration`(3초). 말하는 사람 기본 얼굴 목록은 `DialoguePresenter`와 같은 방식.
  - `SOMonologueTable` — 항목 `Id`, `Sequence`(SODialogue), `LineDuration`(0이면 기본 시간). `TryGetMonologue(id, out sequence, out lineDuration)`.
- `PetGuideView`에 `_gaugeImage` + `ShowGauge(ratio)` / `HideGauge()` 추가.
- `Canvas_InGame.prefab`
  - `PetGuidePanel/Gauge`: Image(Filled, 가로, 왼쪽 기준, 내장 `UISprite`, 하늘색), 패널 아래쪽 좌우 20 여백·y 16~22, 기본 꺼짐.
  - `Canvas_InGame/MonologuePresenter`(새 오브젝트, 늘 켜짐): View = `PetGuidePanel`, 기본 얼굴 PET·PLAYER(DialoguePresenter 값 복사).
  - `UIManager`: `_monologuePresenter`, `_monologueTable` = `ScriptableObjects/UI/SOMonologueTable.asset`(빈 테이블) 연결.

## 2. 결정 사항
- 패널은 컷씬·펫 안내와 같이 쓴다. 동시에 띄우지 않고, 컷씬·펫 안내가 이기며 독백은 끈다.
- 끄는 키 없음: 독백 중엔 Player 입력 맵이라 E가 `Interact`와 겹친다.
- 모든 줄은 같은 시간. 독백마다 테이블에서 바꿀 수 있다. 줄마다 다른 시간은 넣지 않았다.
- 데이터: 튜토리얼 = `SOPetGuideTable`(기존), 컷씬 = `SODialogue` + `SODialogueTable`(기존), 독백 = `SODialogue` + `SOMonologueTable`(새). 독백 SO의 `CameraIndex`는 무시된다.
- `MonologuePresenter`는 꺼져 있는 `PetGuidePanel`에 두면 코루틴을 시작할 수 없어서 별도 오브젝트에 둔다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. TestScene Play에서 런타임으로 만든 테스트 독백·테이블을 리플렉션으로 넣어 확인(씬·에셋에 저장 안 함):
  - 테이블 시간 4초: 2줄이 차례로 뜨고, 2번째 줄 0.81초 시점 게이지 0.80, 끝나면 패널·게이지 숨김, timeScale 1 유지.
  - 시간 0(기본 3초): 2번째 줄 1.34초 시점 게이지 0.56.
  - 독백 중 펫 안내 → 독백 꺼지고 안내 표시(게이지 숨김, `E ▶ 닫기` 표시). 안내 중 독백 요청 무시.
  - 독백 중 컷씬 → 독백 꺼짐, 컷씬 중 독백 요청 무시, 컷씬 종료 후 timeScale 1.
  - 화면 캡처로 패널 아래 게이지 확인(처음 높이 4는 테두리 빛에 묻혀 6으로 키움).
- `SOMonologueTable.asset`은 비어 있다. 실제 독백 대사·호출 위치는 아직 없음.
- 실제 조작 플레이·맵 씬 확인 안 함. 커밋 안 함.
- 작업과 무관한 변경이 작업 트리에 있음: `Pretendard-Bold SDF.asset`, `ProjectSettings/EditorBuildSettings.asset` (건드리지 않음).

## 4. 다음 할 일
- `SODialogue`로 독백 대사를 만들고 `SOMonologueTable`에 번호·시간을 넣은 뒤, 발판 UnityEvent나 스크립트에서 `UIManager.Instance.PlayMonologue(번호)` 호출.
- 게이지 색·두께·위치가 마음에 안 들면 `Canvas_InGame/PetGuidePanel/Gauge`에서 조정.
- 필요하면 글자 수 비례 시간·줄마다 시간 지정을 추가 검토.
