# 열린 레인 스폰 간격 랜덤화 (2026-10-15)

관련 intent: [intent-017 로비 군중](../../../intent/intent-017-lobby-crowd.md)

## 1. 작업 요약 및 방법
- 증상: 열린 레인이 모두 `_spawnInterval` 2.5초 고정 간격으로 시작점에서 개체를 꺼내서, 일정한 박자로 나와 기계적으로 보였다.
- 수정(`Assets/_Project/Scripts/AI/Crowd/CrowdLane.cs`만):
  - `_spawnInterval` 필드를 지우고 `_minSpawnInterval`(1.5) / `_maxSpawnInterval`(3.5)를 추가했다. 평균이 2.5초라 기존 밀도는 비슷하다.
  - `_nextSpawnInterval` private 필드를 추가했다. `Update()`가 이 값을 기준으로 꺼내고, 꺼낼 때마다 `Random.Range(min, max)`로 새로 뽑는다.
  - `Start()`의 `_spawnTimer = _spawnInterval;` 줄을 지웠다. `_nextSpawnInterval` 초기값이 0이라 첫 개체는 지금처럼 바로 나온다.
- 씬(`Map_1_Lobby`)은 손대지 않았다. 새 필드 이름이라 레인 4개 모두 코드 기본값 1.5 / 3.5를 받는다. 씬 YAML에 남은 옛 `_spawnInterval: 2.5` 키는 다음 씬 저장 때 자동으로 빠진다.
- 검증
  - 재컴파일 에러 0
  - Play: 레인 4개(Pedstrian_Lane_1/2, Car_Lane_1/2) 모두 min/max 1.5/3.5, 다음 간격이 1.52 / 1.94 / 2.21 / 2.97초로 서로 다르게 뽑힘
  - 콘솔 에러 0 (경고는 기존 LilRobot Legacy 클립 경고뿐)

## 2. 결정 사항
- 레인 안의 스폰 간격만 랜덤으로 한다. 레인 사이의 첫 스폰 시점은 어긋나게 하지 않는다(사용자 결정).
- 간격 범위는 최소/최대 두 필드로 설정한다(사용자 결정). min > max 검증 코드는 넣지 않았다.

## 3. 현재 상태 및 이슈
- 레인 4개의 첫 개체는 여전히 Play 직후 동시에 나온다(의도된 범위 밖).
- 앞 기록의 미해결 이슈(Car 프리팹 `_detectLayers`가 Player만 켜져 있음 등)는 그대로다.

## 4. 다음 할 일 (Next Steps)
- 사용자가 화면에서 나오는 간격이 괜찮은지 확인하고, 필요하면 레인별 Min/Max Spawn Interval을 조정한다.
