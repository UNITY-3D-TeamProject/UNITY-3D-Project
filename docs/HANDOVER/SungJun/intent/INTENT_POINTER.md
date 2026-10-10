# SungJun Intent 목차

SungJun 작업의 미해결 intent 문서를 정리한다.

## 열려있는 인텐트 (Open)
- **[Stage / Player State]** StageManager 체크포인트 컴포넌트 연결 및 도달 처리 → [문서](./intent-016-stage-checkpoint-events.md)
- **[Stage / Player State]** PlayerFacade 기반 낙하 복귀(이동)·사망 복구(파괴 후 재스폰) 구현, 사망 시 Destroy 전제 확인 필요 → [문서](./intent-017-fall-death-recovery-playerfacade.md)
- **[Player Spawn]** 새 게임 Effect 초기화와 씬 이동 저장값 복원, 사망 재생성 사유 제거, Unity 실행 검증 필요 → [문서](./intent-012-player-spawn-default-effects.md)

## 해결됨 (Resolved)
- **[Stage / Player State]** 생존 낙하 복귀와 라운드 사망 복구 설계 완료, 구현 전 → [문서](./clear/intent-015-stage-round-recovery-design.md) (resolved: 2026-10-08)
