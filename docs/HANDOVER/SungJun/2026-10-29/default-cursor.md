# 2026-10-29 — 기본 마우스 커서 교체

## 1. 작업 요약 및 방법
- 사용자가 준 네온 화살표 이미지(1254×1254 webp, 배경 투명)로 기본 커서를 바꿨다.
- 알파 40 이상 영역 기준으로 정사각형 크롭 → 64×64로 줄여 `Assets/_Project/Art/UI/Cursor/Cursor_Default.png`에 저장했다.
- `.meta`는 기존 PNG meta를 바탕으로 Cursor 타입에 맞게 작성: `textureType: 7`(Cursor), `isReadable: 1`, 밉맵 없음, 압축 없음(`textureCompression: 0`), `spriteMode: 0`.
- `ProjectSettings.asset`의 `defaultCursor`에 위 텍스처를 넣고 `cursorHotspot`을 `(6, 7)`로 설정했다. 핫스팟은 원본 이미지에서 흰 테두리 화살표 끝 픽셀 위치를 64 기준으로 환산한 값이다.
- 이 프로젝트를 연 Unity 에디터가 없어서(열려 있던 건 TestFor3DProject) 파일을 직접 수정했다.

## 2. 결정 사항
- 커서는 코드(`Cursor.SetCursor`) 없이 Player Settings의 Default Cursor로만 지정한다. 상황별 커서 교체가 필요해지면 그때 코드를 추가한다.
- `Assets/ThirdParty/`는 git에 안 올라가므로 커서 텍스처는 `_Project/Art/UI/Cursor/`에 둔다.

## 3. 현재 상태 및 이슈
- 에디터에서 열어 임포트·실제 표시를 확인하지 않았다. 확인할 것: Player Settings의 Default Cursor 칸이 채워져 있는지, 클릭 지점이 화살표 끝과 맞는지, 크기가 적당한지.
- 플레이 중에는 `GameManager.SetCursorLocked`가 커서를 숨기므로 커서가 풀려 있는 화면(메인 메뉴·설정창 등)에서만 보인다.
- 커밋 안 함.

## 4. 다음 할 일
- 에디터에서 위 3가지 확인. 너무 크면 텍스처 Max Size를 32로 줄이고 핫스팟을 `(3, 3)`으로 바꾼다.
