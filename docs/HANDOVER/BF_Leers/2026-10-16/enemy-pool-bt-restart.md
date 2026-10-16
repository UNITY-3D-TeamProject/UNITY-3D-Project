# 풀 재사용 시 BT가 이어서 돌던 문제 — 반납할 때 Restart

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- intent: `docs/intent/intent-013-ranged-enemy-behavior.md` (열린 질문 하위 항목에 원인·수정 기록, open 유지)

## 1. 작업 요약 및 방법
- 증상(기존 알려진 문제 2개): 후퇴 도중 비활성화되면 MoveSpeed가 감속된 채 남을 수 있음 / 풀 재사용 시 BT가 초기화되지 않음(Melee·Ranged·Special 공통).
- 원인: Unity Behavior `BehaviorGraphAgent`에는 `OnEnable`/`OnDisable`이 없다. 반납(SetActive false)해도 그래프는 멈추기만 하고, 실행 중인 노드의 `OnEnd`가 불리지 않는다. 다시 꺼내면 하던 노드를 이어서 돈다. 사망 애니메이션 중에도 BT는 계속 돈다(`DisableControl`은 Move/Skill/Rotate 중재자만 끈다).
- 실제 영향: `MonsterSpawner`가 꺼낼 때마다 DB MoveSpeed를 SetValue로 넣는다. 그 뒤 남아 있던 후퇴 노드가 끝나면 `OnEnd`의 `ScaleMoveSpeed(1/0.4)`가 실행돼 **재사용된 원거리 적이 2.5배 빨라진다**(코드 분석 결과, Play 재현은 하지 않음).
- 수정: `EnemyPoolReturner`에 `_graphAgent`(비우면 `GetComponent`)를 추가하고, `OnDisable`에서 `_graphAgent.Restart()`를 호출한다.
  - 패키지 `BehaviorGraphAgent.Restart`(695행): 비활성 상태에서는 `m_Graph.End()` + `m_IsStarted = false`만 한다 → 실행 중인 노드의 `OnEnd`(감속 복구, StopSkill 등)가 반납 시점에 불리고, 다시 켜지면 `Update`가 그래프를 처음부터 `Start()`한다.
  - `End()`만 부르면 `m_IsStarted`가 true로 남아서, 다시 켜졌을 때 끝난 그래프를 Tick만 하는 정지 상태가 된다.
- Melee/Ranged/Special 프리팹 모두 BT 에이전트와 `EnemyPoolReturner`가 같은 루트 오브젝트에 있다(프리팹 YAML로 확인). GC 프리팹은 `EnemyPoolReturner`가 없어 대상이 아니다.

## 2. 결정 사항
- BT 리셋은 `AIController`가 아니라 `EnemyPoolReturner`에 둔다. AIController는 BT를 모르는 구조(노드가 AIController API만 호출)이고, 풀 재사용 처리는 원래 이 컴포넌트 담당이다.
- 블랙보드 값(`WindReadyTime` 등) 초기화와 사망 애니메이션 중 BT 정지는 범위 밖이다.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0 확인.
- **Play 검증 미완**: Play 중 eval로 비활성 Ranged를 플레이어 앞에 놓고 켜다가 **Unity 에디터가 네이티브 크래시**했다(`PxControllerManager::createController(): desc.isValid() fails` → `GameObject.SetActive` 크래시). eval에서 `NavMesh.SamplePosition` 결과를 확인하지 않아 위치가 무한대였던 것으로 추정한다. 수정 코드는 `OnDisable` 경로만 바꾸므로 원인일 가능성은 낮지만, 확정하지는 못했다.
- 크래시 직전 열려 있던 `Assets/Test/security/Map_4_Security_T.unity`(Git 제외)는 저장되지 않은 변경이 있었다. Unity 백업 `Temp/__Backupscenes/0.backup`을 `C:\Users\inha\UnitySceneBackup-2026-10-16\`에 복사해 두었다.

## 4. 다음 할 일
- Unity를 다시 열 때 씬 복구 여부를 확인한다(필요하면 위 백업 사용).
- Play 검증: Ranged가 후퇴하는 중(감속 상태)에 반납 → MoveSpeed 원래 값 복구 확인 / 다시 꺼낸 뒤 그래프가 루트부터 시작하고 속도가 2.5배가 되지 않는지 확인 / Melee 사망 → 재사용 후 순찰·추격 정상인지 확인.
- eval로 적을 놓을 때는 `SamplePosition` 반환값을 반드시 확인한다.
