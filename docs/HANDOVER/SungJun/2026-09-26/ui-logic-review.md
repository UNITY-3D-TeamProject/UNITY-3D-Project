# UI 스크립트 로직 재검토

## 작업 요약 및 방법
- UI 폴더의 PlayerHudPresenter, PlayerHudView, UIManager 전체와 GameManager, Singleton, PlayerSpawner, PlayerState, AttributeSet, AttributeData 연결 흐름을 정적으로 검토했다.
- 현재 플레이어 SO의 HUD 키 6개가 Presenter 기본값과 일치함을 확인했다.
- Unity 6.4 공식 isActiveAndEnabled 문서를 확인하고 활성화/비활성화 순서에 따른 구독 해제를 검토했다.
- dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet 성공: 오류 0개, 기존 StageManager 미사용 이벤트 경고 3개.

## 결정 사항
- 요청은 리뷰이므로 런타임 코드와 씬/프리팹을 수정하지 않았다.
- 기존 미커밋 변경을 보존했다. 새 기능이나 구조 변경을 하지 않아 새 intent는 만들지 않았다.

## 현재 상태 및 이슈
1. PlayerHudPresenter.Bind의 기존 구독 해제에 isActiveAndEnabled 조건이 있다. UIManager와 Presenter가 같은 비활성화 대상 계층에 있고 UIManager.OnDisable이 먼저 실행되면, Bind(null)이 구독 해제를 건너뛰고 참조를 지울 수 있다. 이후 Presenter.OnDisable에서도 해제할 참조가 없어져 콜백이 남는다. 다음 능력치 변경 시 null 참조, 재활성화 시 중복 구독으로 이어질 수 있는 정적 분석상 경로다. 구독 해제는 활성 상태와 무관하게 기존 참조에 수행하는 것이 안전하다.
2. Bind는 OnEnable과 달리 _view가 없어도 구독한다. RefreshAll은 반환하지만 이후 HP/배터리/열 이벤트에서는 _view.Set...을 직접 호출해 NullReferenceException이 발생한다. Bind에도 View 유효성 검사가 필요하다.
3. Bind(null)은 데이터 연결만 해제한다. UIManager 컴포넌트만 끄고 HUD는 켜 두면 마지막 게이지가 남아 최신 상태처럼 보인다. 연결 해제 시 숨김/초기화 정책을 정할 필요가 있다.
- 게이지 비율 계산, 최대값 0 이하 처리, 현재값/최댓값 변경 감지, 저장값 복원 후 스폰 알림, 뒤늦게 활성화한 UI의 현재 플레이어 조회는 정상적인 구조다.
- 저장된 Assets 하위 씬/프리팹에서 UI 스크립트 GUID 참조를 찾지 못했다. Unity Play Mode 실행 검증은 하지 않았다.

## 변경된 파일
- 이 작업 기록 및 HANDOFF_POINTER.md만 갱신했다.

## 다음 할 일
- 우선 Bind의 구독 해제 조건과 View 검증을 수정한다.
- 연결 해제 시 표시 정책을 정한다.
- Inspector 연결 후 HUD 계층 전체 비활성화/재활성화, UIManager만 비활성화, 플레이어 재바인딩, HP/최대 HP 변경을 Play Mode에서 검증한다.

## 참고
- https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Behaviour-isActiveAndEnabled.html
