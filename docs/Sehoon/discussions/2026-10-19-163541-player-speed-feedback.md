# 플레이어 속도감 조사·방식 검토

- 목적: Roll 카메라 추종 완화, 블루투스 보드 FOV 증가·가장자리 블러, 사용 중 공통 FullScreenFX 효과, 모든 조절값 Inspector 노출.
- 참여: Codex는 요청 전달·계획 검토·근거 확인, Claude Code Opus는 새 세션 조사와 최초 계획, 검토 의견을 받은 별도 새 Opus 세션은 추천 수정. Sonnet 구현은 아직 시작하지 않았다.
- 상태: 방식 선택 전 조사·검토 완료. 제품 코드·프리팹·렌더러는 수정하지 않았다. 이 문서는 협의 요약이며 Intent/HANDOVER가 아니다.

## 확인 근거

- Claude 조사: CM3 기본/조준 카메라 FOV 60/45, ThirdPersonFollow Damping 0. 기존 Roll 시작·종료 경로가 CameraMediator까지 연결되어 있다. 보드의 SkillMediator.OnRidingChanged는 강제 하차도 알린다.
- Codex 직접 확인: PlayerBaseCamera는 현재 AimCamera만 참조하며, CameraMediator.NotifyRollStarted/Ended는 조준 잠금·복원을 담당한다. SkillMediator에 OnRidingChanged/OnVehicleAttached가 존재한다.
- Codex 직접 확인: 설치된 CinemachineThirdPersonFollow 소스에서 Damping은 축별 대략적인 추종 회복 시간이다. 고정된 0.1초 지연과는 다르다.
- Claude 조사: FullScreenFX의 FS_Speed와 FS_Speed_Distortion은 셰이더·머티리얼이며 표시용 런타임 API가 없다. ThirdParty는 Git 제외 대상이다. PC_Renderer에는 현재 SSAO만 있다.
- Codex 직접 확인: 설치된 URP CameraMotionBlur.shader는 속도에 따라 다중 샘플링하지만 가장자리 한정 마스크가 없다. 일반 URP Motion Blur만으로 중앙 선명도 보존을 보장할 수 없다.

## 대안과 추천

| 항목 | 추천 | 대안·근거 |
|---|---|---|
| Roll | 기존 ThirdPersonFollow Damping 조정 | 별도 지연 피벗은 이동 기준·조준과 연관된 구조 변경이 커진다. |
| 보드 가장자리 블러 A | 중앙 마스크를 둔 방사형 다중 샘플 블러 | 속도감 표현이 직접적이며 motion vector가 필요 없다. 새 셰이더·머티리얼 및 렌더러 연결이 필요하다. |
| 보드 블러 B | 가장자리 마스크가 있는 motion-vector 블러 | 실제 화면 이동 방향을 반영하나 커스텀 렌더링 구현·검증 범위가 크다. |
| 보드 블러 C | 일반 URP Volume Motion Blur | 구현은 작지만 중앙까지 흐려질 수 있어 요구를 완전히 충족하지 못하는 타협안이다. 기본 추천에서 제외했다. |
| FullScreenFX 공통 | 사용자가 허용한 TODO 주석 | FS_Speed_Distortion은 왜곡이므로 다중 샘플 블러의 대체가 아니다. 실제 표시 연결은 별도 범위 선택이 필요하다. |

Codex가 가장자리 요구 누락을 지적한 뒤 Claude가 A를 추천하도록 정정했다. 공통 주석에는 Roll 또는 Riding 중 하나라도 활성 상태면 효과를 유지하고, 한쪽 종료만으로 다른 쪽의 효과를 끄지 않는 조건을 담는다.

## 정책 기본값 제안 — 사용자 미승인

- Roll: 카메라 로컬 x/z Damping 0.1초, y는 0, 종료 시 원래 Damping으로 0.15초 동안 복귀.
- 보드: 기본 FOV에 +10도, 전환 0.2초, 조준 FOV는 원본 유지.
- 보드 시작·종료: OnRidingChanged 기준. 강제 하차 포함.
- 연동은 기존 PlayerBaseCamera·CameraMediator·CharacterMediator 패턴을 따르는 것을 추천한다.
- 블러 강도·중앙 보호 범위 등 구체적인 값, 파일 위치·추가 API는 방식 선택 후 상세 계획에서 제안한다. 조절 가능한 값은 모두 Inspector에 노출한다.

## 사용자 확정과 다음 단계

- 확정: 최초 요청에 명시된 요구사항, 기존 기능의 Claude 세션을 재사용하지 않는 것, 공통 표시 함수가 없을 때 주석만 두어도 된다는 허용.
- 미확정: 보드 블러 방식, 제안한 초기값, 방식에 따른 새 파일·렌더러 변경·공개 API 상세 계획.
- 다음: 사용자 방식 선택 → Claude Opus 상세 계획 → Codex 검토 → 필요한 사용자 승인 → Claude Sonnet 구현 → Codex 검증 및 필요시 Claude 수정.
- Git 조작과 기존 변경의 편입은 하지 않는다.

## 검증 범위

- 수행: 지침·관련 소스·설치된 패키지 소스 정적 확인, 기존 작업 트리 변경 구분, 터미널 연결 검증 후 Claude 실시간 협의.
- 미수행: 컴파일, Play 실행, 화면 비교, 성능 측정. 구현 전이므로 동작 검증 결과는 없다.
- 구현 후 검증 예정: Roll 정상 종료·중단, 보드 탑승·하차·강제 하차, 탑승 중 Roll 및 Roll 도중 하차, 조준 전환, 원래 FOV/Damping 복구, 컴포넌트 비활성화·재활성화 상태 동기화, Inspector 값 변경, 중앙 선명도·가장자리 블러·공유 에셋 오염 여부.
