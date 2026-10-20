# 보스 A 칸 예고 중앙 채움 연출

관련 intent: [intent-047](../../../intent/intent-047-bossa-telegraph-fill-in.md)

## 1. 작업 요약 및 방법
- 칸 예고 쿼드(`BossTile._telegraph`)가 켜질 때 중앙 기준 스케일 0→1, 알파 0→원래 값(`MaterialPropertyBlock`의 `_BaseColor`)으로 채워지게 했다.
- 채움 시간은 패턴이 넘긴다: `BossAPatternBase.Execute()`가 `TelegraphSeconds × TelegraphScale`을 `ShowTelegraph(float)`→`IBossArena.SetTelegraph(index, isOn, fillSeconds)`→`BossTile.SetTelegraph(isOn, fillSeconds)`로 전달.
- 수정 파일: `BossTile.cs`, `BossArena.cs`, `IBossArena.cs`, `BossAPatternBase.cs`. 씬·프리팹 변경 없음.
- 실행자: Claude 직접 수행. 예외 이유: 이 세션에 Codex CLI가 없음(`which codex` 실패).

## 2. 결정 사항
- 정사각형 스케일 + 알파 방식. 원형·부드러운 가장자리용 셰이더는 이번 범위 밖.
- 채움은 칸 상태가 실제로 Telegraphed로 바뀔 때만 시작한다(`BossArena._telegraphFillSeconds`를 이벤트 핸들러가 읽음). `BossAClap` 두 번째 내려침은 예고가 이미 켜져 있어 다시 채우지 않는다.
- `BossTile`은 `BossArena.Awake`가 `BossTile.Awake`보다 먼저 돌 수 있어 Awake 대신 첫 사용 때 쿼드 스케일·색을 캐시한다.

## 3. 현재 상태 및 이슈
- `gate.cjs check recompile` 통과. Play·게이트 루프 미실행, **커밋 전**.
- 육안 확인 필요(`waiting_user`): 채워지는 모양, 가득 찬 순간과 손 내려침 타이밍, 최종 단계(0.8배)에서도 맞는지.
- 주석 처리된 보스 테스트의 가짜 아레나는 다시 켤 때 `SetTelegraph` 시그니처를 맞춰야 한다.

## 4. 다음 할 일 (Next Steps)
- Play에서 A 패턴(LineSlam·ChessSlam·Clap·Grab)의 채움 연출 육안 확인.
- 정사각형이 어색하면 원형 셰이더 intent를 따로 작성.
- 게이트 루프 통과 후 커밋, intent-047을 resolved로 옮기기.
