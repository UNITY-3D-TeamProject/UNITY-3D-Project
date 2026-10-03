# 사망한 플레이어 제거 로직 미연결 확인

- 작업 요약: 사용자가 사망한 플레이어 제거 로직이 아직 없음을 지적하여 CharacterCombat, PlayerSpawner와 사망 관련 참조를 다시 확인했다. 런타임 코드는 수정하지 않았다.
- 방법/접근: OnDeath, 사망 처리, 재스폰 호출 및 Destroy 참조를 프로젝트 스크립트 전체에서 검색했다.
- 현재 상태: 사용자가 Start 본문을 SpawnPlayer로 분리한 변경이 반영되어 있다. Start에서 호출하지만 SpawnPlayer는 접근 지정자가 없어 private이다. CharacterCombat은 사망 판정 후 OnDeath를 호출하며 현재 검색 결과에 이를 구독하여 플레이어를 제거하거나 재스폰하는 코드는 없다.
- 결정 사항: 이전에 안내한 기존 플레이어 제거→SpawnPlayer 호출은 추가해야 할 흐름이다. 현재 PlayerSpawner의 Destroy(player)는 AttributeSet이 없는 생성 실패를 정리하는 코드이며 사망 처리용이 아니다.
- 현재 상태 및 이슈: SpawnPlayer를 반복 호출하는 것만으로 기존 플레이어가 제거되지 않는다. 사망 이벤트 구독, 현재 플레이어 참조 및 제거·재스폰 호출 경로가 필요하다. 외부 호출을 선택하면 SpawnPlayer 접근 범위도 변경해야 한다. 정적 확인만 수행했으며 Unity 실행 검증은 하지 않았다.
- 다음 할 일: 사망 처리 책임을 정한 뒤 OnDeath 구독과 기존 플레이어 제거, 재스폰을 연결한다.
