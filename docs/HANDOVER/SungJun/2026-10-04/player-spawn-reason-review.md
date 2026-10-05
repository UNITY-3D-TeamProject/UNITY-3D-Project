# 스폰 이유 분기 사용자 수정 코드 검토

## 작업 요약 및 방법
- GameManager, PlayerState, PlayerSpawner 및 호출 지점을 정적으로 검토했다. 런타임 코드는 수정하지 않았다.
- shouldRestore는 씬 이동에만 저장값을 복원하고 새 게임/부활의 Effect 초기값을 유지하기 위한 인자임을 설명했다.

## 결정 사항
- 능력치 증가와 LoadGame은 현재 범위에서 제외한다.
- 새 게임/부활은 같은 Effect로 초기화하고 씬 이동만 저장값을 복원한다.

## 현재 상태 및 이슈
- GameManager.RegisterPlayer에서 PlayerState.RegisterPlayer의 필수 bool 인자를 전달하지 않아 컴파일 불가.
- PlayerSpawner는 아직 HasSavedPlayerAttributes로 초기화를 분기하므로 저장값이 남은 새 게임/부활에서 Effect가 생략된다.
- C# 호출 검색상 PrepareSpawn은 StartGame의 NewGame 호출만 존재한다. 씬 이동/부활 요청 경로 연결이 필요하다.
- Unity 컴파일 및 PlayMode 검증은 수행하지 않았다.

## 다음 할 일
- GameManager에서 SceneTransition 여부를 shouldRestore로 전달한다.
- PlayerSpawner에서 NewGame/Respawn일 때 Effect를 적용하도록 조건을 교체한다.
- 기존 플레이어 제거/씬 전환 전에 PrepareSpawn 호출을 연결한다.
