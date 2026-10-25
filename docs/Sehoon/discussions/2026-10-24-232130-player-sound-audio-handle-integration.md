# PlayerSound의 SAudioHandle 연결 수정

- 일시: 2026-10-24 23:21 KST
- 브랜치: `feature/PlayerSound`
- 사용자 요청: AudioManager가 반환하는 `SAudioHandle`을 반복음 정지에 사용하도록 캐릭터 사운드 코드 수정.
- 역할: Claude Opus 조사·계획, Codex 계획 검토, Claude Sonnet 구현·보완, Codex 코드 검토·Unity 검증·요약 작성.

## 확인 사실과 확정 범위

현재 `AudioManager.PlaySfx`와 `PlaySfxLoop`는 `SAudioHandle`을 반환하고, `StopSfx`는 이 핸들을 받는다. `default`는 시작 실패이며, `IsValid`는 발급 여부만 나타낸다. 현재 재생 중인지 확인하는 API는 아니다.

작업 시작 시 `SoundController.StartLoop`와 `StopLoop`는 빈 TODO였다. 이전 구현 기록이 존재하지만 그 구현은 현재 파일에 없었다. stash를 읽거나 적용하지 않고 현재 소스 위에 연결했다.

- 제품 수정은 [SoundController.cs](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/CharacterSound/SoundController.cs) 한 파일로 제한했다.
- 원하는 상태 유지·매 프레임 재시도·즉시 정지는 [10월 23일 사용자 승인 기록](2026-10-23-103512-player-sound-loop-api-integration.md)의 정책을 재사용했다. 새 정책 선택이나 수치 변경은 없었다.
- 최초 계획의 재시도 생략안과 현재 Instance를 통한 정지안은 Codex 검토에서 제외했다. 기존 승인 정책과 원래 매니저의 정지 책임을 보존하는 계획으로 Claude가 갱신했다.

## 최종 변경

- private `SActiveLoop`에 원래 `AudioManager`와 반환된 `SAudioHandle`을 함께 보관한다. 키별 실제 시작 기록은 `_activeLoops`, 원하는 상태는 기존 `_desiredLoops`가 담당한다.
- `StartLoop`: 컴포넌트 활성 상태·매니저·SO·클립을 확인하고 `PlaySfxLoop(sfx, transform)`을 호출한다. 발급에 성공한 핸들만 기록하여 같은 키의 중복 시작을 막는다.
- `StopLoop`: 현재 Instance나 SO 조회에 의존하지 않고, 기록한 원래 매니저의 `StopSfx(handle)`을 호출한 뒤 기록을 제거한다. 매니저가 파괴됐으면 기록만 정리한다.
- `LateUpdate`: 이동 상태를 먼저 반영한 뒤 `SyncDesiredLoops`로 미시작 루프를 재시도한다. 이동 조회 함수가 없어도 차지 루프를 복구하며 비활성 상태에서는 시작하지 않는다.
- 기존 매니저가 살아 있으면 Instance 교체만으로 루프를 이관하지 않는다. 원래 매니저가 파괴된 경우 현재 매니저에서 재시도한다.
- TODO와 클래스 설명을 갱신했다. Codex 리뷰 후 새 private 구조체의 공개 멤버 표현을 get-only 프로퍼티로 보완했다.
- 단발 `PlaySfx`는 중간 정지 요구가 없어 반환값을 계속 무시한다. 공개 API, 중재자, AudioManager, 게임플레이 조건, 씬·프리팹·SO는 변경하지 않았다.

## Codex가 실제 수행한 검증

- 최종 수정 후 Unity `6000.4.8f1` 컴파일: `completed`, `failed: false`, `errors: []`, `compilationFailed: false`.
- `git diff --check` 통과.
- 기존 `RequestLoop`, `ReleaseLoop`, `ApplyMoveLoop`, `UpdateLocomotion`, `UpdateLanding`, `StopAllSounds`, `PlayOneShot`, `TryGetSound`, `EnsureSkillSoundMap` 본문 및 기존 공개 메서드 시그니처가 유지됨을 HEAD와 직접 비교했다.
- 기존 사용자 변경인 두 ProjectSettings 파일은 작업 전후 SHA256이 동일했다. 관련 설정을 수정하지 않았다.
- 임시 preview scene에서 실제 AudioManager와 Controller·Mediator API를 실행한 **39개 검증이 최종 코드에서 모두 통과**했다.
  - 유효한 전용 핸들·원래 매니저 기록, 실제 loop·소스 점유·추적 대상 설정.
  - 대소문자만 다른 동일 키 중복 방지, 같은 SO를 쓰는 두 캐릭터의 독립 재생·정지.
  - 발소리·보드음 교체, 하차 복귀, 공중·구르기 중 이동음 정지, 누락된 보드 매핑의 무음 유지.
  - 이동음과 차지음 동시 재생, 이동 정지 시 차지 유지, 전체 정리.
  - 매니저 부재·소스 상한·빈 클립의 실패 기록 방지, 소스 반환 후 재시도, 취소한 요청 부활 방지.
  - 이동 조회 함수 없이 차지 복구, 비활성 재시도 차단, OnDisable 정리.
  - 서로 다른 매니저에 같은 숫자 ID가 있어도 다른 소유자의 핸들로 정지되지 않음.
  - Instance 변경 뒤 원래 매니저로 정지, 매니저 파괴 뒤 복구·정리, SO 매핑 없이도 정지.
  - 캐릭터 위치 추적, CharacterMediator 경유 시작·종료, 외부 사망 관찰자보다 먼저 정지, 사망 중 재진입·재시작 차단, 재사용 후 새 요청.
- 테스트는 무음 클립·임시 SO·객체를 사용했고, 비공개 참조·상태·lifecycle은 reflection으로 구성했다. finally에서 객체·SO·클립·preview scene을 제거하고 원래 AudioManager Instance와 Random 상태를 복원했다. 테스트 파일이나 에셋은 저장하지 않았다.

## 미검증과 설계 전제

- 실제 Play Mode의 프리팹 전체 시퀀스·씬 전환·클립 청취는 실행하지 않았다. 실제 사운드 에셋 할당은 이번 코드 연결 범위 밖이다.
- Controller가 보관한 루프 핸들은 해당 Controller가 정지한다는 기존 전제를 유지한다. 살아 있는 매니저에서 외부 코드가 같은 핸들을 강제 정지한 상황을 `IsValid`로 감지하지 않는다.
- 커밋·푸시·브랜치 조작·stash 적용은 하지 않았다.

## Claude 중계 상태

Opus 조사·계획 수정 호출은 agents 상태 `idle/blocked` 또는 `idle/working`로 중계 완료 검증에 실패했다. 동일 세션에 저장된 최종 응답을 Codex가 직접 읽어 조사 내용과 갱신 계획을 확인했다. Claude는 Read/Glob/Grep 권한 거부나 대기가 없었다고 보고했다. 중계 실패를 파일 권한 거부로 단정하지 않는다.

Sonnet 최초 구현과 리뷰 보완 호출은 Remote Control 상태행 부재 또는 마지막 상태 `unknown`으로 완료 검증에 실패했다. 저장된 구현과 보완은 diff로 직접 확인했으며 위 컴파일·39개 검증은 Codex가 독립 수행했다. `completed`/exit 0의 정상 중계 완료나 앱 원문 공개 성공으로 보고하지 않는다. 호출 후 이 채팅 소유 세션은 정리했고 중계 검증·도구 권한을 완화하지 않았다. 별도 원문 로그·HANDOVER·Intent·포인터는 만들지 않았다.
