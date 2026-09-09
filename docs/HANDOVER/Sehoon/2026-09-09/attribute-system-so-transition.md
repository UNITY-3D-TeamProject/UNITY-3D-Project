# Attribute 시스템 SO 전환

## 작업 요약
- `Assets/_Project/Scripts/Attribute/`의 캐릭터 스탯 관리 컴포넌트를, 상속 기반 리플렉션 방식(`AttributeSetBase`)에서 `ScriptableObject` 데이터 주도 방식(`AttributeSet`)으로 전면 전환했다.
- `/unity-code-review` 스킬로 여러 라운드에 걸쳐 구조 리뷰를 받으며 나온 지적을 순차적으로 반영했다.

## 방법/접근
- **초기 상태**: `AttributeSetBase`(추상 클래스)를 상속한 서브클래스가 `AttributeData` 타입 필드를 선언하면, 부모가 `Awake()`에서 리플렉션(`BindingFlags.DeclaredOnly`)으로 필드를 찾아 자동 등록하는 구조였다.
- 리뷰 과정에서 리플렉션 기반 설계가 "슬롯 정의(상속)"와 "슬롯 값(이후 추가한 SO)"을 이중으로 갖게 되는 문제, `abstract` 제거 시 빈 컴포넌트가 만들어지는 문제를 발견 → 리플렉션을 전면 제거하는 방향으로 전환했다.
- 최종 구조: `SOAttributeData`(이름+초기값 목록, 순수 설정 데이터) → `AttributeSet`(SO를 읽어 `AttributeData` 딕셔너리 구성, Pre/On/Post 이벤트 중계) → `AttributeData`(값 하나 + 변경 콜백 보유, 순수 C#).
- 값 변경에 개입하는 방식은 상속(virtual 훅)과 컴포지션(외부 콜백 등록) 두 가지가 공존하다가, 컴포지션 단일 경로로 정리했다.
- 이벤트 페이로드로 `AttributeData` 객체 참조를 그대로 넘기던 것을 `string attributeName`으로 바꿔, 외부 구독자가 `target.Value = x`나 `target.SetPreValueChangedCallback(...)`로 캡슐화를 뚫는 경로를 차단했다(이를 위해 `AttributeData`에 `Name` 필드 추가).

## 변경된 파일
- `Assets/_Project/Scripts/Attribute/AttributeSetBase.cs` → `AttributeSet.cs`로 리네임(파일/클래스, `.meta` GUID 유지)
- `Assets/_Project/Scripts/Attribute/AttributeData.cs`
- `Assets/_Project/Scripts/Attribute/SOAttributeData.cs` (신규)

## 결정 사항
- `AttributeSetBase`는 더 이상 상속을 전제하지 않는 concrete 클래스(`AttributeSet`)로 간다. 캐릭터별 스탯 구성은 코드가 아니라 `SOAttributeData` 에셋으로 관리한다.
- 값 변경 개입 지점은 `SetPreAttributeChangedCallback` 등 외부 콜백 등록(컴포지션) 하나로 통일한다. 상속 기반 `protected virtual` 훅은 제거했다.
- 이름 기반 쓰기 API(`SetValue(name, value)`)는 이번 범위에서 의도적으로 제외했다 — 추후 Effect 구조체 기반 수정 시스템에서 별도로 다룰 예정.
- `AttributeSet`은 다른 컴포넌트가 `Awake()`에서 곧바로 값을 읽어도 안전하도록 `[DefaultExecutionOrder(-100)]`를 붙였다.
- `GetValue`는 잘못된 이름으로 호출 시 개발 빌드에서는 `Assert`로 잡고, 릴리즈 빌드에서는 예외 없이 `0.0f`를 반환하도록 의도적으로 설계했다(반면 `_initData` 미설정은 `throw`로 확정 실패시킴 — 전자는 개별 호출 단위 오류, 후자는 컴포넌트 전체가 못 쓰게 되는 치명적 오류라 정책을 다르게 뒀다).

## 현재 상태 및 이슈
- 이 3개 파일을 실제로 사용하는 소비 스크립트(`PlayerController` 등)가 프로젝트에 아직 없다. `[DefaultExecutionOrder(-100)]`나 컴포지션 기반 Pre 콜백 흐름이 실전 검증되지 않은 상태다.
- `SOAttributeData` 배열의 이름 중복/공백은 런타임(`Awake`)에서만 걸러진다. 에셋 저장 시점(`OnValidate`)에 바로 잡아주는 장치는 없다.

## 다음 할 일
- 실제 캐릭터(Player/Enemy 등) 컴포넌트를 만들어 `AttributeSet`을 붙여보고, `SOAttributeData` 에셋을 실제로 구성해 실전 검증한다.
- 이름 기반 쓰기(Effect 구조체 기반) 시스템을 설계할 때, `AttributeSet`에 `SetValue`류 API를 추가할지 검토한다.
- `SOAttributeData`에 `OnValidate` 기반 에디트 타임 중복/공백 검증을 추가할지 검토한다.