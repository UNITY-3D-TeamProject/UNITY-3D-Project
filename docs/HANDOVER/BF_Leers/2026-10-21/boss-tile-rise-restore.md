# 보스 아레나 칸 복구 시 솟아오르기 연출 (intent-054)

## 1. 작업 요약 및 방법
- 사용자 요청: 보스 A 패턴으로 무너진 칸이 다시 생길 때 아래에서 위로 덜컥 올라오는 느낌.
- `BossTile.cs`: 무너졌던 칸이 `SetCollapsed(false)`로 복구될 때만 칸 루트를 `_riseDepth`(4 m) 아래에서 `_riseSeconds`(0.35초) 동안 EaseOutBack(`_riseOvershoot` 2, 최고 약 +0.53 m 넘침)으로 제자리까지 올린다. 월드 좌표 기준(부모 스케일 영향 없음). 자식(`Frame`·`SafeSignals`·`WarningSignals`)도 함께 움직인다.
  - 솟는 동안 NavMesh 구멍(`_holeObstacle`)을 유지하고 다 오르면 닫는다. 솟는 중 다시 무너지면 제자리로 되돌리고 구멍 유지.
  - 처음 시작(`BossArena.Awake`의 `SetCollapsed(false)`)은 무너진 적이 없어 연출하지 않는다.
- `Map_4_Security.unity`: 타일 25개 루트가 Static 전체(-1)라 정적 배칭되면 런타임에 메시가 움직이지 않으므로, **Batching Static만** 해제(eval, 프리팹 오버라이드로 저장). 저장 전 씬은 미저장 변경 없음(dirty=False)이었다.
- 실행자 예외: 코드 수정·씬 플래그 25개 변경 모두 Claude가 직접 했다(작은 범위, 같은 세션에서 바로 확인).

## 2. 결정 사항
- 연출 값은 `BossTile` 인스펙터 필드(`_riseDepth`·`_riseSeconds`·`_riseOvershoot`)로 조절. 0 이면 연출 없이 즉시 나타남.
- 칸 상태(`ArenaGrid`)는 복구 즉시 "복구됨"이고, 겉모습·NavMesh 구멍만 0.35초 늦게 맞춰진다. 콜라이더는 복구 즉시 켜져 함께 올라온다.

## 3. 현재 상태 및 이슈
- `gate.cjs check recompile` 통과.
- Play 확인(가운데 칸 R03_C03): 복구 직후 y=-4, 구멍 켜짐 → 0.195초 y=+0.53 → 0.35초 y=0, 구멍 꺼짐. 솟는 중 재붕괴 시 y=0·구멍 유지. 에디터가 포커스 없이 프레임이 멈춰 중간 구간은 리플렉션으로 경과 시간을 지정해 `UpdateRise`를 호출해 확인했다(시작·끝은 실제 프레임 진행으로 확인).
- **육안 확인(손맛)·게이트 루프 미실행, 커밋 전.**
- 테스트 씬(`Assets/Test/Boss`) 타일의 정적 플래그는 확인하지 않았다(Git 밖). 그 씬 타일도 Batching Static이면 움직이지 않는다.
- 솟는 칸이 그 위(구멍 속)로 떨어지던 플레이어를 받아 올릴 수 있다(콜라이더 즉시 켜짐). 의도와 다르면 조정 필요.

## 4. 다음 할 일 (Next Steps)
- Play에서 실제 보스 A 패턴 → 복구 장면을 눈으로 보고 깊이·시간·넘침 값 조정(필요하면 착지 순간 흔들림·소리 추가 여부 결정).
- 테스트 씬 타일 Batching Static 확인.
