# 2026-10-20 — TestScene 발판이 UIManager.Instance로 부르도록 변경

## 1. 작업 요약 및 방법
- 확인 결과: 작업 전 TestScene 발판 10곳(`TriggerPlate.On Player Entered`)은 씬의 UIManager 컴포넌트를 인스펙터로 직접 참조하고 있었다. 코드에서 `UIManager.Instance`를 쓰는 곳은 `StageManager.PlayIntroDialogue` 하나뿐이고, 지금은 모든 스테이지가 번호 0이라 실제로 불리지 않는다.
- `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Script/Test/TestUIManagerCaller.cs` 추가 (namespace `UI`, 테스트 전용, Git 미포함)
  - `ShowPetGuide(int)`·`PlayDialogue(int)`가 `UIManager.Instance`를 부른다. Instance가 없으면 경고만 남긴다.
- TestScene 발판 10곳(`Plate_1`~`Plate_9`, `Plate_Cutscene`)의 `Trigger` 오브젝트에 `TestUIManagerCaller`를 붙이고, UnityEvent 대상을 UIManager → 자기 오브젝트의 `TestUIManagerCaller`로 바꿨다. 메서드 이름과 번호(int 인자)는 그대로.

## 2. 결정 사항
- UnityEvent는 static 속성(`Instance`)을 고를 수 없어서 중계 컴포넌트를 쓴다.
- 사용자 결정으로 중계 컴포넌트는 Test 폴더 전용으로 둔다. `_Project`의 `TriggerPlate`·`UIManager`는 수정하지 않았다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. TestScene Play에서 발판 10곳 이벤트를 직접 Invoke:
  - 펫 안내 9곳: `Instance` 경유로 펫 안내가 열림, timeScale 0, 닫으면 1
  - `Plate_Cutscene`: `Instance` 경유로 `UIManager.PlayDialogue(2)`까지 도달했지만 경고 "2번 컷씬이 테이블에 없습니다". `SODialogueTable.asset`(Git 기준)에는 1번(`SODialogue_Tutorial`)만 있다. 이번 작업 전부터 있던 상태다.
  - 같은 중계 컴포넌트로 `PlayDialogue(1)`을 부르면 대화가 재생됨(IsDialoguePlaying true, timeScale 0)
- 원인: 저장된 TestScene은 `Canvas_InGame` 프리팹 인스턴스에 테스트 테이블(`SOCutsceneTable_Test`, 1·2번) 오버라이드가 있었는데, 에디터 메모리의 씬은 그 오버라이드가 빠져 프리팹 기본값인 공용 `SODialogueTable`(1번만)을 쓰고 있었다. `Canvas_InGame`은 프리팹 연결 상태(Connected)다. 작업 전부터 있던 미저장 변경이다.
- 사용자 결정으로 공용 테이블에 있는 번호를 쓰도록 `Plate_Cutscene`의 번호를 2 → 1로 변경. Play에서 발판 이벤트 → 대화 재생(IsDialoguePlaying true, timeScale 0), 경고 없음.
- 콘솔 기록상 사용자가 직접 발판을 밟았을 때도(`TriggerPlate.OnTriggerEnter` → `TestUIManagerCaller.PlayDialogue`) Instance 경로를 탔다(번호 변경 전이라 2번 경고).
- TestScene 저장 안 함. 작업 전부터 씬이 dirty 상태였다.
- 커밋 대상 없음(Test 폴더는 Git 미포함). 이 작업 기록만 docs에 추가.

## 4. 다음 할 일 (Next Steps)
- TestScene 저장 여부 결정
- TestScene의 `_Project` 프리팹 오버라이드는 모두 씬 전용(씬 오브젝트 참조, Test 폴더 에셋, 테스트 컴포넌트, 켜기·끄기)이라 프리팹에 Apply하지 않는다.
- 실제 맵·프리팹 발판에서도 Instance 방식이 필요해지면 중계 컴포넌트를 `_Project`로 옮길지 검토
