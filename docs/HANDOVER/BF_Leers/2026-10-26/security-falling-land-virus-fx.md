# 보안 맵 0·1 낙하물 착지 바닥 감염(픽셀 붕괴) 연출

## 작업 요약 및 방법
- intent-077. 0·1 낙하물이 바닥에 닿아 사라질 때 착지점에서 빨간 정사각 픽셀이 들쭉날쭉 번진 뒤, 일부가 깜빡이고 무작위 순서로 깨지듯 꺼지는 연출을 추가했다. 보기만 하는 연출이고 판정은 없다.
- 신규 셰이더 `Project/VirusPixelSpread`(`Art/Shaders/Map/VirusPixelSpread.shader`, URP Unlit 직접 작성). Quad UV를 `_GridCount`(16) 칸으로 나눈다. 가운데에서 먼 칸일수록 늦게 켜지고(`_SpreadPortion`), 가장자리는 각도 사인파 + 칸별 난수로 깎는다(`_EdgeJitter`). 꺼지는 순서는 칸별 난수다(`_BreakStart`~0.95). 막 켜진 칸은 `_FlashColor`로 번쩍이고, 깨지기 직전 칸과 일부 칸은 깜빡인다. `_Progress`·`_Seed`는 MaterialPropertyBlock으로 받는다.
- 신규 `Map.Gimmicks.PixelSpreadEffect`: OnEnable에서 시드를 무작위로 정하고 진행도를 0으로 둔다. Update에서 `_duration`(1.2초) 동안 0→1로 올리고 끝나면 Destroy한다.
- `FallingObstacle`: 선택 필드 `_landEffect`(Header "Land Effect")를 추가했다. `_isDestroyOnLand` 착지 분기에서 `SpawnLandEffect()` → `Despawn()` 순서로 실행한다. 바닥 위치 계산은 `GetGroundPosition()`으로 묶어 예고 배치와 함께 쓴다(계산식은 그대로).
- 에셋(Unity MCP eval, YAML 직접 편집 없음): `Art/Materials/Map/M_VirusPixelSpread.mat`, `Prefabs/Map/Gimmicks/Security/Security_VirusSpreadFX.prefab`(루트에 `PixelSpreadEffect`, 자식 `PixelQuad`는 X 90°·스케일 4(예고 원 반지름 2 m와 같은 지름)·그림자·프로브 끔). `Security_FallingZero`·`Security_FallingVirus`의 `_landEffect`에 연결했고, 프리팹 diff는 각각 한 줄이다.
- 실행자: Claude가 직접 했다(Codex 위임 없음). 셰이더 작성과 렌더 확인을 한 흐름에서 바로 반복해야 하는 작은 범위라서, 위임 비용이 더 크다고 판단했다.

## 결정 사항
- 사용자 결정: 시각 연출만, 빨간 경고 톤, 바닥 감염만(파편 없음), 크기는 예고 원과 같게, 시간 약 1.2초.
- 이펙트는 2초 간격으로 1개씩 생겨 수가 적다. 그래서 풀을 쓰지 않고 Instantiate/Destroy 한다. `ObjectPoolManager`나 씬 등록은 바꾸지 않았다.
- `_landEffect`가 비어 있으면 기존 동작과 같다. 다른 `FallingObstacle` 배치(Hazard_FallingBlock 등)에는 영향이 없다.

## 현재 상태 및 이슈
- 리컴파일 성공. 셰이더 `ShaderHasError` false.
- 테스트 `FallingHazardSpawnerTests` 6/6 통과(기존 5 + 신규 `SpawnLandEffect_PlacesEffectOnGround`). `Assets/Test`(Git 제외) 어셈블리에는 TestRunner 참조가 없어 `[UnityTest]`를 쓸 수 없다. 그래서 위치는 동기 테스트로, 자동 소멸은 Play에서 확인했다.
- 테스트를 시작할 때 Unity가 `Map_4_Security` 저장 창에서 대기했다. 사용자가 직접 처리했다.
- 진행도별 프리뷰 렌더(프리뷰 씬, 실제 씬 변경 없음): `Logs/VirusFx/progress_grid.png`. 0.08~0.85 구간에서 점 → 번짐 → 무작위 소멸을 확인했다.
- Play(`Map_4_Security`): 맵 시작 시 timeScale이 0이어서 확인하는 동안만 1로 바꿨다(Play 종료로 원복). 낙하물 생성 → 착지 시 이펙트 y=469.03 생성(계산값 500−30−1+0.03과 일치) → 이펙트와 낙하물 모두 사라짐을 확인했다.
- **육안 확인(실제 플레이 시인성·색·크기·시간) 미실행, 커밋 전.** HDR 색이라 Bloom이 켜진 맵에서는 더 밝게 빛날 수 있다.

## 후속: 0·1 글자 노이즈로 교체 (사용자 피드백 "짜치다")
- 정사각 픽셀만으로는 밋밋하다는 피드백을 받아 셰이더를 고쳤다. 칸마다 0·1 글자를 SDF로 직접 그린다(텍스처 없음, 0은 사선이 들어간 코드 글꼴 모양).
- 칸마다 다른 속도로 0과 1이 계속 바뀐다(`_GlyphRate` 12/s). 일부 칸은 꽉 찬 블록이다(`_BlockRatio` 0.12).
- 가끔 한 줄이 칸 단위로 옆으로 튀는 글리치가 생긴다(`_GlitchRate` 14/s). 감염된 칸 바탕에는 흐르는 스캔라인을 옅게 깐다(`_UnderlayAlpha` 0.22).
- 격자는 16 → 22칸으로 늘렸다. 번짐·깨짐 순서·번쩍임·깜빡임 로직은 그대로다.
- 머티리얼 값을 새 기본값으로 맞췄다. `_PixelGap`은 블록 칸 틈으로 계속 쓴다.
- 셰이더 오류 없음. 프리뷰 렌더: `Logs/VirusFx/progress_grid_v2.png`. 코드 변경은 없어서 테스트는 다시 돌리지 않았다.
- 게임 카메라 거리에서 글자가 잘 읽히는지는 육안 확인이 필요하다. 작으면 `_GridCount`를 낮춘다.

## 후속 2: 예고-착지 시간 맞춤, 예고원·히트박스 크기 맞춤 (사용자 픽스 요청)
- 문제 1: 예고가 `_waitBeforeFall`(1초) 동안만 차고, 그 뒤 30 m 낙하(가속도 20, 약 1.7초)가 이어져 예고가 꽉 찬 뒤 한참 있다가 떨어졌다.
  - 수정: `FallingObstacle.CoFallCycle`에서 예고가 `대기 + √(2×낙하거리÷가속도)` 동안 차도록 바꿨다. 낙하 루프에서도 진행도를 계속 올리고, 착지 때 1로 맞춘다.
  - 이 변경은 `FallingObstacle`을 쓰는 다른 장애물 예고(Renderer 색 변화 포함)에도 똑같이 적용된다. 바닥에 닿는 순간 예고가 다 찬다.
  - 툴팁과 클래스 설명도 고쳤다.
- 문제 2: 히트박스는 모델 크기 그대로(0: 1.16×0.26, 1: 0.33×0.19 m)인데 예고원은 반지름 2 m였다.
  - 사용자 결정 "둘 다 중간으로": 예고원 `CircleRegion._radius` 2 → 1(중첩 프리팹 오버라이드), `BoxCollider` XZ를 1.6×1.6으로 넓혔다(높이 2는 그대로라 착지 위치 계산도 그대로). 두 프리팹 모두 적용했다.
  - 착지 연출 `PixelQuad` 스케일 4 → 2(예고원 지름과 같음). 글자 크기를 비슷하게 유지하려고 머티리얼 `_GridCount` 22 → 12로 바꿨다.
- 검증:
  - 리컴파일 통과. `FallingHazardSpawnerTests` 6/6. 첫 비동기 요청이 지난 결과를 돌려줘서 다시 실행했고, 시간 값이 바뀐 새 결과임을 확인했다.
  - Play 측정: 예고원 FillProgress가 0.99를 넘은 시점은 시작 후 2.49초, 그때 낙하물 높이 470.77(바닥 470). 0.03초 뒤 착지(마지막 진행도 1.0). 예고원 반지름 1, 표시 스케일 2.
  - 미리보기: `Logs/VirusFx/progress_grid_v3.png`. 미리보기 씬에서는 CircleRegion Update가 돌지 않아 원 크기 비교용으로는 부정확하다.
- 넓힌 히트박스로 실제 플레이어가 맞는지는 직접 조작으로 확인하지 않았다.
- Play/테스트 후 `Pretendard-Bold SDF.asset`(TMP 동적 아틀라스)이 수정됨으로 나온다. 이번 작업 의도와 무관한 부수 변경이다.

## 다음 할 일
- 실제 플레이에서 육안 확인한다. 필요하면 `M_VirusPixelSpread`의 `_GridCount`·`_EdgeJitter`·`_SpreadPortion`·`_BreakStart`·색, 이펙트 프리팹의 `_duration`·Quad 스케일을 조정한다.
- 확인이 끝나면 intent-077을 resolved로 바꾸고 `clear/`로 옮긴다.
