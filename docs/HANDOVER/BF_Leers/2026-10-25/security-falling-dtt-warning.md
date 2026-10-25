# 보안 맵 0·1 낙하물 DTT 원형 예고

## 작업 요약 및 방법
- 사용자 요청의 TDD는 기존 보스가 쓰는 DTT AOE 예고로 확인했다. `CircleWarnning.prefab`을 두 낙하물 프리팹에 중첩 연결했다.
- `FallingObstacle`에 선택형 `CircleRegion _warningCircle`을 추가했다. 예고를 낙하물에서 분리해 착지점 + Y 0.03m에 놓고, 기존 대기 루프에서 `FillProgress`를 0→1로 변경한다. 낙하 중 1을 유지하고 착지·비활성 시 숨기며, 파괴 시 분리된 예고도 지운다.
- 중첩 예고의 기존 `AoeVfxPlayer`가 인스턴스별 머티리얼을 복제한다. 자동 재생은 끄고 낙하물이 채움을 제어한다. 풀 재사용 시 같은 예고를 새 위치로 옮기고 진행도를 초기화한다.

## 변경된 파일
- `Assets/_Project/Scripts/Map/Gimmicks/FallingObstacle.cs`
- `Assets/_Project/Prefabs/Map/Gimmicks/Security/Security_FallingZero.prefab`
- `Assets/_Project/Prefabs/Map/Gimmicks/Security/Security_FallingVirus.prefab`
- intent-074 및 포인터, 이 인수인계와 포인터.
- 프리팹 편집은 Unity MCP `PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset`로 진행했다. YAML 직접 편집 없음.

## 결정 사항
- 기존 Renderer 예고는 다른 장애물을 위해 유지한다. 0·1 프리팹은 WarningMarker를 제거하고 DTT 원형만 사용한다.
- 원 반경은 기존 예고 반폭과 충돌 박스의 XZ 대각 반경 중 큰 값: Zero 0.872512m, Virus 0.75m.
- 예고 1초, 낙하·피해·생성·풀링 설정은 그대로다. 공용 `CircleWarnning` 원본 및 보스 로직은 수정하지 않았다.

## 검증 및 현재 상태
- 리컴파일 성공, 오류 없음. 기존 PlayMode `FallingHazardSpawnerTests` 5/5 통과.
- 실제 맵 Play에서 Zero·Virus의 표시, 착지점 위치, 낙하 중 완전 채움·위치 고정, 착지 후 비활성, 조기 반납 숨김, 풀 재사용 시 동일 예고 객체의 위치·채움 초기화, 낙하물 파괴 시 예고 제거를 확인했다.
- 동시 채움의 첫 검증은 1초 예고와 짧은 샘플 간격 때문에 실패했다. 프레임 시간 영향을 줄인 검증(인스턴스 대기 4초/8초)에서 서로 다른 원·머티리얼 인스턴스와 채움 값을 확인했다: FillProgress 약 0.25665/0.12832, 셰이더 값 약 0.25377/0.12689. 검증 후 인스턴스 설정 복구, 저장된 프리팹의 대기는 1초 확인.
- 테스트 시작 시 Unity가 기존 씬 변경 저장 창에서 대기했다. Computer Use로 Save를 눌러 기존 변경을 보존하고 진행했다. `Map_4_Security` 저장이 수반되었으며, 씬 구성 변경을 추가하지 않았다. 일시적인 도구 타임아웃 및 사라진 Inspector 대상 오류가 기록됐으나 기능 관련 런타임 오류는 없었다.
- 종료 시 Play 모드를 껐으며 원래 `Map_4_Security` 씬으로 돌아왔다. 커밋 없음. 기존 다른 수정은 보존했다.
- intent-074 전체는 기존 미완료 항목(SVN·육안 확인)이 있어 open 유지.

## 다음 할 일
- 실제 플레이에서 예고 크기·시인성·손맛 확인 후 필요 시 각 프리팹 WarningCircle의 Radius 조정.
- 기존 `ThirdParty/Projectile` SVN 업로드 및 intent-074 후속 항목 처리.
