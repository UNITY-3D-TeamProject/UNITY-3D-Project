# EffectManager 1단계 구현

## 작업 요약 및 방법
- 설계 검토 후 사용자의 구현 요청에 따라 Core.Effect의 SOEffectData, SEffectHandle, EffectManager를 추가했다.
- Scene 단위 Instance 및 중복 로그는 현재 AudioManager/ObjectPoolManager 규칙을 따랐다.
- Unity CLI로 백그라운드 Unity 6000.4.8f1 에디터를 실행하고 컴파일, PrefabUtility를 통한 프리팹 생성, 별도 테스트 씬 Play를 검증했다.

## 결정 사항
- SO별 ObjectPool<GameObject>을 첫 Play 때 생성한다. 기존 ObjectPoolManager와 PooledObject는 변경하지 않는다.
- MaxSize는 비활성 보관 상한이다. 동시 재생 제한이나 Prewarm은 없다.
- 자식 포함 ParticleSystem 배열은 생성 때 한 번 캐싱한다. 비활성 생성 루트에서 초기화하며 stopAction=None, useUnscaledTime=false, playOnAwake=false를 인스턴스에만 적용한다.
- Play는 위치·회전·스케일을 준비한 뒤 활성화하고 StopEmittingAndClear → Play로 다시 시작한다. 자식 전체에 반복 설정을 덮어쓰지 않는다.
- 일회성은 Duration 후 반납, Stop하면 즉시 반납한다. 루프는 Stop에서 방출을 멈추고 StopFadeDuration 후 반납한다. IsStopping으로 중복 Stop을 무시한다.
- 핸들은 Id와 매니저별 object 소유 토큰을 가진다. 토큰은 GameObject를 참조하지 않으며 매니저 교체 시 같은 숫자 ID가 충돌하는 것을 막는다. IsValid는 현재 재생 여부가 아닌 발급 여부다.
- 부모 파괴로 없어진 인스턴스는 캐시·활성 목록에서만 제거한다. 내부 풀 CountActive를 실제 생존 수나 동시 재생 제한에 사용하지 않는다.
- 매니저 OnDestroy에서 외부 부모 아래 대여 객체를 직접 파괴하고 보관 풀을 Dispose한다.
- 부모 비활성화 시 호출자가 OnDisable에서 Stop하는 계약이다. 자체 Destroy/반납 스크립트가 있는 VFX는 지원하지 않는다.

## 변경된 파일
- Assets/_Project/Scripts/Core/Effect/{SOEffectData,SEffectHandle,EffectManager}.cs 및 meta/폴더 meta.
- Assets/_Project/Prefabs/System/Manager/EffectManager.prefab 및 meta. 인스펙터 설정 필드 없는 빈 GameObject + EffectManager.
- docs/intent/clear/intent-043-effect-manager.md와 INTENT_POINTER.md. 검증 완료로 resolved 처리.
- 이 문서와 HANDOFF_POINTER.md.
- 개인 Test 저장소: UnityAssets/EffectManagerValidation의 테스트 씬, Hit/Loop SO, 임시 호출 러너, 에디터 빌더 및 meta. 팀 저장소에는 포함되지 않는다.

## 검증 결과
- Unity recompile_status: completed, failed=false, errors=[], compilationFailed=false.
- 실제 VFX_ElectricArc(자식 파티클 3개) / VFX_Fire(자식 파티클 5개) 기반 Play 검증 27항목 모두 통과.
- 실행 프레임 95,265 증가. 단순 Play 진입이 아니라 러너의 시간·상태 단언으로 검증했다.
- 검증 범위: Duration 반납 및 동일 객체 재사용, 일회성 즉시 Stop, 루프 유지·자식 방출 중단·0.5초 대기·중복 Stop 무시, 부모 이동·2배→4배 스케일 재사용·부모 비활성화 후 Stop·부모 Destroy, 파괴 후 풀 대체 생성, pause 동안 수명·입자 시간 동결, resume 반납, MaxSize=2에서 동시 4개 재생 후 2개 보관, 같은 동시 재생량 반복 시 개수 안정, 매니저 파괴와 외부 부모 효과 정리, 새 매니저 Id=1과 이전 Id=1 핸들 충돌 방어.
- 로컬 상세 결과: Logs/EffectManager-play-validation.txt (Git 제외).
- 실제 게임 씬 배치 및 기존 VFX 사용처 교체, 시각적 품질 평가는 수행하지 않았다.

## 현재 상태 및 이슈
- 구현 및 기능 검증 완료. 커밋·푸시는 하지 않았다.
- 초기 임포트에서 기존 Canvas_InGame, Pet, Item_HealHp, Item_ChargeBattery의 nested prefab 참조 누락 오류 4건 확인. EffectManager 컴파일·Play 오류는 없었다.
- Test 저장소의 origin/main Pull은 기존 UnityAssets/Scene/TestScene.unity 로컬 수정 때문에 중단됐다. 해당 수정은 보존했고 stash·충돌 해결·기존 씬 저장을 하지 않았다.
- Unity CLI 1.0.0-beta.12에서 beta.13 업데이트 알림이 있었으나 업데이트하지 않았다. 기존 버전으로 검증을 완료했다.
- 검증용 runInBackground는 원래 false로 복구했다. 백그라운드 에디터를 정상 종료하고, 에디터가 자동 변경한 ProjectSettings 두 파일 및 삭제한 솔루션 파일은 작업 전 상태로 복구했다.

## 다음 할 일
- 사용자 지시 후 필요한 게임 씬에 EffectManager 프리팹을 배치한다.
- 각 사용처의 SOEffectData를 만들고 일회성 Duration, 루프 StopFadeDuration을 실제 연출에 맞게 조정한다.
- 기존 VFX Instantiate/씬 직접 재생 코드를 순차적으로 Play/Stop으로 교체한다. 루프 소유자는 OnDisable에서도 Stop하고 핸들을 default로 비운다.
- 실제 성능 문제가 생기면 Prewarm 또는 MaxConcurrent를 별도 검토한다.
