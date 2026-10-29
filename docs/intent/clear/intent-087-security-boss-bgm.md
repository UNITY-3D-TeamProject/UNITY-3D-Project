---
id: intent-087
title: 보안맵과 보스전 단계별 BGM 연결
part: Audio / Stage / SecurityApp
status: resolved
created: 2026-10-30
resolved: 2026-10-30
---

## 문제 (Problem)
보안맵의 일반 구간과 보스전 전후 단계에 맞는 BGM 세 곡이 연결되지 않았다.

## 기대 결과 (Proposed outcome)
- 보스전 전까지 `App Defense Protocol.wav`를 재생한다.
- 보스전 시작부터 필드보스의 최종 처치 전까지 `Antivirus Protocol.wav`를 재생한다.
- 필드보스가 두 번째로 쓰러져 최종 단계로 넘어가면 `Corrupted Virus.wav`를 재생한다.
- 보스전 중 사망하여 다시 시작하면 초반 보스 곡으로 돌아간다.

## 영향 범위 (Affected users and systems)
- 보안맵 플레이어.
- `Map_4_Security`의 AudioManager·SecurityApp, 보스 단계 이벤트, BGM 에셋.

## 제약 (Constraints)
- 기존 `AudioManager.ChangeBgm`과 믹서 경로를 사용한다.
- 보스 전투 로직과 다른 맵의 음악은 변경하지 않는다.
- 사용자 후속 지시: WAV 세 개는 `Assets/ThirdParty/Audio/Bgm/`에 둔다. `SOAudioData`는 기존 프로젝트 위치에 둔다.

## 열린 질문 (Open questions)
- 없음. 파일과 사용 순서는 사용자 요청으로 확정했다.

## 해결 기록 (Resolution)
- 최종 결정: WAV 세 개를 `Assets/ThirdParty/Audio/Bgm/`으로 옮기고 GUID를 유지했다. 보안맵 AudioManager의 씬 BGM은 `App Defense Protocol`, 3라운드 진입 시 `Antivirus Protocol`, 필드보스 두 번째 다운으로 `EBossStage.Final` 진입 시 `Corrupted Virus`로 전환한다. 보스 라운드 중 사망 후 재시작하면 초반 보스 곡으로 돌아간다.
- 검증: Unity 컴파일 오류 0, 세 클립·씬 참조·스트리밍 설정·보스 두 번째 다운의 최종 단계 전환 확인. Map_4_Security Play Mode에서 시작 곡 재생과 전환 메서드 호출로 초반·최종 곡 선택 확인. 실제 입력으로 보스 처치하거나 귀로 음량 밸런스를 확인하지는 않았다.
- 관련 커밋/PR: 없음.
