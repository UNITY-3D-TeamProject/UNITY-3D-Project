# HUD PlayerFacade 전환 · 스킬 쿨 UI · 낙하 Effect SO (2026-10-10)

관련 intent: [intent-018](../intent/intent-018-hud-facade-skill-cooldown.md)

## 1. 작업 요약 및 방법
- `PlayerHudView`, `UIManager`, `StageManager`가 CP949로 저장돼 있어 먼저 UTF-8(BOM 없음, CRLF 유지)로 변환했다. 변환하지 않고 도구로 저장하면 한글 주석이 깨진다.
- `GameManager.OnPlayerSpawned`를 `Action<PlayerFacade>`로 변경하고 `CurrentPlayerState`(AttributeSet 역조회 브리지)를 삭제했다.
- `PlayerHudPresenter`를 `PlayerFacade` 기반으로 재작성했다. `OnAttributeChanged`로 HP/배터리/Heat를 갱신하고, `OnSkillUsed(이름, 쿨타임)`로 Roll/SpawnVehicle/Scan 쿨타임 종료 시각을 기록해 `Update`에서 남은 비율을 View에 전달한다.
- `PlayerHudView`에 쿨 슬라이더 3개와 `SetRollCooldown/SetBluetoothCooldown/SetScanCooldown`을 추가했다.
- `UIManager`의 설정창 입력을 `PlayerFacade.OnOpenSettingsRequested/OnCloseSettingsRequested`, `SwitchToUIInput/SwitchToPlayerInput`으로 교체했다.
- `StageManager`의 `_fallDamage`를 `SOAttributeEffect _fallEffect`로 바꿔 `Apply(_playerFacade.EffectTarget)`로 적용한다. 전용 에셋 `ScriptableObjects/Map/Effect_Fall_Damage10.asset`(CurrentHp Subtract 10)을 만들어 `Prefabs/System/StageManager.prefab`에 연결했다.

## 2. 결정 사항
- 총 게이지 = Heat, 블루투스 = `SpawnVehicle`, 낙하 데미지 = Effect SO 1개 + `Apply`.
- 스킬 남은 시간은 Facade에 조회 API가 없어 HUD가 직접 계산한다(Facade 변경 없음).
- 낙하 `OnHit`은 값 감소 → `CharacterCombat.Health` → `CombatMediator` → `CharacterMediator` → `PlayerFacade.OnHit` 경로로 발생한다. `CombatMediator._healthValueKey`가 `CurrentHp`인지 Player 프리팹에서 Play로 확인 필요.

## 3. 현재 상태 및 이슈
- Unity MCP 도구가 없어 **컴파일은 아직 검증하지 못했다.** 에디터에서 콘솔 오류 확인 필요.
- 쿨 슬라이더 3개가 HUD에 아직 없다. `Assets/_Project/Prefabs/UI/Canvas.prefab`에 슬라이더를 만들고 `PlayerHudView`의 Skill Cooldowns 필드 3개에 연결해야 한다. 연결 전에는 View가 null 슬라이더를 무시하므로 오류는 나지 않는다.
- `PlayerHudPresenter`의 이전 `_playerCurrentAttributeSet` 직렬화 값과 주석 처리돼 있던 `Awake` 블록(구 AttributeSet 참조)은 제거됐다.
- 변경 전 `StageManager.prefab`의 `_fallDamage: 10`은 `_fallEffect`로 대체됐다. `Assets/Test/...KSJ_Home.unity`에도 `_fallDamage`가 남아 있으나 Git 제외 폴더라 손대지 않았다.
- `PlayerFacade._playerInput`이 Player 프리팹에서 연결/탐색되는지 확인 필요(설정창 입력이 Facade 경유로 바뀜).
- 커밋은 하지 않았다. 인코딩 변환은 별도 커밋으로 분리하는 편이 좋다.

## 4. 다음 할 일 (Next Steps)
1. Unity에서 컴파일 오류 0 확인.
2. HUD Canvas에 쿨 슬라이더 3개 추가·연결(채워질수록 쿨이 남은 상태: 1 = 방금 사용).
3. Play 검증: 스폰 직후 HP/배터리/Heat, 구르기·스캔·블루투스 쿨, 낙하 시 HP 감소와 체크포인트 복귀, 사망 후 재스폰 시 HUD 재연결, 설정창 열기/닫기.
4. 검증 끝나면 intent-018을 resolved로 처리하고 `clear/`로 이동.
