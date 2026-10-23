# AudioManager 3D 효과음 소스 부족 시 상한까지 추가 생성 (intent-071)

## 1. 작업 요약 및 방법
- 문제: 3D 소스 16개가 모두 재생 중이면 `FindFreeSfxSource`가 -1을 돌려 새 요청이 버려졌다(먼저 온 소리가 이김).
- `Scripts/Core/Audio/AudioManager.cs`만 수정
  - `_maxSfxSourceCount`(기본 64, `[Min(1)]`): 늘릴 수 있는 최대 소스 수. `_sfxSourceCount`(16)는 미리 만드는 개수로 툴팁만 변경.
  - `_hasWarnedSfxLimit`: 상한 경고를 매니저당 한 번만 남기기 위한 표시.
  - `CreateSfxSource(int)`: 3D 소스 하나 생성(`SetupSources` 반복문 내용을 뽑아냄).
  - `AddSfxSource()`: 배열 4개(`_sfxSources`·`_sfxEndTimes`·`_sfxPlayIds`·`_sfxFollowTargets`)를 `Array.Resize`로 1칸 늘리고 새 소스를 붙여 번호 반환.
  - `FindFreeSfxSource()`: 끝난 소스가 없으면 상한 전까지 `AddSfxSource`, 상한이면 경고(한 번) + -1.
  - 상단 사용 흐름 주석, `PlaySfx`·`PlaySfxLoop` 주석의 "모두 재생 중이면" → "상한까지 모두 재생 중이면".

## 2. 결정 사항
- 우선순위 교체는 직접 만들지 않는다. 32개(Max Real Voices)를 넘게 재생되면 Unity가 priority·크기로 들릴 소리를 고른다.
- 늘린 소스는 줄이지 않고 씬이 내려갈 때까지 재사용한다.
- 같은 클립 0.05초 차단, `SOAudioData` priority 칸은 이번 범위 밖.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. TestScene Play: 반복음 70개 → 핸들 64개·소스 16→64·나머지 6개 default·경고 1회, 다 끊은 뒤 재요청은 재사용(소스 64 유지), 늘어난 19번 페이드 정지·39번 따라가기 확인. 청취는 안 함.
- 에디터가 비활성 상태면 Play 프레임이 멈춤(autotick으로도 안 돎) → `editor_focus` 후 확인했다.
- AudioManager 프리팹은 손대지 않음(새 칸은 기본값 64). 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 커밋.
- 실제로 반드시 들려야 하는 소리가 묻히면 `SOAudioData`에 Priority 칸 추가 검토.
