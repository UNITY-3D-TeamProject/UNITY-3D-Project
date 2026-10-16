# Special 프리팹 약점(WeakPoint) 구조 추가

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`

## 1. 작업 요약 및 방법
- `Special.prefab`에 Melee·Ranged와 같은 약점 구조를 추가했다. 스크립트는 바꾸지 않았다.
- Unity Editor MCP `eval`로 `PrefabUtility.LoadPrefabContents` → 수정 → `SaveAsPrefabAsset` 했다. YAML은 직접 건드리지 않았다.
- `WeakPointCandidate_1~5`를 Nexus Robot 리그 뼈 아래에 붙였다(local 0, Layer 0, Untagged).

  | 후보 | 부모 뼈 |
  |---|---|
  | 1 (WeakPoint 초기 위치) | RigHead |
  | 2 | RigMain |
  | 3 | RigLArm1 |
  | 4 | RigRArm1 |
  | 5 | RigWingTop |

- `WeakPoint`는 Candidate_1의 자식이다. Layer 2, Tag `Scan`이고 컴포넌트는 SphereCollider(trigger, 반지름 0.3), ScanTarget(`_scanColliders` = 루트 CharacterController), SilhouetteReaction(`M_ScanHologram`, `_shellSourceRoot` = 루트), WeakPointShellMask, WeakPointReaction이다.
- 루트 `WeakPointSelector`의 `_weakPoint`와 `_candidates`(5개)를 연결했다.

## 2. 결정 사항
- Special 리그에는 Pelvis·Spine이 없어서 후보 뼈를 Head·Main·LArm1·RArm1·WingTop으로 정했다.
- 콜라이더 반지름은 Melee와 같은 0.3으로 정했다(Ranged는 0.5).

## 3. 현재 상태 및 이슈
- 프리팹을 다시 읽어 계층·태그·레이어·컴포넌트·참조가 모두 맞는 것을 확인했다. 컴파일 에러는 없다.
- Play에서 Special을 스폰해 약점 이동과 스캔 반응을 보는 검증은 하지 않았다.
- 콘솔의 `LilRobot` AnimationClip Legacy 경고는 이번 작업과 무관한 기존 경고다.

## 4. 다음 할 일
- Play에서 Special을 스폰해 WeakPoint가 후보 5곳 중 한 곳으로 옮겨지는지, 스캔 시 실루엣·약점 콜라이더가 반응하는지 확인한다.
- 날개나 머리가 작아서 반지름 0.3이 맞히기 어려우면 인스펙터에서 조정한다.
