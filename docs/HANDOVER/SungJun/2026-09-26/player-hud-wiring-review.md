# 실제 플레이어 HUD 연결 검토

## 작업 요약 및 방법
- UI 스크립트, PlayerSpawner, PlayerState, 플레이어 초기 Attribute 에셋 및 SampleScene 연결 여부를 읽고 실제 값 표시 방법을 검토했다.
- 기존 PlayerHudPresenter는 AttributeSet 변경을 구독하고 PlayerHudView의 HP, 배터리, 열 Slider를 갱신한다.

## 결정 사항
- 구현 변경 없이 기존 MVP를 활용하는 연결 방식을 제안한다.
- 런타임 생성 플레이어는 저장값 적용 후 Presenter.Bind(attributeSet)으로 연결하는 방식을 제안한다.

## 현재 상태 및 이슈
- SampleScene에서 HUD View/Presenter 연결을 찾지 못했다.
- PlayerSpawner에는 HUD Bind 호출이 없다.
- Presenter.Awake의 AttributeSet 필수 Assert는 런타임 Bind 전에 실행되므로 동적 연결 적용 시 조정이 필요하다.
- SOAttributeData_Player의 CurrentHp, CurrentBattery, CurrentHeat는 모두 0이다. 최초 시작값 정책은 별도로 정해야 한다.
- 사용자가 말한 총 게이지가 CurrentHeat(과열)인지 잔탄인지 확인이 필요하다.
- 코드, 씬, 프리팹은 변경하지 않았으며 실행 테스트는 수행하지 않았다. 기존 미커밋 변경은 유지했다.

## 다음 할 일
- 총 게이지의 의미를 확인한다.
- Canvas/Slider 참조를 연결하고 플레이어 스폰 후 Bind를 적용한다.
- 최초 시작 HP/배터리 초기화 정책과 숫자 표시 필요 여부를 확정한다.
