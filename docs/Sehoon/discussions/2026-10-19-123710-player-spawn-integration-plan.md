# 실제 플레이어 스폰 연출 연결 계획 검토

- 일시: 2026-10-19 12:37 KST
- 요청: 사용자가 배치한 `Assets/ThirdParty/SpawnEffect`와 `Assets/_Project/Scripts/Visual/SpawnEffectDriver.cs`를 사용해 실제 플레이어 스폰 시 연출 적용.
- 역할: Claude Opus가 디스크 기반 조사·최초 계획·Codex 검토 반영 계획 수정, Codex가 요청 전달·근거 대조·환경 확인·검토·요약. 구현은 아직 수행하지 않았다. 확정 후 Claude Sonnet 구현, Codex 실행 검증 예정.

## 확인 결과

- 새 Driver는 `Visual.SpawnEffectDriver`이고 기존 Test Driver와 생명주기 로직은 같다. 사용자 배치 파일을 유지한다.
- ThirdParty/SpawnEffect의 셰이더 2개·재질·FBX는 Test 사본과 내용은 같지만 GUID가 다른 복사본이다. TestScene은 디스크 기준 여전히 이전 Test 에셋과 Test Driver를 참조한다.
- 실제 생성은 `PlayerSpawner.Start -> SpawnPlayer -> SpawnerBase.TrySpawnWithAttributes -> Instantiate`이다. 리스폰도 기존 인스턴스 Destroy 후 다음 프레임에 새 인스턴스를 만든다. 현재 이 경로에는 풀 재사용이 없다.
- 실제 사용 프리팹은 `Assets/_Project/Prefabs/Player/Player.prefab`이다. Driver와 효과 계층을 프리팹에 연결하면 매 새 인스턴스의 Awake/Start에서 재생할 수 있다.
- TestScene의 기존 added PlayerSpawnDissolve를 남기면 프리팹에 추가한 연출과 중복되므로 중복 제거가 필요하다.
- 로딩 코드는 목적지 씬 활성화 뒤 화면을 fade out한다. 즉시 재생하면 생성 연출 앞부분이 가려질 수 있다.
- ScriptableObjects/SpawnEffect는 기존 Attribute 효과 데이터이며 이번 시각 연출과 무관하다.
- Codex 직접 환경 확인: Unity 6000.4.8f1 ready, Play 정지, 컴파일 오류 없음. 현재 TestScene에는 미저장 변경이 있다. Unity CLI beta13 최신 확인.

## 공통 최소 변경 범위

- Player.prefab: PlayerSpawnDissolve / Interactive Dissolve / Mask / VFX Graph 계층과 Visual.SpawnEffectDriver를 연결한다. Driver는 ThirdParty/SpawnEffect 사본을 참조하고 몸체·렌즈·무기·PowerPod 렌더러를 연결한다.
- 기존 튜닝 1.2초 생성, playRate 2, Spawn Rate 3000, Scale 4, Emission 1, Edge Width 0.12와 원래 외형·복원 로직을 유지한다.
- TestScene: 기존 중복 PlayerSpawnDissolve만 제거한다. 현재 미저장 사용자 변경을 유실하거나 임의로 저장하지 않는다.
- 원본 INab/Tacko3D, ThirdParty/SpawnEffect 내용, ProjectSettings, 조작·피격·무적 정책은 변경하지 않는다. git 조작은 없다.
- 기존 INab 컴파일 의존성은 유지한다. 의존성 guard나 ProjectSettings 변경은 추가하지 않는다. 구매 에셋 미설치 환경의 컴파일 제한은 남는다.

## 사용자 선택 및 승인 대기

1. 스폰 즉시 재생: 기존 Awake/Start 재사용, Spawner·로딩 코드 무변경. 로딩 페이드와 앞부분이 겹칠 수 있다.
2. 로딩 페이드 완료 후 재생: 시작 신호·대기 처리가 추가로 필요하다. 로딩 공개 인터페이스 및 대기 중 표시/입력 정책은 선택 후 상세 설계해야 한다.

Codex 최소 변경 추천은 1안이다. 현재 사용자 미저장 TestScene은 에디터에서 중복만 제거하고 저장하지 않는 방식으로 보존할 수 있다. 사용자가 현재 미저장 변경을 함께 저장하도록 승인하면 해당 상태를 보존한 채 중복 제거 결과를 저장할 수 있다. 재생 시점과 실행 범위 확정 전 구현하지 않는다.

## 예정 검증 및 한계

- Unity API로 프리팹을 구성하고 재질·메시·셰이더·binder의 모든 참조 및 VFX 노출값을 검사한다. effect 자식 Awake/Start가 튜닝값을 덮지 않는지 확인한다.
- 실제 스폰 및 리스폰의 새 인스턴스에서 연출이 하나만 재생되고, 완료·중단·파괴 후 원래 재질/mesh/meshTransform/binder/playRate로 복원되며 임시 재질과 입자가 정리되는지 확인한다.
- 미저장 씬을 보존한 상태에서 Map의 실제 PlayerSpawner 경로를 실행 검증한다.
- 원본 파일 해시와 프리팹·씬 변경 범위를 대조하고 컴파일·콘솔 오류를 확인한다.
- 이번 단계는 정적 조사와 환경 확인까지이다. 구현, Play, 리스폰·로딩 화면 동작 검증은 아직 하지 않았다.
- HANDOVER·Intent·포인터는 생성·갱신하지 않았다.
