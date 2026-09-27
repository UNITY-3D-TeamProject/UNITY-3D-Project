# SungJun — Handover Pointer

이 파일은 SungJun 명의로 진행한 작업 이력의 목차이다.
새 세션을 시작할 때 이 파일을 먼저 읽고 최신 항목부터 맥락을 파악한다.
작업을 마무리할 때는 오늘 날짜 폴더에 새 문서를 만들고, 아래 목록 맨 위에 한 줄 항목을 추가한다.

## 변경 이력 (최신순)
- **2026-09-28** — 변경 문서를 SungJun 폴더로 통합: [정리 기록](./2026-09-28/document-consolidation.md)
- **2026-09-28** — 플레이어 생성·능력치 복원·HUD 연결 흐름 정리: [스폰 흐름](./2026-09-28/player-spawn-flow.md)
- **2026-09-27** — EnterScene 전환 차단 원인: GameManager SO 직렬화와 이전 필드명 확인: [씬 전환 진단](./2026-09-27/enter-scene-transition-diagnosis.md)
- **2026-09-27** — 능력치 변경 콜백 등록·해제 참조 이름 변경: [콜백 이름 변경](./2026-09-27/player-hud-attribute-handler-rename.md)
- **2026-09-27** — HUD 갱신 함수 4개와 호출부 이름 변경: [함수명 변경](./2026-09-27/player-hud-update-method-rename.md)
- **2026-09-27** — HUD 능력치 필드명 변경 및 Inspector 참조 보존: [변수명 변경](./2026-09-27/player-hud-attribute-field-rename.md)
- **2026-09-26** — Player 자식 CharacterController 배치 및 Motor 참조 문제 확인, 프리팹 설정 안내: [Controller 배치 오류](./2026-09-26/player-child-character-controller.md)
- **2026-09-26** — GameManager의 Player SO는 저장할 능력치 이름 목록용임을 설명: [Player SO 참조 용도](./2026-09-26/game-manager-player-so-purpose.md)
- **2026-09-26** — 박스 접촉으로 Home/Lobby 왕복 및 능력치 저장 테스트 스크립트 추가, 빌드 성공: [씬 전환 테스트 박스](./2026-09-26/test-scene-transition-box.md)
- **2026-09-26** — 지정한 미사용 이벤트 1개·메서드 3개만 제거, 빌드 성공: [미사용 보조 코드 제거](./2026-09-26/remove-unused-player-lifecycle-helpers.md)
- **2026-09-26** — 현재 UI 수정 반영과 삭제 검사 제거 확인, 빌드 오류 0개: [현재 UI 코드 재검토](./2026-09-26/ui-final-current-code-review.md)
- **2026-09-26** — 플레이어는 씬 전환 시 제거되는 범위 확인, 별도 삭제 감지 제안 정정: [씬 전환 범위 정정](./2026-09-26/ui-scene-transition-scope.md)
- **2026-09-26** — 플레이어 컴포넌트 추가 불가 제약 반영, GameManager의 삭제 감지 방식 안내: [플레이어 수정 없는 삭제 감지](./2026-09-26/ui-player-no-component-guide.md)
- **2026-09-26** — UI 생명주기 보완 3종의 파일별 적용 코드 및 검증 안내 작성: [UI 생명주기 적용 가이드](./2026-09-26/ui-lifecycle-implementation-guide.md)
- **2026-09-26** — Bind 구독 해제 수정 반영 확인 및 UI 생성·소멸 경로 재검토: [UI 생명주기 재확인](./2026-09-26/ui-lifecycle-recheck.md)
- **2026-09-26** — UI 연결 순서와 Bind 구독 해제 누락 원인 및 수정 방법 안내: [Bind 구독 해제 안내](./2026-09-26/ui-bind-unsubscribe-guide.md)
- **2026-09-26** — UI 3종 로직 재검토, 구독 해제 순서 및 View 검증 문제 확인, 빌드 성공: [UI 로직 검토](./2026-09-26/ui-logic-review.md)
- **2026-09-26** — 현재 플레이어 조회와 HUD 연결 2~4번 적용 방법 안내: [HUD 현재 플레이어 안내](./2026-09-26/player-hud-current-player-guide.md)
- **2026-09-26** — Singleton 주석과 Unity API 동작 대조 검토: [싱글톤 주석 검토](./2026-09-26/singleton-comment-review.md)
- **2026-09-26** — PlayerSpawner 변경 커밋 771f3c6 생성: [플레이어 스포너 커밋](./2026-09-26/player-spawner-commit.md)
- **2026-09-26** — PlayerState 및 GameManager 변경 커밋 5460c98 생성: [플레이어 Attribute 커밋](./2026-09-26/player-attribute-commit.md)
- **2026-09-26** — Codex 명의 HUD 기록을 SungJun 폴더로 통합: [인수인계 기록 통합](./2026-09-26/handover-folder-consolidation.md)
- **2026-09-26** — 실제 HP·배터리·총 게이지 HUD 연결 검토: [HUD 연결 검토](./2026-09-26/player-hud-wiring-review.md)
- **2026-09-25** — 플레이어 HUD MVP 스크립트 3종 추가: [플레이어 HUD MVP 기반](./2026-09-25/player-hud-mvp-foundation.md)
- **2026-09-24** — `StageBase`를 상속하는 앱별 스테이지 구현체 다섯 개 추가: [앱별 스테이지 구현체 추가](./2026-09-24/stage-implementations.md)
- **2026-09-24** — 현재 StageManager의 실행 흐름 누락 요소와 다음 구현 우선순위 검토: [StageManager 보완 요소 검토](./2026-09-24/stage-manager-gap-review.md)
- **2026-09-24** — `Core.Stage` 네임스페이스에 스테이지 코어 타입 다섯 개의 최소 골격 구성: [스테이지 코어 타입 골격 구성](./2026-09-24/stage-core-foundation.md)
- **2026-09-24** — 확정 기획을 기준으로 앱·스테이지·게임플레이를 분리한 데이터 주도 관리 구조 제안: [스테이지 관리 아키텍처 논의](./2026-09-24/stage-management-architecture.md)
- **2026-09-24** — StageResult/StageType 초기 구조를 검토하고 명명·기본값·불변성 보완점을 정리: [StageResult / StageType 구조 검토](./2026-09-24/stage-result-structure-review.md)
- **2026-09-09** — Attribute 시스템: 상속+리플렉션 기반(`AttributeSetBase`)에서 SO 데이터 주도 방식(`AttributeSet`)으로 전면 전환, 다수의 리뷰 라운드를 거쳐 버그 수정: [Attribute 시스템 SO 전환](./2026-09-09/attribute-system-so-transition.md)

