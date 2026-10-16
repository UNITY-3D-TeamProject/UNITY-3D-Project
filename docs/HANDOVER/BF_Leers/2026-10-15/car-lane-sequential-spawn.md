# 열린 레인 차량을 시작점에서 차례대로 생성 (2026-10-15)

관련 intent: [intent-017 로비 군중](../../../intent/intent-017-lobby-crowd.md)

## 1. 작업 요약 및 방법
- 증상: Play를 누르면 `CrowdLane.Start()`가 레인 전체에 개체를 같은 간격으로 한꺼번에 깔아서, 차량이 도로 위에 한꺼번에 나타났다.
- 수정(`Assets/_Project/Scripts/AI/Crowd/CrowdLane.cs`만):
  - 열린 레인은 `Start()`에서 일괄 생성을 건너뛰고 `_spawnTimer = _spawnInterval`로 둔다. 첫 대는 첫 `Update`에서 바로 나오고, 그 뒤로는 기존 `Update()`의 재생성 로직(`_spawnInterval` 경과 + `IsSpawnPointClear` + `_activeCount < _spawnCount`)이 시작점에서 하나씩 꺼낸다.
  - 닫힌 레인은 다시 꺼내는 로직이 없으므로 기존처럼 전체에 깐다.
  - 클래스 요약 주석과 `_spawnInterval` 툴팁을 바뀐 동작에 맞게 고쳤다. 새 필드·메서드는 없다.
- 검증 (`Map_1_Lobby`, 로비 CrowdLane은 열린 레인 Car_Lane_1~3뿐)
  - 재컴파일 에러 0
  - Play t=0.02s: 활성 3대, 레인마다 시작점에서 0.5~2.9m 거리에 1대씩
  - Play t=14s: 활성 14대, 레인을 따라 간격을 두고 퍼져 있음
  - 콘솔 에러 0 (경고는 기존 LilRobot Legacy 클립 경고뿐)

## 2. 결정 사항
- 열린 레인은 시작점에서 차례대로 채운다. 처음 채울 때도 별도 간격 필드 없이 `_spawnInterval`을 쓴다(사용자 결정, intent-017 기대 결과에 기록).

## 3. 현재 상태 및 이슈
- 레인이 가득 차는 데 대략 `_count × _spawnInterval`초(현재 10 × 2.5 ≈ 25초) 이상 걸린다. 앞차가 시작점을 비워야 다음 차가 나오므로 더 길어질 수 있다.
- 앞 기록의 미해결 이슈(Car 프리팹 `_detectLayers`가 Player만 켜져 있음 등)는 그대로다.

## 4. 다음 할 일 (Next Steps)
- 사용자가 화면에서 차가 나오는 속도가 괜찮은지 확인하고, 필요하면 레인별 `_spawnInterval`을 조정한다.
