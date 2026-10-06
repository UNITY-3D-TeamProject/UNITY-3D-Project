# 몬스터 Init Data 누락 참조 확인

## 작업 요약
- 몬스터 `AttributeSet`의 `Init Data`가 Missing으로 보이는 이유를 코드와 프리팹 직렬화 값에서 확인했다.

## 방법/접근
- `AttributeSet.Awake`가 `_initData`를 읽고, null이면 예외를 던지는 흐름을 확인했다.
- `Melee.prefab`의 `_initData`가 현재 프로젝트에 존재하지 않는 GUID `5803e75645fab3a45ba0f7dee52d840f`를 가리키는 것을 확인했다.
- `SOAttributeData_Enemy` 에셋의 GUID가 `bd5ae6f518272894e9859378de60c065`인 것을 확인했다.

## 변경된 파일
- 코드와 에셋 변경 없음. 이 기록 문서와 `HANDOFF_POINTER.md`만 추가/갱신했다.

## 결정 사항
- 사용자가 Unity Inspector에서 몬스터 프리팹의 `Attribute Set > Init Data`에 `SOAttributeData_Enemy`를 다시 지정하도록 안내한다.

## 현재 상태 및 이슈
- `SOAttributeData_Enemy`의 `CurrentHp` 기본값은 0이다. 생존 상태로 시작하려면 몬스터 스포너에 `CurrentHp`를 설정하는 Effect를 연결하거나 기본 SO 값을 조정해야 한다.
- 인스펙터 설정 변경과 Play Mode 동작은 수행/검증하지 않았다.

## 다음 할 일
- 프리팹의 누락 참조를 재설정하고 Apply한 뒤, 생성 시 예외가 사라지고 초기 체력이 의도한 값인지 Play Mode에서 확인한다.
