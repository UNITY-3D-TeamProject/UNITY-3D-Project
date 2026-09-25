# Intent Pointer

이슈 트래커처럼 "어느 파트에 어떤 문제가 있는지"를 한눈에 보기 위한 목차이다.
새 intent 문서를 만들거나 상태가 바뀔 때마다 이 파일을 함께 갱신한다.

## 열려있는 인텐트 (Open)
<!-- 파트별로 묶어서 나열. 형식: - **[파트]** 문제 한 줄 요약 → [문서](./intent-NNN-slug.md) -->
- **[Movement System]** 이동 코어에 경로/회전/탑승 정책이 섞여 있음 → [문서](./intent-002-movement-core-scope.md)
- **[Movement System]** PlayMode 테스트로 발견된 이동 자체 버그 4건 → [문서](./intent-003-movement-playtest-bugfixes.md)

## 해결됨 (Resolved)
<!-- 형식: - **[파트]** 문제 한 줄 요약 → [문서](./clear/intent-NNN-slug.md) (resolved: YYYY-MM-DD) -->
- **[Stage System]** 앱별 `StageBase` 구현체가 없음 → [문서](./clear/intent-005-stage-implementations.md) (resolved: 2026-09-24)
- **[Stage System]** 앱별 스테이지와 내부 Phase를 위한 공통 코어 타입 부재 → [문서](./clear/intent-004-stage-core-foundation.md) (resolved: 2026-09-24)
- **[Movement System]** 이동 시스템 공용 코어(CharacterMotor/WaypointMover) 부재 → [문서](./clear/intent-001-movement-system-core.md) (resolved: 2026-09-11)
