# 미로 스캔 실루엣 색 구분 (intent-099)

## 1. 작업 요약 및 방법
- 스캔하면 벽 뒤로 비쳐 보이는 실루엣 색을 종류별로 나눴다: 연산자 파랑, 숫자(피연산자) 초록, GC·도둑 빨강.
- 새 머티리얼 `Art/Scan/M_ScanSilhouette_Operator.mat`(HDR 0.3, 0.7, 1.5)와 `M_ScanSilhouette_Enemy.mat`(HDR 1.5, 0.2, 0.2)를 만들었다. 둘 다 `M_ScanSilhouette`을 복제해 `_Color`만 바꿨다.
- `SilhouetteReaction.RefreshBaseColors()` 추가: Awake에서 저장해 둔 페이드 기준 색을 현재 머티리얼에서 다시 읽는다.
- `MazePuzzleItem.Setup`: Kind 머티리얼을 바꾼 뒤 같은 오브젝트의 `SilhouetteReaction.RefreshBaseColors()`를 부른다.
- `MazePuzzleItem.prefab`: `_kindRenderers` 4번째 칸에 `ScanSilhouette` 렌더러를 넣고, 숫자 쪽에 `M_ScanSilhouette`, 연산자 쪽에 `M_ScanSilhouette_Operator`를 넣었다.
- `GC.prefab`·`Thief.prefab`: `SilhouetteReaction._autoShellMaterial` → `M_ScanSilhouette_Enemy`.

## 2. 결정 사항
- 색은 머티리얼에 둔다(스캔 가이드의 "다른 색은 머티리얼 복제" 규칙). `M_ScanSilhouette`은 다른 대상도 쓰므로 그대로 둔다.
- GC와 도둑은 같은 빨강 머티리얼을 함께 쓴다.

## 3. 현재 상태 및 이슈
- 확인: 리컴파일 0에러, 프리팹 연결값 확인, 격리 로드한 아이템에서 연산자 → 파랑, 숫자 → 초록, 페이드 알파 정상.
- 미확인: 실제 스캔 스킬로 본 벽 뒤 모습(맵 씬은 열지 않음).
- `ProjectSettings/ShaderGraphSettings.asset` 변경은 Unity가 자동으로 만든 것이며 이번 작업과 무관하다.
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- TestScene이나 파일 앱 2라운드에서 스캔해 색과 밝기를 확인하고, 필요하면 머티리얼 `_Color`를 조절한다.
