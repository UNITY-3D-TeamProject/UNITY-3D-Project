# 보안 맵 2라운드 낙하 위험물 (intent-074)

## 1. 작업 요약 및 방법
- 요청: `0_Unity_Optimized`·`DigitalVirus_Unity_Optimized`(`Assets/ThirdParty/Projectile/`)가 `04_Platforms` 에서 공중에서 떨어지게.
- 사용자 확정: 피해 주는 위험물 / 2라운드 동안 반복 / 켜진 발판 위 무작위 위치 / 닿으면 사라짐. 수치는 기본값(간격 2초, 1개, 15 m, 예고 1초, 피해 10).
- 코드
  - `Map.Gimmicks.FallingObstacle`: `_isDestroyOnLand`(바닥에 닿으면 Destroy, 예고 표시는 기존 OnDestroy 가 지움), `FallDistance` 속성. 기존 배치는 옵션 꺼짐이라 동작 동일.
  - 신규 `Map.Gimmicks.FallingHazardSpawner`: `StartSpawning`/`StopSpawning`(남은 위험물 지움)/`ClearHazards`/`SpawnHazard`. 켜진(`activeInHierarchy`) 발판 콜라이더 중 하나를 골라 윗면 안(가장자리 여백 1 m) 무작위 지점, `FallDistance + 콜라이더 절반 높이` 위에서 생성. **부모 없이 생성**(`04_Platforms` 스케일이 2라 자식으로 두면 2배로 커짐).
  - `SpecialMobRound`: `_hazardSpawner`, `StartHazards`/`StopHazards`, `ResetUnclearedSteps` 에서 `ClearHazards`.
  - `SecurityApp.OnRoundChanged`: 2라운드면 `StartHazards`, 그 외 라운드(-1 포함)면 `StopHazards`.
- 에셋·씬 (MCP eval)
  - 프리팹 `Prefabs/Map/Gimmicks/Security/Security_FallingZero`·`Security_FallingVirus`: `Hazard_FallingBlock` 구조 그대로(루트 Trigger BoxCollider + Rigidbody + EffectZone(Enter `Effect_Hazard_Damage10`) + FallingObstacle, 자식 Visual(FBX ×2)·WarningMarker(Cube, `M_Hazard_Warning`)).
  - `Map_4_Security`: `Stage_BossRun/04_Platforms/FallingHazardSpawner` 추가(Platform_1~6 루트 BoxCollider, 프리팹 2개), `SecurityApp._specialMobRound._hazardSpawner` 연결, 씬 최상위의 원본 FBX 2개 삭제, 저장.
- 테스트: `Assets/Test/Stage/FallingHazardSpawnerTests.cs`(Git 제외) 4개. Assembly-CSharp 에 `UnityTest` 참조가 없어 동기 `[Test]` 로 작성.

## 2. 결정 사항
- 낙하·예고·피해는 새로 만들지 않고 `FallingObstacle` + `EffectZone` 재사용.
- 위험물은 처음엔 매번 Instantiate/Destroy 였으나 사용자 요청으로 `ObjectPoolManager` 풀링으로 전환(아래 후속 참고).
- 2라운드 외 모든 라운드에서 멈춤, 2라운드 중 사망 시 떨어지는 중인 위험물만 지우고 생성 계속.

## 3. 현재 상태 및 이슈
- 리컴파일 에러 0, PlayMode 4/4. Play 확인: 1라운드 미생성, 2라운드 순간이동 후 생성(켜진 Platform_1 위만), 시작 높이 16 m·크기 1배, 착지 시 예고와 함께 사라짐, 플레이어 위 낙하 시 HP 90 → 80, `ResetUnclearedSteps` 후 위험물 0·생성 계속, 3라운드 진입 시 멈춤·전부 지움.
- **`Assets/ThirdParty/Projectile/` 이 SVN 원본(`D:\ThirdParty\ThirdParty`)에 없음** → 팀원 PC 에서 프리팹 Visual 이 빠짐. 사용자 SVN 업로드 필요.
- 씬 저장 시 `Canvas_InGame` RectTransform 레이아웃 값(SizeDelta 등)이 함께 다시 기록됨(에디터 재계산, 의도한 변경 아님). `Pretendard-Bold SDF.asset`·`ShaderGraphSettings.asset` 도 에디터가 수정함(손대지 않음).
- 상태: `waiting_user` — 육안·손맛(간격·크기·예고 시간·피해량) 확인 전. 게이트 루프 미실행, 커밋 전. Codex CLI 미설치로 Claude 직접 실행.

### 후속: 펫이 안 보이던 문제 (사용자 지적)
- 원인: `Rob11.fbx`(ThirdParty) 의 Library 임포트 결과가 오래돼 렌더러 머티리얼 6개가 전부 NULL. `.meta` 의 externalObjects 매핑과 머티리얼 5개는 정상이었고, 이번 에디터 세션에서 머티리얼만 다시 임포트되고 FBX 는 다시 임포트되지 않았음. 이번 작업 파일·씬 diff 에 펫 관련 변경 없음.
- 조치: `AssetDatabase.ImportAsset(Rob11.fbx, ForceUpdate)` (파일·meta 변경 없음, 로컬 Library 만). Play 에서 펫 머티리얼 정상 확인. `_Project` 프리팹 전체에서 NULL 머티리얼 렌더러 0개.
- 다른 PC 에서 같은 증상이면 `Rob11.fbx` 우클릭 → Reimport.

### 후속: 대화창이 안 뜨고 조작이 막히던 문제 (사용자 지적)
- 증상: `Map_4_Security` 입장 컷씬(14번)·보스 브리핑(23번) 트리거 콜라이더가 계속 꺼져 있고 플레이어 조작도 잠김. 경로: `EnableColliderOnSpawnEnd` → `PlayerFacade.OnSpawnEffectEnded` → `SpawnEffectDriver.CoSpawnDissolve` 가 `aliveParticleCount == 0` 을 무한 대기.
- 원인: `ThirdParty/INab Studio/.../Particles Spam/Angel Dust Dissolve.vfx`(PlayerSpawnEffect 가 사용)가 10-24 00:12 에 다시 저장돼(384 KB) 10-21 백업(459 KB, md5 불일치)과 달라졌고, 이 파일의 VFX 는 입자 수가 계속 `-2`·시스템이 안 깨어남. 같은 폴더의 다른 VFX 3개는 `0` 으로 정상. 에디터 포커스 문제 아님.
- 조치: 현재 파일을 `D:\ThirdParty\Angel Dust Dissolve.vfx.broken_20261025` 로 보관하고, `D:\ThirdParty\Backup_Assets_ThirdParty_20261023\...\Angel Dust Dissolve.vfx` 로 교체(GUID `e5915b9a…` 유지). Play 에서 생성 연출 약 2초 후 종료, 입장 컷씬 재생 확인.
- 이번 낙하 위험물 작업과 무관(파일 수정 시각이 작업 이전). SVN 원본(`D:\ThirdParty\ThirdParty`)에는 이 파일이 없음 → 다른 PC 에서 같은 증상이면 이 교체가 필요.

### 후속: 0·1 모델에 셰이더가 안 입혀지던 문제 (사용자 지적)
- 원인: `0_Unity_Optimized.fbx`·`DigitalVirus_Unity_Optimized.fbx` 의 `.meta` `externalObjects` 가 `{}` 라 FBX 내장 URP/Lit 머티리얼이 그대로 쓰였음. `M_DigitalProjectile_*`(SG_DigitalProjectile) 머티리얼은 만들어만 두고 어디에도 연결 안 됨.
- 조치: 두 프리팹의 렌더러 4개(Zero 1, Virus 3)에 `M_DigitalProjectile_0/_1_Lower/_1_Upper/_1_Body` 를 프리팹 오버라이드로 지정(Git 에 남는 쪽). FBX `.meta` 는 건드리지 않음. 저장된 프리팹 재로드로 4개 모두 `Shader Graphs/SG_DigitalProjectile` 확인. 육안 확인 전.

### 후속: 오브젝트 풀링 전환 (사용자 요청)
- 기존 `Core.ObjectPool.ObjectPoolManager` 사용. 공용 클래스에 카테고리 `_mapHazards`("Map Hazards (낙하 위험물)") 한 칸과 `RegisterPool` 한 줄만 추가.
- `FallingObstacle`: `DropOnto(착지점)`(꼭대기 계산 + 예고 표시 배치 + 낙하 시작)·`Despawn()`(매니저 있으면 Release, 없으면 Destroy) 추가. 착지 시 `Destroy` 대신 `Despawn`. `Start` 는 `_isRunning` 이면 건너뜀(풀에서 꺼낸 직후 DropOnto 가 먼저 불리므로). 예고 표시는 풀 루트 아래로 분리돼 위험물과 함께 재사용되고 오브젝트가 파괴될 때만 지워짐.
- `FallingHazardSpawner`: `Instantiate` 대신 `ObjectPoolManager.Get(프리팹)` 후 `DropOnto`. 매니저가 없는 씬은 Instantiate 로 폴백. 목록은 `List<FallingObstacle>`, 반납된(비활성) 항목은 꺼내기 **전에** 제거(재사용으로 같은 객체가 목록에 두 번 들어가는 것 방지). `ClearHazards`/`StopSpawning` 은 Release.
- 씬: `Map_4_Security` 의 `ObjectPoolManager` 에 `FallingZero`·`FallingVirus` 등록(초기 4, 최대 8) 후 저장.
- 검증: 리컴파일 에러 0, PlayMode `FallingHazardSpawnerTests` 5/5(풀 재사용 테스트 1개 추가, 높이 테스트는 `DropOnto` 가 바로 낙하를 시작하므로 템플릿 대기 시간을 1000초로 변경). Play: 시작 시 8개 미리 생성, 약 17초(스폰 8회 안팎) 동안 총 8개 유지(추가 생성 없음)·동시 활성 1개, `StopSpawning` 후 활성 0·`HazardCount` 0. 콘솔에 이번 변경 관련 에러 없음.

## 4. 다음 할 일 (Next Steps)
- 사용자 플레이로 수치 조정(`FallingHazardSpawner` 간격·개수·여백, 프리팹 `FallingObstacle` 낙하 거리·예고 시간, EffectZone 피해 SO).
- `ThirdParty/Projectile` SVN 업로드 후 intent-074 resolved 처리.
