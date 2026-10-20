# 보스 A A4 — 블리츠크랭크 Q 식 그랩 재설계 (2026-10-20)

## 작업 요약 및 방법
- 요청: A4 가 칸 위 내려찍기 + 판 밖으로 던지기였는데, 의도는 블리츠크랭크 Q 식 그랩. intent-048 에 방향·결정을 정리(`docs/intent/intent-048-bossa-a4-hook-grab.md`).
- 새 A4 흐름: 한 손이 직선 경로를 예고 → 곧장 날아감 → 경로에서 처음 닿은 플레이어를 잡음 → 보스 쪽(그랩 손이 떠난 쪽에서 가장 가까운 멀쩡한 칸 중심)으로 끌어당김 → 반대 손이 그 칸을 내려쳐 1칸 붕괴. 빗나가면 손만 회수.
- 코드(실행자: Claude. 예외 이유: 이 PC에 Codex CLI 없음):
  - `BossAGrab`: `BossAPatternBase` 상속을 버리고 `SkillBase` 직접 상속, 자체 Update 상태 머신(Aiming→Flying→Telegraph→Slamming→Staying→Recalling, 빗나가면 Missed). `[DefaultExecutionOrder(-50)]`·`BossKnockback` 로 끌어당김.
  - `BossHand`: `Flying` 상태와 `Fly(position, seconds, onArrived)` 추가(일정 속도, 날아가는 동안은 손가락이 펴진 상태).
  - `IBossArena`/`BossArena`: `ShowLine(from,to,width)`/`HideLine` + `_linePrefab`/`_linePoolSize` 풀.
  - `BossAPatternSelector`: 규칙 3(모서리 칸이면 A4)과 `IsCorner` 제거, 붕괴 제한 폴백에서 A4 제거(1페이즈는 Rest).
- 에셋(에디터 API, YAML 직접 수정 없음): 신규 `Prefabs/AI/Boss/BossLineTelegraph.prefab`(Cube 납작 판 + `M_BossTelegraph`), `BossA.prefab`의 A4 `_hitEffect` = A1 의 `SOAttributeEffect_BossA1`, `_hookRange` 100. 테스트 씬 `BossArena._linePrefab` 연결.

## 결정 사항
- 새 플레이어 기절 기능은 만들지 않는다(내려치기 예고 1.5초로 비킴 여지, 임시값).
- 비행 높이는 손의 `_hoverHeight`(현재 프리팹 50)와 분리해 `BossAGrab._hookHeight`(1.0)로 둔다.
- 수치 전부 임시값: 조준 1.0초, 속도 60 m/s, 사거리 100 m(보스가 판 밖 약 26 m라 판 끝까지 닿아야 함), 반경 2.5 m, 끌어당김 0.4초, 내려치기 예고 1.5초.

## 현재 상태 및 이슈
- 컴파일 통과(`gate.cjs check recompile`; 첫 시도는 네트워크 오류, 재시도 통과). 게이트 루프(Stop 훅) 미실행, **커밋 전**.
- Play(BT 에이전트 끄고 `TryStartPattern("A4")`, 플레이어를 (250,0.5,14)에 둠)에서 상태 전이 Aiming>Flying>Telegraph>Slamming>Staying>Recalling>Idle, 포획·끌어당김(플레이어 (288,-2) 근처로 이동)·예고·피해·1칸 붕괴 로그 확인. 패턴 종료 후 `IsPatternRunning` false.
- **미확인**: 육안(직선 예고 모양, 날아가는 손 모양·높이, 끌려오는 연출), 빗나감 경로의 실제 실행(상태 코드만 있고 미실행), 플레이어가 끌려오는 중 벽·구멍에 걸리는 경우, 전투 중 BT 에서의 A4 선택.
- 테스트 씬은 사용자 변경으로 이미 dirty 였다. `_linePrefab` 연결은 메모리 상태이고 **씬을 저장하지 않았다**(사용자가 저장해야 함).
- BT 에이전트가 Play 중 꺼도 다시 켜져 다른 패턴이 끼어드는 경우가 있었다(`BossEncounter`가 켬으로 추정, 확인 안 함).

## 다음 할 일 (Next Steps)
- Play 에서 A4 육안 확인 후 임시값 조정(예고 시간, `_hookHeight`, 반경).
- 빗나감 경로와 끌려오는 중 지형 충돌 확인.
- 확인이 끝나면 게이트 루프 → 커밋, intent-048 을 resolved 로 `docs/intent/clear/` 이동.
- Grip 클립(`Right Grip`/`Two Hands Grip`) 사용 여부는 별도 결정.
