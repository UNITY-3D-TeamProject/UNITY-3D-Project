# 튜토리얼 음악 파일의 오른쪽 편향 수정

## 작업 요약 및 방법
- 튜토리얼 씬의 `MusicFile` 오디오 참조에서 재생 코드, 오디오 데이터, 원본 WAV까지 추적했다.
- 원본 WAV의 좌우 채널에 모두 신호가 있음을 확인했다.
- `MusicFile`의 반복 재생만 2D로 지정해 플레이어 방향에 따른 좌우 편향을 제거했다.
- 재사용되는 오디오 소스가 다음 3D 효과음을 재생할 때 `spatialBlend`를 1로 다시 설정했다.

## 변경된 파일
- `Assets/_Project/Scripts/Tutorial/MusicFile.cs`
- `Assets/_Project/Scripts/Map/Gimmicks/GimmickUtility.cs`
- `Assets/_Project/Scripts/Core/Audio/AudioManager.cs`

## 결정 사항
- `MusicFile`이 `PlaySfxLoop`로 곡을 재생하며, 이 경로의 `AudioSource.spatialBlend`는 `1`이다. 따라서 플레이어의 `AudioListener`를 기준으로 위치에 따라 좌우가 달라진다.
- 기존 SFX 믹서와 핸들 기반 중지 동작을 그대로 쓰면서 이 음악만 `spatialBlend: 0f`를 전달한다.

## 현재 상태 및 이슈
- `dotnet build Assembly-CSharp.csproj --no-restore -v:q` 통과: 오류 0개, 기존 경고 87개.
- Unity Play 중 실제 좌우 출력은 측정하지 않았다.
- 카메라 프리팹의 `AudioListener`는 비활성이고 플레이어 프리팹의 `AudioListener`는 활성이다.

## 다음 할 일
- Unity Play에서 음악 파일의 좌우 출력과 다른 위치 효과음의 방향감을 확인한다.
