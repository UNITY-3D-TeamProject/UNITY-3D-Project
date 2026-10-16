# MainScene 크로스헤어 반원형으로 정리 (2026-10-16)

## 작업 요약 및 방법
- 사용자 참고 이미지(가운데 점 + 작은 링, 위 빨강·아래 파랑 원호)에 맞춰 Test MainScene `Canvas/Crosshair`를 Unity MCP `eval`로 수정했다.
  - `HeatArc`(빨강)·`BatteryArc`(파랑) fill 0.4/0.41 → 0.5: 위 반원 180°는 빨강, 아래 반원 180°는 파랑으로 꽉 채움.
  - `CenterDot` 16 → 8px로 줄이고, 그 바깥에 `CenterRing`(30px, 흰색, Shift `Circle Outline - Stroke 12px`, raycastTarget 끔) 추가.
  - 참고 이미지에 없는 `HeatLabel`·`BatteryLabel`(28%/61%)은 비활성화(삭제 안 함).
- 동서남북 작대기: 현재 `Canvas/Crosshair`에는 원래 없음(작대기 `Line_*`는 꺼진 `Canvas_HudReference`에만 있음, 손대지 않음).
- Game 뷰 캡처로 확인. 처음 캡처는 화면이 갱신되지 않아 `RepaintAllViews` 후 다시 찍었다. 캡처용 `Assets/Temp`는 삭제했다.

## 결정 사항
- 원호는 현재 값과 관계없이 각 반원을 꽉 채운 상태(fill 0.5)로 둔다(사용자 요청 "위 빨강 180도, 아래 파랑 180도").

## 현재 상태 및 이슈
- 사용자 요청으로 MainScene 저장(기존 사용자 미저장 변경 포함). 커밋 안 함. 씬 오버라이드로만 바뀌었고 Test `Canvas.prefab`에는 적용하지 않았다.
- 링이 밝은 하늘 배경 위에서는 다소 흐리게 보인다.

## 다음 할 일
- 필요하면 Test `Canvas.prefab`에 Apply.
- 열·배터리 값을 원호에 연결할 때는 fill = 값 × 0.5 규칙 사용.

## 추가 작업: 원호를 크로스헤어에 더 붙임
- 사용자 요청("빨파 원이 너무 커서 더 붙게")으로 `HeatArc`·`BatteryArc` 지름 176 → 84, `RingTrack` 172 → 82로 줄였다.
- 지름을 줄이면 선이 얇아지므로 스프라이트를 원호 `Stroke 5px` → `Stroke 10px`, 트랙 `Stroke 3px` → `Stroke 8px`로 바꿔 두께를 비슷하게 유지했다.
- Game 뷰 캡처가 계속 이전 화면으로 찍혀서, 크로스헤어 복제본을 임시 카메라·RenderTexture로 따로 렌더해 확인했다(임시 오브젝트는 바로 삭제).
- 사용자 요청으로 MainScene 저장(커밋 안 함).
