---
id: intent-084
title: 보스 B 돌진에 준비·돌진·종료 3단계 이펙트(VFX_BossBCharge)를 연결
part: AI / Boss / VFX
status: open   # open | resolved
created: 2026-10-28
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- 보스 B 돌진(`BossBCharge`)의 이펙트는 돌진이 시작될 때 `_chargeVfx` 하나(`BossBChargeWind`)만 루프로 재생한다. 준비 구간(예고)과 돌진이 끝나는 순간에는 이펙트가 없다.
- `Assets/ThirdParty/VFX/BossBCharge/VFX_BossBCharge.prefab` 이 준비(`Windup`)·돌진(`Run`)·종료(`Burst`) 3그룹으로 이미 조립돼 있지만, 프리팹 하나를 `EffectManager` 로 재생하면 세 그룹이 한꺼번에 나온다. `ThirdParty` 는 Git 에 올라가지 않아 팀과 공유도 안 된다.

## 기대 결과 (Proposed outcome)
- 돌진 예고 동안 `Windup`(점점 커지는 Shell·모이는 파편·번개·가장자리 픽셀)이 나오고, 예고가 끝나 돌진이 시작되면 사라진다.
- 돌진하는 동안 `Run`(속도선·번개·파편·먼지·가장자리 픽셀)이 B 를 따라다닌다.
- 돌진이 끝나는 순간(급정거·플레이어 적중·최대 거리 도달 세 경우 모두) 끝난 자리에서 `Burst`(별·플래시·파편·번개·먼지)가 한 번 터진다.
- 필요한 메시·머티리얼·텍스처·프리팹이 `Assets/_Project/` 안에 있어 Git 으로 공유된다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스 B 전투를 보는 플레이어.
- 어떤 시스템/모듈이 건드려지는가:
  - `AI.Boss.BossB.BossBCharge` — 준비·종료 이펙트 슬롯(`SOEffectData`) 2개 추가, 재생 시점 연결
  - `BossB.prefab` — 새 `SOEffectData` 3개 연결(기존 `_chargeVfx` 는 `Run` 으로 교체)
  - 신규 에셋: `Art/VFX/BossBCharge/`(메시·머티리얼·텍스처 복사), `Prefabs/Effect/AI/BossBCharge{Windup,Run,Burst}.prefab`, `ScriptableObjects/Effect/AI/SOEffectData_BossBCharge{Windup,Run,Burst}.asset`
  - 기존 `BossBChargeWind.prefab`·`SOEffectData_Charge.asset` 은 삭제하지 않고 남긴다.

## 제약 (Constraints)
- 예산/기한: 없음.
- 지켜야 할 정책: `ThirdParty` 원본은 수정하지 않고 `_Project` 로 복사해서 쓴다. 프리팹·에셋은 YAML 직접 편집 대신 Unity Editor MCP 로 만든다. 이펙트 재생은 기존 `EffectManager`(`Play`/`PlayLoop`/`Stop`) 만 쓴다.
- 쓰지 말아야 할 것: 돌진 판정·속도·예고선 등 기존 동작 변경. 새 매니저·추상화.

## 열린 질문 (Open questions)
- (사용자 확인 대기, 기본값으로 진행) 종료 파편은 급정거·적중·최대 거리 세 경우 모두 터뜨린다. 일부만 원하면 알려 달라.

## 진행 기록
- 2026-10-28: Codex CLI 미설치(`where codex` 실패)로 Claude 가 직접 실행.
- 2026-10-28: 구현 완료. 메시·머티리얼·텍스처를 `Art/VFX/BossBCharge/` 로 복사하고 `VFX_BossBCharge` 를 `BossBChargeWindup/Run/Burst` 3프리팹으로 분리(`ThirdParty` 의존 0). `BossBCharge` 에 `_windupVfx`·`_burstVfx` 추가, `BossB.prefab` 에 SO 3개 연결(`_chargeVfxOffset` 은 프리팹 안에 높이가 있어 0 으로). 리컴파일 통과. Play(`Map_4_Security`)에서 `EffectManager` 로 3종을 직접 재생해 렌더링 확인. **실제 보스 B 전투에서 돌진 시 연결·크기·타이밍은 미확인**(남음).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
