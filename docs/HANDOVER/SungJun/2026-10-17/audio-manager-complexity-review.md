# AudioManager 과설계 검토

## 작업 요약 및 방법
- 현재 작업 트리의 AudioManager, SOAudioData, SOBgmTable, EAudioChannel과 SettingsPresenter, GameManager, Singleton, 관련 intent 및 이전 작업 기록을 정적으로 검토했다.
- Assets/_Project의 C#·씬·프리팹·에셋에서 PlayUi, PlaySfx, SkipBgmTrack 호출 및 직렬화된 연결을 검색했다. 현재 외부 호출/연결은 찾지 못했다. BGM 씬 자동 선택과 설정창 볼륨 연결은 구현되어 있다.
- AudioManager는 478줄이며 빈 줄·주석·region을 제외하면 294줄이다. 줄 수만으로 과설계로 판단하지 않았다.

## 결정 사항
- 기본 구조는 유지할 만하다: 영구 싱글톤, BGM/UI 소스 각 1개, 고정 3D 소스 배열, Mixer 볼륨 및 PlayerPrefs, 씬별 BGM 표.
- 플레이리스트·셔플·SO별 페이드·곡 넘기기는 기존 intent에 요구사항으로 기록되어 있어 미연결이라는 이유만으로 제거를 권하지 않는다.
- 인터페이스, 이벤트 버스, 범용 풀, 추가 서비스 계층을 도입할 필요는 없다. 구조 변경은 이번 검토에서 수행하지 않았다.

## 현재 상태 및 이슈
- BGM 전환과 곡 넘기기가 서로 다른 진입 경로를 쓴다. PlayBgm은 이전 곡 페이드아웃 전에 _currentBgm을 새 SO로 바꾸지만 loop, 페이드아웃 시간, 트랙 번호는 CoSwitchBgm의 페이드아웃 뒤에서 초기화한다. 그 사이 SkipBgmTrack이 기존 코루틴을 취소하면 CoSkipBgmTrack은 이 초기화를 거치지 않아 이전 설정이 남을 수 있다. 코드 경로상 확인이며 Play 재현은 하지 않았다.
- TryPickClip은 선택·중복 제한·시각 기록을 함께 수행한다. PlayUi에도 적용되어 intent의 'UI 연타 제한은 1단계에서 제외'와 어긋난다. PlaySfx는 빈 소스 확인보다 먼저 시각을 기록하므로 재생되지 않은 요청도 제한 기준이 된다.
- SOAudioData에 BGM 전용과 3D 전용 필드가 섞여 있어 설정 부담은 있으나, 현재 크기에서는 별도 상속 구조를 만드는 것보다 용도별 표시 정리 정도가 적절하다.
- 컴파일, Unity Play, 실제 청취 검증은 이번 검토에서 실행하지 않았다. 기존 미커밋 코드와 에셋 변경을 보존했다.

## 다음 할 일
- 수정 요청 시 BGM 전환 중 곡 넘기기의 정책과 초기화 흐름을 먼저 정리한다.
- 중복 제한의 적용 대상과 기록 시점을 정리한다. UI 적용은 기존 intent와 맞춘다.
- 추가 구조 확장보다 사격음·UI음·곡 넘기기 버튼 연결 및 실제 청취 검증을 우선한다.

## 변경된 파일
- 이 검토 기록과 SungJun/HANDOFF_POINTER.md만 변경했다.
