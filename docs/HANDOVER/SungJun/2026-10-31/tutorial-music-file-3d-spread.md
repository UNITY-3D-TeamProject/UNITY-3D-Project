# 튜토리얼 음악 파일을 3D + Spread 180으로 변경

## 작업 요약 및 방법
- Codex가 오른쪽 귀 쏠림을 없애려고 `MusicFile` 반복음을 2D(`spatialBlend: 0`)로 바꿨는데, 2D에서는 거리 감쇠가 적용되지 않아 맵 담당자가 맞춘 거리 설정(독백 위치부터 들리게 한 것)이 무시되고 씬 어디서나 같은 크기로 들렸다.
- 다시 3D로 되돌리고 `AudioSource.spread`를 180으로 줘서 거리 감쇠는 남기고 좌우 쏠림만 없앴다.
- `AudioManager.PlaySfxLoop`와 `GimmickUtility.PlaySfxLoop`의 `spatialBlend` 인자를 `spread` 인자(기본 0)로 바꿨다.
- `AudioManager.PlaySfx`가 Codex가 넣은 `spatialBlend = 1f` 대신 `spread = 0f`로 되돌려, 다시 쓰는 소스에 180이 남지 않게 했다. 3D 소스의 `spatialBlend`는 `CreateSfxSource`에서 1로 정한 뒤 바뀌지 않는다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/Audio/AudioManager.cs`
- `Assets/_Project/Scripts/Map/Gimmicks/GimmickUtility.cs`
- `Assets/_Project/Scripts/Tutorial/MusicFile.cs` (`spread: 180f` 두 곳)

## 결정 사항
- 위치 기반 반복음에서 한쪽 귀 쏠림이 문제일 때는 2D로 바꾸지 않고 `spread`로 해결한다. 2D로 바꾸면 SOAudioData의 Min/Max Distance가 무시된다.

## 현재 상태 및 이슈
- `dotnet build Assembly-CSharp.csproj --no-restore -v:q`: 오류 0개.
- Unity Play에서 실제로 듣지는 않았다. 거리별 크기와 좌우 균형을 확인해야 한다.
- 켜져 있는 `AudioListener`는 카메라가 아니라 `Player.prefab`에 있어서, 다른 3D 효과음도 카메라 방향이 아니라 플레이어 몸 방향 기준으로 좌우가 정해진다.
- 커밋하지 않았다.

## 다음 할 일
- 튜토리얼에서 독백 위치 전에는 안 들리고, 다가갈수록 커지며, 양쪽 귀로 들리는지 확인한다.
