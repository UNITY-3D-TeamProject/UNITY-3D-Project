# 플레이어 스폰 시 Effect SO 필요 여부 검토

## 작업 요약 및 방법
- 사용자가 제공한 `Attribute Effect 작업 가이드.pdf` 5쪽 전체를 시각적으로 확인하고, `AttributeSet`, `SOAttributeEffect`, 플레이어 SO 및 스포너 코드를 대조했다.
- PDF의 설명은 참고 자료로 읽고, 프로젝트 요구사항으로 자동 채택하지 않았다.

## 결정 사항
- 현재 구조에서 플레이어 스폰을 위해 Effect SO를 별도로 만들 필요는 없다. 기존 `SOAttributeData_Player`가 어트리뷰트 이름과 초기값을 정의하고 `AttributeSet.Awake`가 이를 인스턴스에 생성한다.
- 가이드 4쪽의 `Set + Float` Effect는 이미 존재하는 어트리뷰트 값을 바꾸는 선택적 방식이다. SOAttributeEffect.Apply는 `IsValidTarget`으로 대상 키의 존재를 요구하므로 Effect SO만으로 어트리뷰트 컬렉션을 만들 수 없다.
- 스포너 주입 방식으로 전환하더라도 기본 키와 값은 SOAttributeData로 먼저 구성해야 한다. 스폰 시 추가 변경 효과를 의도할 때만 Effect SO를 고려한다.

## 현재 상태 및 이슈
- 정적 확인만 수행했으며 Unity 실행 검증은 하지 않았다.
- 현 플레이어 데이터 SO의 CurrentHp, CurrentBattery, CurrentHeat 값은 0이다. 원하는 시작값이면 에셋 값을 정해야 한다.
- Effect.Apply는 SetValue 경로를 통해 변경 콜백을 발생시킨다. 초기화에 사용할 때 이벤트 순서를 고려해야 한다.

## 다음 할 일
- 외부 스포너 주입을 구현한다면 AttributeSet 초기화 API와 Mediator 초기 동기화 시점을 함께 설계한다.
