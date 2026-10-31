---
id: intent-105
title: 3D 효과음 한쪽 귀 쏠림 완화
part: Audio / Core
status: open
created: 2026-10-31
resolved: null
---

## 문제 (Problem)
이어폰으로 들으면 3D 효과음이 한쪽 귀에서만 들리고 반대쪽은 전혀 들리지 않는다. 튜토리얼 플레이어 총소리와 플레이어에게서 떨어진 오브젝트 소리 모두 해당한다.
AudioManager가 3D 효과음을 `spread = 0`으로 내서 리스너 옆에서 난 소리가 한쪽 채널로 100% 쏠린다. 플레이어 총소리는 리스너(Player 루트)와 같은 위치에서 나서 방향을 계산할 수 없어 한쪽으로 쏠린다.

## 기대 결과 (Proposed outcome)
3D 효과음이 방향감은 유지하되 반대쪽 귀에서도 들린다. 퍼짐 정도는 AudioManager 인스펙터에서 조절한다.

## 영향 범위 (Affected users and systems)
- AudioManager의 단발(`PlaySfx`)·반복(`PlaySfxLoop`) 3D 효과음 전체. BGM·UI 소리는 영향 없음.

## 제약 (Constraints)
- Core 폴더 안에서만 수정한다. 호출하는 쪽(SoundController, GimmickUtility 등)의 API는 바꾸지 않는다.
- `PlaySfxLoop`에 더 큰 spread를 직접 넘기는 곳(`MusicFile`의 180)은 그 값을 유지한다.
- 리스너를 카메라로 옮기면 들리는 거리가 달라지므로 리스너 위치는 바꾸지 않는다.

## 열린 질문 (Open questions)
- 없음. 기본 퍼짐 값은 실제 청취 후 인스펙터에서 조정한다.
