---
id: intent-082
title: SO별 3D 효과음 거리 감쇠 선택
part: Audio / Core
status: open
created: 2026-10-27
resolved: null
---

## 문제 (Problem)
발소리를 지정한 거리에서 무음으로 만들 수 없고 기존 SO의 거리 설명이 부정확하다.

## 기대 결과 (Proposed outcome)
모든 기존 SO와 신규 SO가 기본적으로 선형 감쇠를 사용하고, SO별로 로그 감쇠를 선택할 수 있다. Inspector에서 거리의 의미를 확인한다.

## 영향 범위 (Affected users and systems)
- SOAudioData와 AudioManager의 단발·반복 3D 효과음.

## 제약 (Constraints)
- 기존 호출 API를 유지하고 BGM·UI 재생은 영향을 받지 않는다. 기존 3D 소리의 거리별 음량 변화는 사용자 요청 범위다.
- 소스 재사용 시 각 재생 요청의 감쇠 방식을 적용한다.

## 열린 질문 (Open questions)
- 없음.

## 진행 상황 (2026-10-27)
- SO의 Logarithmic 기본값과 Linear 선택을 단발·반복 소스 재생에 적용하고 거리 헤더를 추가했다.
- 기존 SO 에셋은 수정하지 않았다. Linear에서 최대 거리가 최소 거리 이하면 런타임에서 최소 거리보다 0.01 크게 적용한다.
- diff 공백 검사는 통과했다. Unity Pipeline 서버에 연결되지 않아 컴파일과 실제 청취 검증은 남아 있다.
- 사용자 요청에 따라 기본값과 기존 SO 32개를 Linear로 변경했다. 기존 Min/Max 값은 유지하며 거리별 음량과 최대 거리 밖 재생이 달라진다.
- SO 32개의 직렬화 값과 거리 범위를 정적 검사했다. Unity 컴파일·재생·청취는 Pipeline 서버에 연결되지 않아 확인하지 못했다.
- Unity 컴파일 성공. 로컬 TestScene의 AudioDistanceTestArea에 테스트 음원과 거리 표시를 만들고 AudioManager 반복음 출력을 측정했다. RMS: 5m 0.1071, 10m 0.1081, 12.5m 0.0538, 14m 0.0216, 15m·18m 0. 실제 청취 및 다른 효과음 밸런스 확인은 남았다. 도둑 발소리 SO는 클립 참조가 비어 있어 테스트용 톤을 사용했다.
