# 차량 레인 끝 Release 안 되는 문제 수정 (2026-10-15)

관련 intent: [intent-017 로비 군중](../../../intent/intent-017-lobby-crowd.md)

## 1. 작업 요약 및 방법
- 증상: `Map_1_Lobby`의 열린 차량 레인(Car_Lane_1~3)에서 차가 마지막 점(문 앞)까지 간 것처럼 보이는데, 풀로 반납되지 않고 그 자리에 계속 서 있었다.
- 원인: `CrowdController.HasReachedLaneEnd()`는 남은 거리가 `_lookAheadDistance`(Car = 1m) 이하일 때만 반납을 알린다. 그런데 세 레인의 끝 2.5~4m 구간이 건물 콜라이더(`SM_Bld_Security_WatchStation_01 (6)`, `SM_Bld_Section_Door_06 (11)/(12)`, `SM_Bld_Section_Wall_01/03`)와 겹쳤다. 그래서 반지름 1.4m CharacterController가 벽에 막혀 `_progress`가 더 늘지 않았다.
  - 차 캡슐(중심 y1.7, r1.4, Player/Crowd/Vehiclebody 제외)로 끝에서부터 겹침을 검사했다. 처음 겹치는 남은 거리: Lane_1 3.5m, Lane_2 2.5m, Lane_3 4.0m
- 수정(코드 변경 없음, 씬만): 레인마다 "처음 겹치는 지점 + 1.5m" 위치로 끝점을 당겼다. Unity MCP eval로 Undo 기록 후 씬을 저장했다.
  - Car_Lane_1: 마지막 knot 삭제, 그 앞 knot를 (-32.37, 0, 84.12)로 이동. 길이 104.39 → 99.38. 원래 끝 knot가 y=0.65로 문 위에 떠 있던 것도 함께 해소됐다.
  - Car_Lane_2: 마지막 knot 삭제, 그 앞 knot를 (21.13, 0, 75.49)로 이동. 길이 76.71 → 72.70
  - Car_Lane_3: 마지막 knot 삭제, (-32.73, 0, 84.51)에 knot 추가. 길이 219.97 → 214.47
  - 모든 knot는 AutoSmooth
- 검증
  - 에디트 모드: 세 레인 모두 남은 거리 8m → 0m 구간에서 캡슐 겹침 0
  - Play 37초: 끝 근처에 멈춘 차 없음(최소 남은 거리 11m). 같은 개체가 다른 레인에서 다시 쓰이는 것으로 Release → Get 순환 확인. 콘솔 에러 0(경고는 기존 LilRobot Legacy 클립 경고뿐)

## 2. 결정 사항
- 반납 거리를 코드로 따로 두는 방식(반납 거리 필드 추가) 대신, 씬에서 레인 끝점을 건물 밖 도로로 당기는 방식을 택했다(사용자 결정).
- 규칙: 열린 레인의 끝점은 건물·벽 콜라이더와 겹치지 않는 곳에 둔다. intent-017 제약에 기록했다.

## 3. 현재 상태 및 이슈
- Play 37초 시점 활성 수: Lane_1 6/10, Lane_2 4/10, Lane_3 10/10. 짧은 레인은 차가 끝에 닿는 속도가 재생성 간격(`_spawnInterval` 2.5초)보다 빨라서 활성 수가 줄어든 것으로 보인다. 이번 버그와는 별개라 그대로 두었다.
- Car 프리팹 `CrowdController._detectLayers`가 `m_Bits: 64`(Player)만 켜져 있다. 툴팁 권장값은 Player·Crowd·VehicleBody라서, 차끼리 서로 감지하지 못해 겹칠 수 있다. 수정하지 않았다.
- 레인 시작점들도 건물(문)과 겹친다(차가 문에서 나오는 연출). 이번에는 손대지 않았다.

## 4. 다음 할 일 (Next Steps)
- 사용자가 화면에서 차가 문 앞 4~5m 지점에서 사라지는 모습이 괜찮은지 확인한다.
- 필요하면 Car 프리팹 `_detectLayers`를 Crowd·VehicleBody까지 켤지 결정한다.
- 짧은 레인의 활성 수가 줄어드는 것이 문제라면 레인별 `_spawnInterval`을 조정한다.
