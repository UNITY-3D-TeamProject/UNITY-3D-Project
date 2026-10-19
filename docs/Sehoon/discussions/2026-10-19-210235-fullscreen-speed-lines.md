# FullScreenFX 스피드 라인 적용 협의

- 목적: 사용자가 지정한 `FullScreenFX/Effects/Speed/Material/FS_Speed.mat`을 Roll 또는 블루투스 보드 사용 중 표시한다. 사용자 요청 “적용해봐”로 구현 범위 승인.
- 역할: Claude Opus 조사·계획 및 보완, Codex 계획 검토·검증, Claude Sonnet 구현. 원문은 현재 채팅의 내장 터미널에 실시간 표시했고 별도 원문 로그는 만들지 않았다.

## 대안과 확정 방식

- 별도 RendererFeature보다 기존 `SpeedEdgeBlurRendererFeature`에 독립적인 라인 패스를 추가하는 방식을 채택했다. 대상 카메라와 해제 수명을 공유하고 변경을 3개 파일로 제한한다.
- 원본 그래프는 Additive이고 화면 색을 샘플링하지 않는다. URP 기본 FullScreenPass와 같은 source 없는 `AddBlitPass`를 사용한다. UI는 필요 없다.
- 원본 머티리얼의 `_enabled`는 0으로 유지하고 패스의 MaterialPropertyBlock에만 1을 전달한다. 라인은 블러 뒤에 그린다.
- Codex 보완: 라인을 블러 강도·블러 머티리얼·보드 설정 존재 여부와 분리했다. Roll 단독(weight 0)에서도 라인이 표시된다.
- 기존 머티리얼의 강도·색·시간 설정을 그대로 사용한다. 별도 페이드나 새 스킬 정책값은 추가하지 않았다. 라인 머티리얼 참조는 렌더러 Inspector에 노출했다.

## 실제 변경

1. `Assets/_Project/Scripts/Rendering/SpeedEdgeBlurRendererFeature.cs`: 라인 머티리얼·패스·표시 상태 API와 초기화/정리 추가. 블러와 라인 등록 조건 분리.
2. `Assets/_Project/Scripts/CameraControl/PlayerBaseCamera.cs`: TODO를 `_isRolling || _isRiding` 연동으로 교체. 두 상태가 끝나면 복귀 전환 중에도 라인은 끈다.
3. `Assets/Settings/PC_Renderer.asset`: Unity SerializedObject로 원본 `FS_Speed.mat` 참조 추가. 해당 에셋만 저장했다.

## 렌더러 검증

1. **Validation Summary:** Unity 6000.4.8f1 / URP 17.4.0에서 컴파일과 Play 표시 확인. 새 오류 없음.
2. **Confirmed Issues:** 이번 변경에서 확인한 결함 없음.
3. **Likely Issues / Risky Assumptions:** ThirdParty가 기존 Git 제외 대상이므로 FullScreenFX가 없는 환경에서는 참조가 누락되어 라인만 생략된다. 실제 누락 환경은 실행하지 않았다.
4. **Recommended Fixes:** 현재 요청 범위에서 추가 수정 없음. 패키지 공유 방식은 별도 결정 사항이다.
5. **Corrected Snippets:** 추가 수정 불필요. 라인은 화면 색 복사·전역 텍스처·새 임시 RT 없이 AddBlitPass를 사용한다.
6. **Missing Information:** Mobile Quality·빌드·장치별 성능은 미검증이다.

## 실제 검증과 보존

- Sonnet 재컴파일 결과 오류 0, 경고 0. Codex 확인에서도 compilationFailed=false, consoleErrors=0. Play에서 기존 도로 BoxCollider 음수 스케일 경고 2건이 재현됐고 이번 렌더링 관련 오류는 없었다.
- Play 캡처로 idle / Roll 라인 / 보드 FOV·블러·라인 동시 표시를 확인했다. 기존 Player 값 Roll 0.5초, 보드 FOV +30도(최종 90도), Aim 45도를 확인했다.
- 실제 SkillController 보드 실행과 MoveMediator Roll 이벤트로 양쪽 종료 순서를 검증했다. Roll 종료 후 탑승 중에는 유지되고, Roll 중 하차해도 유지되며, 모두 종료하면 해제된다.
- Damping 복귀·FOV 전환 상태에서도 라인은 즉시 해제된다. 카메라 비활성화, CharacterMediator 비활성화, 사망 처리 및 ResetForReuse 후 line=false·weight=0·target=null을 확인했다.
- 복제한 feature를 사용한 등록 검사: 정상 시 blur→line, 블러 머티리얼 누락 시 line만, 라인 누락 시 blur만, weight=0 시 line만 등록됐다. 다른 카메라와 SceneView 타입에는 패스가 등록되지 않았다. 실제 전광판 RT 및 Frame Debugger 비교는 하지 않았다.
- 보드 설정 제공자가 없는 Roll 상태에서도 line=true를 확인했다.
- Play 종료, 일시정지 해제, runInBackground=false 복원. 저장되지 않은 Lobby 씬은 계속 dirty=true이며 저장·폐기하지 않았다.
- 원본 FS_Speed.mat·shadergraph, M_SpeedEdgeBlur.mat, Player/Roll/SpawnVehicle 프리팹의 SHA256이 작업 전과 동일하다. 원본 `_enabled=0`, materialDirty=false를 확인했다.
- PC_Renderer diff는 머티리얼 참조 한 줄이며 `git diff --check` 통과. 사용자의 다른 변경은 보존했고 이번 적용 내용은 커밋하지 않았다.

## 다음 단계

사용자가 Play에서 기본 라인 외관을 평가한다. 강도·색 등 추가 조정값은 이번에 임의로 정하지 않았다.
