# EnemySpawner 구조 검토

- 작업 요약 및 방법: 현재 PlayerSpawner, AttributeSet 및 적·스폰 관련 스크립트 경로와 intent 문서를 확인하고 플레이어와 적 스포너의 공통화 범위를 검토했다. 런타임 코드는 수정하지 않았다.
- 제안 사항: 현재는 PlayerSpawner와 EnemySpawner를 각각 MonoBehaviour로 두고, 능력치 딕셔너리를 채우는 초기화 함수는 AttributeSet에 공통으로 두는 구성을 추천했다. Spawner 베이스 클래스 상속은 필수가 아니며, 두 구현에서 생성·초기화·활성화 등 동일한 흐름이 실제로 반복되면 추출을 검토한다.
- 책임 구분: PlayerSpawner는 플레이어 생성과 위치 선택, GameManager 등록을 담당하며 저장값 복원은 기존 PlayerState 경로를 사용한다. EnemySpawner는 적 생성과 초기값 설정을 담당하며, 사용자가 필요 없다고 밝힌 씬 간 능력치 보관용 EnemyState는 추가하지 않는다. 적의 현재 능력치는 각 인스턴스의 AttributeSet이 소유한다.
- 현재 상태 및 이슈: 앞선 대화의 비활성 생성 후 외부 초기화 방식을 전제로 흐름을 설명했다. 현재 코드에는 AttributeSet.Initialize 같은 명시적 초기화 함수가 없고 Awake에서 SO를 읽는다. 이번 제안은 설계 설명이며 구조 변경을 구현하거나 실행 검증하지 않았다.
- 다음 할 일: 구현 요청 시 적 스폰 위치·호출 주체 등 필요한 범위를 확인하고 외부 초기화 흐름을 반영한다. 공통 베이스 상속을 채택하기로 확정한 것은 아니다.

## 몬스터별 데이터 선택과 초기화 책임 후속 설명

- EnemySpawner가 소환할 적 프리팹과 그에 맞는 초기 데이터 SO를 선택하고, 생성한 인스턴스의 AttributeSet.Initialize(초기데이터)를 호출하는 구조를 설명했다. 프리팹과 초기 데이터를 한 쌍으로 구성하면 선택을 함께 처리할 수 있다.
- AttributeSet.Initialize는 전달받은 SO를 읽어 해당 인스턴스의 딕셔너리에 능력치 항목을 생성하는 공통 기능이다. 몬스터 종류별 초기화 코드를 Enemy에 중복 작성할 필요가 없다. Enemy 행동 코드는 초기화된 능력치를 사용한다.
- Initialize는 제안 중인 함수이며 아직 구현하지 않았다. 런타임 파일 변경 없음.

## 같은 몬스터의 개체별 체력 차이로 요구사항 정정

- 사용자의 요구는 몬스터 종류별 SO 선택이 아니라 동일 몬스터를 개체마다 다른 체력으로 생성하는 것이다. 이전 설명이 종류 차이에 집중했음을 바로잡았다.
- 스포너가 개체별 hp를 인자로 받거나 결정하고, 공통 SO로 해당 개체의 AttributeSet을 초기화한 뒤 MaxHp와 CurrentHp를 hp로 설정하고 활성화하는 예시를 설명했다. 공통 SO 원본을 수정하지 않으며 각 인스턴스의 딕셔너리만 달라진다.
- 적 SO에 MaxHp와 CurrentHp 항목이 존재함을 확인했다. Initialize는 여전히 제안 중인 함수이며 예시는 Awake에서 초기화를 분리하고 비활성 프리팹을 생성하는 구성을 전제로 한다. 런타임 코드는 수정하지 않았다.

## 초기 주입 중 SetValue 이벤트 문제로 제안 수정

- 사용자가 비활성 생성 후 SetValue를 호출해도 변경 이벤트가 발생할 수 있다고 지적했다. AttributeSet.SetValue와 AttributeData.Value를 확인했고 Pre → On → Post 콜백 경로가 호출되며 GameObject 비활성 상태로 차단되지 않음을 설명했다.
- 앞선 Initialize 후 SetValue로 체력을 변경하는 예시를 수정했다. 제안하는 Initialize는 기본 SO와 개체별 초기값 덮어쓰기 데이터를 함께 받아 최종값을 결정한 후 new AttributeData(finalValue, name)으로 각 항목을 생성하고 콜백을 연결한다.
- 현재 AttributeData 생성자는 _value 필드에 직접 대입하므로 변경 콜백을 호출하지 않는다. 이 방식으로 초기 체력의 개체 차이는 초기화에 포함하고, 이후 실제 능력치 변경은 기존 SetValue 경로로 처리한다. 초기값 검증이 필요하면 초기화에서 처리한다. 제안만 설명했으며 런타임 변경 없음.

## Initialize 호출 위치 확인

- 개체별 MaxHp/CurrentHp를 전달하는 attributes.Initialize(...) 호출은 EnemySpawner의 스폰 함수에 들어간다고 명확히 설명했다. attributes는 방금 생성한 몬스터 인스턴스의 AttributeSet이며, 공통 초기화 함수의 구현 자체는 AttributeSet에 둔다. 런타임 변경 없음.
