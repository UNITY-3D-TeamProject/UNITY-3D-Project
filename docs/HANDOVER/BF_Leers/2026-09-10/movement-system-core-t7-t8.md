# Movement System 코어 T7 확인 / T8 intent 정리

## 작업 요약
- 이전 세션([T3-T6 문서](../2026-09-09/movement-system-core-t3-t6.md))에서 미해결로 남았던 T6 PlayMode 슬라이딩 이슈를 이어서 확인한 뒤, 사용자 판단에 따라 보류하고 T7~T8로 진행했다.
- **T7**: `Assets/_Project/Scripts/System/IMoveProvider.cs`(구식 스텁) 삭제 예정이었으나, 조사 결과 해당 파일이 이미 저장소에 존재하지 않고(`Glob`/`Grep` 모두 매치 없음) git 히스토리에도 추가된 적이 없음. 참조도 전혀 없어 별도 삭제 작업 없이 T7을 완료로 간주.
- **T8**: `docs/intent/intent-001-movement-system-core.md`에 진행 상황을 기록. 단, 문서에 남아있던 열린 질문 중 "팀의 새 네임스페이스 기준" 확정 여부를 사용자에게 재확인한 결과 **여전히 미정**(`docs/coding-convention.md`도 변경 없음)이라는 답을 받아, intent 상태를 `resolved`로 전환하지 않고 `open`으로 유지했다. `docs/intent/clear/`로도 이동하지 않았다.

## T6 슬라이딩 이슈 처리 경과
- 사용자가 "테스트 해본결과 미끄러지듯 이동하는게 눈에 보이는 정도"라고 보고.
- 코드 조사 결과 `SOMovementConfig` 기본값(Acceleration 20 / Deceleration 25, MaxSpeed 5)은 가속 완료까지 약 0.25초로 슬라이딩처럼 체감될 만큼 느리지 않음 — 설정값 문제는 배제.
- `CharacterControllerMotor.cs`에 이동 방향으로 Transform을 회전시키는 로직이 전혀 없음을 확인(코드 원문 검토 완료) — 오브젝트가 항상 같은 방향을 바라본 채 위치만 이동하면 "아이스스케이팅"처럼 보일 수 있어 가장 유력한 원인으로 지목.
- 회전 여부를 직접 확인하는 질문에는 사용자가 답하지 않고, "애초에 큰 플랫폼 하나 두고 플레이어에 테스트해본 것"이라며 다음 단계로 넘어가기로 결정. **재현 조건/근본 원인은 확정되지 않은 채 보류 상태**.

## 결정 사항
- T6 슬라이딩 이슈는 미해결 보류. 코어(모터) 범위인지 Player 조작감(회전 연출) 담당자 범위인지도 미확정 — 새 intent 문서는 만들지 않고 이 핸드오버 문서와 intent-001에만 기록.
- intent-001은 네임스페이스 미정 상태가 남아있어 `resolved` 전환/`clear/` 이동 보류. CLAUDE.md 6장 규칙(열린 질문 남아있으면 임의 추측 금지)에 따름.

## 현재 상태 및 이슈
- T1~T7(사실상) 완료, T8은 문서 상태 갱신만 하고 `open` 유지.
- T6 슬라이딩 원인 미확정 — 다음 후보: `CharacterControllerMotor.Tick()`에 이동 방향 회전 로직 부재(가장 유력, 미검증), 또는 대각선 방향 전환 시 `MoveTowards` 기반 가속/감속 선택 로직(속도 크기만 비교, 방향 미고려)으로 인한 곡선 전환.
- 네임스페이스 팀 기준 미확정 상태 지속.

## 다음 할 일
- Player 오브젝트 실제 구현(구체 Provider) 진행 시, T6 슬라이딩 현상이 재현되는지, 회전 로직 부재가 원인이 맞는지 실제로 확인 필요. 필요하면 `CharacterControllerMotor.cs`에 이동 방향 회전(`transform.forward`를 목표 방향으로 보간 등) 로직 추가를 검토.
- 네임스페이스 팀 기준이 확정되면 `docs/coding-convention.md` 갱신 후 `Scripts/System/` 전체 리팩터링, intent-001 최종 `resolved` 처리 및 `docs/intent/clear/` 이동.
- 새 세션에서는 이 문서와 `docs/intent/intent-001-movement-system-core.md`를 먼저 읽고 이어서 진행할 것.

## Phase 3(구현) 종료 확인
- `/start-feature` 파이프라인 기준 Phase 3(Incremental Implementation)의 코드 티켓(T1~T7, `[LOGIC]`/`[UNITY]` 태그 대상)이 모두 구현 완료됨. T8은 태그 없는 문서화 티켓이라 Phase 3 구현 순서 자체에는 포함되지 않으며, 위에서 별도로 처리(intent `open` 유지)했으므로 Phase 3은 이걸로 마무리로 간주한다.
- 남은 `[USER]` 성격 작업(예: Player 실제 프리팹에 컴포넌트 부착, `.asset` 인스턴스 세부 조정 등)은 이미 사용자가 T6 검증 과정에서 진행한 바 있고, 추가 `[USER]` 티켓은 spec.md 상 별도로 남아있지 않음.
- **다음 단계**: `/start-feature` Phase 4(1차 정적 리뷰, Unity 배치모드 컴파일 검증) → Phase 5(`/review-feature`로 심층 아키텍처 리뷰) → Phase 6(spec 저장 및 세션 분리) 순서로 진행 가능. 단, T6 슬라이딩 원인 미확정과 네임스페이스 팀 기준 미정 두 가지는 Phase 4/5로 넘어가더라도 여전히 열린 이슈로 남아있음에 유의.
