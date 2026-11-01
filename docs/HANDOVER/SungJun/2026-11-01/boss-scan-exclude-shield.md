# 보스A 스캔 홀로그램에서 에너지 실드 제외

## 1. 작업 요약 및 방법
- 문제: 보안맵 보스전에서 서 있는 보스A를 스캔하면 몸 홀로그램과 함께 에너지 실드 구체(`EnergyShield`)까지 홀로그램으로 보였다.
- 원인: `SilhouetteReaction` 의 셸 자동 생성(`_autoShellMaterial`)이 `_shellSourceRoot`(보스 루트) 아래 모든 MeshRenderer 마다 셸을 만든다. `EnergyShield`(intent-093, `Effect_09_HoloShield` 중첩 프리팹)가 보스 루트의 자식이라 실드 구체 메시에도 셸이 생겼다.
- 수정:
  - `Core/Scanning/SilhouetteReaction.cs`: `_shellExcludeRoots`(Transform 배열) 추가. 셸 자동 생성 두 경로(SkinnedMeshRenderer, MeshRenderer) 모두 이 오브젝트와 그 자식의 렌더러는 건너뛴다(`IsShellExcluded`). 스킨드 경로는 배열 → List 로 바꿔 빈 칸이 생기지 않게 했다.
  - `Prefabs/AI/Boss/BossA.prefab`: 루트 `SilhouetteReaction._shellExcludeRoots` 에 `EnergyShield` Transform 연결(YAML 두 줄).
- 씬은 열지 않았다. `Map_4_Security` 에는 보스 셸 설정 덮어쓰기가 없어 프리팹 값이 그대로 쓰인다.

## 2. 결정 사항
- 셸 자동 생성에서 몸이 아닌 렌더러(이펙트 등)는 `_shellExcludeRoots` 로 뺀다. 기본은 비어 있어 다른 프리팹 동작은 그대로다.

## 3. 현재 상태 및 이슈
- `dotnet build`(없는 ThirdParty 파일만 뺀 임시 csproj)로 컴파일 오류 0 확인.
- TestScene Play 에서 BossA 두 개를 임시로 만들어(씬 저장 없음) 스캔을 켜고 비교: 제외 칸을 비운 쪽은 셸 6개(마지막이 `Effect_09_Shield`), 연결한 쪽은 몸 5개만. 원래 쉴드 이펙트만 숨기고 찍은 사진에서 수정 전은 하늘색 구체 홀로그램이 보이고 수정 후는 몸만 보인다: [비교 사진](./images/boss-scan-shield-compare.png)
- 보호막 효과(구체·파티클)는 그대로 둔 채 스캔한 사진: 보호막은 스캔 여부와 상관없이 보이고, 스캔하면 보호막 안 보스 몸에만 홀로그램이 생긴다: [보호막 포함 사진](./images/boss-scan-with-shield.png)
- 보스맵 확인: TestScene Play 중에 `Map_4_Security` 를 런타임으로 불러오고(에디터에서 열거나 저장하지 않음) `BossFightSpawner.Prespawn` 을 불러 실제 자리에 보스A 를 세운 뒤 스캔. 셸 5개(손·팔·몸), 보호막 효과는 그대로 보이고 보호막 모양 홀로그램 없음: [스캔 전](./images/bossmap-before-scan.png), [스캔 후](./images/bossmap-after-scan.png). 스캔은 `ScanTarget.OnScanned` 를 직접 불러 켰다(플레이어 스캔 스킬로 켠 것은 아님).
- 참고: 쉴드 구체(`Effect_09_Shield`)는 이펙트 스크립트 `NewMaterialChange`(m_timeToReduce 4)가 켜진 뒤 약 5초 지나면 스스로 지운다. 그 뒤에는 구체와 그 셸이 함께 사라지고 파티클(`Effect_09_Cube`)만 남는다.
- 플레이어가 직접 들어가 스캔 스킬을 쓰는 실제 보스전 흐름은 확인하지 않았다.

## 4. 다음 할 일 (Next Steps)
- 보스전에서 스캔해 몸만 홀로그램으로 보이는지 확인.
