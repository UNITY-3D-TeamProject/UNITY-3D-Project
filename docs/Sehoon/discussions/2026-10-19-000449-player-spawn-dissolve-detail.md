# Player Spawn Dissolve A안 상세 설계 및 구현 호출 상태

- 일시: 2026-10-19 00:04 KST
- 목적: Showcase의 오른쪽 Gravity Angel Dust 소멸 연출을 반대로 활용하여 TestScene Player를 아래에서 위로 생성한다.
- 역할·모델: Claude Opus 상세 설계·수정, Codex 계획 검토·검증, Claude Sonnet 구현 담당.
- 사용자 확정: A안(생성 중 임시 INab 재질, 완료 시 Robot 원본 복원), `Assets/Test/SpawnDissolve/` 테스트 격리 및 C# 위치 예외, 테스트 전용 FBX 사본, 본체 Angel Dust와 장비 마스크 동기화. 해당 프로젝트 자료의 Claude 서비스 전달 상시 승인은 유효하다.

## 확정한 설계

- `Assets/Test/TestScene.unity`의 Player 인스턴스 자식에 연출을 추가한다. 원본 Player 프리팹·ThirdParty·FBX importer·프로젝트 설정과 기존 씬 오버라이드는 유지한다.
- 신규 런타임 driver 1개, 최소 Editor installer 1개, 읽기 가능한 FBX 사본과 Burn 재질 템플릿을 테스트 폴더에 둔다. .meta는 Unity가 생성한다.
- INab effect는 비활성 상태로 저장한다. 런타임에서 독립 Material 1개를 만들고 Body/Lens/무기 2개/PowerPod의 총 5개 슬롯에 임시 적용한다. `useInstancedMaterials=false`로 재질 목록 덮어쓰기를 막는다.
- VFX 메시 샘플링은 본체만 수행하고 렌즈·장비는 같은 Plane 마스크로 제어한다.
- 원본과 동일한 6초 선형 진행과 VFX 종료 이벤트 간격을 유지한다. `ReverseEffect`는 파티클을 정지시키므로 사용하지 않는다.
- 높이 여유값은 임의 상수를 도입하지 않는다. 원본 Statue bounds와 양 끝 mask 위치의 패딩 비율을 실측하고, 대상 렌더러 전체 bounds에 적용한다. 좌표계·비율이 유효하지 않으면 중단하여 보고한다.
- 원본 FBX는 유지하고 바이트가 같은 사본의 Read/Write만 켠다. 메시·서브메시·리그·blendshape 호환성을 검증하고 기존 bones/rootBone/Animator 참조를 유지한다.
- 초기 mask 적용 전에 렌더러 노출을 막는다. Player 자식 구조로 이동을 추종하며 TestScene 진입당 한 번 재생한다. 입력 잠금이나 자동 respawn을 추가하지 않는다.
- 생성 완료 시 원본 재질을 먼저 복원한다. 원본 Stop 이벤트 이후 한 프레임 이상 지난 뒤 실제 입자가 0이면 effect 비활성화 → 원본 mesh 복원 → private 재질 파기를 수행한다. 임의 5초 타임아웃은 도입하지 않는다.
- 비활성화·파괴·플레이 중단 시 즉시 멱등 복원한다. 원본 재질은 수정하지 않는다. 복제된 VFX binder에 원본 씬 참조가 남지 않도록 검사한다.
- asmdef 등 범위 확대가 필요하면 자동 추가하지 않고 보고한다.

## 검토에서 수정한 점

- 새 정책 값인 mask 여유 0.1m 및 파티클 대기 최대 5초를 제거했다.
- 6초 시점에는 재질이 복원되고 mesh는 VFX 종료 후 복원된다는 순서를 명확히 했다.
- non-readable 원본 메시의 CPU 배열 접근을 전제하지 않도록 사본 동일성 검증을 보완했다.
- VFX 자동 시작과 binder의 원본 참조 잔존을 검증 조건에 포함했다.

## 실제 수행·미검증

- Opus 상세 설계 및 Codex 검토 반영 수정 응답은 정상 완료했다. 이 단계에서는 새 실행 검증을 수행하지 않았다.
- Sonnet 구현 호출 2회가 중계기의 스트리밍 응답 비교 오류로 중단되었다. 외부 데이터 전송 승인 거부가 아니다.
- 도구 호출과 텍스트가 섞인 응답에서, 중계기가 메시지 전체의 누적 텍스트와 개별 assistant 이벤트의 텍스트를 비교하는 방식이 실패 조건을 만든다. 진행 문장 억제 지침과 재시도로 해결되지 않았다.
- 신규 구현 폴더·파일이 생성되지 않았고, TestScene 적용·컴파일·생성 연출 Play 검증은 수행하지 못했다.
- Showcase는 Play 정지·변경 없음 상태다. 원본 프리팹·FBX/.meta·Burn 재질·Showcase·VFX graph·기존 ProjectSettings 3개 등 보호 대상 9개 해시는 작업 전과 같다.
- 구현 호출 중 잘못된 Unity 명령명으로 인한 Console 오류 1건이 추가되었다. 컴파일 실패는 없다.

## 다음 단계와 승인 경계

- 필요한 환경 복구안: 프로젝트 밖 `C:/Users/inha/.codex/claude-codex-live.py`의 응답 파서만 수정한다. tool-only assistant 이벤트와 text content block을 구분하고, 텍스트 중복 출력·실제 불일치 검증은 유지한다.
- 현재 채팅·프로젝트·표시기 확인, 일회용 코드 검증, 요청 표시 확인 후 Claude 시작 절차와 기존 권한·전송 범위는 유지한다. 검증 자체를 끄거나 직접 Claude 호출로 우회하지 않는다.
- 중계기는 프로젝트 밖 개인 파일이며 현재 승인한 테스트 격리 범위를 벗어난다. Codex가 중계기 파서만 복구하는 역할 변경 및 수정 범위 승인이 필요하다.
- 복구 후 새 probe와 실제 터미널 화면 확인 → Sonnet 구현 재호출 → Codex diff/프레임/입자/복원 검증 → 필요한 Sonnet 수정 순서로 이어간다.

