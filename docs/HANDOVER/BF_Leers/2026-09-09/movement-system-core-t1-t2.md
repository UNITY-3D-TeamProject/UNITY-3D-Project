# Movement System 코어 T1-T2 구현

## 작업 요약
- `/start-feature` 파이프라인으로 Movement System 코어(`spec.md`) Phase 3 구현을 시작해, T1(데이터 계약 정의)과 T2(튜닝 설정 SO)를 완료했다.
- T3(`IMoveMotor` 계약 정의)부터는 사용자가 새 세션(새 창)에서 이어가기로 함.

## 방법/접근
- spec.md는 이전 grill-me 세션에서 이미 확정되어 있었으나, 실제 저장소 상태와 두 가지가 어긋나 사용자에게 확인 후 결정했다:
  - 신규 파일 위치: spec.md 원안(`Scripts/Movement/`) 대신, 기존 스텁이 있던 `Assets/_Project/Scripts/System/`을 재사용.
  - 네임스페이스: `docs/coding-convention.md`는 모든 스크립트에 폴더 반영 네임스페이스를 요구하지만, 팀이 새 네임스페이스 기준을 상의 중이고 아직 문서에 반영되지 않아 `docs/intent/intent-001-movement-system-core.md`에 임시 예외로 기록하고 이번 Movement System 코어 파일들은 네임스페이스 없이 작성 중.
- CLAUDE.md 6장 원칙에 따라 T1 구현 전에 intent 문서(`intent-001-movement-system-core.md`)를 먼저 작성하고 `INTENT_POINTER.md`에 등록했다.
- T2에서 사용자가 "SO 대신 struct로 하면 안 되냐"고 질문 → SO는 참조 타입이라 여러 오브젝트가 하나의 에셋 값을 공유/일괄 튜닝할 수 있고, private + get-only 프로퍼티는 "쓰기 불가"가 아니라 "런타임 코드의 실수 방지"(실제 값 변경은 Inspector가 SerializeField를 통해 함)라는 점을 설명 후 SO 방식 그대로 진행하기로 확인받았다.
- 테스트 인프라: 프로젝트에 asmdef가 전혀 없어 EditMode NUnit 테스트가 실제로 컴파일되지 않는 문제가 있어, 운영 코드에는 asmdef를 추가하지 않고 `Assets/_Project/Scripts/Test/EditMode/`에 테스트 전용 asmdef를 T5(실제 테스트 작성이 명시된 티켓) 시점에 예외로 추가하기로 함 — 아직 만들지 않음.

## 변경된 파일
- `Assets/_Project/Scripts/System/SMoveIntent.cs` (+ `.meta`) — T1
- `Assets/_Project/Scripts/System/IMoveIntentProvider.cs` (+ `.meta`) — T1
- `Assets/_Project/Scripts/System/SOMovementConfig.cs` (+ `.meta`) — T2
- `docs/intent/intent-001-movement-system-core.md` (신규), `docs/intent/INTENT_POINTER.md` (등록)
- `spec.md` (폴더 경로 `Movement/` → `System/`, 네임스페이스 임시 생략 표기, T5 테스트 asmdef 메모 추가)

## 결정 사항
- Movement System 코어 파일은 `Assets/_Project/Scripts/System/`에 작성하며, 네임스페이스는 팀 컨벤션이 `docs/coding-convention.md`에 반영될 때까지 생략한다 (해제 조건: intent-001 참고).
- 테스트 전용 asmdef는 `Assets/_Project/Scripts/Test/EditMode/`에 T5 시점에만 예외로 추가하고, 운영 코드에는 asmdef를 추가하지 않는다.
- 기존 스텁 `Assets/_Project/Scripts/System/IMoveProvider.cs`는 T7(스텁 정리)까지 그대로 남겨둔다 — 타입명이 달라 신규 코드와 충돌하지 않음.
- `.asset` 인스턴스 생성/Inspector 튜닝은 CLAUDE.local.md 0장 규칙에 따라 `[USER]` 작업으로 분리, 이번 세션에서는 만들지 않음.

## 현재 상태 및 이슈
- T1, T2 모두 Unity Editor 콘솔에서 컴파일 에러 없음 확인됨(사용자 확인).
- `SOMovementConfig`의 실제 `.asset` 에셋은 아직 생성되지 않음 — 사용자가 Project 창에서 `Create > Movement > Movement Config`로 생성 필요.
- `docs/coding-convention.md`의 네임스페이스 규칙(3-6장)은 아직 팀의 새 기준으로 갱신되지 않은 상태 — 이 저장소의 다른 스크립트와 Movement System 코어 파일 간 네임스페이스 유무가 당분간 불일치함.

## 다음 할 일
- T3 — Motor 계약 정의: `Assets/_Project/Scripts/System/IMoveMotor.cs` (`interface IMoveMotor { bool UseFixedTick; void Tick(SMoveIntent intent, SOMovementConfig config, float deltaTime); }`), 네임스페이스 없이, 새 세션에서 이 문서와 `docs/intent/intent-001-movement-system-core.md`, `spec.md`를 먼저 읽고 이어서 진행.
- 이후 T4(MovementMotor) → T5(CompositeMoveIntentProvider, 이 시점에 Test asmdef 신설) → T6(CharacterControllerMotor) → T7(기존 스텁 삭제) 순서 유지.
- `docs/coding-convention.md`에 팀의 새 네임스페이스 기준이 반영되면, Movement System 코어 파일 전체를 그 기준에 맞게 리팩터링해야 함(intent-001의 열린 질문).
