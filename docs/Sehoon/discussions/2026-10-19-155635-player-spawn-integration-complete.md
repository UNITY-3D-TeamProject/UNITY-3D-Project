# 플레이어 스폰 연출 연결 — 구현과 TestScene 검증 완료

작성: 2026-10-19 15:56:35 KST

## 목적·역할·사용자 확정

- 목적: 테스트한 아래→위 생성 연출을 실제 `Player.prefab`에 연결한다.
- 선행 협의: [조사·계획 검토](2026-10-19-133624-player-spawn-integration-review.md). Claude Code Opus 조사·계획 → Codex 검토 → Opus 계획 보완을 마쳤다.
- 이번 실행: Claude Code Sonnet이 프리팹 연결과 자체 참조 검사를 수행하고 Codex가 diff·참조·실제 Play 동작·보존 상태를 독립 검증했다.
- 사용자 확정: 현재 씬의 Spawn Rate **1000**, 진한 파란색을 사용한다. 실제 Color **(0, 0.08463347, 0.5, 1)**을 반올림하거나 새로 정하지 않고 복사했다.
- 사용자 확정: 기존 씬의 added 연출은 **비활성화하여 미저장 튜닝 보존**. 씬은 저장하지 않는다.
- 사용자 확정: **TestScene에서 재생·재생성·중단 검증**. 실제 Map 경로 검증은 이번 범위에 포함하지 않는다.
- 스폰 즉시 재생하는 A안, Duration 1.2초, padding 0.02/0.02, VFX playRate 2 및 나머지 기존 튜닝을 유지한다.

## 구현 결과

- 제품 변경 파일: `Assets/_Project/Prefabs/Player/Player.prefab` 하나. diff는 **513줄 추가, 삭제 0줄**이며 기존 블록은 보존하고 루트 자식 연결과 연출 블록을 추가했다.
- Player 루트 아래 활성 `PlayerSpawnDissolve`와 기존 `Visual.SpawnEffectDriver`를 연결했다. 하위 `Interactive Dissolve`는 비활성으로 저장하고 Mask·VFX Graph·property binder·transform binder 구성을 보존했다.
- 몸체·Buster 2개·PowerPod 렌더러 4개는 프리팹 내부 대상에 연결했다. Readable Mesh·마스크 셰이더·Burn 템플릿은 `ThirdParty/SpawnEffect` 사본을 직접 참조한다.
- Driver 코드와 공개 API, PlayerSpawner·StageManager·LoadingScreen은 수정하지 않았다.
- 기존 TestScene added 연출에만 Undo 가능한 비활성화를 적용했다. 상속된 새 연출은 활성이다. 기존 Test Driver와 미저장 튜닝은 보존했다.
- Sonnet은 긴 eval 전달 제약으로 작업을 나눠 진행했다. 로드한 프리팹은 저장 직후 Unload했고, 연결에 쓴 SessionState 키를 정리했다. 별도 구현 스크립트 파일은 만들지 않았다.

## 참조·diff 검증

- Sonnet: 저장 전·재로드 후 직렬화 참조 591건 검사, Missing Script·Test 참조·외부 씬 참조 0. 씬 원본과 컴포넌트 11개·값 533개·참조 49개 대조에서 불일치 0이라고 반환했다.
- Codex: 저장된 프리팹 전체 참조를 독립 순회해 **Driver 1개, Missing Script 0, Test 참조 0, 외부 씬 참조 0**을 확인했다. `AssetDatabase.GetDependencies`의 Test 의존도 0이다.
- Codex: 렌더러 4개, 사본 에셋 경로, Duration·padding·playRate와 VFX 튜닝 전 항목을 실제 에셋에서 읽어 확인했다.
- `git diff --check`에는 Unity가 생성한 빈 YAML 필드 6곳의 trailing whitespace 경고가 있다. YAML 수작업 정리는 하지 않았다.

## Codex 실제 Play 검증

`timeScale=1`, `captureDeltaTime=0`에서 연속 Play로 관측했다. 시작 준비 상태를 확인하기 위한 초기 pause 후 재생했으며, `wait_for(Time.frameCount changed)`로 실제 프레임 진행을 확인했다. 임시 관측 코드는 eval과 메모리 콜백만 사용했다.

| 시나리오 | 관측 결과 |
|---|---|
| 씬 Player 첫 재생 | 307개 프레임 표본. 마스크가 아래→위로 단조 상승. 약 **1.2107초**에 원래 재질 복원, **1.9669초**에 입자 0·완료 |
| 기존 Player 파괴 확인 후 새 프리팹 생성 | 다음 프레임 이후 Instantiate. 248개 프레임 표본. 아래→위 재생, **1.2122초**에 재질 복원, **1.8006초**에 입자 0·완료 |
| 재생 중 연출 비활성화 | 약 **0.301초**에 중단. 원래 재질·메시·meshTransform·binder Target·playRate 복원, 임시 재질 0 |
| 재생 중 Player 전체 파괴 | 약 **0.311초**에 파괴. Player·Driver·effect 소멸, 임시 재질 0, 예외 없음 |

- 정상 재생 두 건 모두 원래 Body/Lens 등을 포함한 모든 대상 재질, 메시, meshTransform, binder Target, 원래 playRate 1을 복원했다. effect 비활성, `ParticleCountAtRestore=0`, 해당 런타임 재질 잔존 0을 확인했다.
- VFX 활성화와 Start 이후에도 Spawn Rate 1000을 유지했다. 활성 Driver는 `Visual.SpawnEffectDriver` 하나였다.
- 생성 중 GameView를 직접 확인해 파란 입자와 중간 생성 경계를 확인했다. 최초 instanceID 기반 캡처 대기는 대상 해석 실패로 캡처되지 않았고, 새 인스턴스에서 타입 기반 조건으로 다시 캡처해 확인했다.
- 콘솔의 실제 오류·경고는 끝까지 **0/0**, compilationFailed=false다. 시작 커서 이후 새 경고·오류가 없으며 기존 로그 버퍼는 지우지 않았다.

## 보존 상태와 미검증

- Codex가 확보한 관련 **80개 파일**의 시작·종료 SHA256을 대조했다. 변경은 `Player.prefab` 하나뿐이다. Driver/meta, TestScene 디스크, ProjectSettings 전체, SpawnEffect 사본/meta, 관련 INab·Tacko3D·Test 원본은 동일하다.
- Play 종료 후 TestScene 단독 dirty=true를 유지한다. 루트 오브젝트들의 활성·위치·회전·스케일과 기존 비활성 연출의 미저장 튜닝이 Play 전과 동일하다.
- 종료 상태: Play=false, pause=false, timeScale=1, captureDeltaTime=0, runInBackground=false. 기존 autotick=true·16ms도 그대로다.
- 실제 Map의 PlayerSpawner·StageManager 리스폰·LoadingScreen 경로, 로딩 페이드와의 시각적 겹침, INab 미보유 PC에서의 컴파일은 **미검증**이다. Destroy/Instantiate 재현을 실제 리스폰 경로 검증으로 간주하지 않는다.
- 승인 범위 안의 구현·TestScene 검증은 완료했고 추가 수정 필요 사항은 발견하지 않았다. 커밋·푸시·브랜치 조작, 씬 저장, HANDOVER·Intent·포인터·대화 원문 로그 작성은 하지 않았다.
