# EffectManager 사용 후보 조사

## 작업 요약 및 방법
- 사용자 요청에 따라 공용 게임 코드의 파티클 직접 재생·Emit·Instantiate와 폭발/피격/획득/사망 발생 지점을 조사했다.
- 이번에는 후보와 적용 기준만 정리했다. 게임 코드·프리팹 변경, Unity 컴파일·Play는 수행하지 않았다.

## 확인 결과 및 제안
- 우선 후보: ExplosiveDevice.CoExplode의 실제 폭발 시점, BossAPatternBase.OnImpact 및 파생 패턴의 실제 충돌 시점, BossShoulderCannon.Explode의 착탄 위치. 일회성 파티클을 생성하고 원본 장치/투사체와 독립적으로 회수하기 좋다.
- 신규 연출 후보: EffectItem.PickUp의 획득 효과, HomingHazard의 접촉/HP 소진 소멸 효과, 적 CharacterMediator.OnHit/OnCriticalHit/OnDeathEnded에 반응하는 시각 컴포넌트.
- HomingHazard.OnDestroyed는 씬 종료에서도 발생하므로 시각 효과는 실제 접촉/HP 소진 경로에서 재생하는 편이 안전하다.
- 공용 BulletController.OnTriggerEnter도 명중 스파크 후보다. 플레이어 본체의 매니저 미사용 방침과는 별도로 공용 투사체 담당 범위를 정해야 하며, 총알 객체 자체는 ObjectPoolManager가 계속 담당한다. 현재 맞은 지점은 총알 중심을 사용하므로 표면 정렬 정밀도에 한계가 있다.
- 기존 직접 파티클 후보: FeedPlatformFeedback의 발판 소멸, SpawnPulseEffect의 생성 펄스, DataIncinerator/DataGrinder의 조각·플래시. 이미 배치한 ParticleSystem을 재사용하므로 변경 자체가 풀링 최적화라고 볼 수 없으며, 공용화 필요 시에만 검토한다. Emit 개수는 프리팹 Burst 설정 등으로 맞춰야 한다.
- 유지 권장: SpawnEffectDriver의 VFX Graph/재질·메시 스폰 연출, RendererBlinker, 무기 EmissionHueChanger, 고정 환경/팬 파티클, 보스 공격 예고 표시. BossArena는 이미 자체 풀을 사용하고 BossBTelegraph는 DTT 크기·채움 제어를 하므로 현 EffectManager의 Play/Stop만으로 대체되지 않는다.

## 결정 사항
- 매니저가 게임 이벤트를 전부 감지하는 구조로 바꾸지 않는다. 사용처 또는 시각 전용 컴포넌트가 발생 시점을 판단하고 SOEffectData로 Play/Stop을 호출하는 기존 책임 분리를 기준으로 설명했다.
- 실제 적용은 하지 않았다. 후보는 현재 구현/신규 연출 제안을 구분해 사용자에게 설명한다.

## 현재 상태 및 다음 할 일
- 연결할 연출과 프리팹이 확정되면 SOEffectData를 만들고 해당 씬의 매니저 배치 및 호출을 연결한다.
- 원본이 파괴/풀 반납되어도 남아야 하는 일회성 효과는 원본을 부모로 지정하지 않는다. 루프 효과는 종료·OnDisable에서 핸들을 Stop해야 한다.
