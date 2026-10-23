# EffectManager 현재 구조 확인

## 작업 요약 및 방법
- 사용자 요청에 따라 EffectManager의 설계와 플레이어의 실제 호출 경로를 코드·에셋 GUID·기존 intent-043 기록으로 확인했다.
- 게임 코드와 에셋은 수정하지 않았다. 이번 확인에서는 Unity 컴파일·Play를 실행하지 않았다.

## 확인한 구조
- Core.Effect.EffectManager는 씬 단위 Instance이며 DontDestroyOnLoad를 사용하지 않는다.
- 호출자가 SOEffectData와 월드 위치·회전·선택적 부모를 Play에 전달한다. SO별 UnityEngine.Pool.ObjectPool<GameObject>을 첫 요청에 생성하며 기존 ObjectPoolManager와 독립적이다.
- 일회성은 Duration 뒤 반납하고 Stop 시 즉시 반납한다. 루프는 Stop 시 방출을 중단하고 StopFadeDuration 뒤 반납한다. 실제 ParticleSystem.loop 설정은 프리팹에 따른다.
- 재생별 SEffectHandle의 ID와 매니저 소유 토큰으로 오래된 핸들의 재사용 객체 간섭을 막는다. IsValid는 현재 재생 여부가 아니다.
- MaxSize는 비활성 보관 상한이고 동시 재생 제한이 아니다. 파티클 수명과 회수는 scaled time을 사용한다.

## 플레이어 경로
- Fire → SkillMediator → CharacterMediator 이벤트 연결 → SpawnMediator → BulletFactory → ObjectPoolManager → BulletController.
- 사격·차지 애니메이션은 PlayerAnimationMediator → PlayerAnimator로 전달된다.
- 스폰은 Player 프리팹의 SpawnEffectDriver가 INab InteractiveEffect 및 재질·메시 복원과 함께 처리한다. CharacterMediator가 종료 이벤트를 받아 스폰 중 잠금을 해제한다.
- 피격 깜빡임은 PlayerAnimationMediator → RendererBlinker, 무기 열에 따른 발광색은 WeaponVisualMediator → EmissionHueChanger가 담당한다.
- PlayerFireEffect 등 SOAttributeEffect는 HP 등 수치 연산용이며 SOEffectData와 별개다.

## 현재 상태 및 이슈
- Assets/_Project 스크립트에서 EffectManager 외부 호출과 SOEffectData 필드 사용을 찾지 못했다.
- Assets/_Project의 저장된 씬·프리팹·SO를 GUID로 확인한 결과 매니저 프리팹 자체 외에 매니저 연결이나 SOEffectData 에셋은 발견되지 않았다. Test/개인 검증 환경의 현재 배치는 확인 범위에서 제외했다.
- intent-043의 기존 VFX 호출부·실제 게임 씬을 변경하지 않는 1단계 구현 범위와 일치한다.
- 현재 매니저는 ParticleSystem용이며 VFX Graph 전용 재생 제어는 구현하지 않는다. 자동 종료는 실제 입자 소멸 감지 대신 SO 시간 설정에 따른다.
- 루프 소유자는 종료·OnDisable에서 Stop해야 하며 자체 Destroy/반납 스크립트가 있는 프리팹은 지원 범위 밖이다.

## 결정 사항 및 다음 할 일
- 이번 작업은 구조 설명만 수행했다. 플레이어를 매니저로 옮기거나 기존 구조를 변경하는 결정을 하지 않았다.
- 사용처를 추가한다면 필요한 씬에 매니저를 배치하고 사용처의 SOEffectData·Play/Stop 호출을 연결한다. 연출별 Duration/StopFadeDuration 조정이 필요하다.
