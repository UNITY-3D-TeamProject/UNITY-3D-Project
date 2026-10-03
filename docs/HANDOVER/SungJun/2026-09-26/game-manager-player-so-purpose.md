# GameManager의 Player SO 참조 용도 안내

## 작업 요약 및 방법
- GameManager, PlayerState, AttributeSet의 현재 코드를 읽고 SO 참조 용도를 확인했다. 소스 수정은 없다.

## 결정 사항
- 현재 구현에서는 GameManager에 SOAttributeData_Player 연결이 필요하다. PlayerState 생성자가 null이면 예외를 던진다.
- Player의 AttributeSet은 SO의 이름과 초기값으로 런타임 데이터를 생성한다.
- GameManager가 전달한 SO는 PlayerState에서 저장할 능력치 이름 목록을 얻는 데 사용한다. 저장값은 현재 플레이어에서 읽어 메모리 Dictionary에 보관하고 다음 플레이어 등록 때 적용한다. SO 에셋 자체는 수정하지 않는다.

## 현재 상태 및 이슈
- 이 의존성은 현재 저장 구현의 선택이며 씬 전환 자체의 필수 요건은 아니다.

## 다음 할 일
- 현재 테스트에서는 GameManager의 SO Player Attribute Data 필드에 Player와 동일한 SO를 연결한다.
