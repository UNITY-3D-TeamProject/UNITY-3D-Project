using UnityEngine;

namespace Platform
{
    /// <summary>
    /// 앱이 분쇄 통로에 도착하면 반응하는 연출 담당: 본체 진동, 파쇄 링 가속, 도착 지점 파쇄 버스트.
    /// 발판 생성(PlatformSpawner)과는 연동하지 않는다. 시각 연출 전용.
    /// </summary>
    public class DataGrinder : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("앱 도착 이벤트를 발생시키는 AppSpawner. 비워두면 씬에서 자동으로 찾는다.")]
        [SerializeField] private AppSpawner _appSpawner;

        [Tooltip("진동시킬 오브젝트. 스포너 출구는 흔들리면 안 되므로 포함하지 않는다.")]
        [SerializeField] private Transform _shakeTarget;
        [SerializeField] private ParticleSystem _arrivalBits;
        [SerializeField] private ParticleSystem _arrivalFlash;

        [Header("Reaction")]
        [Tooltip("앱 1개가 도착할 때 방출하는 파쇄 조각 수")]
        [SerializeField, Min(0)] private int _bitsPerApp = 40;

        [Tooltip("반응(진동/링 가속)이 사라지는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.01f)] private float _reactionDuration = 0.8f;

        [Tooltip("진동 세기(미터). 최대 반응 시점 기준")]
        [SerializeField, Min(0.0f)] private float _shakeAmplitude = 0.08f;

        [Tooltip("최대 반응 시점의 파쇄 링 회전 속도 배율")]
        [SerializeField, Min(1.0f)] private float _spinBoostMultiplier = 3.0f;
        #endregion

        #region Private Fields
        private SelfSpinner[] _spinners;
        private Vector3 _shakeOrigin;
        private float   _reaction;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_appSpawner == null)
            {
                _appSpawner = FindFirstObjectByType<AppSpawner>();
            }

            Debug.Assert(_appSpawner != null, $"[{name}] AppSpawner를 찾지 못했습니다. 씬에 최초 생성 파이프를 배치하거나 직접 연결하세요.");

            _spinners = GetComponentsInChildren<SelfSpinner>();

            if (_shakeTarget != null)
            {
                _shakeOrigin = _shakeTarget.localPosition;
            }
        }

        private void OnEnable()
        {
            if (_appSpawner != null)
            {
                _appSpawner.OnAppArrived += HandleAppArrived;
            }
        }

        private void OnDisable()
        {
            if (_appSpawner != null)
            {
                _appSpawner.OnAppArrived -= HandleAppArrived;
            }
        }

        private void Update()
        {
            if (_reaction <= 0.0f) return;

            _reaction = Mathf.MoveTowards(_reaction, 0.0f, Time.deltaTime / _reactionDuration);
            ApplyReaction();
        }
        #endregion

        #region Private Methods
        private void HandleAppArrived(Vector3 worldPosition)
        {
            EmitBurst(worldPosition);
            _reaction = 1.0f;
        }

        private void EmitBurst(Vector3 worldPosition)
        {
            if (_arrivalBits != null)
            {
                _arrivalBits.transform.position = worldPosition;
                _arrivalBits.Emit(_bitsPerApp);
            }

            if (_arrivalFlash != null)
            {
                _arrivalFlash.transform.position = worldPosition;
                _arrivalFlash.Emit(1);
            }
        }

        private void ApplyReaction()
        {
            if (_shakeTarget != null)
            {
                _shakeTarget.localPosition = _shakeOrigin + (Random.insideUnitSphere * (_shakeAmplitude * _reaction));
            }

            float spinMultiplier = Mathf.Lerp(1.0f, _spinBoostMultiplier, _reaction);
            foreach (SelfSpinner spinner in _spinners)
            {
                spinner.SpeedMultiplier = spinMultiplier;
            }
        }
        #endregion
    }
}
