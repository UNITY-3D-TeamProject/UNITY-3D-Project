# 시스템 스크립트 설계 검토

## 작업 요약 및 방법
- Core(게임 관리, 스폰, 플레이어 상태, 스테이지, 풀링, 스캔), SceneTransition, UI/PlayerFacade를 중심으로 검토했다.
- 연결된 Mediator, Attribute, Skill, Projectile, CharacterMotor/Combat 및 Sensor와 일부 Map 복귀 코드까지 호출 경로를 추적했다. 프로젝트의 모든 맵 생성기·튜토리얼·AI BT 구현을 전수 검토한 것은 아니다.
- C# 호출 검색과 저장된 `_Project` 씬/프리팹의 스크립트 GUID 참조를 대조했다. 기존 미커밋 변경을 포함한 현재 작업 트리를 기준으로 했다.
- Unity 실행·전체 컴파일·Profiler 측정은 하지 않았다. 성능 항목은 코드상 비용 후보이며 측정된 병목이라는 뜻은 아니다.
- CharacterCombat 원본을 메모리에서 컴파일하되 Unity MonoBehaviour만 빈 타입으로 대체한 소규모 C# 검증을 실행했다. Health 100 → 0 → 0에서 OnDeath가 2회 발생했다. Unity 수명주기 검증은 아니다.

## 결론 및 결정 사항
- 감지/상태/표현을 나눈 Scan 구조, 외부 창구인 PlayerFacade, HUD Presenter/View 분리, 공통 Effect 및 스킬 조건/비용 조합은 유지할 가치가 있다.
- 우선 개선할 부분은 명시적 초기 동기화, 사망 이벤트의 중복 방지, 이동 코드의 반복 물리 동기화, 실제 생성 경로의 풀 연결이다.
- 추가 전역 매니저나 EventBus, DI 프레임워크, Scan 상태별 클래스 도입은 현재 근거가 부족하다.
- 아래 내용은 검토 권고이며 구현 결정이나 수정 완료를 의미하지 않는다.

## 현재 상태 및 이슈

### 1. 초기화가 변경 이벤트에 의존함 — 높은 우선순위
- `Mediator/MediatorBase.cs:44`의 SetGetAttribute는 참조만 저장하며 InitValue 호출(48행)은 주석 처리되어 있다. OnEnable의 호출도 주석 처리되어 있고 활성 호출부를 찾지 못했다.
- `MoveMediator.cs:160`의 속도/점프력 초기화와 `CombatMediator.cs:63`의 체력 초기화가 실행되지 않는다. AttributeSet.Awake는 AttributeData 생성자로 값을 넣으므로 초기 변경 알림도 없다.
- 현재 PlayerSpawner 프리팹은 Effect로 HP/배터리/이동속도/점프력을 다시 설정해 이벤트를 발생시킨다. 이 구성이 문제를 가리고 있으며, SO 기본값만 쓰거나 해당 Effect를 생략하면 실제 소비 컴포넌트는 초기값을 받지 못한다.
- 권고: SO 로드 → 스폰 Effect/저장값/라운드 복원 → 소비 컴포넌트 초기 동기화 → 준비 완료 알림의 순서를 명시한다. CurrentHp 기본값이 0이므로 Awake/OnEnable에서 무조건 InitValue를 다시 호출하는 방식은 피한다. 초기 체력 동기화와 실제 피격/사망 판정의 구분도 필요하다.

### 2. 사망 이벤트가 한 번만 발생한다는 계약이 지켜지지 않음 — 높은 우선순위
- `Combat/CharacterCombat.cs:52`에서 `_isDead` 검사와 설정이 주석 처리되어 있다. Health에 0 이하를 반복 대입하면 매번 OnDeath가 발생한다.
- AttributeData는 동일 값 대입도 변경 알림을 발생시킨다. Destroy는 즉시 전체 호출 흐름을 종료시키지 않으므로 같은 프레임의 후속 효과에서도 중복 사망 통지가 가능하다.
- StageManager의 `_isRecovering`은 자기 복구만 방어한다. 다른 사망 구독자는 보호하지 않는다.
- 권고: CharacterCombat이 사망 전환을 1회로 보장한다. 향후 풀링/부활을 적용할 때에만 별도 초기화 API에서 사망 상태를 초기화한다.

### 3. 캐릭터마다 매 프레임 Physics.SyncTransforms 호출 — 성능 우선 점검
- `Movement/CharacterMotor.cs:98`의 Update → Move → 152행 Physics.SyncTransforms 경로가 모든 활성 모터에서 실행된다. 프로젝트의 autoSyncTransforms는 꺼져 있다.
- 순간이동 보정 의도를 일반 이동 매 프레임 경로에 넣었다. 모터 수만큼 물리 동기화 요청이 발생한다.
- 권고: Transform을 직접 바꾼 뒤 최신 물리 위치가 필요한 시점으로 동기화를 제한한다. 기존 순간이동 문제를 재현하며 검증하고, 여러 객체를 옮기는 흐름은 변경을 모은 뒤 동기화한다.
- 근거: [Unity 공식 물리 성능 가이드](https://unity.com/how-to/enhanced-physics-performance-smooth-gameplay)는 최신 Transform 데이터가 필요한 물리 호출 전에 수동 동기화하는 방식을 안내한다.

### 4. 풀링은 구현되어 있으나 실제 생성 경로에 연결되지 않음 — 성능/과설계
- `_Project`에서 ObjectPoolManager의 외부 호출, IPoolable 구현체, 풀 컴포넌트의 씬/프리팹 GUID 참조를 찾지 못했다.
- `Projectile/BulletFactory.cs:38`, `Skill/Skills/EnemyRangedAttack.cs:46`은 Instantiate하고 `BulletController.cs:49,68`은 Destroy한다. MonsterSpawner도 Instantiate 기반이다.
- MonsterPool/NpcPool은 이름과 설정값을 제외하면 같은 구현을 중복한다. ObjectPoolManager는 사용 여부와 관계없이 4개 풀 참조를 모두 요구한다.
- 권고: 반복 생성 빈도가 높은 총알부터 실제 Get/Release 경로를 완성한다. `_elapsedTime`, 방향, 효과 주체 등의 재사용 초기화가 필요하다. Monster/NPC 공통부는 작은 공통 구현이나 조합으로 줄일 수 있으나, 사용처가 없는 범용 풀 프레임워크 확장은 보류한다.
- 연결 전 확인: 각 풀은 Get 내부 콜백에서 활성화하고 반환 후에 위치를 설정한다. OnEnable/OnGet은 이전 위치를 보게 된다. 비활성 획득 → 위치/데이터 설정 → 활성화 순서가 명확해야 한다.
- EffectPool은 수동 Release 시 자동 반환 코루틴을 취소하지 않는다. 재사용을 반복하면 이전 대기 코루틴이 새 대여 주기에 남을 수 있고, 풀 초과로 파괴되면 이전 코루틴의 ParticleSystem 접근도 문제가 될 수 있다. 인스턴스별 코루틴 취소나 대여 세대 검증을 고려한다.

### 5. 스테이지에 미사용 추상화와 상태가 남음 — 단순화
- StagePhaseBase 파생형·호출·저장된 참조가 없다. StageManager의 OnStageCompleted/OnAllStagesCompleted/OnStageFailed는 발행하지 않고 CurrentClearProgress는 갱신하지 않는다.
- Tutorial/FileApp/SecurityApp/LiveApp의 StartStage/EndStage는 비어 있고 FeedApp도 진행도 초기화 정도만 한다. 실제 스테이지별 행동이 생기기 전에는 상속 계층의 실익이 작다.
- 권고: 현재 요구사항과 예정 기능을 구분해 미사용 공개 계약을 정리한다. 스테이지별 실행 로직이 실제로 다르면 StageBase를 유지하고, 타입 식별만 필요하다면 직렬화된 타입 값으로 충분하다. 기존 프리팹 참조가 있으므로 클래스 삭제는 별도 마이그레이션 검토가 필요하다.
- `GameManager.cs:207`에서 찾은 StageBase를 StageManager에 직접 넘기면 `StageManager.cs:118`의 재탐색과 타입 대조가 필요 없어진다. 이는 주로 가독성 개선이며 씬 로드 1회 탐색을 주요 병목으로 보지는 않는다.

### 6. 같은 SetValue 이름이 서로 다른 연산을 뜻함 — 작은 수정으로 큰 가독성 개선
- `Attribute/Core/AttributeSet.cs:89`는 절대값 대입, `Mediator/SubMediators/AttributeMediator.cs:94`는 현재값에서 amount를 차감한다.
- `CharacterMediator.PayAttribute`에서만 후자를 비용 지불에 사용한다. 현재 호출은 의도대로지만 이름만 보고 사용하면 잘못된 값을 설정하기 쉽다.
- 권고: 차감은 Spend/DecreaseValue 등으로 명명하고 Set은 절대값 대입으로 통일한다. 패턴 추가 없이 의미가 명확해진다.

### 7. HUD가 쿨타임 상태를 별도로 소유함 — 조건부 불일치
- 실제 사용 제한은 CooldownCost._readyTime, 표시는 PlayerHudPresenter의 EndTime에 별도로 저장한다.
- Presenter.OnDisable은 사용 이벤트 구독을 해제하지만 OnEnable은 능력치 게이지만 다시 조회한다. Presenter 비활성 중 스킬을 사용하거나 HUD가 늦게 연결되면 실제 대기 시간과 표시가 달라진다.
- 권고: 실제 비용/스킬 쪽에서 남은 쿨타임 또는 종료 시각을 조회하고 Facade를 통해 HUD에 전달한다. 이벤트는 갱신 계기로 사용하고 재연결 때 현재 상태를 읽는다.
- 고정된 HUD 3슬롯의 if 분기를 범용 UI 등록 프레임워크로 바꿀 필요는 없다.

### 8. 준비 완료 이벤트 내부에서 복원을 수행함 — 호출 순서 단순화
- `GameManager.cs:180`의 OnPlayerSpawned를 받은 `StageManager.cs:287`에서 라운드 HP/배터리를 복원한다.
- 다른 구독자가 StageManager보다 먼저 실행되면 복원 전 값을 읽는다. 현재 HUD는 변경 이벤트로 다시 갱신될 수 있으므로 항상 잘못 표시된다고 단정하지 않는다.
- 권고: 복원을 명시적 준비 단계로 옮기고 OnPlayerSpawned는 최종 상태를 관찰하는 알림으로만 사용한다. 함수 호출은 완료 순서가 필요한 작업에, 이벤트는 완료 사실을 알리는 데 사용하면 흐름이 읽기 쉽다.

### 9. 낙하 복귀가 이동 상태를 초기화하지 않음 — 조건부 동작 문제
- `StageManager.cs:469`는 CharacterController를 끄고 위치/회전만 바꾼다. CharacterMotor의 수직/수평 속도, 외부 변위 및 진행 중 구르기는 남는다.
- Map/Gimmicks/PlayerRespawner에도 같은 문제가 TODO로 기록되어 있다. 복귀 직후 잔여 낙하나 구르기 이동이 적용될 수 있다.
- 권고: 이동 계층에 위치 변경과 필요한 이동 상태 초기화를 묶는 API를 두고 Facade로 노출한다. Core와 Map의 복귀 정책 전체를 억지로 통합할 필요는 없다.

### 10. 스캔 구조는 유지하고 중복 표현 갱신만 줄일 수 있음
- ScanWave → IScannable/ScanTarget → 4개 ScanReactionBase 구현은 실제 변화 지점에 맞는 조합이다. enum 3개 상태도 현재 크기에 적절하다.
- 이미 NonAlloc 결과 버퍼, Collider별 탐색 캐시, 프레임별 대상 중복 제거, 대기 중 ScanTarget.Update 중단이 적용되어 있다.
- `MeshRevealReaction.cs:72`의 OnScanUpdate와 81행 OnFade는 접촉 중 같은 프레임에 각각 PropertyBlock을 쓴다. OnScanUpdate는 최신 반경만 저장하고 최종 프레임 적용을 한 곳에서 처리하는 방식으로 줄일 수 있다. Update 순서로 인한 한 프레임 지연은 함께 확인해야 한다.
- 실루엣도 fade가 1로 유지되는 동안 동일한 블록을 반복 적용하므로 전용 렌더러라는 조건에서 값 변경 시에만 쓰는 개선이 가능하다. 정적 메시의 거리 범위 캐시는 이동 대상과 구분해 적용한다.
- 256개 버퍼 및 전체 레이어 기본값은 많은 콜라이더에서 누락 가능성이 있다. 먼저 대상 레이어를 좁히고 실제 맵에서 포화를 확인한다.
- Scan.cs가 스텁이고 `_Project` 내 ScanWave.Begin 호출/에셋 참조가 없는 것은 확인했다. intent-019에서 플레이어 스킬 연결을 타 파트 후속 작업으로 명시했으므로 현재 스캔 반응 설계의 결함으로 분류하지 않는다.

## 변경된 파일
- 이 검토 기록과 Codex HANDOFF_POINTER만 작성·갱신했다. 런타임 코드/씬/프리팹은 수정하지 않았다.

## 다음 할 일
1. 명시적 초기 동기화와 사망 1회 통지 계약을 먼저 정리한다.
2. 순간이동/낙하 복귀를 재현하면서 이동 상태 초기화와 SyncTransforms 위치를 검증한다.
3. 총알 풀링을 실제 생성/종료 경로에 연결한 뒤 Profiler로 비교한다.
4. 미사용 스테이지 계약, SetValue 의미, HUD 쿨타임 조회 및 스폰 완료 이벤트 순서를 작은 변경으로 정리한다.
