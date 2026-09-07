# Unity 6 C# 코드 컨벤션 가이드

**팀 협업을 위한 통일된 코드 작성 기준**

---

## 이 문서의 목적
이 가이드는 팀원 모두가 일관된 코드를 작성할 수 있도록 명명 규칙, 코드 구조, 서식 기준을 정의한다.

---

## 0. 기초 지식: camelCase와 PascalCase

코드 컨벤션을 이해하려면 먼저 다음의 명명 스타일을 알아야 한다.

### ◆ camelCase (카멜 케이스)
첫 단어는 소문자로 시작하고, 이후 단어의 첫 글자만 대문자로 쓴다. 이름이 낙타(camel) 등처럼 올록볼록해 보여서 붙여진 이름이다.
```csharp
// 단어: move + speed  →  moveSpeed
// 단어: player + name →  playerName
// 단어: is + grounded →  isGrounded

float moveSpeed = 5.0f;
string playerName = "Hero";
bool isGrounded = false;
```

### ◆ PascalCase (파스칼 케이스)
모든 단어의 첫 글자를 대문자로 쓴다. 클래스, 메서드, 프로퍼티 등에 사용한다.
```csharp
// 단어: move + speed  →  MoveSpeed
// 단어: take + damage →  TakeDamage
// 단어: game + manager → GameManager

public class PlayerController { }
public void TakeDamage(int amount) { }
public int CurrentHealth { get; private set; }
```

### ◆ UPPER_SNAKE_CASE (어퍼 스네이크 케이스)
모든 글자를 대문자로 쓰고 단어 사이를 언더스코어(`_`)로 연결한다. 상수(`const`)에만 사용한다.
```csharp
// 단어: max + player + count  →  MAX_PLAYER_COUNT
// 단어: default + move + speed →  DEFAULT_MOVE_SPEED

private const int MAX_PLAYER_COUNT = 4;
private const float DEFAULT_MOVE_SPEED = 5.0f;
```

---

## 1. 변수 명명 규칙

### 1-1. 지역 변수 / 매개변수 → `camelCase`
메서드 안에서 선언하는 변수와 메서드 인자에 사용한다.
```csharp
void Move(float deltaTime, Vector3 direction)
{
    float adjustedSpeed = _moveSpeed * deltaTime;
    int playerCount = GetActivePlayerCount();
}
```

### 1-2. private 필드 → `_camelCase`
클래스 멤버 변수 중 `private`인 경우 언더스코어(`_`) 접두사를 붙인다.
```csharp
public class PlayerController : MonoBehaviour
{
    private int   _health;
    private float _moveSpeed;
    private bool  _isGrounded;
}
```

### 1-3. public 필드 → Property 사용 (`PascalCase`)
💡 **참고**: `public` 필드를 직접 노출하면 외부에서 값을 제어 없이 변경할 수 있어 버그로 이어진다. 반드시 Property를 사용한다.

**❌ public 필드 직접 사용 — 지양**
```csharp
public int Health;   // 외부에서 Health = -999 등 마음대로 수정 가능
```

**✅ Property 사용 — 권장**
```csharp
private int _health;

// 읽기는 public, 쓰기는 private
public int Health { get; private set; }

// 값 검증 로직 포함
public int MaxHealth
{
    get => _maxHealth;
    private set => _maxHealth = Mathf.Max(1, value);
}

// 계산형 (읽기 전용)
public bool IsAlive  => _health > 0;
public float HealthRatio => (float)_health / _maxHealth;
```

### 1-4. 상수 (const) → `UPPER_SNAKE_CASE`
```csharp
private const int   MAX_PLAYER_COUNT   = 4;
private const float DEFAULT_MOVE_SPEED = 5.0f;
private const string SCENE_NAME_LOBBY  = "Lobby";
```

### 1-5. static readonly → `PascalCase`
런타임에 생성되지만 이후 변경되지 않는 값에 사용한다.
```csharp
private static readonly WaitForSeconds WaitOneSecond  = new WaitForSeconds(1.0f);
private static readonly WaitForEndOfFrame WaitEndFrame = new WaitForEndOfFrame();
```

### 1-6. bool 변수 → `is` / `can` / `has` / `should` 접두사
`bool` 타입 변수는 의미에 맞는 접두사를 붙여 변수명만 봐도 뜻이 바로 읽히도록 한다. 문맥에 가장 자연스러운 것을 선택한다. 메서드의 `bool` 반환값도 동일한 접두사를 사용한다.

```csharp
// is  — 상태를 나타낼 때
bool isAlive    = true;
bool isGrounded = false;
private bool _isInvincible;
private bool _isDead;

// can  — 가능 여부를 나타낼 때
bool canAttack = true;
bool canJump   = false;
private bool _canMove;

// has  — 보유 여부를 나타낼 때
bool hasKey  = false;
bool hasItem = true;
private bool _hasWeapon;

// should  — 해야 하는지 여부를 나타낼 때
bool shouldRespawn = false;
private bool _shouldUpdate;
```
> **주의**: `isCanAttack`, `isHasItem`과 같이 억지로 `is`에 맞추어 중복된 의미를 부여하는 것은 지양한다.

---

## 2. 메서드 명명 규칙

### 2-1. 일반 메서드 → `PascalCase` + 동사 시작
```csharp
public void TakeDamage(int amount) { }
public void Heal(int amount) { }
private void UpdateHealthUI() { }
private void HandleInput() { }
```

### 2-2. bool 반환 메서드 → `Is` / `Has` / `Can` / `Should` 접두사
```csharp
private bool IsGrounded()          { }
private bool HasEnoughMana(int cost) { }
public  bool CanAttack()           { }
private bool ShouldRespawn()       { }
```

### 2-3. 코루틴 → `Co` 접두사
```csharp
private IEnumerator CoSpawnEffect()  { }
private IEnumerator CoRespawn()      { }
private IEnumerator CoFadeIn()       { }

// 호출 시
StartCoroutine(CoSpawnEffect());
```

### 2-4. 클래스 내부 선언 순서
아래 순서를 지켜 선언하면 팀원 누구나 위치를 예측할 수 있다.

```csharp
public class EnemyController : MonoBehaviour, IDamageable
{
    // 1. 상수
    private const float DETECTION_RANGE = 10.0f;

    // 2. static 필드
    private static int _instanceCount;

    // 3. Serialized 필드 (Inspector 노출)
    [SerializeField] private float _moveSpeed;
    [SerializeField] private Animator _animator;

    // 4. private 필드
    private int _currentHealth;

    // 5. Property
    public bool IsAlive => _currentHealth > 0;

    // 6. Events / Actions
    public event Action<int> OnHealthChanged;

    // 7. Unity 라이프사이클 메서드
    private void Awake() { }
    private void Start() { }
    private void Update() { }

    // 8. public 메서드
    public void TakeDamage(int amount) { }

    // 9. private 메서드
    private void UpdateHealthUI() { }

    // 10. 코루틴
    private IEnumerator CoSpawnEffect() { }
}
```

---

## 3. 타입(Type) 명명 규칙

### 3-1. 클래스 → `PascalCase`
```csharp
public class PlayerController : MonoBehaviour { }
public class GameManager       : MonoBehaviour { }
public class InventorySystem               { }   // 순수 C# 클래스
```

### 3-2. 인터페이스 → `I` + `PascalCase`
```csharp
public interface IDamageable
{
    void TakeDamage(int amount);
}

public interface IInteractable
{
    void Interact(GameObject interactor);
}
```

### 3-3. 구조체 → `S` + `PascalCase`
💡 **참고**: 접두사 `S`를 붙여 인터페이스(`I`), 열거형(`E`)과 한눈에 구분할 수 있도록 한다.
```csharp
public struct SDamageInfo
{
    public int    Amount;
    public EGameDamageType Type;
    public Vector3 HitPoint;
}

public readonly struct SHealthData
{
    public readonly int Current;
    public readonly int Max;
    public float Ratio => (float)Current / Max;
}
```

### 3-4. 열거형 → `E` + `PascalCase`, 멤버는 `PascalCase`
💡 **참고**: 접두사 `E`를 붙여 타입만 봐도 열거형임을 즉시 인지할 수 있도록 한다.
```csharp
public enum EGameState
{
    None,
    MainMenu,
    Loading,
    Playing,
    Paused,
    GameOver,
}

public enum EGameDamageType
{
    Physical,
    Fire,
    Ice,
    Poison,
}

// Flags 열거형: 복수형 + 2의 거듭제곱
[Flags]
public enum EStatusEffects
{
    None   = 0,
    Burn   = 1 << 0,
    Freeze = 1 << 1,
    Poison = 1 << 2,
    Stun   = 1 << 3,
}
```

### 3-5. ScriptableObject → `SO` + `PascalCase`
```csharp
[CreateAssetMenu(fileName = "SOWeaponData", menuName = "Game/Weapon Data")]
public class SOWeaponData : ScriptableObject
{
    public string  WeaponName;
    public int     Damage;
    public float   AttackSpeed;
}

[CreateAssetMenu(fileName = "SOEnemyData", menuName = "Game/Enemy Data")]
public class SOEnemyData : ScriptableObject { }
```

---

## 4. 서식(Formatting) 규칙

### 4-1. 중괄호 ( `{ }` ) — 항상 새 줄에
중괄호는 항상 새 줄에 작성한다. 같은 줄에 여는 중괄호를 쓰지 않는다.

**❌ 지양**
```csharp
if (IsAlive) {
    TakeDamage(10);
}
```

**✅ 권장**
```csharp
if (IsAlive)
{
    TakeDamage(10);
}
```

### 4-2. 공백 규칙
연산자 양쪽, 쉼표(`,`) 뒤에 공백 한 칸을 둔다. 괄호 안쪽에는 공백을 넣지 않는다.

**❌ 지양**
```csharp
int result=a+b;
Move(x,y,z);
if( isAlive )
for(int i=0;i<count;i++)
```

**✅ 권장**
```csharp
int result = a + b;
Move(x, y, z);
if (isAlive)
for (int i = 0; i < count; i++)
```

### 4-3. if 문 조건식 — 복잡한 조건의 개행 처리
조건이 여러 개 이어질 때는 논리 연산자(`&&`, `||`) 뒤에서 줄을 바꾸고 들여쓰기한다. 각 피연산자 그룹은 괄호로 묶어 가독성을 높인다.

**❌ 지양 — 한 줄에 조건 나열**
```csharp
if (isAlive && currentHealth > 0 && !isInvincible && attackCooldown <= 0)
{
    Attack();
}
```

**✅ 권장 — 연산자 뒤에서 개행 + 괄호 그룹화**
```csharp
if ((isAlive && currentHealth > 0) &&
    (!isInvincible) &&
    (attackCooldown <= 0))
{
    Attack();
}
```
조건이 3개 이상이고 각 조건에 의미 단위가 있을 때는 `bool` 변수로 분리하는 것도 좋다.
```csharp
bool canFight  = isAlive && (currentHealth > 0);
bool canAttack = !isInvincible && (attackCooldown <= 0);

if (canFight && canAttack)
{
    Attack();
}
```

### 4-4. 메서드 인자 — 인자가 많을 때 개행
인자가 3개를 넘거나 줄 길이가 길어지면 각 인자를 새 줄에 쓴다.

**✅ 인자가 적을 때 — 한 줄**
```csharp
Move(direction, speed);
```

**✅ 인자가 많을 때 — 개행**
```csharp
SpawnEnemy(
    prefab,
    spawnPoint.position,
    Quaternion.identity,
    parentTransform);
```

### 4-5. 삼항 연산자 — 간단할 때만 사용

```csharp
// ✅ 간단한 경우: 한 줄 허용
string label = isAlive ? "Alive" : "Dead";

// ❌ 복잡한 경우: if-else 사용
// string msg = hp > 50 ? (mp > 30 ? "Good" : "Low MP") : "Danger";

// ✅ 복잡한 경우: if-else로 변경
string msg;
if (hp > 50)
    msg = (mp > 30) ? "Good" : "Low MP";
else
    msg = "Danger";
```

---

## 5. 협업을 위한 추가 규칙

### 5-1. SerializedField & Header — Inspector 정리
```csharp
[Header("Movement")]
[SerializeField] private float _moveSpeed = 5.0f;
[SerializeField] private float _jumpForce = 8.0f;

[Header("Combat")]
[SerializeField] private int   _maxHealth  = 100;
[SerializeField] private float _attackRange = 2.0f;

[Header("References")]
[SerializeField] private Animator    _animator;
[SerializeField] private AudioSource _audioSource;

[Space(10)]
[Tooltip("디버그 전용 — 빌드 전 반드시 해제")]
[SerializeField] private bool _debugMode;
```

### 5-2. 주석 컨벤션
```csharp
/// <summary>
/// 플레이어에게 데미지를 입힌다.
/// </summary>
/// <param name="amount">입힐 데미지 수치 (양수)</param>
/// <returns>실제로 적용된 데미지 수치</returns>
public int TakeDamage(int amount)
{
    // 무적 상태이면 데미지 무효
    if (_isInvincible) return 0;

    // TODO: 방어력 계산 로직 추가 필요 (@홍길동, 2025-06-01)
    // FIXME: 음수 amount 예외 처리 필요
    int finalDamage = Mathf.Max(0, amount);
    CurrentHealth -= finalDamage;
    return finalDamage;
}
```

### 5-3. Null 처리 & 방어 코딩
```csharp
// Null 조건 연산자 적극 활용
OnHealthChanged?.Invoke(_currentHealth);
_animator?.SetTrigger(AnimHash.Attack);

// TryGetComponent 사용
if (TryGetComponent<Rigidbody>(out var rb))
{
    rb.AddForce(Vector3.up * _jumpForce);
}

// Awake에서 컴포넌트 검증
private void Awake()
{
    Debug.Assert(_animator != null,
        $"[{name}] Animator가 연결되지 않았습니다.");
}
```

### 5-4. 성능 관련 규칙
```csharp
// Animator 파라미터는 해시로 캐싱
private static class AnimHash
{
    public static readonly int IsRunning = Animator.StringToHash("IsRunning");
    public static readonly int Attack    = Animator.StringToHash("Attack");
}

// 컴포넌트 캐싱: Awake에서 한 번만 호출
private Transform _cachedTransform;
private Animator  _animator;

private void Awake()
{
    _cachedTransform = transform;
    _animator = GetComponent<Animator>();
}

private void Update()
{
    // ❌ 매 프레임 GetComponent 금지
    // GetComponent<Animator>().SetBool(...);

    // ✅ 캐싱된 참조 사용
    _animator.SetBool(AnimHash.IsRunning, _isMoving);
}
```

### 5-5. #region으로 코드 구조화
```csharp
public class PlayerController : MonoBehaviour
{
    #region Constants
    private const float DEFAULT_MOVE_SPEED = 5.0f;
    #endregion

    #region Serialized Fields
    [SerializeField] private float _moveSpeed;
    #endregion

    #region Private Fields
    private int _currentHealth;
    #endregion

    #region Properties
    public bool IsAlive => _currentHealth > 0;
    #endregion

    #region Unity Lifecycle
    private void Awake() { }
    private void Update() { }
    #endregion

    #region Public Methods
    public void TakeDamage(int amount) { }
    #endregion

    #region Coroutines
    private IEnumerator CoSpawnEffect() { }
    #endregion
}
```

### 5-6. 폴더 & 파일 구조
```text
Assets/
├── _Project/                  ← 프로젝트 전용 폴더
│   ├── Scripts/
│   │   ├── Core/              ← GameManager, SceneLoader 등
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── UI/
│   │   ├── Systems/
│   │   └── Utils/             ← 확장 메서드, 헬퍼 클래스
│   ├── Scenes/
│   ├── Prefabs/
│   ├── ScriptableObjects/
│   └── Art/
└── ThirdParty/                ← 외부 에셋 (절대 수정 금지)
```

---

## 6. 명명 규칙 요약표

| 대상 | 규칙 | 예시 |
|---|---|---|
| **지역 변수 / 매개변수** | `camelCase` | `moveSpeed`, `playerName` |
| **bool 변수 (지역)** | `is`/`can`/`has`/`should` + `camelCase` | `isAlive`, `canAttack`, `hasItem` |
| **bool 필드 (private)** | `_is`/`_can`/`_has` + `camelCase` | `_isInvincible`, `_canMove` |
| **private 필드** | `_camelCase` | `_health`, `_rigidbody` |
| **public Property** | `PascalCase` | `CurrentHealth`, `IsAlive` |
| **상수 (const)** | `UPPER_SNAKE_CASE` | `MAX_PLAYER_COUNT` |
| **static readonly** | `PascalCase` | `WaitOneSecond` |
| **메서드** | `PascalCase` + 동사 | `TakeDamage()`, `IsGrounded()` |
| **코루틴** | `Co` + `PascalCase` | `CoSpawnEffect()` |
| **클래스** | `PascalCase` | `PlayerController` |
| **인터페이스** | `I` + `PascalCase` | `IDamageable` |
| **구조체** | `S` + `PascalCase` | `SDamageInfo` |
| **열거형** | `E` + `PascalCase` | `EGameState` |
| **ScriptableObject** | `SO` + `PascalCase` | `SOWeaponData` |

---

## 7. 전체 예시 코드
위의 모든 컨벤션이 적용된 실제 스크립트 예시다.

```csharp
public class PlayerController : MonoBehaviour, IDamageable
{
    #region Constants
    private const int   MAX_HEALTH       = 100;
    private const float DEFAULT_MOVE_SPEED = 5.0f;
    #endregion

    #region Serialized Fields
    [Header("Stats")]
    [SerializeField] private float _moveSpeed = DEFAULT_MOVE_SPEED;

    [Header("References")]
    [SerializeField] private Animator _animator;
    #endregion

    #region Private Fields
    private static readonly WaitForSeconds WaitRespawn = new WaitForSeconds(3.0f);
    private int   _currentHealth;
    private bool  _isInvincible;
    #endregion

    #region Properties
    public int  CurrentHealth => _currentHealth;
    public bool IsAlive       => _currentHealth > 0;
    #endregion

    #region Events
    public event Action<int> OnHealthChanged;
    public event Action      OnDied;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Debug.Assert(_animator != null, $"[{name}] Animator 누락");
        _currentHealth = MAX_HEALTH;
    }
    #endregion

    #region Public Methods
    public void TakeDamage(int amount)
    {
        bool canTakeDamage = IsAlive && !_isInvincible;
        if (!canTakeDamage) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth);

        if (!IsAlive)
            StartCoroutine(CoDie());
    }
    #endregion

    #region Coroutines
    private IEnumerator CoDie()
    {
        OnDied?.Invoke();
        yield return WaitRespawn;
        _currentHealth = MAX_HEALTH;
    }
    #endregion
}
```
