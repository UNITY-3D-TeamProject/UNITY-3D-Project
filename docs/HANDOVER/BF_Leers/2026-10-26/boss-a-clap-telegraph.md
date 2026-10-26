# 보스 A 박수(A6) 예고 구분 — 2026-10-26

관련 intent: [intent-076](../../../intent/intent-076-boss-a-clap-telegraph.md)

## 1. 작업 요약 및 방법
- 문제: A6 박수 1타 예고가 줄 양 끝 칸 2개 + 기본색이라 A1Double 과 같아 보이고, 손은 예고가 끝나야 떠올라 박수만의 신호(주먹·상자 예고)가 늦게 나왔다.
- `BossAClap.cs`
  - `IsHoverDuringTelegraph => true`: 예고 시작과 함께 두 손이 주먹(`IsFistStrike`)을 쥐고 양 끝 위로 떠오른다.
  - `BuildTargets` 를 재정의해 사이 칸 계산과 `ShowWall` 을 예고 시작 시점으로 옮겼다. 상자는 박수 순간(`EndClap`)이나 `Stop` 에서 꺼진다(기존 그대로).
  - 줄 가운데·방향·칸 간격 계산을 `GetRowLayout` 으로 묶어 `BuildTargets`·`StartFollowUp` 이 함께 쓴다.
  - 클래스 요약·`_wallPrefab` 툴팁 문구를 바뀐 동작에 맞춰 수정했다.
- 상자 예고를 경고 줄무늬로 교체(사용자 선택).
  - 신규 셰이더 `Art/Shaders/Boss/BossWarningStripe.shader`(`Project/BossWarningStripe`): URP 반투명 Unlit, 월드 좌표 x+y+z 로 사선을 그어 상자 비율이 바뀌어도 줄 폭이 같고 `_ScrollSpeed` 로 흐른다. 앞면만 그린다(양면이면 뒷면 줄과 겹쳐 격자로 보임). 속성 `_StripeColor`(주황 0.6)·`_GapColor`(검정 0.35)·`_StripeWidth` 0.6 m·`_StripeRatio` 0.5·`_ScrollSpeed` 1.
  - 신규 머티리얼 `Art/Materials/Boss/M_BossClapWarning.mat` 을 `BossLineTelegraph.prefab` 렌더러에 연결(eval `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`).
  - 처음에 주황으로 바꿨던 `M_BossTelegraph.mat` 은 HEAD 내용으로 되돌렸다(`git checkout` 이 index.lock 때문에 실패해 `git show HEAD:… >` 로 복원, diff 없음 확인).
- 실행자: Claude 직접. 예외 이유: 코드 몇 줄 + 머티리얼 색 하나라 Codex 위임 비용이 더 크다.

## 2. 결정 사항
- 2번 안(바닥 예고 전용 색)은 쓰지 않고, 박수에만 있는 상자 예고의 머티리얼 색으로 구분한다(사용자 결정).
- 상자 예고는 전용 머티리얼 `M_BossClapWarning` 을 쓴다. `M_BossTelegraph` 는 미사용 `BossCircleTelegraph` 와 `BossArenaSetupWindow` 기본 타일 예고 머티리얼로만 남아 있어 원래 색 그대로 둔다.

## 3. 현재 상태 및 이슈
- 리컴파일 통과(에러 0), 셰이더 메시지 0. 숨김 임시 오브젝트로 줄무늬 상자 미리보기 렌더 확인(씬 변경 없음). Play 확인·육안 확인 미실행 → `waiting_user`.
- (후속, 사용자 지적) 앞면만 그리면 플레이어·카메라가 박수 범위(상자) 안에 있을 때 예고가 안 보였다. 셰이더를 2패스로 바꿈: `Outside`(Cull Back, `UniversalForward`)는 그대로, `Inside`(Cull Front, `SRPDefaultUnlit`)는 카메라가 상자 안(오브젝트 공간 -0.5~0.5, 기본 Cube 기준)일 때만 안쪽 면을 `_InsideOpacity`(0.5) 만큼 옅게 그리고, 바닥과 겹쳐 깜빡이는 바닥면은 버린다. 밖·안 미리보기 렌더로 격자 겹침 없음·안쪽 줄무늬·바닥 얼룩 없음 확인.
- 손이 예고 중에 미리 떠 있으므로 예고 채움이 끝나면 곧바로 내려친다. 예전에는 예고 → 손 이동 → 내려치기였으므로 1타가 손 이동 시간만큼 빨라졌다. 바닥 예고 채움은 그대로 내려치기 전에 끝난다.
- 커밋 전. 작업 트리에 있던 다른 변경(BossA/NPC 프리팹, ProjectSettings 등)은 이번 작업과 무관하다.

## 4. 다음 할 일 (Next Steps)
- Play 에서 최종 단계 A6 를 보고 확인: 예고 시작부터 주먹 자세로 떠오르는지, 주황 상자가 1타 예고부터 박수 순간까지 보이는지, 1타 타이밍이 너무 빨라지지 않았는지.
- 1타가 빠르게 느껴지면 `BossAClap._telegraphSeconds`(프리팹 1.5)를 늘린다.
- 확인 후 intent-076 을 resolved 로 바꾸고 `clear/` 로 옮긴다.
