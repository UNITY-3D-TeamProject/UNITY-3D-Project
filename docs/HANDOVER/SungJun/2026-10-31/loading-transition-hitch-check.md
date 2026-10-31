# 로딩씬 경유 씬 전환 렉 점검 (2026-10-31)

## 1. 작업 요약 및 방법
- 코드 변경 없음. `SceneLoader` / `LoadingScreen` 흐름을 읽고, 에디터 Play에서 실제 전환 프레임 시간을 측정했다.
- 맵 씬은 열지 않고 TestScene에서 Play → eval로 `SceneLoader.TryContinueGame()`(저장 8단계 = Map_5_Live) 호출.
  - 1회차: TestScene → Loading → Map_5_Live
  - 2회차: `TryLoadMainMenu()`로 MainScene 이동 후 MainScene → Loading → Map_5_Live
- `Application.onBeforeRender`로 매 프레임 간격·활성 씬·로딩 화면 alpha·메모리, `sceneLoaded/sceneUnloaded` 시각을 기록.

## 2. 측정 결과 (2회차, 에디터 기준)
| 구간 | 화면 | 프레임 |
|---|---|---|
| Loading 씬 Additive 활성화 직후 | **로딩 화면 alpha 0 (게임 화면 보임)** | 239ms 1회 |
| 페이드인 0.3s | 덮이는 중 | 26~34ms (≈30fps) |
| MainScene 언로드 직후 ~ Live 로드 시작 | alpha 1 (가려짐) | **1381ms 1회** (달리는 캐릭터·진행 바 멈춤) |
| 백그라운드 로드 약 5.1s | alpha 1 | 중앙값 6.8ms, 끊김 없음 |
| Live 활성화 | alpha 1 (가려짐) | 561ms 1회 |
| 페이드아웃 첫 프레임 | alpha 0.83 | 132ms 1회 |
- 전체 약 8초. 전환 중 에러·AudioListener 중복 경고 없음. 메모리는 활성화 시 1460→857MB로 정리됨(Single 로드의 미사용 에셋 해제).
- 1회차(TestScene 출발)도 같은 패턴: 1055ms(로드 시작) / 630ms(활성화) / 112ms(페이드아웃).

## 3. 현재 상태 및 이슈
- 기능 문제 없음. 눈에 보이는 끊김은 두 곳: ① 포탈 진입 직후 로딩 화면이 뜨기 전 0.24s 멈춤, ② 페이드아웃 시작 0.13s 멈춤.
- 1.4s 멈춤은 로딩 화면 뒤라 화면은 가려지지만 러너 애니메이션·진행 바가 0%에서 멈춰 보임.
- 전부 에디터 수치. 에디터 씬 로드는 빌드보다 무거우므로 빌드에서 Profiler로 재확인 필요.

## 4. 다음 할 일
- 빌드(Development Build + Profiler)에서 같은 전환을 재측정해 실제 체감 수치 확인.
- 개선하려면 후보: 페이드아웃을 활성화 후 1~2프레임 기다렸다 시작, Loading 씬을 가볍게(러너 RT 2048×1024 축소 등) 해 첫 0.24s 줄이기.
