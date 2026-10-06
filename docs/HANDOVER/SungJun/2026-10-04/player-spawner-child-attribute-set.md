# 플레이어 자식 AttributeSet 조회

## 작업 요약 및 방법
- PlayerSpawner의 스폰 직후 AttributeSet 조회를 `GetComponentInChildren<AttributeSet>()`로 변경했다.
- Player.prefab에 AttributePrefab이 자식으로 들어 있고, 활성 상태인 AttributePrefab에 AttributeSet이 붙어 있음을 확인했다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/PlayerSpawner.cs`
- `docs/HANDOVER/SungJun/HANDOFF_POINTER.md`
- 이 작업 기록

## 결정 사항
- 생성된 플레이어 인스턴스 내부에서 AttributeSet을 한 번 찾아 기존 Effect 적용과 GameManager 등록에 같은 참조를 전달한다.

## 현재 상태 및 이슈
- 코드 차이는 조회 호출 한 줄로 확인했다. Unity Play Mode 실행 검증은 하지 않았다.
- 기존 작업 트리의 `ProjectSettings/EditorBuildSettings.asset` 변경과 `UNITY-3D-Project.slnx` 삭제는 건드리지 않았다.

## 다음 할 일
- Unity Play Mode에서 플레이어 스폰 시 AttributeSet 누락 오류가 없어지고 Effect 적용과 HUD 연결이 진행되는지 확인한다.
