# 속도감 설정을 스킬 Inspector로 이전

## 요청·역할·확정 범위

- 사용자 요청: Roll의 카메라 Damping·복귀 시간을 Roll에, 보드 FOV·가장자리 블러·전환 설정을 SpawnVehicle에 노출하고 M_SpeedEdgeBlur 위치를 안내한다.
- Claude Code Opus가 기존 기능 작업의 계획 세션에서 이전 방식을 조사했고 Codex가 검토했다. Claude Code Sonnet이 기존 구현 세션에서 수정했으며 Codex가 소스·프리팹·Play를 검증했다.
- 설정 위치 변경은 사용자의 직접 요청 범위로 진행했다. 기본값·효과 동작은 유지했다. 추가 사용자 결정은 없다.

## 최종 결과와 위치

| Inspector 대상 | 설정 |
|---|---|
| `Assets/_Project/Prefabs/Skills/Roll.prefab`의 Roll → Camera Feedback | Camera Follow Damping `(0.1, 0, 0.1)`, Camera Damping Recover Time `0.15` |
| `Assets/_Project/Prefabs/Skills/SpawnVehicle.prefab`의 SpawnVehicle → Speed Feedback | FOV Offset `10`, Speed Feedback Transition Time `0.2`, Edge Blur Strength `0.06`, Center Radius `0.30`, Feather `0.25`, Sample Count `8`, Center `(0.5, 0.5)` |
| `Assets/_Project/Art/PostProcess/SpeedEdgeBlur/M_SpeedEdgeBlur.mat` | 기존 렌더링 머티리얼. 보드 연출의 튜닝은 SpawnVehicle이 전달하는 값을 따른다. |

- PlayerBaseCamera의 중복 조절 필드 4개를 제거했다. BaseCamera·ThirdPersonFollow 참조는 유지했다.
- 기존 카메라 튜닝 필드에 저장된 프리팹·씬 사용자 값이 없는 것을 Claude가 조사했다. 두 스킬 프리팹에는 새 값만 추가됐고 기존 게임플레이 필드는 바뀌지 않았다.
- 스킬은 기본 타입 프로퍼티만 노출한다. SkillMediator → CharacterMediator → CameraMediator → PlayerBaseCamera의 getter 연결로 설정을 전달하며, 스킬이 카메라·렌더러 컴포넌트를 직접 참조하지 않는다.
- 효과와 종료 전환 중에는 최신 설정을 읽는다. 연결이 해제되면 마지막 설정을 유지해 복귀 전환을 마친다. 비활성화·재활성화·사망·재사용의 기존 복구 경로를 유지했다.
- 새 데이터 구조체는 기존 파일에 함께 두고 새 파일·이벤트·외부 의존성을 추가하지 않았다.
- 렌더러의 기존 SetTarget(Camera, float)은 유지하고 설정을 받는 오버로드를 추가했다. 보드 설정은 MaterialPropertyBlock으로만 전달하며, 기존 2인자 호출은 머티리얼 기본값을 사용한다. 셰이더·머티리얼·PC_Renderer는 수정하지 않았다.

## 이번 후속 변경 파일

- `Assets/_Project/Scripts/Skill/Skills/Roll.cs`, `SpawnVehicle.cs`
- `Assets/_Project/Scripts/CameraControl/PlayerBaseCamera.cs`
- `Assets/_Project/Scripts/Rendering/SpeedEdgeBlurRendererFeature.cs`
- `Assets/_Project/Scripts/Mediator/SubMediators/SkillMediator.cs`, `CameraMediator.cs`
- `Assets/_Project/Scripts/Mediator/CharacterMediator.cs`
- `Assets/_Project/Prefabs/Skills/Roll.prefab`, `SpawnVehicle.prefab`

프리팹 직렬화는 Unity API로 수행했다. 기존 기능 변경과 사용자 변경은 유지했으며 Git 조작, Intent/HANDOVER/포인터·대화 원문 로그 작성은 하지 않았다.

## 검토·검증

- Opus의 getter callback 계획을 기존 중재 패턴과 비교해 수용했다. 스킬이 비활성 상태여도 설정은 읽을 수 있어야 하므로 활성 상태와 설정 읽기를 분리했다.
- C# 컴파일 실패 없음, 셰이더 메시지 0. 기존 다른 코드·ThirdParty 경고는 수정하지 않았다.
- Codex가 Unity SerializedObject와 프리팹 diff로 Roll 2개·SpawnVehicle 7개 새 필드와 기본값, 카메라의 기존 필드 4개 제거를 확인했다.
- Play 프레임 카운터 변화 확인 후 실제 SkillController 경로에서 Roll 발동·Damping 적용·복귀 및 보드 탑승 FOV 70·블러를 검증했다.
- 런타임 Roll 필드를 `(0.25, 0, 0.2)`로 바꿨을 때 카메라에 반영됐고, Roll 복귀 시간을 0으로 바꾸면 즉시 복구됐다.
- 런타임 SpawnVehicle 필드를 바꿨을 때 FOV 78, 블러 강도 0.11·반경 0.41·Feather 0.33·샘플 12·중심 `(0.4, 0.6)`이 렌더러 설정에 반영됐다.
- 렌더 완료 시점에 실제 패스의 MPB에서 강도 0.11·샘플 12 전달을 확인했다. 공유 머티리얼은 원래 강도 0.06·샘플 8이었다.
- 카메라 비활성 복구·재활성 최신 스킬값 적용, CharacterMediator 연결 해제·재연결, 놓친 하차의 동기화, getter 해제 후 마지막값 유지 및 종료 복구가 통과했다.
- 사망은 OnDeathCallback 직접 호출로 해당 처리 경로를 검증했고, ResetForReuse에서 재연결·복구를 확인했다. 실제 전투 입력부터 사망까지의 전체 흐름은 검증하지 않았다.
- 최초 테스트에서는 조건이 충족되지 않은 Roll 발동 요청과 렌더 전 MPB 관찰을 실패로 기록했다. 발동 조건을 정리해 실제 요청이 수락된 것을 확인하고, MPB는 WaitForEndOfFrame 뒤 다시 읽어 모두 통과했다. 이 재검증을 위해 제품 코드를 수정하지 않았다.
- 새 런타임 오류 없음. Play 전후 PC_Renderer·Player.prefab·머티리얼·Roll.prefab·SpawnVehicle.prefab SHA256이 모두 동일했다. 검증용 런타임 필드 변경은 저장되지 않았다.
- Play 종료, blur weight 0, runInBackground 원래 false 복구, 검증용 SessionState 정리 완료. 종료 시 열린 Lobby가 dirty=true였으나 변경의 출처를 확인하지 못했으므로 저장하거나 폐기하지 않았다. 씬 파일은 저장하지 않았다.

## 미검증

- 성능·Mobile Quality·Frame Debugger 직접 확인, 모든 맵 수동 플레이는 이번 후속 범위에서 검증하지 않았다.
- 공통 FullScreenFX는 이전 요청에서 허용한 TODO 상태를 그대로 유지한다.
