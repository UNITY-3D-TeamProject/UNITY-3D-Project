# Intent Pointer

이슈 트래커처럼 "어느 파트에 어떤 문제가 있는지"를 한눈에 보기 위한 목차이다.
새 intent 문서를 만들거나 상태가 바뀔 때마다 이 파일을 함께 갱신한다.

## 열려있는 인텐트 (Open)
<!-- 파트별로 묶어서 나열. 형식: - **[파트]** 문제 한 줄 요약 → [문서](./intent-NNN-slug.md) -->
- **[Enemy AI]** 전투 적/보스 AI 부재 — BT 설계안이 기존 이동 코어와 맞물리는 지점 미정 → [문서](./intent-003-enemy-ai.md)

## 해결됨 (Resolved)
<!-- 형식: - **[파트]** 문제 한 줄 요약 → [문서](./clear/intent-NNN-slug.md) (resolved: YYYY-MM-DD) -->
- **[Movement System]** 이동 시스템 공용 코어(CharacterMotor/WaypointMover) 부재 → [문서](./clear/intent-001-movement-system-core.md) (resolved: 2026-09-11)
- **[Movement System]** 이동 코어에 경로/회전/탑승 정책이 섞여 있음 → [문서](./clear/intent-002-movement-core-scope.md) (resolved: 2026-09-14)
