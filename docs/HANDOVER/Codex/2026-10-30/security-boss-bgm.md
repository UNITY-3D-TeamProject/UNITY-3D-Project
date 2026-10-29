# 보안맵 단계별 BGM

## 작업 요약 및 방법
- 사용자 WAV 3개를 처음에는 `Assets/_Project/Audio/Bgm/Security/`로 복사하고 스트리밍 임포트로 설정했다. 사용자 후속 지시에 따라 세 WAV와 `.meta`를 `Assets/ThirdParty/Audio/Bgm/`으로 옮기고 빈 원래 폴더를 삭제했다.
- 각 곡에 `SOAudioData` 에셋을 만들었다. 보안맵 씬의 AudioManager 시작 곡은 `App Defense Protocol`, `SecurityApp`의 보스 라운드 시작 곡은 `Antivirus Protocol`, `BossEncounter.OnStageChanged(Final)` 곡은 `Corrupted Virus`로 연결했다.
- 보스 라운드에서 사망·재시작하면 초반 보스 곡으로 복귀하도록 했다. 첫 B 다운의 그로기에서는 곡을 유지한다.

## 결정 사항
- 기존 `AudioManager.ChangeBgm`의 페이드 전환과 음악 믹서 그룹을 사용한다.
- 각 BGM SO는 한 곡을 반복하며 음량 0.6, 피치 1, 페이드 인·아웃 각 1초로 설정했다.

## 현재 상태 및 이슈
- Unity 재컴파일 오류 0. 세 곡의 씬 참조, 자동 재생, 스트리밍 임포트, B 두 번째 다운의 Final 단계 전환을 에디터에서 확인했다. Map_4_Security Play Mode에서 `App Defense Protocol` 재생을 확인하고 단계 전환 메서드를 호출해 `Antivirus Protocol` → `Corrupted Virus` 선택을 확인했다. 원래 열려 있던 Map_3_File 씬으로 복귀했다.
- 실제 보안맵 플레이 중의 청취·음량 밸런스는 확인하지 않았다.
- `Assets/ThirdParty/`는 `.gitignore` 대상이지만, 이 세 WAV와 메타 파일은 `f629ad1e`에 명시적으로 포함했다.
- `Map_4_Security.unity`의 연결 변경은 사용자 요청에 따라 커밋에서 제외했다.

## 다음 할 일
- 보안맵에서 1·2라운드 → 3라운드 → B 두 번째 다운 → 사망·재시작의 음악 전환을 청취하며 음량을 조정한다.
