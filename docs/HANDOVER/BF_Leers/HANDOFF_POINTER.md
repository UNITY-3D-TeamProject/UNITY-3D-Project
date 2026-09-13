# BF_Leers — Handover Pointer

이 파일은 BF_Leers 명의로 진행한 작업 이력의 목차이다.
새 세션을 시작할 때 이 파일을 먼저 읽고 최신 항목부터 맥락을 파악한다.
작업을 마무리할 때는 오늘 날짜 폴더에 새 문서를 만들고, 아래 목록 맨 위에 한 줄 항목을 추가한다.

> ⚠️ 아래 2026-09-09 / 2026-09-10 항목의 링크 3개는 `chore : 초기화` 커밋으로 대상 파일이 삭제되어 **전부 깨져 있다**. 또한 2026-09-10 항목은 intent-001을 `open`으로 적고 있으나 실제로는 `resolved`다(`docs/intent/clear/`로 이동 완료).

## 변경 이력 (최신순)
- **2026-09-12** — 이동 코어 범위 재정렬: `WaypointMover` → `TransformMotor`로 축소(경로 산출·회전·정지 제거), `CharacterMotor`에서 탑승 로직·LayerMask 제거하고 `AddExternalDisplacement()` 신설. 코어에서 뺀 것은 전부 동작 스니펫으로 문서에 보관(웨이포인트 순회 / 회전 / 탑승). **Unity Editor 검증 미완 — 다음 세션 최우선**, 특히 슬라이딩 회귀 테스트: [Movement System 코어 범위 재정렬](./2026-09-12/movement-core-scope-cleanup.md)
- **2026-09-10** — T7(스텁 삭제, 대상 파일 이미 부재로 완료 처리) 확인, T8 intent 정리 시도했으나 네임스페이스 열린 질문 미해결로 intent-001 `open` 유지, Phase 3(구현) 종료 확인. T6 슬라이딩 이슈는 원인 미확정 채 보류. 다음은 Phase 4(정적 리뷰): [Movement System 코어 T7-T8 정리](./2026-09-10/movement-system-core-t7-t8.md)
- **2026-09-09** — Movement System 코어 T3(IMoveMotor)~T6(CharacterControllerMotor) 구현 완료, T6 PlayMode 수동 검증 중 문제 발생(미해결) — 다음 세션 최우선 디버깅 대상: [Movement System 코어 T3-T6 구현](./2026-09-09/movement-system-core-t3-t6.md)
- **2026-09-09** — Movement System 코어 T1(SMoveIntent/IMoveIntentProvider), T2(SOMovementConfig) 구현 완료, T3부터는 새 세션에서 진행 예정: [Movement System 코어 T1-T2 구현](./2026-09-09/movement-system-core-t1-t2.md)
