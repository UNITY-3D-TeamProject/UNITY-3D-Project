# 군중 레이어 3개 → 2개로 축소

## 작업 요약 및 방법
- 레이어는 `ProjectSettings/TagManager.asset` 한 파일에 번호로 저장되는 팀 공유 설정이라, 군중용으로 추가할 레이어 수를 줄였다.
- `Pedestrian`, `Vehicle`을 `Crowd` 하나로 합치고 `VehicleBody`는 유지한다 → `Crowd`, `VehicleBody` 2개.
- 수정 파일: `CrowdController.cs`(`_detectLayers` 툴팁 문구만), `docs/intent/intent-017-lobby-crowd.md`(영향 범위), `docs/planning/crowd-system.md`(Round 2 확정 사항에 변경 메모, 최종 요약의 구조·정지·`[USER]` 항목).

## 결정 사항
- 횡단보도가 없어 보행자와 차량은 만나지 않으므로, 감지 마스크를 보행자·차량 모두 `Player`, `Crowd`, `VehicleBody`로 같게 둬도 동작이 같다.
- 충돌 행렬: `Crowd` × `VehicleBody` 무시(차량 캡슐이 자기 박스에 걸리지 않게). `Player` × `VehicleBody`는 충돌 유지.
- 기존 레이어(`Enemy`, `Obstacle`) 재사용은 다른 시스템에 부작용이 생길 수 있어 쓰지 않는다.
- 횡단보도를 넣게 되면 다시 나눈다.

## 현재 상태 및 이슈
- 코드 로직 변경 없음(툴팁 문자열만). 레이어는 아직 만들지 않았다.

## 다음 할 일
- 사용자: 팀에 레이어 2개(이름·번호) 공지 → Project Settings > Tags and Layers에서 추가하고 충돌 행렬 설정(USER-2). 이 변경만 담아 먼저 공유 브랜치에 올리는 것을 권장.
- 이후 USER-3~5 순서대로 진행.
- (사용자 결정) 팀 협의 전까지는 레이어를 로컬 테스트용으로만 추가한다. `TagManager.asset`, `DynamicsManager.asset`(충돌 행렬)과 레이어를 지정한 군중 프리팹은 레이어 번호가 확정될 때까지 커밋하지 않는다.
