# 플레이어 속도감 구현·검증

## 목적·승인·역할

- 사용자 요청: Roll 중 카메라 추종 완화, 블루투스 보드 탑승 중 FOV 확대와 가장자리 블러, 사용 중 공통 FullScreenFX 연출, 모든 조절값 Inspector 노출.
- 사용자는 이전 협의의 A안과 제안값에 대해 **“그대로 진행해”**라고 승인했다. A안은 중앙을 보존하는 방사형 다중 샘플 블러이며 셰이더·머티리얼 추가와 렌더러 연결을 포함한다.
- Claude Code Opus가 새 작업 세션에서 상세 계획을 작성·수정했고, Codex가 계획을 검토했다. Claude Code Sonnet이 별도 새 구현 세션에서 구현·후속 수정을 담당했으며 Codex가 diff·에셋·실행을 검증했다.
- 조사 단계 기록: [최초 방식 검토](2026-10-19-163541-player-speed-feedback.md). 이번 기록은 사용자 승인 이후 협의만 담는다.

## 최종 구현

| 대상 | 동작·초기값 |
|---|---|
| Roll | BaseCamera의 ThirdPersonFollow Damping `(0.1, 0, 0.1)`, 종료 후 0.15초 동안 원래 Damping으로 복귀. 고정 시간 지연이 아니라 추종 완화다. |
| 보드 | OnRidingChanged 기준 기본 FOV에 +10도, FOV와 블러 전환 0.2초. AimCamera FOV 45는 유지한다. |
| 가장자리 블러 | 중앙을 선명하게 보존하는 방사형 다중 샘플링. 강도 0.06, 중앙 반경 0.30, Feather 0.25, 샘플 8, 중심 `(0.5, 0.5)`. |
| 공통 FullScreenFX | 표시용 런타임 API가 없어 허용된 TODO 주석만 추가했다. 향후 `Roll || Riding` 동안 유지하고 둘 다 끝나야 끄도록 명시했다. 현재 화면에는 이 공통 효과가 표시되지 않는다. |

- Roll·FOV 조절값과 카메라 참조는 PlayerBaseCamera Inspector, 블러 조절값은 M_SpeedEdgeBlur 머티리얼 Inspector, 머티리얼·패스 시점은 PC_Renderer의 SpeedEdgeBlur Inspector에 노출했다.
- Roll/보드는 상태를 분리한다. 사망·CharacterMediator 비활성화·재사용 때 실제 상태와 동기화하며 PlayerBaseCamera 비활성화 때 원래 FOV/Damping과 블러 대상을 복구한다.
- 블러 대상은 Cinemachine이 출력하는 실제 Camera다. 태그만으로 선택하지 않으며 MaterialPropertyBlock으로 강도를 전달해 머티리얼 에셋을 수정하지 않는다.
- Game 카메라·대상 Camera·강도 조건을 모두 만족해야 패스를 등록한다. RenderGraph 임시 텍스처로 복사한 후 원래 화면에 그려 읽기·쓰기 충돌을 피한다.

## 변경 파일

- 수정: `Assets/_Project/Scripts/CameraControl/PlayerBaseCamera.cs`
- 수정: `Assets/_Project/Scripts/Mediator/CharacterMediator.cs`
- 수정: `Assets/_Project/Scripts/Mediator/SubMediators/CameraMediator.cs`, `MoveMediator.cs`, `SkillMediator.cs`
- 추가: `Assets/_Project/Scripts/Rendering/SpeedEdgeBlurRendererFeature.cs`
- 추가: `Assets/_Project/Art/PostProcess/SpeedEdgeBlur/SpeedEdgeBlur.shader`, `M_SpeedEdgeBlur.mat`
- 연결: `Assets/_Project/Prefabs/Player/Player.prefab`, `Assets/Settings/PC_Renderer.asset`
- `.meta`와 머티리얼·프리팹·렌더러 직렬화는 Unity가 생성·저장했다. PC_Renderer 재저장에는 URP 에셋 버전 2→3 및 현재 버전의 직렬화 변경이 함께 포함됐으며 기존 SSAO 설정은 유지됐다.
- 기존 CLAUDE.md·SkySign RenderTexture 4개·EditorBuildSettings 변경은 이번 작업에 포함하지 않았고 검증 전후 해시가 동일하다. Git 커밋·푸시·브랜치 조작은 하지 않았다.

## 주요 검토와 수정

1. Codex가 사망 때 종료 이벤트 유실로 새 효과가 남는 문제와 재활성 상태 동기화 누락을 지적했다. Opus가 실제 상태 읽기 API 및 중재 계층의 즉시 동기화 경로로 계획을 수정했다.
2. Loading 씬과 MainCamera 태그가 겹칠 수 있어 태그 필터 대신 실제 Cinemachine 출력 Camera 참조를 사용했다. 전역 셰이더 강도 전달은 철회하고 MPB를 사용했다.
3. Sonnet 최초 구현의 AddCopyPass는 Native Render Pass와 동일 MSAA 조건에 의존했다. Codex는 A) 일반 AddBlitPass 복사와 B) CanAddCopyPass 분기·fallback을 비교해, 기존 URP FullScreenPass와 맞고 변경이 작은 A를 요청했다. Sonnet이 수정하고 Codex가 Play 렌더링을 재검증했다.
4. Damping을 “줄인다”는 설명은 실제 0→0.1과 반대여서 Sonnet이 “지정한 Damping으로 추종을 완화한다”로 수정했다.

## RenderGraph 검토 결과

- 검증 요약: 최종 구현에서 미해결 결함은 발견하지 않았다.
- 확정 이슈: AddCopyPass 설정 의존성은 AddBlitPass로 수정됐다. 입력 텍스처는 BlitMaterialParameters의 source로 전달하며 weight는 MPB의 셰이더 이름과 일치한다.
- 위험 가정: 단일 플레이어의 출력 Camera를 static 대상으로 관리한다. 복수 로컬 플레이어 지원은 이번 범위에 없다.
- 권장 수정: 위 수정 완료. 별도 교정 코드 조각은 필요하지 않다.
- 누락 정보: Frame Debugger 패스 목록 직접 확인·실기 성능 측정은 수행하지 않았다. 무효 상태 패스 미등록과 다른 카메라 제외 조건은 소스 검토로 확인했다.

## 실제 검증

- 환경: Unity 6000.4.8f1, URP 17.4.0, Cinemachine 3.1.7, 최신 Unity CLI 1.0.0-beta.13.
- Sonnet과 Codex가 컴파일 상태 및 셰이더 지원·메시지를 확인했다. 최종 C# 컴파일 실패 없음, 셰이더 지원 true·메시지 0. 기존 StageManager·ThirdParty 등의 컴파일 경고는 수정하지 않았다.
- 실제 Lobby Play에서 프레임 카운터 변화까지 확인한 뒤 검증했다.
- 실제 SkillController 경로: Roll 시작·정상 종료, SpawnVehicle 탑승, 자원 0 설정으로 발생한 실제 강제 하차가 모두 기대값을 만족했다.
- 추가 API·런타임 상태 검증: 탑승 중 Roll, RollMover 중단 후 보드 효과 유지, 조준 FOV 45 보존, PlayerBaseCamera 비활성화 중 setters의 비적용·재활성 즉시 적용, CharacterMediator 비활성 중 놓친 하차의 재동기화, 전환 시간 0, 런타임 조절값 변경을 확인했다.
- 사망은 private OnDeathCallback을 직접 호출해 해당 처리 경로를 검증했다. FOV·Damping·블러가 즉시 복구됐고 ResetForReuse도 확인했다. 실제 피격 입력부터 사망까지의 전체 전투 흐름을 검증한 것은 아니다.
- 화면 검증: Game View에서 중앙 조준점·HUD는 선명하고 가장자리는 방사형으로 흐려졌다. 동일 paused 구도의 블러 켜짐·꺼짐 RGB 평균 절대차는 중앙 0.061, 가장자리 4.491(0~255 스케일)이었다. 이는 화면 결과 확인이며 성능 지표가 아니다.
- 후속 수정 후 다시 Play에 진입해 실제 보드 탑승, FOV 70·weight 1, 화면 렌더링과 새 런타임 오류 없음을 재확인했다.
- 두 Play 검증 전후 머티리얼·PC_Renderer·Player.prefab SHA256이 동일했다. 종료 후 blur weight 0, Lobby dirty=false, 편집 상태 stopped, runInBackground는 원래 false로 복구했다. 검증용 코루틴은 런타임 메모리에서만 사용했고 임시 캡처 에셋은 정리했다.
- 재컴파일 중 Pipeline 요청이 도메인 재로드로 중단된 도구 연결 오류가 한 차례 있었다. 이후 재연결·최종 Play에서는 새 오류가 없었다.

## 미검증·남은 범위

- Mobile Quality에는 블러를 연결하지 않았다. PC_Renderer 적용이 이번 범위다.
- 성능 측정, Frame Debugger 직접 확인, Loading 전환 중 실제 화면, 모든 맵·플랫폼의 수동 플레이는 미검증이다.
- 공통 FullScreenFX의 실제 표시는 사용자가 허용한 TODO 상태로 남긴다.
- 승인 범위의 구현·수정·검증은 완료했다. 새 사용자 결정은 필요하지 않다.
