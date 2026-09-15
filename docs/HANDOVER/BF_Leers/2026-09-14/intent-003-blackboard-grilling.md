# intent-003 블랙보드 설계 grilling 및 문서 반영

## 1. 작업 요약 및 방법
`docs/intent/intent-003-enemy-ai.md` 열린 질문 3번("블랙보드")에 이미 적혀 있던 혼합 전략(패키지 블랙보드 + 컴포넌트 직접 조회)을 `mattpocock-skills:grilling` 스킬로 검증했다. Unity Behavior 패키지 1.0.16 소스(`Library/PackageCache/com.unity.behavior@1b9c0eccba9c/`)를 직접 읽어 기존 근거와 대조하고, "누가 언제 블랙보드를 쓰는가"가 비어 있던 부분을 질문-답변으로 채웠다.

방법: 코드는 건드리지 않고 문서만 4곳 수정했다 — ① "판단부와 실행부 분리" 블랙보드 문단(41~45행), ② "전투 적 트리" 다이어그램(53~69행), ③ "예외 처리"의 재진입 초기화 항목, ④ 열린 질문 3번 해결 문단에 "보완 (2026-09-14, 소스 대조)" 절 추가.

## 2. 결정 사항
- **감지는 Condition으로**: "Sense Player" Action 대신 `Is Player Visible` 커스텀 Condition이 `Priority Abort`/`Restart`에서 매 틱 `EnemySensor`에 직접 묻는다. 근거: `Try In Order`(`SelectorComposite.cs:34`)는 반응형이 아니라 자식이 Running이면 기다리기만 하므로, Action으로는 대기·추격 중 재평가가 안 됨.
- **"기본 노드 재사용" 근거 철회**: `Check Distance`의 기준값이 `BlackboardVariable<float>`(`CheckDistanceCondition.cs:19`)라 전투/SO 값을 그래프에 적거나 복사해야 하고, "전투 수치 중복 정의 금지" 제약에 걸림. 거리·범위 판정은 커스텀 Condition(`In Attack Range` 등)이 컴포넌트에 직접 묻는 것으로 통일.
- **Condition은 부수효과 없음** 규칙 추가: 매 틱 평가되므로 블랙보드에 쓰지 않는다. 기록은 전용 Action(`Acquire Target`/`Clear Target`/`Record Last Seen`/`Init Home`)이 판단이 바뀌는 순간에만 한다. 근거: 값 변경 판정이 동등 비교(`BlackboardVariable.cs:144`)라 매 프레임 바뀌는 값을 계속 쓰면 변경 이벤트가 매 틱 발생.
- **프로토타입 블랙보드 변수 확정**: `Self`, `Target`(Transform), `HomePosition`(리쉬·재진입용), `LastSeenTime`(추격 포기 판정용). `LastSeenPosition`은 수색 기능이 생길 때 추가(미로 AI 보류 항목과 연결).
- **Target 획득**: `EnemySensor.Awake()`에서 태그로 플레이어 Transform을 1회 탐색. 프리팹이 씬 오브젝트를 인스펙터로 참조할 수 없어서 결정.
- **HomePosition 덮어쓰기 방지**: 그래프를 `Start → Sequence: [Init Home] → Repeat(본 트리)`로 짜고, Abort/Restart는 `Repeat` 루프 안에만 배치.
- **재진입 초기화**: 프로토타입은 파괴 후 재생성(새 블랙보드 자동 생성). 최적화 단계에서 오브젝트 풀링으로 전환 예정 — 전환 시 `BehaviorGraphAgent.Restart()`가 블랙보드를 초기화하지 않는다는 점(`BehaviorGraphAgent.cs:719`, 새 블랙보드는 `Init()`에서만 생성 `:368`)과 `Init Home`이 루프 밖이라 재실행되지 않는다는 점을 주의사항으로 문서에 남겨 둠 — 적 쪽 `ResetState()`에서 Target/LastSeenTime 초기화 + HomePosition 재기록 + `Restart()`를 직접 처리해야 함.

## 3. 현재 상태 및 이슈
- 문서 수정만 완료, 아직 커밋하지 않음(`git status`에 M으로만 표시될 것).
- 코드(커스텀 Condition/Action 클래스, BT 그래프 에셋)는 아직 없음 — 이번 작업은 설계 문서 보완까지만.
- 열린 질문 3번은 "해결" 상태 유지(이미 resolved로 표시돼 있었고, 이번엔 그 해결 내용을 보완만 함).
- intent-003의 나머지 열린 질문(4~6번: SO 네이밍, 전투 인터페이스, 폴더/네임스페이스)은 미해결로 남아 있음.

## 4. 다음 할 일 (Next Steps)
1. `docs/intent/intent-003-enemy-ai.md`의 수정된 4곳을 커밋(브랜치/커밋 컨벤션은 `docs/commit-convention.md` 확인 후 `commit` 스킬 사용 권장).
2. 열린 질문 5번(전투 인터페이스, `IDamageable` 등)을 전투 시스템 담당 팀원과 합의 — 이게 정해져야 `EnemyCombat`/`EnemyHealth` 실제 구현이 가능.
3. 열린 질문 6번(폴더/네임스페이스 — `Assets/_Project/Scripts/Enemy/AI/`, `Project.Enemy.AI` 등)을 `docs/coding-convention.md` 기준으로 확정.
4. 이번에 확정된 블랙보드 설계(변수 4개, Action 4개, Condition 최소 2개: `Is Player Visible`, `In Attack Range`)를 바탕으로 커스텀 노드 C# 클래스 골격을 작성하고, Unity Behavior 그래프 에셋으로 실제 전투 적 트리를 구성.
5. `EnemySensor`/`EnemyMotor`/`EnemyCombat`/`EnemyHealth` 컴포넌트 실제 구현 (intent-003 34~39행 API 계약 참고, `CharacterMotor.Move(direction, speed)` + `steeringTarget` 기준 회전 사용 — intent-001/002/003 스파이크 검증 완료된 이동 방식).
