# 군중 성능 관문 측정 (USER-5 후반) — 150개 불합격

## 작업 요약 및 방법
- 코드·씬 변경 없음. `Map_1_Lobby_T` Play 중에만 MCP `eval`로 측정했다.
- 로비 레인 8개는 길이 ÷ 최소 간격 제한 때문에 최대 112개(활성 86~98개)라서, 150개는 Play 중 맵 아래(y=-200)에 임시 바닥 큐브와 닫힌 원형 레인(반지름 24m, 보행자 55개)을 런타임 생성해 채웠다. Stop 후 사라짐 확인, 씬 dirty 아님.
- 측정: `ProfilerDriver`(profileEditor 끔)를 켜고 `HierarchyFrameDataView`로 최근 300프레임의 `[Invoke]` 샘플 TotalTime 평균을 합산했다.
- 에디터 코드 최적화가 Debug라 스크립트가 부풀려지므로 측정 동안만 Release로 바꿨다가 Debug로 되돌렸다.

## 결과 (Release, 에디터 Play, 프레임당 평균)

| 개체 수 | CrowdController | CharacterMotor | CharacterRotator | CrowdLane | 합계 |
|---|---|---|---|---|---|
| 96 (로비만) | 1.16ms | 4.04ms | 0.18ms | 0.06ms | **5.45ms** |
| 153 (로비 + 링) | 1.72ms | 5.57ms | 0.26ms | 0.07ms | **7.61ms** |

- 참고: Debug 모드 86개는 합계 6.03ms였다.
- 관문(150개, 2ms 이하) **불합격**. 96개에서 이미 넘으므로 300개 측정은 하지 않았다(사용자 조건: "150개 괜찮으면 300개").
- 개체당 약 50µs, 거의 선형으로 증가한다.

## 원인 분석
- **`CharacterMotor.cs:152`가 `Move` 직전 매번 `Physics.SyncTransforms()`를 직접 호출한다**(주석: 순간이동 대응). 개체 수만큼 호출되어 153개에서 평균 1.9ms를 쓴다. 단일 항목으로 가장 크다.
- `CharacterMotor`의 나머지는 `CharacterController.Move` 자체 비용이다(전용 마커 없음).
- `CrowdController`의 `SphereCastNonAlloc`은 153개에서 0.49ms이고, 나머지는 스플라인 Evaluate 비용이다.
- `CrowdLane` 최대 6~9ms 스파이크: 풀에서 다시 꺼낼 때 `SetActive(true)` → LilRobot `Animation` 컴포넌트가 클립 8개마다 "must be marked as Legacy" 경고를 낸다. 에디터 로그 비용이 대부분이다. 프리팹/클립 설정 문제(`[USER]`).

## 결정 사항
- 없음(측정만). `CharacterMotor`는 공용 컴포넌트라 수정하지 않았다.

## 현재 상태 및 이슈
- 에디터 측정이라 빌드에서는 줄어들 수 있다. 하지만 `SyncTransforms`/`CharacterController.Move`는 네이티브 비용이라 빌드에서도 2ms 안으로 들어올 가능성은 낮다. PC 빌드 측정은 미실시.
- HANDOFF_POINTER의 2026-10-11 항목 2개(`lobby-pedestrian-lane.md`, `lobby-vehicle-lane.md`)가 가리키는 파일이 이 폴더에 없다(링크 깨짐).

## 다음 할 일
- 세션 C 성능 대응 후보(효과 큰 순):
  1. `CharacterMotor`의 매 이동 `SyncTransforms` 호출 제거/조건부화(순간이동 시에만). 공용 코드라 담당자 협의 필요.
  2. 군중은 `CharacterController` 대신 스플라인 위치를 직접 쓰는 가벼운 이동으로 바꾸기(지면 높이는 스플라인이 정함). 중재자 구조와의 정합성 검토 필요.
  3. intent-017 원안: 감지 주기 분산 → 거리 기반 비활성화.
- `[USER]` LilRobot 클립 Legacy 설정 또는 `Animation` 컴포넌트 제거(풀 재활성화 경고 스파이크).
- 위 대응 후 같은 방식으로 150 → 300개 재측정.
