# Enemy·탄환 풀 획득과 반납 수명주기 검토

## 작업 요약 및 방법
- 사용자의 초기화/반납 책임 질문에 답하기 위해 현재 ObjectPoolManager, PooledObject, IPoolable 및 스포너·탄환·적 상태 코드를 정적으로 확인했다.
- 실제 생성 지점은 MonsterSpawner → SpawnerBase, Player의 BulletFactory, EnemyRangedAttack이며 아직 Instantiate를 사용한다.
- Unity 공식 문서로 활성화/OnEnable, 최초 생성, Start 수명주기를 확인했다. 런타임 코드·프리팹 수정 및 Play 검증은 하지 않았다.

## 설명 및 권고 사항
- 매번 덮어쓰는 체력·방향·타이머 등은 획득 시 한 번 초기화하면 된다. 반납에서는 이전 사용의 이벤트 구독·예약 작업·외부 참조 등을 필요에 따라 정리한다. 정리할 것이 없으면 OnRelease는 비워도 된다.
- 씬 매니저 참조는 스포너에서 런타임 생성 Player의 BulletFactory와 Enemy의 발사 컴포넌트에 전달한다. 이 주입 API는 현재 구현되어 있지 않다.
- 반납은 PooledObject.ReleaseOrDestroy(획득한 프리팹 루트)로 요청한다. IPoolable.OnRelease 직접 호출이나 SetActive(false)만으로는 풀 반납이 되지 않는다.
- SO 에셋 자체를 변경하지 않고 인스턴스의 런타임 값에 적용한다. 기본값 복원 후 스폰별 Effect를 적용하거나, 모든 런타임 값을 완전하게 설정하는 초기화 계약이 필요하다.

## 현재 상태 및 이슈
- Get은 위치/회전 → SetActive(true)/OnEnable → PrepareForGet/OnGet → 반환 순서다. 호출자의 Initialize는 이 뒤다. CreateObject도 활성 프리팹을 먼저 Instantiate하므로 Bind 이전 OnEnable이 발생할 수 있다.
- Get<T>는 루트 컴포넌트만 찾으며 실패 시 이미 꺼낸 객체를 반납하지 않는다.
- BulletController의 Destroy는 수명 종료 50행, 충돌 69행이다. 두 Initialize 모두 _elapsedTime을 리셋하지 않으며, 기본 효과 오버로드는 이전 명시 효과를 복원하지 않는다. 피해 중복 방지는 풀 중복 반납 방지와 별도로 필요하다.
- 적 사망은 CharacterMediator.OnDeathCallback의 Destroy다. 현재 Melee/Ranged 프리팹에서는 CharacterMediator가 루트에 있다. 공용 Player 경로 영향을 고려해야 한다.
- AttributeSet은 Awake에서만 기본 SO를 읽는다. AIController의 순찰 시작점은 Start에서만 설정된다. Sensor의 탐지 상태, CooldownCost의 _readyTime, 이동 상태 등의 재사용 초기화가 추가로 필요하다.
- 비활성 상태 초기화 경로를 추가한다면 Awake 캐싱과 활성화 시점도 함께 설계해야 한다. MediatorBase의 InitValue 호출은 주석 상태이므로, 비활성 중 속성값만 바꾼 후 활성화하면 전투/이동 컴포넌트가 자동 동기화된다고 가정할 수 없다.
- EnemyBullet은 피격 시 cursor의 AttackDamage를 읽는다. 발사한 적을 먼저 반납/재사용하면 같은 참조가 새 개체의 능력치를 가리킬 수 있다. 발사 시 피해값 스냅샷 또는 사용 회차 검증 정책이 필요하다.

## 결정 사항
- 코드 변경을 확정하지 않은 설명/검토 작업이다. 위 항목은 후속 연결 시 적용할 권고다.

## 다음 할 일
- 실제 연결 요청 시 생성·초기화·반납을 함께 적용하고, 동일 인스턴스 재사용과 최초 생성, 체력/AI 복원, 탄환 수명/중복 피해, 발사자 선반납을 검증한다.

## 변경된 파일
- 이 작업 기록과 SungJun/HANDOFF_POINTER.md만 추가·갱신했다.
