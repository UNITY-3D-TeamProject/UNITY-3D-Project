# HUD 갱신 함수 이름 변경

- 작업 요약 및 방법: PlayerHudPresenter의 RefreshAll, RefreshHp, RefreshBattery, RefreshHeat를 각각 UpdateAllHudGauges, UpdateHpHud, UpdateBatteryHud, UpdateHeatHud로 변경하고 모든 호출부를 반영했다.
- 결정 사항: 함수 이름만 변경하고 동작과 기존 사용자 수정은 유지했다.
- 현재 상태 및 이슈: Assets/_Project 검색으로 이전 이름이 남지 않음을 확인했다. Unity 컴파일은 실행하지 않았다.
- 다음 할 일: Unity에서 컴파일 및 HUD 동작 확인.