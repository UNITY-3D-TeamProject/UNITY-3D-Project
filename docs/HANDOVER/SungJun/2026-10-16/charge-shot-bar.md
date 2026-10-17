# 차지샷 바 추가 + 크로스헤어 색 부드럽게 (2026-10-16)

## 작업 요약 및 방법
- 사용자 참고 이미지(크로스헤어 오른쪽 아래 칸형 바, 퍼센트 제외)에 맞춰 Test MainScene `Canvas/Crosshair` 아래 `ChargeBar`를 Unity MCP `eval`로 만들었다.
  - 위치 (118, -78), 크기 132×22 (크로스헤어 기준, 배터리 라벨 바로 아래)
  - `Background`: Shift `Rounded Filled (500ppu)`, (0.02, 0.08, 0.12, 0.78)
  - `Frame`: Shift `Rounded Outline - Stroke 5px`, (0.62, 0.8, 0.9, 0.75), PPU 배수 3
  - `Segments`: HorizontalLayoutGroup(간격 3, 안쪽 여백 좌우 7·위아래 6) + `Seg (0)`~`(9)` 10칸, `Rounded Filled (500ppu)` PPU 배수 12, 켜짐 (1, 0.42, 0.31, 1) / 꺼짐 같은 색 알파 0.15. 미리보기로 6칸 켜 둠.
  - `ThresholdMark`: 50% 위치 흰 세로선 2×28 (`Filled Square`)
- 사용자 요청("빨강·파랑 바가 너무 찐함")으로 크로스헤어 색을 참고 이미지 톤으로 변경:
  - `HeatArc` (1, 0.094, 0, 0.784) → (1, 0.36, 0.31, 0.92), `BatteryArc` (0, 0, 1, 0.784) → (0.25, 0.66, 1, 0.92)
  - `Canvas/HeatLabel`·`BatteryLabel`의 Icon·Value도 순빨강/순파랑 → (1, 0.42, 0.38) / (0.36, 0.72, 1)로 맞춤
- Game 뷰 캡처가 갱신되지 않아 Canvas 복제본을 임시 카메라·RenderTexture로 렌더해 확인(임시 오브젝트 삭제).

## 결정 사항
- 차지 바는 퍼센트 텍스트 없이 칸 10개 + 50% 눈금으로 표현한다.

## 현재 상태 및 이슈
- 사용자 요청으로 MainScene 저장(사용자 미저장 변경 포함), Test 저장소 e254916 커밋(push 안 함). 씬 오버라이드만 바뀌었고 Test `Canvas.prefab`에는 적용 안 함.
- 바는 시각물만 있고 게임 로직과 연결되지 않았다.
- "50% 넘으면 무조건 차지샷"은 현재 `Fire` 동작(차지 시간 100%에서 자동 발사, 그 전 손 떼면 일반샷)과 다르다 → intent-029 열린 질문.

## 다음 할 일
- intent-029 열린 질문 결정 후 진행률 연결(Fire 진행률 → PlayerFacade → PlayerHudPresenter/View → 칸 켜기).
- 필요하면 Test `Canvas.prefab`에 Apply.
