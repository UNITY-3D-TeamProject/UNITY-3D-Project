# 스포너 Effect 적용 경로 변경 + 몬스터 고정/랜덤 소환

관련 intent: [intent-024](../intent/clear/intent-024-spawner-facade-effect-random-monster.md)

## 1. 작업 요약 및 방법
- **PlayerSpawner** (`Scripts/Core/PlayerSpawner.cs`)
  - 시작 Effect 적용을 `SpawnerBase.ApplySpawnEffects(AttributeSet, ...)` → 새 private 메서드 `ApplyPlayerEffects(PlayerFacade)`로 교체.
  - `ApplyPlayerEffects`는 `_playerEffects`를 돌며 `playerFacade.ApplyEffect(effect)` 호출, false면 `LogWarning`(effect 이름 포함).
  - 더 이상 안 쓰는 out 변수 `_spawnedPlayerattributeSet`은 `out AttributeSet _`로 변경. `TrySpawnWithAttributes`는 생성 + AttributeSet 존재 검사용으로 계속 사용.
  - 이 파일은 **CP949 인코딩**이라 Python으로 cp949 읽기/쓰기 해서 인코딩 유지(`git diff | iconv -f cp949`로 바뀐 줄만 바뀐 것 확인).
- **MonsterSpawner** (`Scripts/Core/MonsterSpawner.cs`)
  - `ESpawnMode { Fixed, Random }`(클래스 내부), `_spawnMode`(기본 Fixed), `_randomMonsterPrefabs` 추가. 각 칸에 Tooltip.
  - `SelectMonsterPrefab()`: Fixed면 `_monsterPrefab`, Random이면 목록에서 `UnityEngine.Random.Range`로 하나. 목록이 비면 null → `SpawnMonster`에서 에러 로그 후 소환 안 함.
  - Effect 적용은 몬스터에 Facade가 없어 기존 `ApplySpawnEffects` 그대로, 어떤 몬스터든 `_monsterEffects` 공통 적용.
- `SpawnerBase`, `PlayerFacade`, `CharacterMediator`는 변경 없음.

## 2. 결정 사항
- 플레이어는 PlayerFacade를 단일 창구로 Effect 적용. 반복문은 스포너 안(이세훈 담당 PlayerFacade 미수정).
- 몬스터는 Facade가 없으므로 AttributeSet 직접 적용 유지.
- 기존 `_monsterPrefab` 칸 유지 + 모드 기본값 Fixed → MonsterSpawner 프리팹·Map_4_Security 스포너 13개 값 그대로.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0.
- Play 검증(Map_3_File, 새 게임): 플레이어 스폰 직후 CurrentHp 50 / JumpPower 8 / CurrentBattery 80 / MoveSpeed 6 — `PlayerSpawnEffect` SO 값대로 적용, 새 경고 없음.
- Play 검증(런타임 임시 생성, 씬 저장 안 함): Random 스포너 10개 → Melee 5·Ranged 5, Fixed 기본값 → Melee, 빈 랜덤 목록 → 에러 로그 후 소환 안 함.
- 사망 후 라운드 재스폰(RoundRecovery)·StageRestart 경로는 같은 코드 경로지만 따로 Play 확인은 안 함.
- 에디터 포커스가 없으면 Play 프레임이 멈춤(`Time.frameCount` 고정) — MCP로 시험할 때 `editor_focus` 필요.
- 코드 커밋: 4a9a9e0. intent-024 resolved 처리.

## 4. 다음 할 일 (Next Steps)
- 실제 맵에서 랜덤이 필요한 스포너를 Random으로 바꾸고 목록 채우기(목록의 프리팹은 모두 ObjectPoolManager에 등록 필요).
- Map_4_Security에 ObjectPoolManager 배치(기존 이슈, 없으면 몬스터 안 나옴).

---

# 추가: 몬스터 개체별 MoveSpeed 비율 오차

관련 intent: [intent-025](../intent/intent-025-monster-attribute-variance.md)

## 1. 작업 요약 및 방법
- `MonsterSpawner`에 `_moveSpeedVariance`(Range 0~1) 칸과 상수 `MOVE_SPEED_KEY = "MoveSpeed"` 추가.
- 소환 직후 `ApplySpawnEffects` 다음에 `ApplyMoveSpeedVariance`가 MoveSpeed를 `값 × Random(1 - 오차, 1 + 오차)`로 `AttributeSet.SetValue`. 오차 0이면 아무것도 안 함, 몬스터에 MoveSpeed가 없으면 경고 후 건너뜀.
- `Prefabs/System/MonsterSpawner.prefab` 기본값 `_moveSpeedVariance: 0.15`(±15%). 프리팹을 쓰는 Map_4_Security 스포너들도 이어받음.
- 처음엔 (어트리뷰트 이름, 오차) 목록으로 만들었다가 사용자 요청으로 MoveSpeed 전용 칸 하나로 줄임.

## 2. 결정 사항
- SO를 개체 수만큼 만들지 않고 스포너가 오차를 붙인다. 종류별 기본 속도는 능력치 데이터/Effect, 개체별 흔들림은 스포너.
- 비율(±%) 방식, MoveSpeed만.
- 몬스터 이동 속도는 MovePrefab의 MoveMediator(`_speedValueKey: MoveSpeed`)가 변경 콜백으로 CharacterMotor.Speed에 넘기므로 SetValue만으로 실제 속도가 바뀜.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0. Play(Map_3_File, 런타임 임시 스포너 10개, 씬 저장 안 함): Melee MoveSpeed 8.64~11.29로 퍼지고 CharacterMotor.Speed도 같은 값, CurrentHp 100 유지.
- 콘솔의 `GameObjectInspector` / `SerializedObjectNotCreatableException`은 Play 전환 중 인스펙터가 지워진 오브젝트를 잡아서 난 에디터 내부 예외(코드와 무관).
- 풀 재사용 시 MoveSpeed를 Set하는 Effect가 없으면 배율 누적(현재 Monster_Effect _MoveSpeed가 Set 10).
- 미커밋. 워킹 트리의 `Monster_Effect`/`Monster_Effect 1` 삭제(이름 변경)·EditorBuildSettings 변경은 사용자 작업이라 손대지 않음.

## 4. 다음 할 일 (Next Steps)
- 실제 맵에서 몬스터 여러 마리가 쫓아올 때 ±15%가 적당한지 체감 확인 후 조정.

---

# 추가: 몬스터 DB SO(SOMonsterData)로 초기값 적용

관련 intent: [intent-025](../intent/intent-025-monster-attribute-variance.md)

## 1. 작업 요약 및 방법
- 신규 `Scripts/Core/SOMonsterData.cs`: `_monsterPrefab`(DB 찾는 키) + `SOAttributeData.SAttribute[] _attributes`(기존 구조체 재사용). 메뉴 `Monster/MonsterData`.
- `MonsterSpawner`: `_monsterDatabase`(SOMonsterData[]) 추가. 소환 후 `ApplyMonsterData`가 소환한 프리팹과 같은 `MonsterPrefab`을 가진 데이터를 찾아 각 항목을 `SetValue`. 몬스터에 없는 이름은 경고 후 건너뜀, 데이터가 없으면 경고 후 프리팹 기본값. 순서: DB → `_monsterEffects`(유지) → MoveSpeed 오차.
- 에셋 `ScriptableObjects/MonsterData/`: `SOMonsterData_Melee`(Melee), `SOMonsterData_Ranged`(Ranged), `SOMonsterData_Special`(프리팹 비움, 특수몹 프리팹 생기면 연결). 값은 모두 MaxHp 100 / CurrentHp 100 / MoveSpeed 10 / AttackDamage 1.
- `MonsterSpawner.prefab`: `_monsterDatabase`에 3개 연결, `_monsterEffects`의 CurrentHp·MoveSpeed Set 2개는 DB와 겹쳐 비움(Effect 에셋 파일은 유지).
- 이 작업 전에 지금까지의 MoveSpeed 오차 작업을 커밋(a4c4212 코드, 9a08e75 문서).
- 인스펙터: MonsterSpawner 각 칸 위에 `[Header]` 한 줄 설명(ScanTarget과 같은 "필수/안 해도 됨" 형식)을 달고, Tooltip에 자세한 설명(쓰는 모드, 오른쪽 숫자 = 목록 칸 수, ObjectPoolManager·DB 등록 필요)을 둠.

## 2. 결정 사항
- DB 찾는 키는 몬스터 프리팹(스포너가 이미 알고 있어 enum 등 새 타입·중복 입력 불필요).
- 몬스터 프리팹의 `_initData`(SOAttributeData_Enemy)는 어트리뷰트 이름 정의용으로 그대로 둠.
- DB 값을 SetValue로 넣으므로 InitValue 미호출 문제와 풀 재사용 누적 문제가 같이 해결됨.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0. Play(Map_3_File, 런타임 임시 스포너 9개, 씬·에셋 저장 안 함):
  - Melee·Ranged(DB 에셋): CurrentHp 100(기존 SO 0 → DB 적용 확인), MoveSpeed 8.97~11.46, 모터 속도 일치.
  - Ranged에 런타임 전용 DB(MoveSpeed 20, CurrentHp 55): MoveSpeed 18.22·22.86, CurrentHp 55 → 종류별 값 적용 확인.
  - DB 비운 스포너: "[Melee] Monster Database에 데이터가 없어 프리팹 기본값으로 소환합니다." 경고 후 소환(CurrentHp 0).
- 에디터가 포커스를 가져도 Play 프레임이 멈춰 있는 경우가 있어 `EditorApplication.Step()`으로 프레임을 넘겨 확인함.
- 커밋함. `Monster_Effect` 이름 변경(GUID 유지)·EditorBuildSettings(줄바꿈만)는 사용자 작업이라 커밋에 넣지 않음.

## 4. 다음 할 일 (Next Steps)
- Melee·Ranged·특수몹 실제 밸런스 값을 DB 에셋에 입력.
- 특수몹 프리팹이 생기면 `SOMonsterData_Special`의 Monster Prefab 연결 + ObjectPoolManager 등록.
- 실제 맵에서 ±15% 체감 확인.
