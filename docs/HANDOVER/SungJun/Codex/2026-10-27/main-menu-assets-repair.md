# 메인화면 에셋 참조 복구

## 작업 요약 및 방법
- MenuShade.png, MainMenuSplashBold.mat, MainMenuVolume.asset 및 meta가 정식 경로에서 없어져 참조가 끊긴 상태를 확인했다.
- LeftShade는 Sprite 없이 흰색/alpha0.55로 남아 화면을 하얗게 덮고 있었다. 페이드 Canvas alpha는0으로 정상 상태였다.
- Test 원본을 Assets/_Project/Art/UI/MainMenu에 복원하고 기존 씬/프리팹 GUID를 유지했다. 프로필 자체 참조의 GUID도 복원했다.

## 결정 사항 및 검증
- 씬과 프리팹을 새 GUID로 바꾸지 않고 원래 참조를 복구했다. Test 원본 수정 및 코드 변경 없음.
- MainScene 재오픈 후 LeftShade/TitleReadabilityShade Sprite, 타이틀 머티리얼, VolumeProfile5개 컴포넌트 정상 연결 확인.
- Test 의존성0개. Play에서 카메라 도착/메뉴 표시와 흰색 덮임 해소를 캡처로 확인했다. Console 오류0개.
- runInBackground 원래 값 복원. 커밋/푸시 없음.

## 현재 상태 및 다음 할 일
- 정식 MainScene을 연 상태. 복원한 에셋과 meta를 함께 관리한다.
- 저장 데이터가 있을 때 START 확인 패널을 띄우는 요청은 아직 구현하지 않았다.
