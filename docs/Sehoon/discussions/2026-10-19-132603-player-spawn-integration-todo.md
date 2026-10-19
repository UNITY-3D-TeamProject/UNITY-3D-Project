# 플레이어 스폰 연출 연결 — 남은 작업

작성: 2026-10-19 13:26 KST  
프로젝트: `C:/Users/inha/Desktop/Project/Unity/3DProject`

## 목표와 현재 상태

TestScene에서 검증한 아래→위 캐릭터 생성 연출을 **실제 Player 프리팹의 모든 새 스폰·리스폰에 적용**한다.

- 사용자는 **A안: 스폰 즉시 재생**을 승인했다. 로딩 완료까지 기다리는 B안은 적용하지 않는다.
- 로딩 페이드와 생성 연출 앞부분이 겹칠 수 있다는 점을 설명한 뒤 A안을 선택받았다.
- 사용자는 이후 Codex가 직접 작업하는 방향에 동의했다. 이번 작업은 Codex가 직접 구현·검증할 수 있다. 이를 전역 협업 역할 변경으로 확대하지 않는다.
- 현재까지 조사·계획 검토만 끝났다. **실제 Player 프리팹 연결과 TestScene 중복 제거는 아직 하지 않았다.**
- 자동 승인 검토의 `Selected model is at capacity` 오류로 터미널 연결 확인과 Unity 상태 조회가 실행되지 않았다. 안전성 거부가 아닌 검토 실패다. 필수 검토를 우회하지 않고 정상화 후 이어간다.
- A안 승인은 이미 받았으므로 같은 범위를 다시 승인받지 않는다. 새 공개 API·정책·구조 변경이 필요해지면 별도로 확인한다.

## 정확한 대상 파일

| 경로 | 용도·처리 |
|---|---|
| `Assets/_Project/Prefabs/Player/Player.prefab` | **주 변경 대상.** 스폰 연출 계층과 Driver를 추가·연결 |
| `Assets/_Project/Scripts/Visual/SpawnEffectDriver.cs` | 사용자가 배치한 코드. `namespace Visual`, `class SpawnEffectDriver`. 현재 자동 재생 로직을 그대로 사용 |
| `Assets/ThirdParty/SpawnEffect/M_Material_Parent_SpawnMask.shadergraph` | 불투명 재질용 생성 마스크 셰이더 |
| `Assets/ThirdParty/SpawnEffect/M_TransparentMaterial_Parent_SpawnMask.shadergraph` | 투명 재질용 생성 마스크 셰이더 |
| `Assets/ThirdParty/SpawnEffect/MikeZ_Readable.fbx` | 생성 중 VFX가 샘플링할 Read/Write 메시 |
| `Assets/ThirdParty/SpawnEffect/SpawnBurn.mat` | 렌더러 표시용이 아닌 VFX 속성 공급 템플릿 |
| `Assets/Test/TestScene.unity` | 기존 연출 구성의 참고 원본. 프리팹 연결 후 씬의 기존 중복 연출만 제거 |

유료 에셋 사본 4개는 `ThirdParty/SpawnEffect`에 유지한다. `_Project`에 복제해 공개 대상에 포함하지 않는다. Unity가 생성한 `.meta`와 GUID를 유지한다.

## 이미 조사한 스폰 흐름

- `Assets/_Project/Scripts/Core/PlayerSpawner.cs`: `Start → SpawnPlayer`.
- `Assets/_Project/Scripts/Core/SpawnerBase.cs`: `TrySpawnWithAttributes → Instantiate(prefab, spawnPoint.position, spawnPoint.rotation)`.
- `Assets/_Project/Scripts/Core/Stage/StageManager.cs`: 기존 Player를 Destroy한 뒤 리스폰 요청. PlayerSpawner가 다음 프레임에 새 인스턴스를 생성한다.
- 조사 시점의 실제 플레이어 스폰 경로는 풀 재사용이 아니다. 새 인스턴스마다 Driver의 `Awake` 준비 → `Start` 재생을 사용할 수 있다.
- `Assets/_Project/Scripts/SceneTransition/LoadingScreen.cs`는 목적지 씬을 활성화한 뒤 fade out한다. A안에서는 이 흐름을 변경하지 않는다.
- `ScriptableObjects/SpawnEffect`의 기존 Attribute 효과 데이터는 이번 시각 연출과 무관하므로 수정하지 않는다.

위 내용은 이전 정적 조사와 코드 대조 결과다. 재개 시 사용자 변경 여부를 확인하고 현재 파일을 우선한다.

## 구현할 내용

### 1. 작업 전 상태 보존

- Unity 상태 조회가 정상 실행되는지 확인한다. 파일·에셋 작업은 실행 중 Editor의 Unity API로 수행하고 프리팹 YAML을 직접 붙이지 않는다.
- Player 프리팹과 관련 파일의 작업 시작 상태를 기록한다. 기존 사용자 변경을 이번 변경으로 묶거나 되돌리지 않는다.
- 마지막으로 성공한 Unity 조회에서는 **TestScene이 열려 있고 dirty=true**였다. 이후 조회는 실행되지 않았으므로 현재 상태는 재확인이 필요하다.
- 사용자에게 미저장 씬을 임의 저장하지 않고 유지하겠다고 알렸다. TestScene 중복 제거도 메모리에서 수행하고 dirty 상태를 유지한다. 사용자의 별도 저장 지시가 없으면 저장·재로드·단독 씬 전환으로 미저장 변경을 유실하지 않는다.

### 2. Player.prefab에 연출 계층 연결

기존 TestScene 구성을 기준으로 다음 계층을 Player 루트 아래에 구성한다.

```text
Player
└─ PlayerSpawnDissolve                 활성
   ├─ SpawnEffectDriver              Visual.SpawnEffectDriver
   └─ Interactive Dissolve            비활성 상태로 저장
      ├─ Mask
      └─ VFX Graph
```

`SpawnEffectDriver`의 직렬화 참조를 연결한다.

- `_effect`: 위 Interactive Dissolve의 `INab.Common.InteractiveEffect`.
- `_burnTemplate`: ThirdParty/SpawnEffect의 `SpawnBurn.mat`.
- `_bodyRenderer`: 프리팹 자신의 MikeZ SkinnedMeshRenderer.
- `_readableBodyMesh`: ThirdParty/SpawnEffect의 `MikeZ_Readable.fbx` 안의 해당 Mesh. Unity에서 실제 서브에셋을 확인해 연결한다.
- `_targetRenderers`: 기존 테스트와 같은 몸체·Buster 2개·PowerPod의 Renderer 4개. 씬 오브젝트가 아닌 **프리팹 내부 렌더러**를 연결한다.
- `_opaqueSourceShader`, `_transparentSourceShader`: Tacko3D의 기존 원본 셰이더 2개.
- `_opaqueMaskShader`, `_transparentMaskShader`: ThirdParty/SpawnEffect의 마스크 셰이더 2개.
- `_startPaddingRatio`, `_endPaddingRatio`: 각각 0.02.
- `_vfxPlayRate`: 2.

Interactive Dissolve와 Mask, VFX Graph의 컴포넌트·참조·노출값도 기존 테스트 구성을 보존한다.

- 원본 VFX: `Assets/ThirdParty/INab Studio/Vfx Assets/Interactive Dissolve/Core/Graphs/Particles Spam/Angel Dust Dissolve.vfx`.
- `VisualEffect`, `VFXPropertyBinder`, `VFXLossyTransformBinder`, Mask 및 관련 설정을 누락 없이 연결한다.
- 시작·종료 위치는 Driver가 캐릭터 bounds로 계산한다.
- 표시 재질은 원래 외형을 유지하는 런타임 사본을 쓰고 종료 후 원래 재질·메시로 복원한다.
- VFX 좌표는 재생 중 rootBone을 기준으로 하고, 종료 후 meshTransform과 binder Target을 모두 원래 MikeZ로 복원하는 기존 처리를 유지한다.
- 사용자 복사본의 GUID는 기존 Test 사본과 다르므로, Test 에셋 GUID를 그대로 재사용하지 않는다.
- `PrefabUtility.LoadPrefabContents` 등 Editor API로 구성·저장한 뒤 다시 읽어 참조를 확인한다. 에셋 전체를 대상으로 하는 불필요한 SaveAssets는 피한다.

### 3. TestScene의 중복 연출 제거

- 프리팹 반영 후 TestScene의 Player에는 새 프리팹 연출과 기존 added GameObject가 중복될 수 있다.
- **씬에 따로 추가됐던 기존 PlayerSpawnDissolve만** 제거한다. 프리팹에서 상속된 새 연출은 유지한다.
- 새 `Visual.SpawnEffectDriver`와 이전 Test Driver가 같은 렌더러를 동시에 제어하지 않도록 한다.
- 사용자 미저장 변경과 카메라 등 무관한 오브젝트는 보존한다. 이번 작업에서는 TestScene을 임의 저장하지 않는다.

## 유지할 튜닝과 위치

| Hierarchy 선택 → Inspector 위치 | 항목 | 값 |
|---|---|---:|
| PlayerSpawnDissolve → Spawn Effect Driver → Mask Range | Start / End Padding Ratio | 0.02 / 0.02 |
| PlayerSpawnDissolve → Spawn Effect Driver → Playback | Vfx Play Rate | 2 |
| Interactive Dissolve → Interactive Effect → Interactive Settings | Duration | 1.2초 |
| 같은 위치 | Effect Curve | 기존 EaseOut 곡선 |
| VFX Graph → Visual Effect 노출 속성 | Spawn Rate | 3000 |
| 같은 위치 | Spawn Rate Multiplier | 1 |
| 같은 위치 | Scale Multiplier | 4 |
| 같은 위치 | Emission Power | 1 |
| 같은 위치 | Spawn Edge Width | 0.12 |
| 같은 위치 | Spawn Edge Offset | 0 |
| 같은 위치 | Color | (0.85, 0.95, 1, 1) |
| 같은 위치 | Color 1 | (0.4, 0.75, 1, 1) |

기존 테스트에서 마지막 입자 소멸은 약 1.74~1.85초로 관측됐다. 실제 게임 연결 후의 결과는 아직 검증하지 않았다.

## 완료 전 검증 체크리스트

- [ ] Player.prefab에 스폰 연출 1개, Missing Script/참조 없음, Test 폴더 에셋에 대한 의도치 않은 의존 없음.
- [ ] 기존 프리팹 블록·속성은 보존되고 필요한 연출 구성만 추가됐는지 diff 확인.
- [ ] 새 PlayerSpawner 인스턴스 생성 경로에서 Driver가 준비되고 약 1.2초 동안 아래→위로 생성됨.
- [ ] effect 자식 활성화의 Awake/Start 이후에도 VFX 노출값이 유지됨. 실제 유효값을 읽어 확인.
- [ ] GameView가 실제로 진행하는지 frameCount 변화 확인. GPU 입자 검증은 연속 Play·captureDeltaTime=0에서 수행.
- [ ] 정상 종료 후 원래 Body/Lens 재질과 메시, meshTransform·binder Target, 원래 playRate로 복원됨.
- [ ] effect 비활성, 입자 0, 임시 재질 정리 확인.
- [ ] 실제 리스폰 요청 → 이전 Player Destroy → 새 인스턴스 생성에서도 다시 한 번 정상 재생됨.
- [ ] 생성 도중 파괴·씬 전환 등 중단 시 오류와 임시 재질 잔존 없음.
- [ ] TestScene에서 신·구 Driver가 중복되지 않음. 사용자 미저장 상태 보존.
- [ ] 컴파일·런타임 콘솔 확인. 기존 오류와 이번 작업 오류를 구분하고 사용자 로그를 임의 삭제하지 않음.
- [ ] Play 종료 후 Time.timeScale, captureDeltaTime, runInBackground, pause 등 검증용 상태 복원.
- [ ] 원본 에셋과 보호 대상 설정 해시 비교. 실제 검증/미검증 범위를 구분해 완료 보고.

## 변경하지 않을 범위와 주의사항

- PlayerSpawner·StageManager·LoadingScreen의 기존 스폰/로딩 흐름, 이동·사격·피격·무적 정책은 유지한다.
- Driver의 공개 API·재사용 구조를 불필요하게 추가하지 않는다. 현재 Instantiate 경로 연결에 필수인 수정만 한다.
- INab/Tacko3D 원본, ThirdParty/SpawnEffect 사본 내용, 기존 Test 원본, ProjectSettings는 무관한 변경을 하지 않는다.
- **Showcase 씬을 열지 않는다.** 이전에 ExecuteAlways로 원본 데모 재질이 재저장된 이력이 있다. 이전 데모 재질 복구 문제는 이번 범위에 포함하지 않는다.
- `capture_game_view`의 상대 `save_path=Library/...`는 이전에 Assets/Library로 저장되는 문제가 있었다. 인라인 캡처 또는 확인한 절대 Library 경로를 사용한다.
- INab 코드 의존성은 이미 존재한다. 패키지가 없는 PC의 컴파일 문제를 해결한다는 이유로 임의 define·ProjectSettings 변경을 추가하지 않는다.
- 커밋·푸시·브랜치 조작은 요청되지 않았다.
- 이 문서만 작성한다. 별도 HANDOVER·Intent·포인터를 자동 생성·갱신하지 않는다.

## 참고 기록

- `docs/Sehoon/discussions/2026-10-19-113906-player-spawn-reference-revision.md`: GIF 기준 연출 구현·검증 기록.
- `docs/Sehoon/discussions/2026-10-19-123710-player-spawn-integration-plan.md`: Claude Opus 조사·Codex 검토 기록. 해당 문서의 A/B 승인 대기 상태는 이후 사용자의 **A안 승인**으로 해소됐다.

다음 실행은 **Unity 조회 및 필수 승인 검토 정상화 확인 → 현재 사용자 변경 대조 → 프리팹 연결 → 중복 제거 → 실제 스폰/리스폰 검증** 순서다.
