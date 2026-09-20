# BF_Leers — Handover Pointer

이 파일은 BF_Leers 명의로 진행한 작업 이력의 목차이다.
새 세션을 시작할 때 이 파일을 먼저 읽고 최신 항목부터 맥락을 파악한다.
작업을 마무리할 때는 오늘 날짜 폴더에 새 문서를 만들고, 아래 목록 맨 위에 한 줄 항목을 추가한다.

> ⚠️ 아래 2026-09-09 / 2026-09-10 항목의 링크 3개는 `chore : 초기화` 커밋으로 대상 파일이 삭제되어 **전부 깨져 있다**. 또한 2026-09-10 항목은 intent-001을 `open`으로 적고 있으나 실제로는 `resolved`다(`docs/intent/clear/`로 이동 완료).

## 변경 이력 (최신순)
- **2026-09-20** — `CharacterCombat.ReceiveHit` 이벤트 순서 보정: `OnHit` 발사를 `Effect.Apply` 앞으로 옮겨, 팀원의 `AttributeSet` 옵서버 배선이 들어와도(`Apply` 도중 `NotifyAttributeChanged`가 끼어들어도) `OnHit → OnDeath` 순서가 항상 보장되게 함. 5-b(비-피격 경로의 피격 체크)는 `SHitInfo`를 만들 수 없어 이번 범위에서 제외. intent-004 해결 기록에 추가 반영. **Unity 컴파일/PlayMode 미확인 — 다음 세션 최우선**: [OnHit/OnDeath 순서 역전 방지](./2026-09-20/combat-hit-order-fix.md)
- **2026-09-20** — 전투(Combat) 컴포넌트 통신 채널 구현: `Assets/_Project/Scripts/Combat/`에 `IHitReceiver`/`SHitInfo`/`CharacterCombat` 신규 작성. 피격(`ReceiveHit`)은 캐릭터 간 직접 접촉점으로 중재자를 거치지 않고, 비-피격 HP 변화용 `NotifyAttributeChanged` 입구를 따로 두어 `_isDead` 플래그로 중복 사망 이벤트를 막음. `AttributeSet`/`SOAttributeData` 등 구체 타입은 코드에서 참조하지 않음(이식성 원칙). intent-004 즉시 resolved 처리. **Unity 컴파일/PlayMode 확인 미완 — 다음 세션 최우선**: [Combat 컴포넌트 채널 구현](./2026-09-20/combat-component-channel.md)
- **2026-09-17** — PlayMode 테스트 실행 결과 분석: 지난 세션에 남겨둔 KnownBug 후보 5개(6개 테스트 케이스)가 전부 실제 버그로 확정됨. 원인은 `CharacterMotor.Direction` 정규화 시 y성분 혼입, `Move()`의 비활성 컨트롤러 미체크, teleport 시 위치 되돌림, `TransformMotor.DeltaThisFrame` 프레임 리셋 누락(플랫폼 정지 후 탑승자 밀림과 동일 원인), 음수 speed 시 역방향 이동. 아직 수정은 안 함, 다음 세션에서 수정 여부/우선순위 결정 필요: [PlayMode 테스트 실행 결과](./2026-09-17/test-run-results.md)
- **2026-09-17** — 이동 로직 코드 리뷰(Standards/Spec) 반영: 코딩 컨벤션 문서의 네임스페이스 규칙을 실제 관례(`Project.` 접두사 없음)에 맞게 수정, `FixedUpdate`/`Direction`·`Speed` 설계는 유지 결정하고 관련 주석·기능 문서·intent 문서를 갱신. 이동 스크립트를 `Movement.asmdef`로 분리하고 `Assets/Test/Movement/`에 PlayMode 자동 테스트(KnownBug 후보 6종 포함) 추가. **Unity 컴파일/테스트 실행 미확인 — 다음 세션 최우선**: [이동 로직 리뷰 반영 + PlayMode 테스트](./2026-09-17/movement-review-and-tests.md)
- **2026-09-14** — 회의 결과(플레이어 마우스 회전)로 `CharacterMotor`에서 회전 제거, `SOMovementConfig` 삭제하고 중력은 Motor `[SerializeField] _gravity`로 이동. 컨트롤러는 `Move(방향, 속도)`만 호출. **Unity 컴파일/PlayMode 확인 미완, 미커밋**: [CharacterMotor 회전 제거 + SO 삭제](./2026-09-14/motor-rotation-removal.md)
- **2026-09-12** — 이동 코어 범위 재정렬: `WaypointMover` → `TransformMotor`로 축소(경로 산출·회전·정지 제거), `CharacterMotor`에서 탑승 로직·LayerMask 제거하고 `AddExternalDisplacement()` 신설. 코어에서 뺀 것은 전부 동작 스니펫으로 문서에 보관(웨이포인트 순회 / 회전 / 탑승). **Unity Editor 검증 미완 — 다음 세션 최우선**, 특히 슬라이딩 회귀 테스트: [Movement System 코어 범위 재정렬](./2026-09-12/movement-core-scope-cleanup.md)
- **2026-09-10** — T7(스텁 삭제, 대상 파일 이미 부재로 완료 처리) 확인, T8 intent 정리 시도했으나 네임스페이스 열린 질문 미해결로 intent-001 `open` 유지, Phase 3(구현) 종료 확인. T6 슬라이딩 이슈는 원인 미확정 채 보류. 다음은 Phase 4(정적 리뷰): [Movement System 코어 T7-T8 정리](./2026-09-10/movement-system-core-t7-t8.md)
- **2026-09-09** — Movement System 코어 T3(IMoveMotor)~T6(CharacterControllerMotor) 구현 완료, T6 PlayMode 수동 검증 중 문제 발생(미해결) — 다음 세션 최우선 디버깅 대상: [Movement System 코어 T3-T6 구현](./2026-09-09/movement-system-core-t3-t6.md)
- **2026-09-09** — Movement System 코어 T1(SMoveIntent/IMoveIntentProvider), T2(SOMovementConfig) 구현 완료, T3부터는 새 세션에서 진행 예정: [Movement System 코어 T1-T2 구현](./2026-09-09/movement-system-core-t1-t2.md)
