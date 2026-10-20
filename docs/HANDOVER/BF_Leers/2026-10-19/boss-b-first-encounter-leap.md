# 보스 B 첫 조우 도약 + 센서 시야

## 작업 요약 및 방법
- 문제: `SmallBoss` BT 에 수색 노드가 없고 `BossB.prefab` 센서가 시야 5m·90도라, 스폰 직후 플레이어를 못 보면 `BossBChooseAttackAction` 이 아무것도 고르지 않았다.
- `BossB.prefab` `Sensor`: `_sightRange` 50(이미 반영돼 있었음), `_sightAngle` 90 → 360 (Unity MCP `set_serialized_field` 후 저장 확인). 에디터 작업을 Codex 티켓 없이 직접 수행한 예외 이유: 사용자 명시 요청, 단일 필드 변경.
- `BossBChooseAttackAction`: `_hasEncountered` 필드 추가. 대상·아레나 가드를 통과해 처음 공격을 고를 때만 `EBossBAttack.Leap` 을 쓰고, 이후는 기존 `BossBAttackRule.Choose`.
- intent: `docs/intent/intent-044-bossb-first-encounter-leap.md`, 목차 갱신.

## 결정 사항
- 첫 조우 1회만 도약. 재발견 재조우는 하지 않는다.
- 규칙(`BossBAttackRule`)·BT 그래프는 수정하지 않았다. 1-1 도약 차단은 기존 `isLeapOpen` 이 그대로 담당한다.
- 도약 쿨타임(8초)은 BT 그래프 Leap 가지의 `AISetReadyTime` 이 기록하므로 첫 조우 도약도 같은 가지를 타 별도 처리 불필요.

## 현재 상태 및 이슈
- 컴파일 통과(`gate.cjs check recompile` exit 0). 게이트 루프는 미실행(이전 요청에서 사용자가 실행을 거부), Play 미검증, 커밋 전.
- Stop 훅이 세션 키 `4e34c2be…` 의 `verification-requirements.json`(automaticInputs 4개) 선언을 요구해 해당 파일을 훅 지시대로 작성했다.
- 도약 착지 칸은 대상이 아레나 밖이면 `FindNearestSafe` 로 보정된다. 첫 도약 예고·착지 연출은 육안 확인이 필요하다.

## 다음 할 일 (Next Steps)
1. 게이트 루프 실행(`gate.cjs loop <세션키>`) 후 커밋.
2. Play 에서 스폰 직후 발견 → 도약 → 이후 1-1 에서 도약 미사용 확인.
3. 보스 A 쪽 정지 원인(`SetStage` 초기 호출 누락 등)은 별도 세션에서 이어서 진단.
