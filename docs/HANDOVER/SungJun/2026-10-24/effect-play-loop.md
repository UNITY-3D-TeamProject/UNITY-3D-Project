# EffectManager PlayLoop 분리 (intent-074)

## 작업 요약 및 방법
- `EffectManager`에 `PlayLoop(data, position, rotation, parent = null)`을 추가했다. `Play`·`PlayLoop`는 private `PlayEffect(..., bool isLoop)`를 함께 쓴다.
- 인스턴스를 만들 때(`CreateInstance`) 자식 파티클의 프리팹 Loop 값을 `_prefabLoopsByInstance`에 기억한다. 재생할 때마다 `main.loop = isLoop || 원래 값`으로 정해서, 같은 풀 인스턴스를 Play·PlayLoop가 번갈아 써도 맞게 재생된다.
- 루프 여부는 `SActiveEffect.IsLoop`에 담고 `Stop`이 이 값을 본다(Play는 즉시 반납, PlayLoop는 방출 중단 후 `StopFadeDuration` 뒤 반납).
- `SOEffectData`에서 `_isLoop`·`IsLoop`를 지우고 Header를 `Play Only (PlayLoop는 무시)`/`PlayLoop Only (Play는 무시)`로 바꿨다.
- 개인 시험 코드(Assets/Test, Git 미포함): `TestEffectCaller.ToggleAtPoint/ToggleOnPlayer`, EffectManagerValidation Runner의 루프 호출을 `PlayLoop`로, Builder의 `_isLoop` 설정 삭제.

## 결정 사항
- 사용자 선택 A안: 루프는 부르는 쪽이 `PlayLoop`로 정하고, 매니저가 자식 파티클 Looping까지 켠다. AudioManager(`PlaySfx`/`PlaySfxLoop`)와 사용법을 맞춤.
- SO의 런타임 상태·원본 프리팹은 바꾸지 않는다. 원래 Loop 값은 매니저가 인스턴스별로 기억한다.

## 현재 상태 및 이슈
- Unity 재컴파일 에러 0건. 미리보기 씬 eval로 Loop가 섞인 임시 프리팹(부모 off·자식 on)에서 Play `0,1` → PlayLoop `1,1` → Play `0,1` 복구, 반납·중복 Stop 동작, 원본 값 유지 확인.
- 알려진 한계: PlayLoop는 자식 파티클을 전부 루프로 만들므로 "시작 때 한 번만 터지는" 자식도 반복된다.
- 기존 SO 에셋에 `_isLoop` 직렬화 값이 남아 있지만 쓰이지 않는다.
- TestScene Play에서 루프 발판 직접 확인·시각 확인은 안 함. 커밋 안 함.

## 다음 할 일 (Next Steps)
- TestScene의 EffectManagerTest 루프 발판 2개(ToggleAtPoint·ToggleOnPlayer)를 Play로 확인.
- 커밋 (`feature/...` 브랜치, docs/commit-convention.md).
