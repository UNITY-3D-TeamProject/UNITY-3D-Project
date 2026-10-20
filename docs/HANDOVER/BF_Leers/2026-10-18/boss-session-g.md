# 보스전 세션 G — A 1페이즈 패턴 스킬 UNITY-8 (2026-10-18)

- 스펙: `spec/boss.md`(로컬 전용) · 이전 기록: [보스 세션 F](./boss-session-f.md)
- 브랜치: `feature/AI/Boss`

## 1. 작업 요약 및 방법

- `AI/Boss/BossA/`에 5개 추가: `BossAPatternBase`(SkillBase 상속 추상, `Update` 상태 머신 Telegraph→Moving→Slamming→Staying→Recalling), `BossASlam`(A1·A1Double), `BossALineSlam`(A2), `BossAGrab`(A4, `[DefaultExecutionOrder(-50)]`), `BossAGroggyPalm`. `.meta` 포함.
- 검증: `unity` Assets/Refresh 후 `gate.cjs check recompile` → 에러 0, 경고 0.

## 2. 결정 사항

- 피해·붕괴 대상은 `CurrentChoice.Tiles` 전체(손 없는 칸 포함). 예고가 보이는 칸은 모두 위험하다는 기준.
- A4는 예고 시작과 함께 손이 떠오르고 내려치는 순간 판정. 반경·넉백 거리·높이는 임시값.
- `_isDouble`은 지금 쓸 곳이 없어 뺐다. 세션 H에서 포격 연결 시 추가.

## 3. 현재 상태 및 이슈

- Play 미검증(프리팹·스킬 컴포넌트 배치 없음), Codex 리뷰 게이트 루프 미실행, 커밋 전.
- A4 넉백 방향·거리, 그로기 손바닥 위치는 Play에서 조정 필요.
- intent-044 문서 부재, `ShaderGraphSettings.asset` 줄바꿈 변경은 내 작업 아님(이전부터).

## 4. 다음 할 일 (Next Steps)

- 세션 H: UNITY-9(`BossAChessSlam`·`BossAClap`·포격 연결, `BossASlam`에 `_isDouble` 추가).
