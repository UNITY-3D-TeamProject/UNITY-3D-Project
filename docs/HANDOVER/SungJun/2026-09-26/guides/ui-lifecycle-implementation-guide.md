# UI 생성·소멸 처리 적용 안내

> 최신 범위 정정: 사용자는 플레이어의 별도 삭제 기능이 아직 없고 씬 전환 시 자동 제거를 예상한다고 설명했다. 플레이어와 HUD가 함께 제거되는 일반적인 단일 씬 교체라면 아래의 단독 삭제 감지(LateUpdate), PlayerLifetime, 지연 생성 대응을 일괄 추가할 필요가 없다. 기존 UI OnDisable 정리와 새 씬 스폰 연결을 유지하고 씬 전환 직전 능력치 저장을 연결하는 것을 우선한다. 아래 코드는 추가 상황을 실제 지원할 때 참고하는 선택 사항이며 현재 필수 적용안이 아니다.

이 문서는 적용 방법과 코드 예시다. 런타임 소스에는 아직 적용하지 않았다. 예시를 컴파일하거나 Play Mode에서 실행한 결과가 아니다.

## 처리할 범위

1. View가 없을 때 Presenter가 구독하지 않는다.
2. 현재 플레이어가 삭제되면 등록을 해제하고 HUD 게이지를 0으로 비운다.
3. GameManager가 늦게 생성돼도 UIManager와 PlayerSpawner가 초기화 완료까지 기다린다.

표시 정책은 예시에서 **게이지 초기화**로 정한다. HUD Root를 숨기면 그 아래의 UIManager도 함께 꺼질 수 있으므로 자동 숨김을 섞지 않는다. 기존 Show/Hide는 별도로 사용할 수 있다.

GameManager는 준비된 뒤 기존 Singleton처럼 계속 유지한다. 플레이어에 새 컴포넌트를 붙일 수 없다는 사용자 제약에 따라 GameManager에서 보관한 AttributeSet의 파괴 여부를 확인한다. 풀링의 SetActive(false)는 삭제로 취급하지 않는다. GameManager 자체의 런타임 교체까지 지원하는 예시는 아니다.

## 1. PlayerHudPresenter.cs: 기존 Bind와 RefreshAll 교체

대상: `Assets/_Project/Scripts/UI/PlayerHudPresenter.cs`

```csharp
public void Bind(AttributeSet attributeSet)
{
    // 활성 상태와 관계없이 이전 구독부터 해제한다.
    if (_attributeSet != null)
    {
        _attributeSet.RemoveOnAttributeChangedCallback(OnAttributeChanged);
    }

    _attributeSet = attributeSet;

    if (_view == null)
    {
        return;
    }

    if (_attributeSet == null)
    {
        RefreshAll(); // 연결 해제 시 마지막 값을 지운다.
        return;
    }

    if (isActiveAndEnabled)
    {
        _attributeSet.AddOnAttributeChangedCallback(OnAttributeChanged);
        RefreshAll();
    }
}

public void RefreshAll()
{
    if (_view == null)
    {
        return;
    }

    if (_attributeSet == null)
    {
        _view.SetHp(0.0f, 1.0f);
        _view.SetBattery(0.0f, 1.0f);
        _view.SetHeat(0.0f, 1.0f);
        return;
    }

    RefreshHp();
    RefreshBattery();
    RefreshHeat();
}
```

기존 `OnEnable`/`OnDisable`은 유지한다.
`OnAttributeChanged` 본문 맨 앞에는 아래 검사를 추가한다. 기존 HP/배터리/열 분기는 그대로 둔다.

```csharp
if ((_attributeSet == null) || (_view == null))
{
    return;
}
```

## 2. PlayerState.cs: 현재 플레이어 등록 해제 메서드 추가

대상: `Assets/_Project/Scripts/Core/PlayerState.cs`

```csharp
public bool UnregisterPlayer(AttributeSet attributeSet)
{
    if (ReferenceEquals(attributeSet, null) ||
        !ReferenceEquals(_currentAttributeSet, attributeSet))
    {
        return false;
    }

    _currentAttributeSet = null;
    return true;
}
```

새 플레이어 B가 이미 등록된 뒤 이전 플레이어 A가 삭제되는 상황을 고려한 비교다. A의 삭제로 B의 등록을 지우면 안 된다. 파괴 중인 Unity 객체의 관리 참조 자체를 비교하려고 `ReferenceEquals`를 쓴다. 저장된 능력치 Dictionary는 그대로 둔다.

## 3. GameManager.cs: 제거 알림과 초기화 상태 추가

대상: `Assets/_Project/Scripts/Core/GameManager.cs`

프로퍼티 및 이벤트 선언 위치에 추가한다. 기존 OnPlayerSpawned는 유지한다.

```csharp
public bool IsInitialized => playerState != null;
public event Action OnPlayerDespawned;
```

아래 메서드를 추가한다.

```csharp
public void UnregisterPlayer(AttributeSet attributeSet)
{
    if (playerState == null || !playerState.UnregisterPlayer(attributeSet))
    {
        return;
    }

    OnPlayerDespawned?.Invoke();
}
```

`IsInitialized`는 객체가 발견됐더라도 Awake에서 PlayerState를 만들기 전이면 기다리도록 하는 조건이다.

## 4. Singleton.cs: 오류를 출력하지 않는 조회 메서드 추가

대상: `Assets/_Project/Scripts/Core/Singleton.cs`

기존 Instance, Awake, OnDestroy는 유지하고 클래스 안에 다음 메서드를 추가한다.

```csharp
public static bool TryGetInstance(out T instance)
{
    if (_instance == null)
    {
        _instance = FindAnyObjectByType<T>();
    }

    instance = _instance;
    return instance != null;
}
```

기존 Instance를 대기 루프에서 호출하면 GameManager가 없는 동안 오류 로그가 매 프레임 발생한다. TryGetInstance는 '아직 없음'을 정상적인 대기 상태로 처리한다. 먼저 발견한 객체는 기존 Singleton.Awake의 자기 자신 검사와도 호환된다.

## 5. UIManager.cs: 전체 교체 예시

대상: `Assets/_Project/Scripts/UI/UIManager.cs`

```csharp
using System.Collections;
using UnityEngine;
using Attribute.Core;
using Core;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHudPresenter _playerHudPresenter;

        private GameManager _gameManager;
        private Coroutine _connectRoutine;

        private void OnEnable()
        {
            if (_playerHudPresenter == null)
            {
                Debug.LogError("PlayerHudPresenter가 연결되지 않았습니다.", this);
                return;
            }

            _playerHudPresenter.Bind(null);
            _connectRoutine = StartCoroutine(CoConnectGameManager());
        }

        private void OnDisable()
        {
            if (_connectRoutine != null)
            {
                StopCoroutine(_connectRoutine);
                _connectRoutine = null;
            }

            if (_gameManager != null)
            {
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
                _gameManager.OnPlayerDespawned -= HandlePlayerDespawned;
            }

            _gameManager = null;

            if (_playerHudPresenter != null)
            {
                _playerHudPresenter.Bind(null);
            }
        }

        public void ShowPlayerHud()
        {
            if (_playerHudPresenter != null)
            {
                _playerHudPresenter.Show();
            }
        }

        public void HidePlayerHud()
        {
            if (_playerHudPresenter != null)
            {
                _playerHudPresenter.Hide();
            }
        }

        private void HandlePlayerSpawned(AttributeSet attributeSet)
        {
            if (_playerHudPresenter != null)
            {
                _playerHudPresenter.Bind(attributeSet);
            }
        }

        private void HandlePlayerDespawned()
        {
            if (_playerHudPresenter != null)
            {
                _playerHudPresenter.Bind(null);
            }
        }

        private IEnumerator CoConnectGameManager()
        {
            while (!GameManager.TryGetInstance(out _gameManager) ||
                   !_gameManager.IsInitialized)
            {
                yield return null;
            }

            _gameManager.OnPlayerSpawned += HandlePlayerSpawned;
            _gameManager.OnPlayerDespawned += HandlePlayerDespawned;
            HandlePlayerSpawned(_gameManager.CurrentPlayerState);
        }
    }
}
```

대기 루프는 GameManager 초기화가 끝나면 종료된다. 이후 능력치 표시는 기존 이벤트로 갱신하며 매 프레임 검사하지 않는다. UIManager가 꺼지면 대기도 중단하고, 다시 켜지면 새로 연결한다. `enabled = false`만으로는 코루틴이 자동 종료되지 않으므로 OnDisable에서 명시적으로 중단한다.

## 6. GameManager.cs: 플레이어 수정 없이 삭제 감지

GameManager 클래스에 아래 메서드를 추가한다. PlayerLifetime 파일은 만들지 않는다.

```csharp
private void LateUpdate()
{
    AttributeSet currentPlayer = CurrentPlayerState;

    // 등록된 참조 자체가 없으면 검사할 대상도 없다.
    if (ReferenceEquals(currentPlayer, null))
    {
        return;
    }

    // C# 참조는 남아 있지만 Unity 객체가 파괴된 경우.
    if (currentPlayer == null)
    {
        UnregisterPlayer(currentPlayer);
    }
}
```

Unity 객체는 파괴된 뒤에도 관리 참조가 남을 수 있다. ReferenceEquals와 Unity의 == null을 구분해 삭제를 감지하고, 기존 UnregisterPlayer가 참조를 비워 알림을 한 번만 보내게 한다. 씬 검색 없이 기존 참조 하나를 검사한다. 삭제가 완료된 뒤 GameManager의 다음 LateUpdate에서 반영하므로 다음 프레임에 처리될 수 있다. HP 0 또는 SetActive(false)는 이 검사로 감지하지 않는다.

## 7. PlayerSpawner.cs: 전체 교체 예시

대상: `Assets/_Project/Scripts/Core/PlayerSpawner.cs`

UI만 기다리도록 바꾸면 기존 PlayerSpawner.Start가 GameManager가 없는 시점에 실패한다. 스포너도 함께 기다려야 한다. 아래는 기존 Start를 OnEnable/OnDisable과 코루틴으로 바꿔 대기 중 껐다 켜도 재시도하게 만든 예시다. 기존 클래스의 전역 네임스페이스는 유지했다.

```csharp
using System.Collections;
using UnityEngine;
using Attribute.Core;
using Core;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;

    private Coroutine _spawnRoutine;
    private bool _hasSpawned;

    private void OnEnable()
    {
        if (_hasSpawned)
        {
            return;
        }

        if (_playerPrefab == null || !_playerPrefab.activeSelf)
        {
            Debug.LogError("활성 상태의 플레이어 프리팹을 연결하세요.", this);
            return;
        }

        _spawnRoutine = StartCoroutine(CoSpawnPlayer());
    }

    private void OnDisable()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    private IEnumerator CoSpawnPlayer()
    {
        // 기존 Start처럼 최초 씬 초기화 뒤에 생성을 진행한다.
        yield return null;

        GameManager gameManager;
        while (!GameManager.TryGetInstance(out gameManager) ||
               !gameManager.IsInitialized)
        {
            yield return null;
        }

        GameObject player = Instantiate(
            _playerPrefab, transform.position, transform.rotation);

        if (!player.TryGetComponent(out AttributeSet attributeSet))
        {
            Debug.LogError("플레이어 루트에 AttributeSet이 없습니다.", player);
            Destroy(player);
            yield break;
        }

        _hasSpawned = true;
        gameManager.RegisterPlayer(attributeSet);
    }
}
```

플레이어에 새 컴포넌트를 추가하지 않는다. 원래처럼 스포너 인스턴스당 한 번 생성한다. 플레이어가 삭제되었다고 자동으로 재스폰하는 기능은 추가하지 않았다. AttributeSet의 SO 초기 데이터는 정상 연결돼 있어야 한다.

## 적용 후 흐름

생성:

```text
UIManager / PlayerSpawner가 GameManager 준비를 기다림
→ GameManager.Awake에서 PlayerState 초기화
→ 스포너가 활성 플레이어 생성 → AttributeSet.Awake에서 초기값 준비
→ GameManager.RegisterPlayer → 저장값 복원 → OnPlayerSpawned
→ UIManager → Presenter.Bind → 구독 및 화면 갱신
```

UI 연결보다 플레이어 등록이 빨라도 UIManager가 CurrentPlayerState를 조회하므로 놓치지 않는다.

삭제:

```text
Destroy(player)
→ GameManager의 다음 LateUpdate에서 삭제 감지
→ GameManager.UnregisterPlayer
→ 현재 등록된 플레이어와 동일한 경우에만 PlayerState 참조 제거
→ OnPlayerDespawned
→ UIManager → Presenter.Bind(null)
→ 기존 구독 해제 및 게이지 0으로 초기화
```

UI가 먼저 없어졌다면 OnDisable에서 GameManager 이벤트 구독을 이미 해제했으므로 UI 콜백은 실행되지 않는다. 스폰/제거 때만 알리고 HP 변경 때는 기존 AttributeSet 이벤트를 사용한다.

## 검증할 상황

- GameManager 선배치: 기존처럼 HP/배터리/열 표시.
- GameManager 지연 생성: UI/스포너가 기다린 뒤 연결. 대기 중 오류 로그 반복 없음.
- 대기 중 UIManager 또는 스포너 비활성화/재활성화: 재연결하며 스폰 중복 없음.
- HUD 계층 전체 비활성화/재활성화: 구독 누적 없이 현재값 반영.
- 플레이어만 Destroy: HUD 게이지가 0으로 초기화.
- B를 등록한 뒤 A를 Destroy: B 연결 유지.
- UI를 먼저 Destroy한 뒤 플레이어 Destroy: 삭제된 UI 콜백 없음.
- View 미연결: Awake 진단은 발생하지만 Bind가 능력치 이벤트를 구독하지 않음.

## 별도인 씬 전환 저장

이 예시는 삭제와 HUD 정리까지다. 기존 HandleBeforeSceneChange는 아직 호출자가 없으므로, 저장값을 넘겨야 하는 씬 전환에서는 **살아 있는 플레이어의 SaveCurrentAttributes → 씬 전환 → 삭제/등록 해제 → 다음 플레이어 생성** 순서가 필요하다. OnDestroy에서 자동 저장하지 않는다. 실제 씬 전환 담당 코드에 연결해야 하며 그 파일은 현재 확인되지 않았다.

## 공식 문서

- [코루틴 중단 규칙](https://docs.unity3d.com/cn/6000.0/ScriptReference/MonoBehaviour.StopCoroutine.html)
- [Unity 객체의 파괴 후 null 비교](https://docs.unity3d.com/cn/6000.0/ScriptReference/Object.html)
