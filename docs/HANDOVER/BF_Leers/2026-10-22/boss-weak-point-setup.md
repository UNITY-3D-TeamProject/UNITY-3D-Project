# 보스 A·B 약점 루트 선택기·스캔 구성 추가

## 1. 작업 요약 및 방법
- 사용자가 두 보스에 `HeadWeakPoint`(SphereCollider, `CharacterCombat._weakPoint` 연결됨)만 넣어 둔 상태에서, 일반 적(Melee)과 같은 약점 구성을 맞춤.
- `BossA.prefab`, `BossB.prefab` (Unity MCP `PrefabUtility.LoadPrefabContents` → `SaveAsPrefabAsset`, YAML 직접 수정 없음):
  - `RigHead` 아래 `WeakPointCandidate_1` 을 기존 `HeadWeakPoint` 자리(A: (-0.414, 0.09, 0), B: (0, 0, 0))에 만들고 `HeadWeakPoint` 를 그 아래 (0, 0, 0) 으로 옮김 → 월드 위치 변화 없음.
  - 루트에 `WeakPointSelector` (`_weakPoint` = HeadWeakPoint, `_candidates` = [WeakPointCandidate_1]).
  - `HeadWeakPoint` 에 Scan 태그, `ScanTarget`, `SilhouetteReaction`, `WeakPointShellMask`, `WeakPointReaction` 추가.
    - `ScanTarget._scanColliders`: A = `BodyHitbox`(BoxCollider, A 루트에 CharacterController 없음), B = 루트 CharacterController.
    - `SilhouetteReaction._autoShellMaterial` = `M_ScanHologram`, `_shellSourceRoot` = 보스 루트 (Melee 와 동일).
- 실행자: Claude 직접(MCP). 예외 이유: 프리팹 2개 컴포넌트 추가뿐이라 Codex 위임 비용이 더 큼.

## 2. 결정 사항 (사용자 확정)
- 후보는 머리 1곳만(무작위 이동 없음, 다른 적과 구성만 맞춤).
- 스캔 구성도 같이 붙임 → 스캔 중에만 약점 콜라이더가 켜지고 크리티컬도 그때만 판정.
- 콜라이더 설정(Enemy 레이어, 트리거 아님, 반경 A 0.23 / B 0.4)은 손대지 않음. Melee 약점은 Ignore Raycast·트리거·0.3 이라 다름.

## 3. 현재 상태 및 이슈
- 저장된 에셋 재로드로 참조·경로·위치·누락 스크립트 0 확인. **Play·스캔 동작·컴파일 게이트 미실행, 육안 확인 전, 커밋 전.**
- BossA `HeadWeakPoint` 는 `BossHitRelay`(Head 부위) 콜라이더를 겸한다. `WeakPointReaction` 이 스캔 밖에서 콜라이더를 끄므로, 최종 단계(All)에서도 머리는 스캔 중에만 맞는다(몸·손은 그대로).
- 보스 콜라이더가 트리거가 아니고 Enemy 레이어라 총알·스캔과의 상호작용이 Melee 와 다를 수 있음(미확인).

## 4. 다음 할 일
- 보스전 Play 에서 스캔 → 머리 홀로그램 색·크리티컬 로그·스캔 종료 후 꺼짐 확인.
- BossA 머리 피격이 스캔 중에만 되는 것이 의도에 맞는지 사용자 확인.
