# 메인 메뉴 플레이어 발광 줄이기

## 작업 요약 및 방법
메인씬(`MainScene`) 메뉴 화면에서 플레이어 몸의 불빛이 너무 세서 많이 줄였다. `Player.prefab`은 수정하지 않았다.

- 원인: 머티리얼 `_Emissive_Power`(몸 10, 총·파워팟 29.62)가 높고, 씬의 Bloom(세기 5, 임계값 0.8)이 이를 크게 번지게 한다.
- `Assets/_Project/Art/Materials/MainMenu/`에 원본 머티리얼 복사본 3개를 만들고 발광 값만 낮췄다.

| 복사본 | 대상 | `_Emissive_Power` |
|---|---|---|
| `M_MikeBody_MainMenu` | 몸(`MikeZ` 0번) | 10 → 2 |
| `M_RadicalBuster_Blue_MainMenu` | 양손 총 | 29.62 → 5 |
| `M_PowerPod_Blue_MainMenu` | 등 파워팟 | 29.62 → 5 |

- 렌즈(`M_Mike_Lens`)와 Bloom은 그대로 뒀다.
- `MainScene.unity`의 `LobbyMenuBackdrop` 인스턴스에 `MainMenu_Player` 렌더러 머티리얼 오버라이드 4개만 추가했다(HEAD 대비 16줄 추가).

## 결정 사항
- 원본 머티리얼은 `ThirdParty`(Git 제외)에 있고 다른 곳도 함께 쓰므로 수정하지 않고 복사본을 쓴다. 코드 추가도 없다.
- 오버라이드는 프리팹이 아니라 씬에 둔다. 인게임 플레이어, `Player.prefab`, `LobbyMenuBackdrop.prefab`에는 영향이 없다.
- 수치는 Play 중 MaterialPropertyBlock으로 시험해 정했다. 더 조절하려면 위 복사본의 `Emissive Power`만 바꾸면 된다.

## 현재 상태 및 이슈
- 커밋 안 함. 최종 화면 확인은 사용자가 한다.
- 씬 저장 시 불필요한 변경이 함께 기록됨: 커밋 안 된 `LobbyMenuBackdrop.prefab` 수정 때문에 Unity에서 `MainScene`을 저장하면 RectTransform 오버라이드 70개가 추가되고, `m_Sun`이 비워진다. 프리팹에서 그 라이트(`5335209271577443653`)가 빠졌기 때문이다. 이번 커밋에서는 HEAD 씬에 머티리얼 오버라이드만 넣어 제외했지만, 다음에 씬을 저장하면 다시 생긴다.
- Unity 창에 포커스가 없으면 Play가 진행되지 않는다(`runInBackground` 꺼짐). 검증할 때 Play 중에만 켰고, 종료 후 꺼짐으로 돌아왔으며 `ProjectSettings`는 변경 없음을 확인했다.

## 다음 할 일 (Next Steps)
- 메인 메뉴 화면에서 밝기를 확인하고 필요하면 수치 조정.
- `LobbyMenuBackdrop.prefab` 커밋 때 위 `m_Sun`·RectTransform 변경을 어떻게 처리할지 결정.
