# 플레이어 Current 어트리뷰트 초기값 확인

## 작업 요약 및 방법
- 사용자가 제시한 Unity Inspector 캡처와 현재 `SOAttributeData_Player.asset`, `AttributeSet`, `PlayerState`, 새 `Player_Effect.asset`을 확인했다.
- 이번 작업은 초기화 방법 설명이며 에셋과 런타임 코드는 수정하지 않았다.

## 결정 사항
- 현재 `AttributeSet.Awake`는 SO의 각 Value를 그대로 인스턴스에 복사한다. `CurrentHp: 0`이면 첫 생성 시 0이다.
- 시작 체력이 항상 100이면 `SOAttributeData_Player`의 `CurrentHp` 값을 100으로 설정하는 것이 가장 간단하다. `CurrentBattery` 등도 실제 원하는 시작값으로 정한다.
- 최대치에서 시작하되 최대치가 달라질 수 있으면 SO로 키를 먼저 만든 후 `CurrentHp = MaxHp`를 적용한다. 이때 `Set + Attribute` Effect는 선택적 방법이고 cursor에는 같은 AttributeSet을 전달해야 한다.
- 저장된 플레이어 능력치가 있을 때는 `PlayerState.RegisterPlayer`의 복원을 최종값으로 유지해야 하므로 초기값 설정은 복원보다 앞선다.

## 현재 상태 및 이슈
- 사용자가 만든 `SpawnEffect/Player_Effect.asset`은 Target Attribute가 빈 값인 미설정 Effect이며 현재 스폰 코드에서 참조하지 않는다. 사용자가 별도로 작업 중인 에셋이므로 변경하지 않았다.
- 일반 Effect.Apply는 SetValue 변경 이벤트를 발생시킨다. 초기화 시 이벤트 순서를 검토해야 한다.

## 다음 할 일
- 구현을 진행한다면 플레이어의 첫 생성 시 HP/배터리/열 시작값 정책을 확정하고, 스포너 주입과 저장값 복원 순서를 정리한다.
