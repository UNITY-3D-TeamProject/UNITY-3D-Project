# 특수몹 약점 위치 조정 (AI_TODO2 잡몹-8, intent-088)

## 1. 작업 요약 및 방법
- 문제: 특수몹 약점 5곳 중 일부를 아예 맞힐 수 없음.
- 조사(Play `Map_4_Security`, eval):
  - 실행 중 특수몹 `CombatPrefab` 의 `CharacterCombat._weakPoint` 가 null이었다. Melee·Ranged 는 중첩 프리팹 오버라이드로 자기 `WeakPoint` 를 연결해 두었으나 `Special.prefab` 은 처음부터 오버라이드가 없었다(`git log -S` 확인). 그래서 특수몹은 치명타가 전혀 나지 않았다.
  - 치명타 판정(`CharacterCombat.IsCriticalHit`)은 CharacterController 에 닿은 지점의 탄도 연장선과 약점 구 거리다. 캡슐은 월드 반지름 0.6m·높이 0~2.2m 이고 로봇은 폭 2.8m·높이 3.3m(공중에 떠서 몸이 1.6~2.2m 에 몰림).
  - 약점을 임시 연결하고 정면 141칸을 조준한 결과: 1번 25, 2번 24, 3번 1~2, 4번 4, 5번(머리 위 2.47m) 0칸. 1·2번은 정면에서 몸에 가려짐.
- 수정(`Special.prefab`, Unity MCP `LoadPrefabContents`/`SaveAsPrefabAsset`, 코드 변경 없음):
  - `CombatPrefab._weakPoint` = `WeakPoint` SphereCollider
  - 2번 → `RigWingBottom` (0, 0.55, 0), 3번 → `RigLArm1` (-0.2, 0.8, 0.1), 4번 → `RigRArm1` (0.2, 0.8, 0.1), 5번 → `RigHead` (0, 1.05, 0.03). 프리팹 루트 기준(씬 스케일 2). 1번 유지.
- 검증: Play 에서 임시 연결 없이 프리팹 연결 확인, 정면·측면·위쪽 시점 시뮬레이션 1번 27~33, 2번 48~53, 3번 31~43, 4번 31~34, 5번 10~14칸. Play 중 새 콘솔 에러 없음.
- 실행자: Claude 직접(Unity MCP). 예외 이유: 프리팹 Transform 4개 + 참조 1개의 작은 에디터 작업이고 조사 맥락이 이 세션에 있어 Codex 위임 비용이 더 큼.

## 2. 결정 사항
- 사용자 결정: 캡슐 확대·피격 콜라이더 추가 없이 후보만 캡슐 안 정면으로 이동.
- 후보는 뼈 자식으로 둬 애니메이션을 따라가게 함.

## 3. 현재 상태 및 이슈
- 피격 캡슐이 작아 약점 구(0.8m)끼리 많이 겹친다. 1·5번은 머리 부근이라 스캔 표시가 비슷하다.
- 5번은 캡슐 꼭대기라 치명타 칸이 가장 적다(10~14).
- Idle 자세만 확인했다. 공격 애니메이션 중 팔 후보(3·4번) 위치는 미확인.
- 스캔 표시(셸 마스크) 렌더 확인은 Play 프레임이 진행되지 않아 실패, 직접 플레이 확인 필요.
- `ProjectSettings/ShaderGraphSettings.asset` 줄바꿈만 바뀐 에디터 자동 변경이 작업 트리에 있음(이번 작업과 무관).
- 작업 시작 시 에디터에 "The open scene(s) have been modified externally" 모달이 떠 MCP 가 전부 타임아웃됐다. 사용자가 Reload 로 닫음.
- 게이트 루프 미실행, 커밋 전.

## 4. 다음 할 일 (Next Steps)
- 직접 플레이: 특수몹 스캔 후 5곳 약점 표시가 정면에서 보이고 맞히면 치명타(2배 피해)가 나는지 확인.
- AI_TODO2 잡몹-7(약점 피격 사운드·히트 이펙트) 진행 — 이번 연결로 특수몹도 치명타가 나므로 피드백 대상에 포함.
