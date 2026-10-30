# 보스-21 그로기 손 안내 이펙트 (intent-092)

## 작업 요약 및 방법
- "팔" = 큰 보스 A 의 손(그로기 때 `HandsOnly`)으로 확인받고, 표현은 손 위치 이펙트로 결정.
- `BossController` 에 `_handHintVfx` 추가. `SetDamageMode(HandsOnly)` 때 Hand 릴레이 transform 에 `PlayLoop`, 다른 모드·OnDisable 에서 `Stop`.
- 임시 프리팹 `BossHandHint`(노란 펄스 링 파티클)·SO 생성, `BossA.prefab` 연결.

## 결정 사항
- 모드 전환(`SetDamageMode`)에 묶어 그로기 시작/종료/최종 전환을 별도 처리 없이 따라가게 함.

## 현재 상태 및 이슈
- 리컴파일 통과. 실제 보스전에서 표시·소멸·가시성 미확인. 게이트 루프 미실행, 커밋 전.

## 다음 할 일
- 직접 플레이로 그로기 시 손 위 이펙트 확인, 필요하면 크기·색 조정 또는 아트 교체.
