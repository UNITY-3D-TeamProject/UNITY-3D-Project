# GIF 기준 플레이어 스폰 연출 재작업

- 협의 종료: 2026-10-19 11:39 KST
- 요청: 사용자 `C:/Users/inha/Desktop/spawn.gif`처럼 캐릭터가 발부터 머리까지 생성되고, 생성 경계에서 밝은 파편이 흩어지는 연출을 `Assets/Test/TestScene.unity`의 Player에 적용한다.
- 역할: Claude Opus가 조사·계획·원인 분석 및 계획 수정, Claude Sonnet이 구현·튜닝·수정, Codex가 요청 전달·계획 검토·독립 실행 검증·요약을 담당했다. 요청과 응답 원문은 현재 채팅의 내장 터미널에 공개했다.

## 사용자 확정 사항과 설계

- 사용자가 선택한 방식: **원래 외형 보존 — Test 셰이더 사본 사용**. 임시 INab 표시 재질을 캐릭터에 적용하는 기존 방식은 외형 차이가 커서 교체했다.
- 기존 승인된 테스트 격리 범위와 튜닝 위임을 재사용했다. 원본 Player 프리팹과 ThirdParty 원본은 수정하지 않는다.
- 원래 Body/Lens 셰이더 그래프를 Test 폴더에 복제하고 생성 마스크만 추가했다. 기존 색·조명·투명도·Emission 연결은 유지한다. 표시용 임시 재질은 원래 재질을 복제하고 원래 renderQueue를 유지한다.
- 중력형 대신 기존 비중력 Angel Dust 그래프를 사용한다. INab용 재질 사본은 VFX 속성 공급에만 사용하고 캐릭터 표시에는 사용하지 않는다.
- 생성 경계는 몸체 bounds 아래에서 위까지 약 1.2초 동안 EaseOut으로 이동한다. 경계와 VFX에 같은 위치를 공급한다.

## 핵심 수정과 최종 값

1. 입자가 발밑에서만 생긴 원인은 좌표 기준이었다. SkinnedMeshRenderer의 MikeZ와 실제 root 뼈 위치가 약 1.138m 달랐다. 활성화 전 VFX meshTransform을 rootBone으로 연결하고, 종료·비활성·파괴 시 원래 meshTransform과 binder Target을 모두 복원한다.
2. 좌표 수정 후 과도해진 입자 밀도와 밝기를 낮춰 개별 흰색·하늘색 파편이 보이도록 조정했다.
3. VisualEffect.playRate가 직렬화되지 않아 Driver의 private SerializeField로 보관하고 재생 시 설정·종료 시 복원한다. 새 공개 API와 의존성은 추가하지 않았다.

| 항목 | 최종 값 |
|---|---:|
| 생성 시간 | 1.2초 |
| Spawn Rate | 3000 |
| Spawn Rate Multiplier | 1 |
| Scale Multiplier | 4 |
| Emission Power | 1 |
| Spawn Edge Width | 0.12 |
| VFX playRate | 2.0 |
| 파편 기본 색 | (0.85, 0.95, 1, 1) |
| 파편 보조 색 | (0.4, 0.75, 1, 1) |

제품 변경 파일:

- `Assets/Test/SpawnDissolve/PlayerSpawnDissolveDriver.cs`
- `Assets/Test/SpawnDissolve/M_Material_Parent_SpawnMask.shadergraph` 및 Unity 생성 meta
- `Assets/Test/SpawnDissolve/M_TransparentMaterial_Parent_SpawnMask.shadergraph` 및 Unity 생성 meta
- `Assets/Test/TestScene.unity`의 Player 스폰 연출 관련 설정

## 실제 검증

- Unity 6000.4.8f1에서 연속 Play와 실제 Time.time 기준으로 검증했다. 정지 스텝과 captureDeltaTime 강제 지정으로 GPU 입자 수가 고정됐던 초기 측정은 최종 시간 판정에서 제외했다.
- Sonnet 반복 실행: 생성 완료 관측 약 1.15~1.16초(측정 시작 기준 차이 포함), 마지막 입자 소멸 약 1.74~1.85초, 이후 약 0.01초 안에 복원. 생성 설정은 1.2초이며 입자 수명에는 실행별 편차가 있다.
- 최종 설정의 입자 최대 수는 약 7.5~8.3천 개였다. 초기 과도한 설정의 수십만 개에서 줄었다. 성능 상한 정책 코드는 추가하지 않았다.
- Codex 독립 Play에서 생성 경계 y=1.629일 때 입자 bounds 상단 y=1.555로 머리 높이까지 따라오는 것을 확인했다. 초기에는 표시 재질과 메시가 교체되고 VFX binder가 root를 가리켰다.
- Codex가 완료 상태에서 원래 Body/Lens 재질, 원본 MikeZ 메시, meshTransform=MikeZ, binder Target=MikeZ, 원래 playRate=1, effect 비활성, 남은 입자 0, 임시 재질 0을 확인했다.
- Sonnet은 생성 도중 컴포넌트 비활성·파괴 시 복원도 실행 확인했다. Codex는 정상 완료 복원을 독립 확인했다.
- 원본 셰이더 그래프와 사본의 구조 비교에서 기존 노드를 보존한 것을 확인했고 셰이더 오류가 없었다. Lens·반사·그림자는 캡처로 정성 확인했다.
- 최종 Editor 상태: TestScene만 열림, Play 정지, dirty=false, 컴파일 실패 없음, 현재 Editor console 오류 0. 과거 도구 진단 쿼리의 예외가 보관 버퍼에 남아 있으나 제품 컴파일 오류와 구분했다.

## 변경 범위 보존

- 현재 작업 시작 시 보관한 원본 셰이더 2개·데모 재질 8개와 보호 대상 설정·Player 프리팹 등 15개 해시가 모두 일치했다. Sonnet 구현 시작 스냅샷의 보호 대상 265개도 모두 일치했다.
- TestScene의 기존 55개 YAML 블록 중 변경은 스폰 연출 관련 4개 블록뿐이며 블록 추가·삭제는 없다. 카메라 near clip은 디스크 기준 원래 0.3으로 복원했다.
- ProjectSettings.asset은 Unity 저장 중 CRLF가 LF로 바뀌었다. 설정값은 처음부터 activeInputHandler=2, runInBackground=0이었다. 줄바꿈만 원복해 작업 시작 SHA256과 완전히 일치함을 Codex가 재확인했다. Git HEAD와 비교해 입력 설정 변경으로 판단한 앞선 보고는 정정한다.
- 사용자 GIF의 SHA256은 변경 전후 동일하다. 원본 VFX·모델 importer·SpawnBurn 재질은 보존했다. 임시 Editor 측정 코드와 잘못 생성된 Assets/Library는 남아 있지 않다.
- 이전 작업에서 발생한 데모 재질 8개의 원상 복구 과제는 별도 미해결 사항이다. 이번 재작업은 현재 작업 시작 상태의 8개를 그대로 보존했다.

## 검증 한계와 다음 단계

- 빌드·장시간 플레이·사용자 입력을 포함한 게임 흐름 전체 테스트는 하지 않았다. 외형 비교는 현재 로봇 모델 기준이며 GIF의 다른 캐릭터 모델과 완전 일치를 주장하지 않는다.
- Lens 반사·투명도와 숨김 그림자는 정량 비교가 아니며, 동일 pose의 완료 직전/직후 픽셀 차이를 측정하지 않았다.
- 제품 변경에 대한 미결정 설계 항목은 없다. 커밋은 요청되지 않아 수행하지 않았다.
- 최종 연속 Play 캡처와 측정값은 `Library/SpawnDissolveReference/Sonnet2/`에 있다. 비교 이미지: `c6_cap1.png`(초기), `c6_cap3.png`(상단 생성), `c6_cap5.png`(잔광).
