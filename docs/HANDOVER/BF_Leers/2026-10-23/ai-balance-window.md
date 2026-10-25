# AI 밸런스 수치 표 에디터 창 (2026-10-23)

## 1. 작업 요약 및 방법
- 적 밸런스 수치가 SO(`SOMonsterData_*`)·프리팹 컴포넌트(중첩 스킬 포함)·BT 그래프에 흩어져 있어, 한 화면에서 비교·수정하는 창을 추가했다.
- `Assets/_Project/Scripts/AI/Editor/AIBalanceWindow.cs` (네임스페이스 `AI.Editor`), 메뉴 `Tools > AI > 밸런스 수치 표`.
- 행 = 수치, 열 = `SOMonsterData` 6종(이름순). 그룹(접기): 어트리뷰트 → BT 블랙보드 → BT 노드 값 → 컴포넌트 타입별.
- 자동 수집: SO `_attributes`, 프리팹 계층의 `Assets/_Project/` 스크립트 MonoBehaviour 의 int/float/bool 필드(중첩 구조체 포함, 배열 제외), 프리팹 `BehaviorGraphAgent` 그래프의 숫자 블랙보드 변수와 블랙보드에 연결 안 된 노드·조건 로컬 값.
- 수정은 Enter/포커스 이동 시 즉시 저장: SO = `SerializedObject`(Undo 됨), 프리팹 = `PrefabUtility.EditPrefabContentsScope`(중첩 프리팹은 바깥 프리팹 오버라이드로), BT = 저작 그래프 값 변경 후 `RebuildAndSave()`(런타임 그래프 재생성).
- 실행자: Claude 직접. 예외 사유 = 이 PC에 Codex CLI 미설치(PATH 에 `codex` 없음). intent 문서는 사용자 지시로 생략.

## 2. 결정 사항
- 사용자 결정: 대상 = SOMonsterData 전부, 비교 표, 자동 수집, BT 포함.
- BT 는 Behavior 1.0.16 내부 API(`BehaviorAuthoringGraph`·`NodeRegistry` 등, `InternalsVisibleTo("Assembly-CSharp-Editor")`)를 쓴다. `NodeRegistry` 는 패키지에 두 개라 `using NodeRegistry = Unity.Behavior.NodeRegistry;` 별칭 사용.
- 헤더 아래 "프리팹" 줄로 같은 프리팹을 쓰는 열을 표시(프리팹·BT 값은 공유됨).

## 3. 현재 상태 및 이슈
- 리컴파일 통과(gate check recompile), 새 파일 경고 없음.
- eval 검증: 6열·그룹 수집 확인. Melee 열에서 MaxHp·EnemyMeleeAttack Hit Delay·BT AttackRange·BT 공격 ExitRange 4종 쓰기 → 파일 저장·런타임 블랙보드 반영 확인 → 원래 값 복원. 테스트로 바뀐 3개 에셋은 `git checkout` 으로 HEAD 복원(테스트 전 무변경 확인).
- BT 값을 고치면 런타임 그래프가 다시 빌드되어 `.asset` 의 rid 가 대량으로 바뀐다(그래프 창 저장과 같은 현상, 값 변화 없음).
- 보스 SO 프리팹 수정(사용자 승인): `SOMonsterData_BossA`·`BossB` 의 `_monsterPrefab` 이 생성 커밋 `8d5c001` 부터 `Melee.prefab`(Melee SO 복제 추정) → `BossA.prefab`·`BossB.prefab` 으로 변경. 보스 SO 참조처는 `Map_4_Security` 의 `BossFightSpawner` 뿐이고 이 필드는 안 읽음(자체 `_bossAPrefab`·`_bossBPrefab` 사용) → 게임 동작 변화 없음. 표에서 보스 패턴 컴포넌트 수치 수집 확인.
- `SOMonsterData_Thief` 는 `_monsterPrefab` 이 비어 있어 어트리뷰트만 보임(미수정).
- 창을 실제로 열어 조작하는 육안 확인·게이트 루프는 미완료, 커밋 전.

## 4. 다음 할 일
- 사용자가 창을 열어 레이아웃·조작감 확인.
- Thief SO 프리팹 연결 여부 결정.
