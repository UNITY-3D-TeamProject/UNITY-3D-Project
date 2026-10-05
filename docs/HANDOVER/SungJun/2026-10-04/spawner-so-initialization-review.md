# 스포너 상속 및 SO 초기화 구조 검토

## 작업 요약 및 방법
- 현재 `feature/System-Manager`의 AttributeSet, MediatorBase, CharacterMediator, PlayerSpawner, PlayerState 및 플레이어·적 SO 에셋을 정적으로 확인했다.
- 자체 초기화 제거 여부와 SO 주입을 공통으로 처리할 스포너 상속 구조를 검토했다. 런타임 코드와 에셋은 변경하지 않았다.

## 확인 결과
- AttributeSet은 여전히 Awake에서 `_initData`를 읽어 AttributeData 딕셔너리를 만든다. 외부 Initialize 함수는 없다.
- MediatorBase의 OnEnable과 SetGetAttribute에 있던 InitValue 호출은 주석 처리되어 있다. 이동·전투 컴포넌트에 초기값을 전달하는 호출이 중단된 상태다.
- PlayerSpawner는 생성 후 GameManager.RegisterPlayer를 호출하며, PlayerState는 저장된 값만 SetValue로 복원한다.
- 공통 Spawner 및 EnemySpawner는 아직 없다. SOAttributeData 타입과 플레이어·적 데이터 에셋은 이미 존재한다.

## 제안 사항 (구현 미확정)
- 생성·SO 초기화 흐름을 공유하는 작은 Spawner를 두고 PlayerSpawner와 EnemySpawner가 상속하는 구성을 제안한다.
- 부모는 공통 생성·초기화만 담당하고, 플레이어의 위치 선택·저장값 복원·GameManager 등록과 적의 소환 정책은 자식에 둔다.
- 기존 SOAttributeData를 초기값 형식으로 재사용하고, 스포너에서 필요한 에셋을 선택한다. 원본 SO는 수정하지 않는다.
- 초기값 확정과 Mediator 초기 동기화를 별도 단계로 다뤄야 한다. SO 값을 딕셔너리에 넣는 것만으로 이동·전투 컴포넌트가 초기화되지는 않는다.
- 이전 논의대로 초기 주입을 일반 SetValue 변경 이벤트로 처리하면 피격 등 이벤트가 발생할 수 있으므로, 외부 초기화 API와 저장값 복원 시점을 함께 설계해야 한다.

## 현재 상태 및 이슈
- 정적 검토만 수행했다. Unity 실행 검증은 하지 않았다.
- 기존 플레이어·적 SO의 CurrentHp는 0이다. 실제 스폰 데이터로 사용할 때 시작 체력 의도를 확인해야 한다.

## 다음 할 일
- 구현 요청 시 intent를 작성하고 외부 초기화 API, Mediator 초기 동기화, 플레이어 복원 순서 및 적 소환 호출 방식을 구체화한다.
