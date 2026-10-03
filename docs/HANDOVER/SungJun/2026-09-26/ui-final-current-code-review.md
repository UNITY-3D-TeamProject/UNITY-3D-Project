# 현재 UI 코드 최종 재검토

## 작업 요약 및 방법
- 씬 전환 시 플레이어와 HUD를 함께 교체하고 GameManager는 유지한다는 범위로 UI 3종과 Core 연결 코드를 다시 읽었다.
- AttributeSet 초기화 및 AttributeData의 값 대입 후 변경 이벤트 호출 순서를 확인했다.
- dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet 성공: 오류 0개, 기존 StageManager 미사용 이벤트 경고 3개.

## 결정 사항
- 검토만 수행했으며 런타임 소스 및 씬/프리팹은 수정하지 않았다.
- GameManager 선배치와 유효한 Inspector 참조를 전제로 현재 UI 생성/재연결/해제 흐름에 추가 실행 순서 지정이나 삭제 감지 컴포넌트가 필요하지 않다.

## 현재 상태 및 이슈
- Presenter.Bind에서 기존 구독을 활성 상태와 무관하게 해제하고, View 검사 및 null 바인딩 시 게이지 초기화가 적용됐다.
- OnAttributeChanged의 View/AttributeSet 검사도 적용됐다.
- GameManager.LateUpdate의 파괴 검사는 제거됐다.
- UIManager는 생성 알림 구독과 현재 플레이어 조회를 모두 수행하고 비활성화 시 구독/연결을 해제한다.
- GameManager.HandleBeforeSceneChange는 아직 호출/이벤트 연결이 없다. 따라서 씬 간 능력치 저장은 미완성이다. 이는 기존에 합의된 후속 씬 전환 연결 작업이다.
- OnPlayerDespawned/UnregisterPlayer/IsInitialized/TryGetInstance는 현재 흐름에서 사용하지 않는 보조 코드다. 남아 있어도 현재 UI 흐름을 깨뜨리지 않으며 새로 배선할 필요가 없다.
- 저장된 Assets 하위 씬/프리팹에서 UI 스크립트 GUID 및 GameManager의 플레이어 SO 필드 연결을 찾지 못했다. Inspector 연결과 Play Mode 동작은 검증하지 못했다.

## 다음 할 일
- 실제 씬에 GameManager와 SO, UIManager/Presenter/View/Slider 참조를 연결한다. GameManager SO 필드의 현재 이름은 _soPlayerAttributeData이다.
- 씬 전환 담당 코드에서 이전 플레이어 파괴 전에 능력치 저장을 호출한다.
- Play Mode에서 HP/최대 HP 변경, UI 재활성화, 다음 씬 플레이어 연결을 확인한다.
