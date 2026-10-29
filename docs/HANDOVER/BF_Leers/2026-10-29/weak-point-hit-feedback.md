# 약점 피격 피드백 (AI_TODO2 잡몹-7, intent-089)

## 1. 작업 요약 및 방법
- 문제: 약점을 맞혀도 일반 피격과 화면·소리가 같음. `CharacterMediator.OnCriticalHit(hitPoint)` 는 이미 발행되지만 구독자가 없었다.
- 신규 `Assets/_Project/Scripts/AI/CriticalHitFeedback.cs` (namespace `AI`): `CharacterMediator.OnCriticalHit` 를 OnEnable/OnDisable 로 구독, 맞은 지점에서 `EffectManager.Play(_effect, 지점, 메인 카메라 향함)`·`AudioManager.PlaySfx(_sound, 지점)` 를 한 번. 둘 다 비어 있으면 생략.
- 이펙트 에셋: `YellowLaserImpact` 를 `Assets/_Project/Prefabs/Effect/AI/WeakPointHit.prefab` 으로 복사하고 AudioSource·`SciFiPitchRandomizer`·`Point light`(`SciFiLightFade` 가 0.2초 뒤 Destroy) 제거. `ScriptableObjects/Effect/AI/SOEffectData_WeakPointHit.asset` (duration 1).
- 프리팹 부착(Unity MCP `LoadPrefabContents`/`SaveAsPrefabAsset`, 루트에 `_characterMediator`·`_effect` 연결, `_sound` 비움): Melee·Ranged·Special·BossA·BossB. 각 프리팹 diff 는 컴포넌트 16줄만.
- 검증: 리컴파일 통과(`gate.cjs check recompile`). Play `Map_4_Security` 에서 꺼진 약점 콜라이더를 임시로 켜고 `CharacterCombat.ModifyDamage` 호출 → 적 5마리 모두 명중 시 피해 20·이펙트 +1, 빗나감 피해 10·이펙트 0, 카메라 방향 dot 1, AudioSource·Light 0. 새 콘솔 에러 없음.
- 실행자: Claude 직접. 예외 이유: 이 셸에서 Codex CLI 가 PATH 에 없음(도구 미지원).

## 2. 결정 사항
- 사용자: 일반 명중 피드백은 그대로 두고 덧입히기, 잡몹 + 보스 적용, 이펙트 `YellowLaserImpact`, 소리는 비워 두고 사용자가 찾아 연결.
- 이벤트 시그니처(`Action<Vector3?>`)는 바꾸지 않고 이펙트는 카메라를 향하게 함.
- ThirdParty 이펙트는 `_Project` 로 복사해 풀링에 맞게 정리(기존 `CFXR Hit C` 관례).

## 3. 현재 상태 및 이슈
- 소리 미연결(`_sound` 비어 있음, 의도).
- 보스는 이 씬에 없어 부착만 확인, 보스전 Play 미확인.
- 라이트 플래시를 뺐으므로 원본보다 덜 번쩍인다.
- 작업 중 `ProjectSettings/ProjectSettings.asset` 에 Standalone 스크립팅 심볼 `UNIVERSAL_PACKAGE` 가 추가되고 `EditorBuildSettings.asset` 줄바꿈이 바뀌었다(에디터·패키지 자동 변경, 이번 작업과 무관, 손대지 않음).
- 게이트 루프 미실행, 커밋 전.

## 4. 다음 할 일 (Next Steps)
- 사용자: 약점 피격 소리 에셋을 찾아 SOAudioData 로 만들고 5개 프리팹 `CriticalHitFeedback._sound` 에 연결.
- 직접 플레이: 스캔 후 약점 사격 시 노란 이펙트가 일반 명중과 구분되는지, 크기·밝기가 적당한지(`SOEffectData_WeakPointHit._scale`), 보스전에서도 나는지 확인.
