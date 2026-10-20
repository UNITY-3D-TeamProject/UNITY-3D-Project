# 보스 B 예고 색 분리 + 돌진 예고 + 도약 원 페이드 인 (2026-10-20)

관련 intent: [intent-049](../../../intent/intent-049-bossb-telegraph-color.md)

## 1. 작업 요약 및 방법
- **문제**: 보스 A·B 예고가 모두 `M_BossTelegraph`(빨강)라 헷갈림. B 돌진은 바닥 예고가 없고, B 도약 원은 켜지자마자 가득 참.
- **새 에셋(에디터 eval 로 생성, YAML 직접 수정 없음)**
  - `Art/Materials/Boss/M_BossBTelegraph.mat`: `M_BossTelegraph` 복사 후 `_BaseColor`/`_Color` = (0.15, 0.6, 1.0, 0.4) 파랑.
  - `Prefabs/AI/Boss/BossBCircleTelegraph.prefab`, `BossBLineTelegraph.prefab`: A 의 원·직선 예고 프리팹 복사 후 머터리얼만 B 용으로 교체.
  - `BossB.prefab`: Leap(일반) `_circlePrefab` → `BossBCircleTelegraph`, Charge `_linePrefab` → `BossBLineTelegraph`(새 필드). Evade 는 원래대로 비어 있음.
- **코드**
  - 새 `BossB/BossBTelegraph.cs`(순수 C#): 예고 판 생성(`Show`)·배치(`Place`, 로컬 X 폭·Z 길이)·페이드(`SetFade`, MaterialPropertyBlock 의 `_BaseColor` 알파 = 머터리얼 알파 × 진행률)·제거(`Hide`).
  - `BossBLeap`: 원을 `BossBTelegraph` 로 바꾸고, 예고 중 `1 - 남은시간/예고시간` 으로 페이드 인. 체공 중에는 가득 찬 채 남았다가 착지 때 사라짐(기존과 같음).
  - `BossBCharge`: 예고 시작 때 B 발밑에서 돌진이 멈출 지점(몸 앞 끝)까지 직선 예고, 폭 = `_hitRadius × 2`. 예고 중 매 프레임 길이를 다시 재고(0.5 m 간격으로 앞 칸이 구멍·예고·판 밖인지 확인, 최대 `_maxDistance`) 페이드 인. 급정거 순간·스킬 종료(`OnRunEnded`) 때 사라짐.
- **실행자**: C# 은 Claude 직접(작은 수정이라 위임 비용 대비 이득 없음), 에셋 생성·연결도 Claude 가 MCP eval 로 직접 수행(Codex 위임 생략 — 같은 이유).

## 2. 결정 사항
- 보스 A 쪽 머터리얼·프리팹·코드(`BossArena`·`BossTile`·`BossShoulderCannon`·`BossAGrab`)는 건드리지 않음.
- B 예고는 B 스킬이 직접 만들어 관리(아레나 풀 미사용). 페이드는 알파만(보스 A 칸 예고처럼 스케일은 키우지 않음).
- 돌진 직선도 도약 원과 같은 페이드 인 방식으로 통일(사용자는 도약만 명시, 일관성 위해 돌진에도 적용).

## 3. 현재 상태 및 이슈
- `gate.cjs check recompile` 통과(에러 0), `BossBTelegraph.cs.meta` 생성 확인.
- Play(테스트 씬 `Map_4_Security_Boss_Phase_T`)에서 reflection 으로 배치·페이드 수치 확인: 직선 예고가 가운데 칸 구멍 앞(270.25)에서 멈춤·폭 6·회전 정면·알파 0/0.2/0.4, 도약 원 칸 중심·지름 12·알파 0→0.2(절반), 둘 다 `M_BossBTelegraph`.
- **BT 가 실제로 돌진을 고르는 흐름에서의 확인은 못 함**: 이번 Play 에서 보스 B 센서가 플레이어를 보지 못해(`HasVisibleTarget` false, 플레이어를 판 안으로 옮겨도 같음) 스킬을 정상 경로로 발동시키지 못했다. 원인 미조사.
- 육안 확인(색 구분·페이드 느낌)·게이트 루프 미실행, **커밋 전**.

## 4. 다음 할 일 (Next Steps)
- 실제 전투에서 B 돌진·도약 예고를 육안 확인하고 색(파랑) 조정 여부 결정.
- 보스 B 센서가 플레이어를 못 보는 원인 확인(테스트 씬 설정 문제인지).
- 게이트 루프 통과 후 커밋.
