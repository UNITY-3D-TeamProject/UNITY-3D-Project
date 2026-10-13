# 오브젝트 풀 구조 정리 (씬별 매니저 + 키로 꺼내기 + 카테고리 6종)

브랜치: `feature/System/ObjectPoolRefactoring`

## 1. 작업 요약 및 방법
- 기존 상태 확인: 풀 코드는 있었지만 사용처 0(씬·프리팹 배치 없음, 총알·근접판정·몬스터 모두 Instantiate/Destroy).
- 기존 문제: DDOL 싱글톤이라 씬 전환 시 대여 오브젝트 잔존 / Monster·Npc·Effect 풀이 프리팹 1개만 지원 / Monster·Npc 코드 중복 / 활성화 후 위치 설정 / EffectPool 자동 반납 코루틴이 수동 반납 후 재대여된 인스턴스를 조기 반납 / IPoolable.Release()를 구현할 수단 없음.
- 변경 (`Assets/_Project/Scripts/Core/ObjectPool/`):
  - `PrefabPool.cs` 신규: `[Serializable]` 카테고리 풀. 인스펙터에 키-프리팹(`SPoolEntry`) 등록, 프리팹별 `ObjectPool<GameObject>`, 위치 설정 → 활성화 → OnGet 순서. Get은 internal(매니저 경유만).
  - `PooledObject.cs` 신규: 생성 시 자동 부착, 소속 풀 기억. `Release()`(이중 반납 무시), `static ReleaseOrDestroy(GameObject)`, `OnParticleSystemStopped` 자동 반납, IPoolable 캐싱 후 호출.
  - `IPoolable.cs`: `Release()` 제거(반납은 PooledObject 담당).
  - `ObjectPoolManager.cs`: 싱글톤·DDOL 제거, PrefabPool 6개(Monster, Npc, Effect, PlayerBullet, EnemyBullet, BossSkill). `Get(string key, pos, rot)` / `Get<T>(key, ...)`, Awake에서 키 → 카테고리 표 생성(카테고리 간 중복 키는 에러 로그).
  - `MonsterPool/NpcPool/EffectPool/BulletPool.cs` 삭제.
- 문서: `docs/HANDOVER/SungJun/2026-10-12/object-pool-guide.md`(Player/AI용 사용 가이드), intent-012 갱신.

## 2. 결정 사항
- 싱글톤 아님. 씬마다 ObjectPoolManager 1개.
- 꺼내는 쪽은 매니저 참조 + 키 문자열만 안다(프리팹은 몰라도 됨). 카테고리는 인스펙터 정리·개수 설정용.
- 매니저 참조: 씬 배치 오브젝트는 인스펙터, 런타임 생성 오브젝트는 생성한 쪽이 직접 전달. 처음 넣었던 `IObjectPoolUser` 주입 인터페이스는 사용처가 적어 과설계로 판단해 삭제.
- 반납은 주입과 무관: PooledObject가 돌아갈 풀을 기억 → 반납 쪽은 매니저를 몰라도 됨.
- 플레이어 본체는 풀 대상 제외(씬 1개, 재스폰 드묾, 상태 리셋 비용만 큼).
- 이번 작업 범위는 풀 코드 + 가이드까지. 타 파트 코드 연결은 가이드로 요청.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0 (unity-editor-mcp recompile). 새 경고 없음.
- 키 문자열 오타는 런타임에야 드러난다(등록 안 된 키 → 에러 로그 + null).
- Play 동작 검증(Get/Release, 이중 반납, 파티클 자동 반납)은 미실시 — 사용처·씬 배치가 아직 없음.
- 활성 프리팹은 풀 생성 시 Instantiate 직후 Awake/OnEnable이 한 번 실행된 뒤 비활성화된다(기존과 동일). Awake 초기화는 생성 때 1회뿐이므로 재사용 초기화는 IPoolable.OnGet에서 해야 한다.

## 4. 다음 할 일 (Next Steps)
- 씬 담당: 로비·스테이지 씬에 ObjectPoolManager 배치, 카테고리별 키-프리팹 등록(씬마다 같은 키).
- Core: MonsterSpawner에 매니저 참조 + 키로 Get, PlayerSpawner는 스폰 직후 플레이어 생성 컴포넌트에 매니저 전달.
- Player: BulletFactory → PlayerBullet, BulletController 반납·`_elapsedTime` 리셋.
- AI: EnemyRangedAttack/EnemyMeleeAttack → EnemyBullet 등, MeleeHitController 지연 Destroy 교체.
- 첫 사용처 연결 후 Play 검증 → intent-012 resolved.
