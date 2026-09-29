# 현재 캐릭터 인스턴스 참조 확인

- 작업 요약: GameManager가 현재 캐릭터 인스턴스를 추적하는지 GameManager, PlayerState, PlayerSpawner와 UI 참조를 확인했다. 런타임 코드는 수정하지 않았다.
- 방법/접근: 스폰 후 AttributeSet 등록부터 PlayerState의 현재 참조 보관과 GameManager 조회 프로퍼티까지 경로를 추적했다.
- 확인 사항: 별도의 현재 플레이어 GameObject 필드 또는 프로퍼티는 없다. 다만 PlayerSpawner는 생성된 플레이어 루트에서 GetComponent<AttributeSet>으로 실제 컴포넌트를 얻고 RegisterPlayer에 전달한다. PlayerState는 이 참조를 _playerCurrentAttributeSet에 저장하고 GameManager.CurrentPlayerState가 이를 노출한다. 따라서 등록된 컴포넌트가 유효하면 CurrentPlayerState.gameObject로 해당 플레이어 객체에 접근할 수 있다.
- 결정 사항: 캐릭터 인스턴스를 전혀 모르는 상태로 설명하면 부정확하다. 현재 실제 플레이어의 AttributeSet을 통해 간접적으로 참조하는 상태다.
- 현재 상태 및 이슈: 사망 이벤트 구독·플레이어 제거·등록 해제는 아직 없다. 이번 확인은 정적 코드 검토이며 실행 검증은 하지 않았다.
- 다음 할 일: 사망 처리 구현 시 현재 참조와 이벤트 구독·해제, 제거 후 참조 정리를 함께 설계한다.
