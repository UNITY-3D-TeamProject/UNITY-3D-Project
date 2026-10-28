# SO별 오디오 거리 감쇠

## 작업 요약 및 방법
- SOAudioData에 Linear(기본값)와 Logarithmic 선택 및 거리 설명 헤더를 추가했다. 기존 SO 32개에도 Linear를 명시했다.
- AudioManager의 단발·반복 3D 소스 재생 때마다 SO의 감쇠 방식을 적용해 소스 재사용 시 이전 설정이 남지 않게 했다.

## 결정 사항
- 기존 거리 값과 호출 API는 유지한다. Linear의 최대 거리가 최소 거리 이하면 적용값을 최소 거리 + 0.01로 보정한다.
- 거리 기준은 기존 Audio Listener 위치다. 도둑 발소리 SO 값은 변경하지 않았다.

## 현재 상태 및 이슈
- SO 32개가 Linear로 직렬화되고 저장된 거리 범위가 유효한지 확인했다. git diff --check 통과. Unity Pipeline 서버에 연결되지 않아 컴파일·실제 청취와 기존 SO 역직렬화는 확인하지 못했다. 기존 3D 소리의 음량 감쇠와 최대 거리 밖 재생이 달라진다.
- 이후 Unity에 연결해 컴파일 완료. TestScene의 AudioDistanceTestArea(-100, 0, 55)와 테스트 톤을 이용해 AudioManager 반복음의 출력 RMS를 측정: 5m 0.1071, 10m 0.1081, 12.5m 0.0538, 14m 0.0216, 15m·18m 0. 에디터 실청과 다른 음원 밸런스는 미확인. 실제 도둑 발소리 SO는 클립이 비어 있다. 테스트 씬과 자산은 Git 제외된 Assets/Test에 있다.

## 다음 할 일
- 발소리 클립을 연결하고 플레이어·보스 소리의 거리별 음량과 풀 재사용을 청취 검증한다.
- 최신 develop을 fast-forward한 뒤 feature/system-audio-distance 브랜치에서 변경을 복원했다. 기존 intent-081과 겹쳐 이 작업은 intent-082로 변경했다.
- 검증 후 intent-082를 해결 처리한다.
