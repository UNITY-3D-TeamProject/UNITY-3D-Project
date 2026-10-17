# UIManager 설계 평가

- 작업 요약 및 방법: UIManager, PlayerHudPresenter, SettingsPresenter/View, MainMenuPresenter, GameManager, Singleton, PlayerFacade를 정적으로 검토. 구독과 초기 동기화, 설정창 표시와 입력/Pause 복구 경로를 추적했다.
- 평가: 씬 단위 조율자와 Presenter/View 분리는 현재 규모에 적절하다. Facade 경유와 구독 해제, 이미 등록된 플레이어 처리도 장점이다.
- 개선점: HUD 참조 누락이 설정창 입력 연결까지 중단한다. 열린 Presenter가 먼저 파괴되면 CloseSettings가 조기 반환하여 입력/Pause 복구를 건너뛴다. UIManager의 Presenter GameObject 제어와 View root 제어가 겹쳐 프리팹 계층에 숨은 제약이 생긴다.
- 확장 시 고려: 다른 일시정지 UI가 추가되면 Pause 소유권을 구분해야 한다. 현재 PauseGame/ResumeGame 외부 호출자는 UIManager뿐이므로 현존 충돌로 단정하지 않았다.
- 결정 사항: 구현 수정 없이 평가만 진행. 범용 UI 스택이나 DI 도입보다 수명주기와 의존성 보강을 우선 권장한다.
- 변경된 파일: 이 기록과 HANDOFF_POINTER.md. 기존 EditorBuildSettings 변경은 유지했다.
- 현재 상태 및 이슈: Play Mode와 프리팹 계층은 검증하지 않았다. 파괴 관련 문제는 해당 조건에서의 코드 경로 분석 결과이다. 기본 실행 도구 초기화 오류로 승인된 셸을 사용했다.
- 다음 할 일: 수정 요청 시 설정창 상태와 객체 수명 분리, HUD 연결과 입력 연결 분리, Presenter의 표시 수명주기 책임 정리.