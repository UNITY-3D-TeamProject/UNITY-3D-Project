# 대화·펫 안내 넘기기 키 Space → E

## 1. 작업 요약 및 방법
- `Assets/FID_InputSystem.inputactions` Dialogue 맵 `NextLine` 바인딩을 `<Keyboard>/space` → `<Keyboard>/e`로 변경 (바인딩 id 그대로).
- 넘기기 안내 문구를 `Space ▶ 다음/닫기` → `E ▶ 다음/닫기`로 변경
  - 스크립트 기본값: `DialoguePresenter._nextLineLabel`·`_lastLineLabel`, `PetGuidePresenter._closeLabel`
  - 프리팹 직렬화 값: `Canvas_InGame.prefab`의 위 세 필드 + 안내 TMP 기본 `m_text`
- `UIManager`·`UIInputHandler`·`DialoguePresenter`·`PetGuidePresenter`·`PetGuideView` 주석/툴팁의 "Space"를 "E"로 맞춤.

## 2. 결정 사항
- Player 맵 `Interact`도 E지만, 대화 중에는 Dialogue 맵만 켜지므로 충돌하지 않는다. 맵을 바꾸는 순간 E가 눌려 있어도 Button 액션은 새로 눌러야 발동하므로 같은 입력으로 연달아 넘어가지 않는다.

## 3. 현재 상태 및 이슈
- 재컴파일 0에러. Play로 실제 E 입력 확인은 하지 않음. 커밋 안 함.
- 씬에서 위 라벨을 덮어쓴 오버라이드는 없음(grep 확인).

## 4. 다음 할 일
- TestScene에서 NPC에 E로 말 건 직후 첫 대사가 건너뛰어지지 않는지, 마지막 대사를 E로 닫을 때 상호작용이 다시 발동하지 않는지 Play로 확인.
