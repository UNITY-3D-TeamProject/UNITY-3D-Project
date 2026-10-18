---
id: intent-043
title: SO 기반 EffectManager VFX 재생 창구
part: Core / Effect / VFX
status: resolved
created: 2026-10-18
resolved: 2026-10-18
---

## 문제 (Problem)
VFX 재생 공용 창구가 없어 각 사용처가 Instantiate하거나 씬 ParticleSystem을 직접 켠다. 재사용, 일회성 수명, 루프 종료를 공통으로 처리할 수 없다.

## 기대 결과 (Proposed outcome)
- 사용하는 쪽이 SOEffectData를 참조하고 EffectManager.Instance.Play(data, position, rotation, parent)로 재생한다. Play는 항상 SEffectHandle을 반환한다.
- SO별 ObjectPool<GameObject>을 첫 Play에서 생성한다. 미등록 SO도 재생하며 MaxSize는 비활성 보관 상한이다.
- 일회성은 Time.time + Duration에 반납하고 Stop 시 즉시 반납한다. 루프는 Stop 시 방출을 멈추고 StopFadeDuration 후 반납한다. 중복 Stop은 무시한다.
- 자식 포함 파티클을 생성 시 캐싱하고 StopEmittingAndClear 후 다시 재생한다. SO IsLoop는 반납 정책이며 실제 반복 구성은 프리팹이 담당한다.
- 핸들의 재생 ID와 매니저별 소유 토큰으로 재사용 및 매니저 교체 후 오래된 핸들을 차단한다. IsValid는 발급 여부다.
- 부모 부착, 부모 Destroy, 재사용 시 스케일 복구, 매니저 Destroy 시 외부 부모 아래 효과 정리를 지원한다.
- 공용 스크립트 3개와 빈 GameObject + EffectManager 프리팹을 제공한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 피격·폭발·지속 VFX가 필요한 기능의 개발자.
- 어떤 시스템/모듈이 건드려지는가: Assets/_Project/Scripts/Core/Effect, Assets/_Project/Prefabs/System/Manager/EffectManager.prefab, 관련 문서.
- 테스트 씬·임시 호출 스크립트·테스트 SO는 Assets/Test 아래에서만 검증용으로 사용하고 본 저장소에는 포함하지 않는다.

## 제약 (Constraints)
- 씬 단위 Instance. Singleton<T> 상속 및 DontDestroyOnLoad를 사용하지 않는다. Awake/OnDestroy의 Instance 규칙은 AudioManager와 같다.
- ObjectPoolManager, PooledObject, 기존 VFX 호출부와 실제 게임 씬은 변경하지 않는다.
- 파티클 반납 콜백, Prewarm, VFX Graph, 동시 재생 제한, 별도 상태 머신·이벤트 버스는 이번 범위에서 제외한다.
- SO의 런타임 상태를 변경하지 않는다. 생성된 파티클의 stopAction=None, useUnscaledTime=false, playOnAwake=false로 수명·재생 제어권을 통일한다. 자체 Destroy/반납 스크립트가 붙은 프리팹은 지원하지 않는다.
- 부모 비활성화 시 호출자가 OnDisable에서 Stop한다. 기존 입자의 추적은 프리팹 Simulation Space 설정에 따른다.
- 새 인스턴스는 비활성 생성 루트에서 만들어 잘못된 위치의 최초 OnEnable을 방지한다.

## 열린 질문 (Open questions)
- 없음. 2026-10-18 설계 검토·전체 요약·풀 설명 후 사용자 구현 요청에 따라 진행한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 구현: Core/Effect 스크립트 3개와 EffectManager.prefab 생성. 실제 게임 씬과 기존 VFX 사용처는 변경하지 않았다.
- Unity 6000.4.8f1 재컴파일 완료, failed=false / errors=[] / compilationFailed=false.
- Assets/Test/UnityAssets/EffectManagerValidation의 별도 씬에서 실제 ElectricArc·Fire 프리팹으로 Play 검증 27항목 통과. 95,265 프레임 진행 확인.
- 검증: 일회성 반납·재사용, 자식 파티클 루프 및 중복 Stop, 오래된 핸들, 부모 이동·Destroy·비활성화·스케일 변경, Time.timeScale=0, 풀 보관 상한, 매니저 파괴 정리.
- 매니저 소유 토큰은 매니저별 새 object로 구현했다. GameObject 참조나 static 세대 카운터 없이 매니저 교체를 구분한다.
- 시각적 품질 평가는 수행하지 않았다. 프리팹별 실제 잔여 파티클 수명에 맞춘 Duration/StopFadeDuration 조정은 사용처 연결 시 진행한다.
- 프로젝트 초기 임포트에서 기존 Canvas_InGame/Pet/Item_HealHp/Item_ChargeBattery의 누락 nested prefab 오류 4건이 확인됐으나 이번 구현의 컴파일·Play 검증과는 별개다.
- 관련 커밋/PR: 없음.
