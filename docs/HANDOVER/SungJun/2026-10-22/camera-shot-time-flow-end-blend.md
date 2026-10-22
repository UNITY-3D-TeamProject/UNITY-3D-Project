# 카메라 샷 끝 블렌드에서 시간 강제 정지 제거

## 작업 요약 및 방법
- 로비 전경처럼 게임 시간은 흐르고 플레이어 조작만 막아야 하는데, 연출이 끝나 플레이어 카메라로 돌아오는 끝 블렌드 동안 시간이 무조건 멈췄다.
- Codex 설계 검토([camera-shot-input-only-design.md](../Codex/2026-10-22/camera-shot-input-only-design.md))대로 `CinematicManager.Finish()`의 `GameManager.PauseGameTime(this)` 호출만 지웠다. 끝 블렌드 동안은 마지막 단계의 시간 설정을 그대로 두고, 블렌드가 끝나면 기존 `ReleaseControl()`이 시간·입력 요청을 푼다.
- `docs/cinematic-so-guide.md` 6-2에 "끝 블렌드는 마지막 단계 시간 설정을 따름, 시간은 흐르고 조작만 막으려면 모든 샷에서 Should Pause Time 끄기"를 추가했다.

## 결정 사항
- 새 입력 잠금 기능은 만들지 않는다. 조작 막기는 연출 시작 때 거는 `Cutscene` 입력 맵 요청을 그대로 쓴다.
- 시간을 멈추는 샷이나 대사로 끝나면 끝 블렌드 동안에도 계속 멈춰 있다(그 단계의 정지 요청이 `ReleaseControl`까지 남음).

## 현재 상태 및 이슈
- 컴파일 0에러.
- TestScene Play에서 실행 중에 만든 시간이 흐르는 샷(1초, 끝 블렌드 1초)을 재생해 확인: 샷·끝 블렌드 동안 timeScale 1·입력 맵 `Cutscene`, 끝 블렌드가 끝난 프레임(105)에 `Player`로 복귀.
- 시간을 멈추는 샷은 확인하지 못했다. 에디터 게임 루프가 저절로 진행되지 않아 `EditorApplication.Step()`으로 진행했는데, 정지 샷(1초)이 260프레임이 지나도 끝나지 않았고, `SOCameraShot_Test`(정지 샷 → 흐르는 샷)도 루프가 다시 멈춰 끝까지 못 봤다. 이 경로는 `Finish()` 이전이라 이번 수정과는 무관하지만, 실제 Play에서 정지 샷이 끝나는지 직접 확인이 필요하다.
- 이동 중 연출에 들어갔을 때 입력 해제·관성, 로비 씬 적용은 미확인. 커밋 안 함.

## 다음 할 일 (Next Steps)
- Unity에서 직접 TestScene 카메라 샷 발판(`SOCameraShot_Test`)을 밟아 정지 샷 → 흐르는 샷 → 끝 블렌드(시간 흐름) → 조작 복귀 확인.
- 로비용 SOCameraShot의 모든 샷 Should Pause Time 끄기(로비 담당).
