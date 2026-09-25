# 앱별 스테이지 구현체 추가

## 작업 요약
- `StageBase`를 상속하는 `Tutorial`, `FeedApp`, `FileApp`, `SecurityApp`, `LiveApp` 스크립트를 추가했다.

## 방법/접근
- 각 구현체가 자신의 `EStageType`을 반환하도록 구성했다.
- 앱별 구체 동작이 확정되지 않아 `StartStage`와 `EndStage`는 빈 골격으로 유지했다.
- 클래스명과 타입명이 일치하도록 기존 enum 오탈자를 수정하고 `LiveApp`을 추가했다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/Stage/EStageType.cs`
- `Assets/_Project/Scripts/Core/Stage/Tutorial.cs`
- `Assets/_Project/Scripts/Core/Stage/FeedApp.cs`
- `Assets/_Project/Scripts/Core/Stage/FileApp.cs`
- `Assets/_Project/Scripts/Core/Stage/SecurityApp.cs`
- `Assets/_Project/Scripts/Core/Stage/LiveApp.cs`

## 결정 사항
- 앱별 스테이지 구현체와 `EStageType` 멤버는 동일한 이름을 사용한다.

## 현재 상태 및 이슈
- 기존 Unity 생성 프로젝트 빌드는 오류 0개였으나, 생성 시점상 신규 스크립트는 프로젝트 파일에 아직 포함되지 않았다.
- Unity Editor에서 프로젝트 파일을 갱신한 뒤 신규 스크립트까지 포함한 컴파일 확인이 필요하다.

## 다음 할 일
- 각 앱의 요구사항이 정해지면 `StartStage`와 `EndStage`에 실제 동작을 구현한다.
