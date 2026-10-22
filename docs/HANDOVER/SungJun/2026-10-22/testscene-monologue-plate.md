# TestScene 컷씬 시험 공간에 독백 발판 추가

대상: 개인 TestScene(Git 미포함)과 `Assets/Test/` 테스트 파일만. `_Project` 코드·프리팹·에셋은 바꾸지 않았다.

## 1. 작업 요약 및 방법
- `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Script/Test/TestUIManagerCaller.cs`에 `PlayMonologue(int id)` 추가. 기존 `ShowPetGuide`·`PlayDialogue`와 같은 모양으로 `UIManager.Instance.PlayMonologue(id)`를 부른다.
- 테스트 에셋 (`Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Dialogue/`)
  - `SODialogue_TestMonologue.asset`: Id `TestScene.Monologue`, 3줄(PLAYER 2줄, PET 1줄). 초상화는 비워 기본 얼굴을 쓴다.
  - `SOMonologueTable_Test.asset`: 번호 1 → 위 대사, LineDuration 0(기본 3초).
- TestScene `Canvas_InGame/UIManager`의 `_monologueTable`을 씬 오버라이드로 `SOMonologueTable_Test`로 바꿨다. 프리팹은 공용 빈 테이블(`ScriptableObjects/UI/SOMonologueTable.asset`)을 그대로 가리킨다.
- `CutsceneTest`에 추가
  - `Plate_Monologue` (45, 0.55, -28.03): `Plate_Cutscene` 복제. `Trigger`의 TriggerPlate 이벤트를 `TestUIManagerCaller.PlayMonologue(1)`로 바꿨다.
  - `Sign_Monologue` (48.5, 0.5, -26.63): `Sign_Plate` 복제, 문구 `독백 (시간 안 멈춤)`, 판 폭 5.6.
  - 입구 표지판 문구 `컷씬 시험` → `컷씬·독백 시험`, 판 폭 3.2 → 7.
- 씬 저장 완료.

## 2. 결정 사항
- 테스트 독백은 공용 테이블에 넣지 않고 Test 테이블 + 씬 오버라이드로 연결한다. Git에 올라가는 에셋이 Git 밖 에셋을 가리키지 않게 하기 위해서다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. Play(EditorApplication.Step으로 프레임 진행, 순간이동으로 발판 밟기):
  - 3줄이 3초씩 차례로 뜨고 줄마다 게이지가 1 → 0으로 줄어든다. 약 9초 뒤 패널·게이지가 숨겨진다.
  - 독백 중 timeScale 1, 입력 맵 Player 유지, 플레이어 이동 가능, HUD도 그대로 보인다(게임 화면 캡처로 확인).
  - 발판에서 나갔다 다시 밟으면 처음부터 다시 재생된다(`_isOneShot` false).
- 실제 키보드 조작으로 걸어서 밟기는 확인하지 못했다.
- 표지판은 통로(북쪽)를 향해 있어서, 공간 안쪽에서 보면 글자가 뒤집혀 보인다.

## 4. 다음 할 일 (Next Steps)
- 실제 독백 대사가 정해지면 공용 `SOMonologueTable.asset`에 넣고 맵에서 호출한다.
- 줄 시간을 바꿔 보려면 `SOMonologueTable_Test`의 LineDuration을 조정한다.
