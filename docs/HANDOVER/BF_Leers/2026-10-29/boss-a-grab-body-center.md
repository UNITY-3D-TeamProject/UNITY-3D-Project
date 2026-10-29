# 보스A 그랩: 집게로 쥐고 끌고 가기 (intent-087, intent-059 후속)

## 1. 작업 요약 및 방법
- 문제: 그랩(A4)에 잡힌 플레이어를 손 오브젝트 위치에 맞췄는데, 이 위치는 손목 아래(손바닥 본 `RigLArmPalm`/`RigRArmPalm` 보다 4.58m 아래)다. 손(Gripper Robot 집게)의 턱은 손바닥 본에서 손가락 방향으로 15.3~23.9m 앞에 있어, 플레이어가 손 뒷부분에 매달려 끌려가는 것처럼 보였다.
- 경과: ① 몸 중심을 손 위치에 맞춤 → 머리를 잡는 느낌 ② 발을 손 위치에 맞춤 → 여전히 손 뒷부분 ③ 사용자 B안 결정: 집게가 감싸 쥐고 끌고 간다.
- `BossHand.cs`
  - `[Header("Grab")] _grabPoint`(집게 턱 사이 Transform), `_gripCurl`(임시값 1, 주먹 `_fingerCurls` 각도 배율), `GrabPoint` 속성(비면 손 위치).
  - `SetGrabPose(direction)`: 집게(손가락)를 그 수평 방향으로 돌린다. `Grip()`: 집게를 오므린다. 둘 다 `StartRecall`·`OnDisable`에서 풀리고, `LateUpdate`에서 `_moveSeconds` 동안 섞여 들어간다(박수·주먹과 같은 방식, 본 추가 없음).
- `BossAGrab.cs`
  - `StartFly`: `SetGrabPose(_direction)`. 손 위치→`GrabPoint` 수평 거리(`_grabReach`)만큼 덜 날아가 집게가 판 끝에 닿게 함.
  - `TryCapture`: 잡기 판정을 `GrabPoint` 기준으로, 잡으면 `Grip()`.
  - `DragPlayer`: 플레이어 발을 `GrabPoint`에 맞춤.
  - `OnFlyArrived`: 돌아올 목적지를 칸 중심에서 `_direction * _grabReach` 만큼 보스 쪽으로 당겨 집게가 칸 위에 오게 함.
  - 놓기(`OnReturnArrived`)는 기존과 같음(칸 중심 바닥 + 잡을 때 발 높이).
- `BossA.prefab`(Editor MCP, `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`): 양손 손바닥 본 밑에 `GrabPoint` 추가, 로컬 (-0.98, 0.0375, 0) = 손가락 방향 19.6m 앞(턱 사이 중앙), 몸 중심이 턱 높이에 오도록 0.75m 아래. `_grabPoint` 연결.
- 측정 근거: 메시 베이크로 턱 범위 손바닥 본 로컬 x -0.764~-1.193, y·z ±0.3(본 스케일 20). 날아가는 동안 손가락 방향은 수평 앞, `_palmAxis`(로컬 -Y)는 월드 위.
- 실행자: Claude 직접 수행. 예외 사유: 이 PC에 Codex CLI 미설치(도구 미지원).

## 2. 결정 사항 (사용자 확정, 2026-10-29)
- B안: 날아가는 동안 집게를 날아가는 방향으로 돌리고, 잡으면 오므려 쥐고 끌고 간다. 본·리깅 변경 없음.
- 플레이어 발(`PlayerBody` 위치)을 `GrabPoint`에 맞춘다(`GrabPoint` 자체를 몸 중심이 턱 사이에 오도록 배치).
- 놓을 때는 기존처럼 칸 중심 바닥으로 옮겨 놓는다.

## 3. 현재 상태 및 이슈
- `gate.cjs check recompile` 통과(에러 0, 경고 0). 프리팹 저장 확인.
- **Play 미확인**: 열린 `Map_4_Security` 씬에 저장되지 않은 변경이 있어 테스트 씬(`Assets/Test/Boss/Map_4_Security_Boss_Phase_T`)으로 바꾸지 않았다.
- 확인할 위험: ① 손 평소 위치에서 판 가장자리까지가 `_grabReach`(약 19.6m)보다 가까우면, 보스 쪽 판 가장자리의 플레이어는 집게 시작점보다 뒤라 잡히지 않음 ② 집게 방향이 섞이는 0.4초 동안 `GrabPoint`가 호를 그려 선 밖을 스칠 수 있음 ③ `_gripCurl` 1(±25°)이면 턱이 플레이어를 뚫고 겹칠 수 있음.
- 게이트 루프 미실행, 커밋 전.

## 4. 다음 할 일
- 보스전 Play에서 집게 방향·잡는 위치·오므림 육안 확인. 필요하면 `GrabPoint` 위치·`_gripCurl` 조정.
- 위 위험 ①이 보이면 비행 시작 위치나 판정 범위 조정 논의.
- 확인 후 intent-087 resolved, 게이트 루프 후 커밋.
