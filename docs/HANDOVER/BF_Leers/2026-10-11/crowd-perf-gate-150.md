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
- USER-5는 150개 측정으로 완료 처리했다(사용자 결정). 성능 대응은 **보류**한다. 해결법 2가지는 아래에 정리했다.
- `CharacterMotor`는 수정하지 않았다.

## 보류한 해결법 2가지
1. **`CharacterMotor`의 `SyncTransforms`를 조건부로 바꾸기** (153개 기준 약 -1.9ms 예상)
   - 그냥 삭제하면 안 된다. Update로 옮긴 지금도 같은 프레임에 "순간이동 → Move"가 되면 되돌아간다. Play 중 임시 CharacterController로 재현했다: 동기화 없이 z=20 순간이동 → Move → z=0 복귀, 동기화하면 z=20 유지, 몇 프레임 뒤 Move하면 정상. 즉 물리 스텝이 사이에 끼는지에 따라 가끔만 터지는 버그다.
   - 조건: `(_controller.transform.position != _lastMovedPosition) || (displacementToApply != Vector3.zero)`일 때만 `Physics.SyncTransforms()`를 부르고, `Move` 뒤 `_lastMovedPosition`을 갱신한다. 순간이동(StageManager, Monster/NpcPool, CrowdLane 재생성)과 발판 탑승/발판 점프(`PlatformRider`의 외부 변위)를 지금과 같이 처리한다.
   - 다른 파일 수정은 필요 없다. 차량 `Body` 자식 콜라이더와 카메라 레이는 최대 20ms 늦게 반영되지만 무시할 수준이다.
   - 공용 컴포넌트라 별도 커밋/PR과 팀 공지(콜라이더를 transform으로 옮기고 같은 프레임에 물리 질의하는 코드가 있으면 알려 달라)를 권장한다. `CharacterMotor` 관련 PlayMode 테스트는 저장소에 없으므로 회귀는 Play로 확인해야 한다(순간이동 4곳, 오르내리는 발판, 군중 150개 재측정).
2. **군중 전용 경량 이동**: `CharacterController` 없이 스플라인 위치를 직접 쓴다. 감지는 그리드 공간분할 대신 같은 레인의 바로 앞 개체와의 진행 거리 비교, 플레이어는 거리 비교로 충분하다(레인끼리 교차하지 않으므로). 중재자 구조와 맞는지 검토가 필요하다.
- 1번만으로는 약 5.7ms라 2ms 관문을 통과하지 못한다. 관문을 통과하려면 2번까지 필요할 가능성이 크다.

## 현재 상태 및 이슈
- 에디터 측정이라 빌드에서는 줄어들 수 있다. 하지만 `SyncTransforms`/`CharacterController.Move`는 네이티브 비용이라 빌드에서도 2ms 안으로 들어올 가능성은 낮다. PC 빌드 측정은 미실시.
- HANDOFF_POINTER의 2026-10-11 항목 2개(`lobby-pedestrian-lane.md`, `lobby-vehicle-lane.md`)가 가리키는 파일이 이 폴더에 없다(링크 깨짐).

## 다음 할 일
- (보류 중) 성능 대응을 재개하면 위 해결법 1 → 2 순서로 진행한다. intent-017 원안(감지 주기 분산 → 거리 기반 비활성화)은 감지 비용이 0.49ms뿐이라 효과가 작다.
- `[USER]` LilRobot 클립 Legacy 설정 또는 `Animation` 컴포넌트 제거(풀 재활성화 경고 스파이크).
- 위 대응 후 같은 방식으로 150 → 300개 재측정.
