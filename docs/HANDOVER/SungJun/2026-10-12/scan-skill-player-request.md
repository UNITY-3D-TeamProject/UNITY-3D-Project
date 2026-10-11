# Scan 스킬 파동 연결 요청 (Player 파트 전달용, 2026-10-12)

## 작업 요약 및 방법
- Player 파트에 전달할 Scan 스킬 연결 요청문을 작성했다(아래 "전달문").
- 확인 결과
  - 입력 → `PlayerInputComponent.OnScan` → `SkillMediator` → `SkillController.TryExecuteSkill("Scan")` → `Scan.Execute()` 경로는 이미 연결돼 있다. `Scan.Execute()`에서 `ScanWave.Begin`만 부르면 된다.
  - Player 프리팹 안에서 Scan 인스턴스의 local position은 (0, 0, 0)으로 덮어써져 있다. 이전 답변의 "위치 확인 필요"는 잘못된 지적이었다(Scan.prefab 원본 값만 보고 판단).
  - `Art/Scan/TerrainScanner_tut_blue.mat`(+셰이더 그래프)는 이미 `_Project`에 커밋돼 있고 `Assets/Test` 사본과 값이 같다. 옮길 필요 없이, TempScene 파동 파티클(Sphere)이 Test 사본을 쓰던 것만 `_Project` 머티리얼로 교체했다. 씬은 저장하지 않았다(dirty 상태).

- 파동을 `Prefabs/Skills/ScanWave.prefab`으로 만들었다(루트 위치 0, `ScanWave` + 자식 `Sphere` 파티클, 머티리얼은 `Art/Scan/TerrainScanner_tut_blue`, Test 폴더 참조 없음). 시험용 `ScanTestTrigger`는 프리팹에서 빼고, TempScene의 인스턴스에만 씬 오버라이드(추가 컴포넌트)로 다시 붙였다(값 유지: 10초, 300m, 자기 ScanWave 참조).

## 결정 사항
- `SkillMediator`에 `case Scan`을 추가하지 않는다. 파동은 캐릭터와 무관하게 동작하므로 `Scan` 스킬이 `ScanWave`를 직접 부른다.
- 스캔 쿨타임은 파동 지속 시간 이상이어야 한다(`ScanTarget`의 "스캔이 겹치지 않음" 전제).

## 현재 상태 및 이슈
- `Scan.cs`, `Scan.prefab`은 Player 파트 소유라 수정하지 않았다.
- `Scannable` 레이어는 아직 없다.
- TempScene 저장 필요(파동 머티리얼 교체 포함).

## 다음 할 일
1. 전달문을 Player 파트에 공유.
2. 연결 후 `ScanTestTrigger`(R키) 비활성화.
3. `Scannable` 레이어 생성 후 공유.

## 전달문

> **[스캔] Scan 스킬에 스캔 파동 연결 부탁드립니다**
>
> 스캔 대상 쪽(`Core/Scanning`: 숨은 플랫폼 드러내기, 실루엣, 마커, 약점)은 끝났고, 이제 Scan 스킬에서 파동만 재생하면 됩니다.
> 입력 → SkillMediator → `Scan.Execute()` 경로는 이미 연결돼 있어서 **SkillMediator는 수정할 필요 없고, `Scan.cs`와 `Scan.prefab`만 바꾸면 됩니다.**
>
> **1. `Scan.cs`**
> ```csharp
> using Core.Scanning;
> using Skill.Core;
> using UnityEngine;
>
> namespace Skill.Skills
> {
>     public class Scan : SkillBase
>     {
>         #region Serialized Fields
>         [Header("Scan")]
>         [Tooltip("파동이 확산하는 시간이자 닿은 대상이 활성화되는 시간(초)")]
>         [SerializeField, Min(0.0f)] private float _duration = 6.0f;
>         [Tooltip("파동의 최대 지름(m)")]
>         [SerializeField, Min(0.0f)] private float _maxDiameter = 30.0f;
>
>         [Header("References")]
>         [Tooltip("재생할 스캔 파동. Scan 오브젝트 아래에 둔다.")]
>         [SerializeField] private ScanWave _scanWave;
>         #endregion
>
>         #region Unity Lifecycle
>         protected override void Awake()
>         {
>             base.Awake();
>             Debug.Assert(_scanWave != null, $"[{name}] ScanWave 가 연결되지 않았습니다.");
>         }
>         #endregion
>
>         #region Protected Methods
>         protected override void Execute()
>         {
>             // 플레이어 위치에서 파동을 시작한다. 중심은 이 시점에 고정되어 이후 움직여도 제자리에 남는다.
>             if (_scanWave) _scanWave.Begin(transform.position, _duration, _maxDiameter);
>             base.Execute();
>         }
>         #endregion
>     }
> }
> ```
> - `base.Execute()`는 꼭 남겨 주세요. HUD 쿨타임 표시가 이걸로 동작합니다.
>
> **2. `Scan.prefab` 구성**
> - `Prefabs/Skills/ScanWave.prefab`(파동 파티클까지 구성 완료)을 Scan 오브젝트의 자식으로 넣어 주세요. 위치는 (0, 0, 0).
> ```
> Scan  (Scan, CooldownCost)
> └─ ScanWave  (ScanWave.prefab)
> ```
> - `Scan`의 Scan Wave 칸에 자식 `ScanWave`를 연결해 주세요.
> - 파티클 설정은 건드리지 않으셔도 됩니다. 지속 시간과 크기는 `Scan`의 `_duration`, `_maxDiameter`가 실행 때 넣어 줍니다.
> - Scannable Layers는 일단 기본값(Everything)입니다. 스캔 대상 레이어는 만들어서 따로 공유드릴게요.
>
> **3. 쿨타임**
> - `CooldownCost`의 Cooldown을 `_duration`(6초) 이상으로 맞춰 주세요. 지금은 1초입니다.
> - 쿨타임 안에 다시 쓰면 파동이 처음부터 다시 시작되고, 대상 쪽 유지 시간 계산이 "스캔이 겹치지 않는다"는 전제로 짜여 있어서 꼬입니다.
>
> **확인 방법**: Play에서 스캔 키를 누르면 플레이어 위치에서 구가 퍼지고, 숨은 플랫폼이 닿은 곳부터 드러나고, HUD에 쿨타임이 돌면 됩니다.
> 테스트 씬의 R키 시험 스크립트는 제가 끌게요. 궁금한 점 있으면 말씀 주세요!
