# 이펙트 매니저 설계 상담

## 작업 요약 및 방법
- 시각 효과(VFX)를 전제로 싱글톤 여부와 역할 분리를 검토했다.
- ObjectPoolManager, PooledObject, Singleton 구현 및 Unity ParticleSystem.IsAlive 문서를 확인했다.

## 제안 사항 (사용자 확정 전)
- 현재처럼 전투 씬 하나를 대상으로 한다면 씬 단위 EffectManager.Instance를 사용하고 DontDestroyOnLoad는 사용하지 않는다.
- 공용 Singleton<T>는 영구 유지되므로 상속하지 않는다.
- EffectManager는 재생 요청, 기존 ObjectPoolManager는 생성·재사용, 개별 이펙트는 재생 종료·반납을 맡는다. 풀에 VFX 등록 영역은 추가가 필요하다.
- 일회성 효과는 종료 후 반납하고, 지속 효과는 재생 시 받은 참조로 종료한다.
- 기존 PooledObject의 루트 파티클 종료 콜백과 자식 파티클 전체 종료 판정이 충돌하지 않게 설계해야 한다.
- 다중 전투 씬을 동시에 운용한다면 static Instance 대신 씬별 참조 전달을 고려한다.

## 변경된 파일
- 이 설계 기록과 HANDOFF_POINTER.md만 작성·갱신했다. 런타임 코드 및 에셋 변경 없음.

## 현재 상태 및 이슈
- 설계 상담 단계이며 채택된 아키텍처 결정은 없다.
- 사용자가 말한 이펙트가 VFX인지, 능력치 효과인지 확인이 필요하다.
- 기존 미추적 intent-038-audio-manager-phase1.md는 수정하지 않았다.

## 다음 할 일
- 효과의 범위와 씬 간 유지 요구를 확인한다. 구현 요청 시 관련 intent와 코딩 규칙을 확인한 뒤 구체화한다.
