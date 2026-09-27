# PlayerHudPresenter 변수명 변경

- 작업 요약 및 방법: `_attributeSet`의 모든 필드 참조를 `_playerCurrentAttributeSet`으로 변경했다. 이미 바뀌어 있던 선언은 유지했다.
- 결정 사항: `FormerlySerializedAs("_attributeSet")`를 추가해 기존 Inspector 참조를 보존한다. 동작과 메서드명은 유지한다.
- 현재 상태 및 이슈: 이전 변수명이 호환성 속성 외에 남지 않음을 검색으로 확인했다. Unity 컴파일은 실행하지 않았다. 기존 작업 파일에 공백 경고가 있다.
- 다음 할 일: Unity에서 컴파일 및 HUD 표시 확인.