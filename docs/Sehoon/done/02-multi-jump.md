# 02. 멀티점프

- **우선순위**: 최우선
- **상태**: 완료
- **관련 작업**: [03. 점프 중 대쉬 1회 제약](../03-air-dash-limit.md)

## 목표
`DoubleJump` 를 멀티점프로 바꿔, 정해진 횟수만큼 연속 점프할 수 있게 한다.

## 현재 상황
- 접지 판정: `CharacterMotor.IsGrounded` (`Assets/_Project/Scripts/Movement/CharacterMotor.cs:64`).

## 확정 사항
- `DoubleJump` 를 멀티점프로 변경하고 **클래스 이름도 바꾼다**.
- 멀티점프 높이 = 지상 점프 높이 × 비율. 높이는 속도의 제곱에 비례하므로 `MoveMediator.CommandAirJump` 는 속도에 제곱근을 적용한다 (`JumpPower` 어트리뷰트 현재값 × √비율). `MultiJump` 에 `[SerializeField] float _jumpPowerRatio = 1.0f` 를 두고, 요청 이벤트로 `MoveMediator` 에 넘긴다. (이전의 "절반"은 폐기)
- 멀티점프 가능 횟수를 값으로 가진다. 횟수는 **첫 점프를 포함**한다.
- 역할 분리: `MultiJump` 는 점프 가능 여부를 **판단만** 하고, 실제 점프는 `MoveMediator` 가 한다.
- 지상 점프는 지금처럼 `MoveMediator` 가 바로 한다. **공중일 때만** 중재자를 통해 멀티점프 가능 여부를 확인하고, 가능하면 점프한다.
- `SkillMediator.OnSkillExecuted` 의 `case MultiJump` 는 허용한다 (`case Roll` / `case Fire` 와 같은 패턴).
- 걸어서 떨어진 경우 지상 점프는 **사용한 것으로 간주**한다. `CharacterMotor` 에 이벤트를 추가하지 않는다.
- 횟수 조건은 별도 스크립트 `Skill/Conditions/UseCountCondition.cs` (`_maxCount = 2`, `MaxCount`, `UsedCount`, `Use()`, `ResetCount()`, `IsCanExecute()`). `MultiJump` 가 `ISkillCondition` 을 직접 구현하지 않는다.
- 공중 점프 시 `OnSkillUsed` 는 발행하지 않는다.
- AI 에는 멀티점프 프리팹이 없다. AI 공중 점프 요청은 스킬이 없어 무시된다.
- 별도 intent 문서는 만들지 않는다.
- 금지: `IMoveController`·`AIController` 수정, `SkillMediator` 에 `MultiJump` 필드·조회(`GetComponentInChildren<MultiJump>`, `TryGetSkill<MultiJump>`). (이름은 허용)
- `SkillBase` 에는 특정 상황(착지 등)을 이름에 담은 함수를 두지 않는다.

## 작업 범위 (예상)
- 클래스 이름 변경, 횟수 카운트와 착지 시 초기화, 비율 점프 파워 적용.

## 열린 질문
- [x] 새 클래스 이름 → `MultiJump`. `.cs.meta` GUID(`f0ba3107aebb15a4dbd047f5859fff4a`) 유지. 프리팹 파일명 `MultiJump.prefab`(GUID `3f8d6434a9f925a40b8d5d7f27b2e500` 유지), `_skillName` 도 `MultiJump`.
- [x] 가능 횟수 기본값 → **2** (첫 점프 포함), 인스펙터 노출.
- [x] 첫 점프 담당 → 지상 점프는 `MoveMediator`, 공중 점프만 `MultiJump` 가 판단.
- [x] 걸어서 떨어진 뒤 점프 → 지상 점프를 쓴 것으로 친다. 모터 이벤트 추가 없이 처리.
- [x] 점프 파워 기준 → `JumpPower` 어트리뷰트 현재값 기준. 비율(1.0)은 높이 비율이며 속도에는 제곱근을 적용한다.
- [x] 전용 애니메이션 → 만들지 않는다. 기존 `PlayJump()` 재사용.
- [x] 입력 키 / 쿨타임 / 코스트 → 기존 Jump 키, 쿨타임·코스트 없음. 프리팹의 `CooldownCost` 제거.
- [x] 첫 점프 포함 횟수(2)를 공중 점프만 세는 `UseCountCondition` 에 어떻게 반영하는가 → `_maxCount = 2` 유지. `MultiJump` 가 시작 시와 착지 시 `ResetCount(); Use();` 로 지상 점프를 사용한 상태로 둔다 (걸어서 떨어진 경우도 이것으로 처리).
- [x] 착지 초기화 범위 → 멀티점프만 초기화. `SkillController.ResetSkill(string)` 이 지정한 스킬의 `SkillBase.ResetState()`(virtual, `StopSkill`/`Stop` 과 같은 패턴) 를 호출한다. `SkillBase` 에 착지 같은 특정 상황 이름의 함수는 두지 않는다.
- [x] 공중 점프 요청 경로 → 이벤트·함수 호출로 `SkillController` 까지 가서 컨트롤러가 처리한다 (기존 `TryExecuteSkill(string)` 사용).
- [x] 멀티점프를 고르는 방법 → `SkillMediator` 가 멀티점프 스킬 이름을 안다 (사용자 허용, 2-4 금지 사항 중 이름 부분 해제). `SkillController` 는 범용 그대로.
- [x] `SkillMediator` 를 `SkillName` 기반으로 바꾸는 건 → 이번엔 하지 않는다. `case MultiJump` 타입 분기 유지.

## 완료 기준
- 플레이 모드에서 설정 횟수만큼 점프 가능, 공중 점프 높이는 지상 점프 높이 × 비율, 걸어서 떨어진 뒤에는 공중 점프만 가능, 착지 후 횟수 초기화 확인. 컴파일 에러 없음.
