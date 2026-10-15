# 스캔 컴포넌트 Inspector 헤더 정리 + TestScene Ranged 약점 확인 (2026-10-15)

## 1. 작업 요약 및 방법
- TestScene `Ranged`의 `WeakPoint`(`.../RigHead/WeakPointCandidate_1/WeakPoint`) 구성을 eval로 확인했다. 모두 정상이었다.
  - 오브젝트 켜짐, 태그 `Scan`, 레이어 `Ignore Raycast`, SphereCollider(trigger, 반지름 0.5)
  - `ScanTarget._scanColliders` = Ranged(CharacterController)
  - `SilhouetteReaction._autoShellMaterial` = M_ScanHologram, `_shellSourceRoot` = Ranged
  - `WeakPointReaction`, `WeakPointShellMask`(비움, 자동)
  - 씬 인스턴스 오버라이드 상태이고, `Ranged.prefab`에는 아직 Apply하지 않았다.
- Inspector에서 설정해야 하는 값과 안 해도 되는 값이 구분되지 않는다는 피드백을 받았다. 그래서 필드마다 헤더를 달았다(Header 문구만 바꿈).
  - `ScanTarget`
    - Fade: 안 해도 됨
    - Reactions: 안 해도 됨
    - Scan Colliders: 약점은 필수(CharacterController ← 몬스터 루트)
  - `SilhouetteReaction`
    - Silhouette Renderers: 약점은 안 해도 됨, 그 외는 필수
    - Auto Shell Material: 약점은 필수(M_ScanHologram)
    - Shell Source Root: 약점은 필수(몬스터 루트)
  - `WeakPointShellMask`: 두 필드 모두 안 해도 됨

## 2. 결정 사항
- 헤더 형식: `필수 여부 (타입 ← 가져올 것)`. 필드 이름은 헤더 바로 아래에 보이므로 헤더에 다시 쓰지 않는다(여러 필드를 묶는 Fade만 예외).

## 3. 현재 상태 및 이슈
- 컴파일 에러 0.
- Inspector 폭이 좁으면 긴 헤더가 잘릴 수 있다. 화면에서는 아직 확인하지 않았다.
- TestScene Ranged의 약점 구성은 `Ranged.prefab`에 Apply되지 않았다. 풀에서 나오는 Ranged에는 스캔 컴포넌트가 없다.

## 4. 다음 할 일 (Next Steps)
- [USER] Inspector에서 헤더가 잘리지 않는지 확인한다.
- [USER] TestScene Ranged 오버라이드를 `Ranged.prefab`에 Apply할지 결정한다. Melee도 같은 구성이 필요하다.
