---
id: intent-089
title: 약점 피격 피드백 없음 — 약점 전용 히트 이펙트·소리 덧입히기
part: AI / Enemy / Combat / Feedback
status: open   # open | resolved
created: 2026-10-29
resolved: null
---

## 문제 (Problem)
- AI_TODO2 잡몹-7: 약점을 맞혀도 일반 피격과 화면·소리가 똑같아 약점을 맞혔는지 알 수 없다.
- 약점 명중 시 `CharacterCombat.OnCriticalHit` → `CombatMediator` → `CharacterMediator.OnCriticalHit(hitPoint)` 까지 이벤트가 올라오지만 구독하는 곳이 없다.
- 일반 명중 피드백은 총알(`BulletController._hitVfx`·`_hitSfx`)과 적 `SoundController.PlayHit` 이 낸다.

## 기대 결과 (Proposed outcome)
- 약점에 맞으면 맞은 지점에서 약점 전용 히트 이펙트와 소리를 한 번 낸다.
- 일반 명중 이펙트·소리는 그대로 두고 그 위에 덧입힌다(사용자 결정).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어 사격, 약점이 있는 적(Melee·Ranged·Special·BossA·BossB)
- 어떤 시스템/모듈이 건드려지는가:
  - 신규 `AI/CriticalHitFeedback` — `CharacterMediator.OnCriticalHit` 구독, `EffectManager`·`AudioManager` 로 재생
  - 신규 `SOEffectData_WeakPointHit` (`YellowLaserImpact`)
  - 프리팹 5개에 컴포넌트 부착

## 제약 (Constraints)
- 총알·전투 판정 코드는 바꾸지 않는다(덧입히기 결정).
- 프리팹·에셋은 YAML 직접 수정 없이 Unity Editor(MCP)로 수정한다.

## 결정 사항
- 2026-10-29 (사용자): 덧입히기, 잡몹 + 보스 적용, 이펙트는 `YellowLaserImpact`, 소리는 비워 두고 사용자가 따로 찾아 넣음.
- 이펙트 방향은 맞은 지점에서 메인 카메라를 향하게 한다. 이벤트가 맞은 지점만 넘기므로 이벤트 시그니처를 바꾸지 않고, 총알이 쏜 쪽(-탄도)을 향하는 것과 비슷하게 보이게 한다.
- `YellowLaserImpact` 원본은 켜질 때 자체 AudioSource(`explosion_laser2`, playOnAwake)를 울리고, `SciFiLightFade` 가 0.2초 뒤 `Point light` 를 Destroy 해 풀 재사용 때 모양이 달라진다. 기존 관례(`Prefabs/Effect/AI/CFXR Hit C`)대로 `_Project/Prefabs/Effect/AI/WeakPointHit.prefab` 으로 복사하고 AudioSource·`SciFiPitchRandomizer`·`Point light` 를 뺐다(파티클 3개만 남김). 소리는 `_sound` 칸으로만 낸다.
- 검증(Play `Map_4_Security`, 스캔 전 꺼져 있는 약점 콜라이더를 임시로 켜고 `CharacterCombat.ModifyDamage` 호출): Melee 3·Ranged 1·Special 1 모두 약점 명중 시 피해 20·이펙트 +1, 빗나감은 피해 10·이펙트 0. 이펙트가 카메라를 향함(dot 1), AudioSource·Light 0. 보스는 이 씬에 없어 부착만 확인.

## 열린 질문 (Open questions)
- 약점 피격 소리 에셋 — 사용자가 찾아 `CriticalHitFeedback._sound` 에 연결.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
