# 스테이지 코어 타입 골격 구성

## 작업 요약
- 스테이지 시스템의 공통 타입 다섯 개를 `Core.Stage` 네임스페이스 아래에 구성했다.

## 방법/접근
- 기존 `StageManager`는 로직을 유지하고 네임스페이스만 변경했다.
- 기존 `StageType`과 `StageResult`는 프로젝트 명명 컨벤션에 맞게 `EStageType`, `SStageResult`로 이름을 변경했다.
- 앱 스테이지와 내부 Phase 구현을 위한 최소 추상 베이스 클래스를 추가했다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/Stage/StageManager.cs`
- `Assets/_Project/Scripts/Core/Stage/EStageType.cs`
- `Assets/_Project/Scripts/Core/Stage/SStageResult.cs`
- `Assets/_Project/Scripts/Core/Stage/StageBase.cs`
- `Assets/_Project/Scripts/Core/Stage/StagePhaseBase.cs`

## 결정 사항
- 공통 네임스페이스는 `Core.Stage`를 사용한다.
- `StageBase`는 앱별 스테이지의 시작·종료 계약을 제공한다.
- `StagePhaseBase`는 Phase 진입·종료와 완료 이벤트의 공통 구현을 제공한다.
- 별도의 `IStagePhase`는 현재 필요하지 않아 만들지 않았다.

## 현재 상태 및 이슈
- 요청된 타입의 최소 골격만 존재하며 실제 스테이지 순서 및 앱 로직은 아직 구현하지 않았다.
- enum 멤버의 기존 철자(`Turtorial`, `Filepp`)는 요청 범위 밖이라 유지했다.
- Unity Editor 컴파일 검증은 수행하지 못했다.

## 다음 할 일
- `StageManager`의 전체 스테이지 순서 및 전환 책임을 구현한다.
- 첫 앱 스테이지와 Phase 구현체를 추가한다.
