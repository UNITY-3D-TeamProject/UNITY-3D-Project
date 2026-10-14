---
id: intent-023
title: 적 약점(무작위 위치) 거리 판정으로 치명타 구분
part: AI / Enemy / Combat
status: open   # open | resolved
created: 2026-10-14
resolved: null
---

## 문제 (Problem)
- 적을 어디에 맞혀도 같은 피해를 준다. 약점을 노리는 플레이가 없다.
- 스캔 파트(김성준)는 "몬스터 파트가 OnEnable에서 약점을 무작위 후보 위치로 옮긴다"는 전제로 작업하고 있다. 하지만 몬스터 쪽 약점 오브젝트와 위치 선택 코드가 아직 없다.

## 기대 결과 (Proposed outcome)
- 적마다 약점 후보 Transform 5개를 둔다. 활성화될 때마다 그중 하나를 무작위로 골라 약점(SphereCollider)을 붙인다. 개체끼리 같은 후보를 골라도 된다.
- 총알이 적 CharacterController에 맞으면 맞은 지점(`SEffectContext.HitPoint`)에서 총알 방향(`SEffectContext.Direction`)으로 연장한 탄도선과 약점 중심 사이의 최단 거리를 잰다. **거리 ≤ 약점 반지름**이면 치명타, 아니면 일반 피격이다.
- 약점 참조가 없거나, 콜라이더가 꺼져 있거나(`enabled`/`activeInHierarchy`), HitPoint 또는 Direction이 없으면 일반 피격으로 판정한다. 스캔이 머지되면 스캔 중에만 치명타가 된다.
- **1단계(이번 작업)는 판정 결과를 Debug.Log로만 확인한다.** 피해량은 바꾸지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어 사격, 적(Melee/Ranged), 이후 스캔·UI
- 어떤 시스템/모듈이 건드려지는가:
  - `Attribute.Core.SEffectContext`: `HitPoint`, `Direction` 필드 추가. 이세훈 영역이고, 기존 시그니처는 깨지지 않는다.
  - `SOAttributeEffect.Apply`: context를 다시 만들 때 HitPoint와 Direction을 복사한다.
  - `BulletController`: 총알 위치를 HitPoint로, 비행 방향(`_direction`)을 Direction으로 넘긴다.
  - `CharacterCombat.IsCriticalHit`
  - 신규 `Combat.WeakPointSelector`
  - 신규 `AI.WeakPointScanBinder`: 약점 오브젝트를 스캔 반응(`Core.Scanning.WeakPointReaction`)에 연결한다
  - `CombatMediator`의 Health 콜백 로그
  - Melee/Ranged 프리팹: 루트에 `ScanTarget`, `WeakPointReaction`, `WeakPointScanBinder`를 붙이고 `WeakPoint`는 꺼진 상태로 저장한다

## 제약 (Constraints)
- Combat 폴더는 UnityEngine 외 시스템을 참조하지 않는다(이식성).
- AttributeSet, AttributeMediator, CharacterMediator는 수정하지 않는다. 적은 루트 AttributeSet을 직접 쓰고, On 콜백이 이미 context를 Combat까지 전달한다.
- 판정은 물리 충돌이 아니라 거리 계산이다. 약점 콜라이더는 `Ignore Raycast` 레이어의 trigger로 둔다. 총알이나 조준 레이가 약점에 먼저 걸려 피해 없이 사라지는 것을 막기 위해서다.

## 결정 사항
- 맞은 지점은 트리거 진입 시점의 총알 중심(`transform.position`)으로 한다. 테스트용이고 TODO 주석을 남겼다.
  - 오차는 탄도 방향으로 최대 약 0.33m다. 총알 월드 반지름이 0.25m이고, 60fps에서 한 프레임에 0.33m 이동하기 때문이다.
- 약점 반지름은 0.3m를 기준으로 한다. 맞은 지점이 CC 표면 근처에 찍히므로 약점 구가 표면에 닿아야 한다.
- 2026-10-14: 점-점 거리 판정은 항상 일반 피격이 나와 탄도선 거리 판정으로 바꿨다.
  - 원인: 총알은 CC(반지름 1) 표면에 닿자마자 사라진다. 그래서 맞은 지점이 몸 안쪽 약점(반지름 0.5)의 범위 밖에 찍힌다.
  - 방식: 약점 중심을 탄도선에 투영(내적)해 최근접점을 구하고, 그 점과의 거리를 반지름과 비교한다. 맞은 지점보다 뒤쪽은 보지 않는다.
  - Raycast가 아니라 거리 계산을 택한 이유: 기존 계산을 유지할 수 있고, 물리 동기화나 트리거 동작에 의존하지 않는다.
  - 방향 전달: Origin에서 다시 계산하지 않고, 총알이 가진 전방벡터를 `SEffectContext.Direction`으로 넘긴다.
- 2026-10-14: `SEffectContext.Origin`을 삭제했다. 공격자 위치는 `Cursor`의 transform으로 얻을 수 있고, 값을 채우거나 읽는 곳이 없었다.
- 2026-10-14: 약점은 스캔 중에만 켠다. 스캔 전에는 `SetActive(false)`로 꺼 두고, 스캔이 닿으면 `SetActive(true)`로 켠다.
  - 켜고 끄기는 스캔 파트의 `WeakPointReaction`이 맡는다. 약점 지정(`SetWeakPoint`)은 신규 `AI.WeakPointScanBinder`가 Awake에서 한 번 호출한다.
  - Combat은 다른 시스템을 참조하지 않는다는 제약 때문에, `WeakPointSelector`가 직접 연결하지 않고 전용 스크립트를 따로 둔다.
  - 약점은 부모만 바뀌고 같은 인스턴스라서 다시 지정하지 않는다. 풀에 반납될 때는 `ScanTarget.OnDisable`이 끈다.
  - 검증(Play, eval): Melee와 Ranged 모두 생성 직후 꺼져 있었다. `ScanTarget.OnScanned` 호출 시 켜졌고, 유지 시간(1초)이 지나자 꺼졌다.
- 피해 곱 적용 방식은 2026-10-14에 "HP 반영 전 곱하기"로 정했다. Pre 콜백에 context가 필요해 Attribute 변경이 커지므로 2단계로 미뤘다.

## 열린 질문 (Open questions)
- 2단계: 피해 곱을 어디서 적용할지.
  - HP 반영 전 곱하기: AttributeSet Pre 콜백에 context가 필요하다.
  - 반영 후 추가 피해: OnHit 중복 처리가 필요하다.
  - 배율 위치: `CombatMediator._criticalMultiplier` 안이 나왔다.
- 2단계: `OnCriticalHit` 이벤트 추가 여부. 안은 OnHit는 항상 발생하고, 치명타일 때만 추가로 발생하는 것이다.
- 정밀한 맞은 지점: 되돌린 탄도로 `Collider.Raycast`를 써서 표면 진입점을 구하는 방식. 판정이 들쭉날쭉하다는 피드백이 나오면 검토한다.
- 후보 Transform 위치와 반지름: 현재는 뼈 위치 기본값이다. 사용자가 조정해야 한다.
- 플레이어 `Bullet.prefab`의 `_hitLayers`가 128(Obstacle)뿐이라, Enemy 레이어 적을 맞히지 못한다(intent-022 관련, 이번 범위 밖).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
