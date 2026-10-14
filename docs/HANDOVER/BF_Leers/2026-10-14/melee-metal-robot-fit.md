# Melee(Metal Robot) CC·근접 판정 맞추기 — 2026-10-14

## 1. 작업 요약 및 방법
- 테스트 씬 `Assets/Test/security/Map_4_Security_T.unity`의 `Melee` 인스턴스는 모델이 SciFi_Dog에서 Metal Robot으로 바뀌어 있었다. 프리팹에는 아직 적용하지 않은 상태다.
- 이 인스턴스의 CharacterController와 근접 판정 위치(`HitPoint`)를 로봇 크기에 맞췄다. Unity MCP eval로 프리팹 인스턴스 오버라이드를 기록하고 씬을 저장했다.

| 항목 | 이전(개 모델 기준) | 변경(로컬, 루트 스케일 2) | 월드 기준 |
|---|---|---|---|
| CC center | (0, 0.5, 0.1) | (0, 0.55, 0) | 높이 1.1 |
| CC radius | 0.4 | 0.3 | 0.6m |
| CC height | 1 | 1.1 | 2.2m (메시 높이 2.23m) |
| HitPoint | (0, 0.5, 1) | (0, 0.6, 0.6) | 앞 1.2m, 높이 1.2m |

- 근접 판정 반지름은 1m(`MeleeHit` 자식의 `MeleeHitController._radius`)로 그대로 두었다. 판정은 앞 0.2~2.2m를 덮고, BT `AttackRange` 2m와 맞는다.

## 2. 결정 사항
- **씬 인스턴스만 수정한다**(사용자 결정). `Melee.prefab`과 코드는 바뀌지 않았다.
- `Sensor._eye`, `AIController._body` 참조는 개 모델과 함께 사라져 비어 있다. 사용자 결정에 따라 그대로 두었다.
  - `_eye`가 비면 루트(발밑)에서 시야 레이를 쏜다.
  - `_body`가 비면 루트를 쓴다.

## 3. 현재 상태 및 이슈
- 검증 결과
  - 베이크한 메시 정점 77.6%가 캡슐 안에 있다. 바깥으로 나온 거리는 최대 0.16m이고, 대부분 팔 끝이다. 팔 끝에 맞은 총알은 CC에 걸리지 않을 수 있다.
  - Play 판정 범위: 중심 거리 0.9 / 1.5 / 2.0m에서는 맞고, 2.5m에서는 빗나간다.
  - AI가 실제로 공격해 `MeleeHitController.ApplyHitEffect` 경로로 `Player(Clone) is hit!`가 나왔다. 에러는 0건이다.
- Play를 종료한 뒤 씬이 다시 dirty 상태가 됐다. 변경 값은 Play 전에 이미 저장했다.
- `AllySeparationPusher._pushRadius` 0.9는 이전 CC(월드 r 0.8) 기준이다.
- Metal Robot 에셋은 `Assets/ThirdParty/`에 있다. 나중에 프리팹에 적용하면 팀원도 같은 에셋이 있어야 한다.

## 4. 다음 할 일 (Next Steps)
- 로봇 모델을 확정하면 `Melee.prefab`에 Apply한다. 모델 교체, WeakPoint 후보, CC, HitPoint 오버라이드를 함께 적용한다.
- 그때 `Sensor._eye`를 로봇 머리 뼈로 다시 연결할지 정한다.
- 필요하면 `AllySeparationPusher` 반경을 새 CC 크기에 맞게 조정한다.
