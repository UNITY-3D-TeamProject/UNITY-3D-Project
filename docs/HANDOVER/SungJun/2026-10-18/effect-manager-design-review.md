# EffectManager 설계 검토

## 작업 요약 및 방법
- 사용자 요청은 제시한 설계의 검토 및 추가 패턴 제안으로 해석했다. 런타임 구현, 프리팹 생성, 씬 배치는 수행하지 않았다.
- CLAUDE.md, 응답·코딩 컨벤션, 기존 인수인계, AudioManager, SOAudioData, ObjectPoolManager, PrefabPool을 읽었다.
- Unity 6000.4.8f1 프로젝트임을 확인하고 Unity 공식 ParticleSystem 문서와 ObjectPool 공개 구현을 참고했다.
- VFX_ElectricArc와 VFX_Fire 프리팹의 YAML을 정적으로 확인했다. 두 루트 모두 Transform만 있고, 자식 ParticleSystem은 각각 3개와 5개다.

## 사용자 확정 사항
- Core.Effect의 EffectManager / SOEffectData / SEffectHandle.
- SO별 지연 생성 풀, ObjectPoolManager 변경 없음.
- 단일 Play API와 핸들, 일회성 및 루프, 선택적 부모 부착.
- SO 시간과 Time.time 기반 Update 반납. PooledObject 콜백 사용 안 함.
- 씬 단위 Instance, 씬 배치와 기존 VFX 사용처 변경은 제외.
- Prewarm, VFX Graph 지원은 후속.

## 검토 의견 (아직 사용자 확정 아님)
1. 루트 ParticleSystem 하나만 저장하면 두 테스트 프리팹의 자식 방출을 Stop하지 못한다. 인스턴스 생성 시 자식까지 수집·캐싱하고 각 시스템을 withChildren=false로 제어하는 구성이 적절하다. 서브 이미터를 포함한 복합 효과는 재생 대상 구분이 필요할 수 있다.
2. Stop은 멱등이어야 한다. SActiveEffect에 IsStopping을 추가하고 첫 호출에서만 종료 시각을 정한다. List의 struct를 복사해 수정했다면 목록에 다시 넣는다.
3. Loop Only 헤더와 일회성 Stop의 StopFadeDuration 적용이 충돌한다. 현재 구역 구분을 유지하려면 일회성 Stop은 즉시 반납, 루프 Stop만 잔여 파티클 대기를 권장한다. 일회성도 잔여 재생이 필요하면 공용 Stop 설정으로 옮기고 자연 종료 시각보다 연장되지 않게 해야 한다.
4. 재생 초기화는 StopEmittingAndClear 후 Play로 명시하는 편을 권장한다. IsLoop는 자동 반납 여부이며 실제 ParticleSystem 루프 구성은 프리팹이 담당한다고 문서화한다. 자식 전체에 main.loop를 강제로 덮으면 일회성 보조 연출까지 반복될 수 있다.
5. 매니저 교체마다 Id를 1부터 발급하면 이전 핸들이 새 매니저의 재생과 충돌할 수 있다. 핸들에 매니저 세대 식별을 추가하거나 실행 세션에서 번호를 공유하는 방식 검토. IsValid는 발급 여부이지 현재 재생 여부가 아니다.
6. OnDestroy에서 외부 부모 아래의 살아 있는 active 인스턴스도 정리하고 각 풀을 Dispose한다. 풀 Dispose는 대여 중 인스턴스를 정리하지 않는다. Update뿐 아니라 Stop도 파괴된 Instance를 방어해야 한다.
7. 프리팹 계약: stopAction=None, useUnscaledTime=false, 자체 Destroy/반납 스크립트 금지. 반복 여부는 프리팹에서 설정한다. 부모 부착은 방출 위치 추적이며 이미 방출된 입자 추적 여부는 simulationSpace 설정에 달려 있다. 부모 비활성화 시 자동 Stop할지 또는 호출자가 OnDisable에서 Stop할지 명시한다.
8. MaxSize는 동시 재생 상한이 아니라 비활성 보관 상한이다. 성능 문제가 확인될 때 MaxConcurrent를 별도 도입한다. 외부 Destroy된 대여 오브젝트를 버리면 내장 풀 CountAll/CountActive 집계가 실제 생존 수와 달라질 수 있으므로 동시 재생 판단에 사용하지 않는다.
9. Instantiate 후 SetActive(false)는 생성 순간의 OnEnable을 막지 못한다. 최초 활성화 시 부작용이 있는 프리팹까지 지원해야 한다면 비활성 생성 루트가 필요하다. 부모 변경으로 스케일이 남지 않도록 재사용 시 프리팹 기준 스케일을 복구하는 규칙도 검토한다.

## 패턴 판단
- 현재의 재생 창구(Facade), 설정 SO 공유, 지연 Object Pool, 재생 핸들로 충분하다.
- 추가 상태는 IsStopping 하나로 시작한다. 별도 State 클래스, Strategy, Command, 이벤트 버스, 범용 VFX 백엔드는 현재 범위에서 불필요하다.
- 내부 인스턴스별 파티클 캐시가 필요하면 작은 래퍼/컴포넌트만 검토한다. 외부 공개 API를 확대할 필요는 없다.

## 검증 및 다음 할 일
- 정적 설계 검토만 수행했다. 컴파일 및 Unity Play 테스트는 수행하지 않았다.
- 검토 의견 확정 후 intent-043-effect-manager.md 및 Open 포인터를 작성하고 구현한다. 현재는 구현 완료를 의미하는 phase1 기록을 만들지 않았다.
- 기존 검증에 Stop 페이드 도중 재호출, 반납 후 재사용 뒤 오래된 핸들 Stop, 매니저 교체 후 오래된 핸들, 자식 전용 파티클, 매니저만 파괴, 부모 비활성화 및 서로 다른 스케일 부모 간 재사용을 추가한다.
- 연속 Play 중 동시 재생 수가 증가하면 생성 수가 늘어나는 것은 정상이다. 일정 동시 재생량으로 반납·재사용을 반복했을 때 인스턴스 수가 안정되는지 확인한다.

## 변경된 파일
- 이 검토 기록과 HANDOFF_POINTER.md만 변경했다.

## 참고
- [ParticleSystem.Play](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/ParticleSystem.Play.html)
- [ParticleSystem.stopAction](https://docs.unity3d.com/ScriptReference/ParticleSystem.MainModule-stopAction.html)
- [ParticleSystem.useUnscaledTime](https://docs.unity3d.com/ScriptReference/ParticleSystem.MainModule-useUnscaledTime.html)
- [ParticleSystem.simulationSpace](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/ParticleSystem.MainModule-simulationSpace.html)
- [Unity ObjectPool 공개 구현](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/ObjectPool/ObjectPools.cs)
