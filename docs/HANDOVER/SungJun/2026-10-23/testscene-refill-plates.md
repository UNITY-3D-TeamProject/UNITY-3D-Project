# TestScene: 체력·배터리 채우기 발판 — 2026-10-23

## 1. 작업 요약 및 방법
- 사용자 요청: TestScene 가운데에 체력과 배터리를 채울 수 있는 장치.
- `_Project` 코드·프리팹은 바꾸지 않았다. 새 파일은 Git에 올라가지 않는 개인 Test 폴더에 있다.
  - `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Script/Test/TestRefillCaller.cs` (namespace `Test`): TriggerPlate UnityEvent에서 `RefillHp()`·`RefillBattery()`를 부르면 `GameManager.Instance.CurrentPlayerFacade`의 `CurrentHp`를 `MaxHp`로, `CurrentBattery`를 `MaxBattery`로 맞춘다. 재스폰 후에도 매번 지금 플레이어를 찾는다.
- 배치 (TestScene 루트 `RefillTest`, 저장함): 시작 지점(0, 0, 0) 바로 서쪽 통로 위.
  - `1_RefillHp`: 발판 (-10, -3), 표지판 "체력 채우기 / 밟으면 최대치로" (-13.5, -3)
  - `2_RefillBattery`: 발판 (-10, -8), 표지판 "배터리 채우기 / 밟으면 최대치로" (-13.5, -8)
  - 발판·표지판은 AudioManager 구역 것을 복사했고, 표지판은 회전 270으로 동쪽(시작 지점 쪽)에서 읽힌다.

## 2. 결정 사항
- 처음에는 발판을 동서로 놓고 표지판이 북쪽을 보게 했으나, 뒤(남쪽) 스캔 구역 표지판 글자가 앞 판을 뚫고 겹쳐 보였다. 씬의 모든 표지판 글자가 Unity 기본 `Font Material`(`GUI/Text Shader`)이라 깊이 검사 없이 위에 그려지기 때문이다. 기존 표지판도 같은 방식이라 재질은 바꾸지 않고, 뒤에 다른 글자가 없는 방향(서쪽)을 보도록 발판을 남북으로 놓고 표지판을 동쪽으로 향하게 바꿨다.

## 3. 현재 상태 및 이슈
- Play 확인 (발판을 남북으로 옮기기 전 위치, 플레이어 순간이동으로 진입): 체력 20/100 → 체력 발판 → 100, 배터리 15/100 → 배터리 발판 → 100. 다른 속성은 바뀌지 않음.
- 옮긴 뒤에는 편집 모드 임시 카메라 캡처로 시작 지점에서 두 표지판이 겹침 없이 읽히는 것만 확인했다. 발판 컴포넌트·연결은 그대로라 다시 Play하지는 않았다.
- HUD 체력·배터리 바가 바로 갱신되는지는 화면으로 확인하지 않았다.
- TestScene·Test 폴더는 Git 대상이 아니다.

## 4. 다음 할 일 (Next Steps)
- 표지판 글자가 다른 물체를 뚫고 보이는 문제가 거슬리면, 깊이 검사를 하는 글자 재질을 만들어 표지판 전체에 적용하는 것을 검토.
