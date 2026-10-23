---
id: intent-071
title: AudioManager 3D 효과음 소스가 모자라면 상한까지 더 만들기
part: Audio / Core
status: resolved   # open | resolved
created: 2026-10-23
resolved: 2026-10-23 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`AudioManager`는 Awake에서 3D 효과음 소스를 `_sfxSourceCount`(16)개만 만들고, 모두 재생 중이면 `FindFreeSfxSource`가 -1을 돌려 새 요청을 그냥 버린다.
- 먼저 온 소리가 이기는 구조라, 막 발생한 중요한 소리가 오래 남은 꼬리음·반복음 때문에 빠질 수 있다.
- 반복음(`PlaySfxLoop`)은 `StopSfx`까지 소스를 점유하므로 동시에 여러 개 켜지면 단발음 자리가 더 빨리 바닥난다.

## 기대 결과 (Proposed outcome)
- 빈 소스가 없으면 3D 소스를 하나 더 만들어 그 자리에서 재생한다. 끝난 소스는 이후 요청에서 다시 쓴다.
- `_sfxSourceCount`(16)는 미리 만들어 두는 개수로 남기고, 새 칸 `_maxSfxSourceCount`(기본 64)까지만 늘린다.
- 상한까지 모두 재생 중이면 지금처럼 새 요청을 생략하고, 매니저마다 한 번 경고 로그를 남긴다. (멈추지 않은 반복음 같은 누수를 드러내기 위함, 매 요청마다 찍으면 로그가 넘친다)
- 소스가 32개(프로젝트 설정 Max Real Voices)를 넘게 재생되면 실제로 들릴 소리는 Unity가 priority·들리는 크기로 고른다. 직접 우선순위 교체는 만들지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 3D 효과음을 내는 모든 기능(`PlaySfx`·`PlaySfxLoop`). 호출 방법은 바뀌지 않는다.
- 어떤 시스템/모듈이 건드려지는가: `Scripts/Core/Audio/AudioManager.cs`만. AudioManager 프리팹은 새 칸이 기본값(64)으로 들어오므로 손대지 않는다.

## 제약 (Constraints)
- 지켜야 할 정책: 핸들·StopSfx·페이드·같은 클립 0.05초 차단·UI·BGM 동작은 바꾸지 않는다.
- 쓰지 말아야 할 것: 우선순위 교체(재생 중인 소리 끊기), `SOAudioData` priority 칸, 같은 클립 차단 예외는 이번에 넣지 않는다. 실제 문제가 보이면 따로 다룬다.

## 열린 질문 (Open questions)
- 없음 (상한 64, 경고는 매니저당 한 번으로 결정)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `_maxSfxSourceCount`(기본 64, `[Min(1)]`) 추가. `FindFreeSfxSource`는 끝난 소스가 없으면 상한 전까지 `AddSfxSource`(배열 4개 `Array.Resize` + `CreateSfxSource`)로 하나 더 만들고, 상한이면 -1 + `_hasWarnedSfxLimit`로 매니저당 경고 한 번. 늘린 소스는 씬이 내려갈 때까지 남아 재사용. `_sfxSourceCount`(16)는 미리 만드는 개수로 툴팁만 변경, 프리팹 수정 없음.
- 확인: 컴파일 0에러. TestScene Play에서 반복음 70개 요청 → 64개 핸들 발급·소스 16→64·나머지 6개 `default`·경고 1회, 모두 끊은 뒤 새 요청은 소스를 더 만들지 않고 재사용. 늘어난 소스 19번 페이드 정지·39번 대상 따라가기 확인. 청취는 안 함.
- 관련 커밋/PR: 커밋 안 함
