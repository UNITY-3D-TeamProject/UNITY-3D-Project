# 플레이어 스폰 준비 설정 안내

## 작업 요약 및 방법
- 현재 스포너, 게임매니저, AttributeSet, 플레이어 데이터 SO, 스폰 Effect 에셋과 빌드 씬 목록을 확인해 설정 순서를 안내했다.
- 런타임 코드와 씬/프리팹/에셋은 수정하지 않았다.

## 결정 사항
- 시작 씬 GameManager에 SOAttributeData_Player를 연결한다.
- 활성 플레이어 프리팹 루트의 AttributeSet.Init Data에도 같은 데이터 SO를 연결한다.
- 스폰 Effect는 Set + Float로 CurrentHp/CurrentBattery/CurrentHeat의 원하는 시작값을 설정하고 각 씬 PlayerSpawner의 Player Effects에 연결한다.
- 씬별 PlayerSpawner는 하나씩 두고, Player Prefab과 생성 위치를 지정한다.

## 현재 상태 및 이슈
- 스폰 Effect 에셋 세 개 모두 디스크 기준 Target Attribute가 비어 있고 Amount는 0이다.
- SceneLoader 이벤트 경로에서는 씬 이동 전 저장/이유 설정이 연결돼 있다.
- 로비 복귀 위치는 SpawnStage를 설정하는 코드가 아직 없어 Inspector 배열만 연결해도 스테이지별 위치를 선택할 수 없다.
- 사망 후 실제 재생성 연결 및 Unity 실행 검증은 남아 있다.

## 다음 할 일
- Inspector 배선 후 첫 스폰과 일반 씬 이동 시 능력치 유지 확인.
- 사망 후 재생성 및 로비 복귀 스테이지 기록은 별도 구현.
