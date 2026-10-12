using System;
using UnityEngine;
using Attribute.Core;

namespace Map.Feed
{
    /// <summary>
    /// 태그 주위에 떠다니는 "다른 사람의 피드". 1라운드에서는 제자리에서 위아래로 흔들리는 장식이다.
    /// 2라운드에서 FeedRound2 가 홀로그램으로 바꾸고 옮기며, 총에 맞으면 OnShot 으로 알린다.
    /// 같은 오브젝트의 AttributeSet 에 CurrentHp 가 있어야 하고, 피격용 콜라이더는 Is Trigger를 켜고 레이어를 공격이 맞는 레이어(Obstacle)로 둔다.
    /// </summary>
    [RequireComponent(typeof(AttributeSet))]
    public class FeedPost : MonoBehaviour
    {
        #region Constants
        private const string HP_ATTRIBUTE = "CurrentHp";
        #endregion

        #region Serialized Fields
        [Header("Tag")]
        [Tooltip("이 피드가 속한 태그의 순서 (0부터). 2라운드에서 같은 순서의 벽으로 보내야 한다.")]
        [SerializeField, Min(0)] private int _tagIndex;

        [Header("Float")]
        [Tooltip("제자리에서 위아래로 흔들리는 폭")]
        [SerializeField, Min(0f)] private float _bobHeight = 0.4f;

        [Tooltip("한 번 오르내리는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _bobPeriod = 3.0f;

        [Header("Hologram")]
        [Tooltip("홀로그램으로 바뀔 때 머티리얼을 바꿀 렌더러")]
        [SerializeField] private Renderer[] _renderers;

        [Tooltip("홀로그램 머티리얼")]
        [SerializeField] private Material _hologramMaterial;

        [Tooltip("총에 맞는 콜라이더. 쏠 수 있는 동안에만 켠다.")]
        [SerializeField] private Collider _hitCollider;
        #endregion

        #region Private Fields
        private AttributeSet _attributeSet;
        private float _initialHp;

        // 흔들림의 기준 위치와 진행 시간
        private Vector3 _restPosition;
        private float _bobTime;

        private bool _isMoving;
        private Vector3 _moveStart;
        private Vector3 _moveTarget;
        private float _moveDuration;
        private float _moveElapsed;
        private Action _onArrived;
        #endregion

        #region Properties
        /// <summary>이 피드가 속한 태그의 순서 (0부터).</summary>
        public int TagIndex => _tagIndex;
        #endregion

        #region Events
        /// <summary>쏠 수 있는 상태에서 총에 맞았을 때 호출된다.</summary>
        public event Action<FeedPost> OnShot;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // AttributeSet 은 실행 순서가 앞이라 이 시점에 값이 준비되어 있다
            _attributeSet = GetComponent<AttributeSet>();

            Debug.Assert(_attributeSet.IsValidTarget(HP_ATTRIBUTE),
                $"[{name}] AttributeSet에 {HP_ATTRIBUTE}가 없습니다.");
            Debug.Assert(_hitCollider != null, $"[{name}] Hit Collider가 연결되지 않았습니다.");

            _initialHp = _attributeSet.GetValue(HP_ATTRIBUTE);
            _restPosition = transform.position;

            // 피드마다 박자가 어긋나게 한다
            _bobTime = UnityEngine.Random.value * _bobPeriod;

            SetShootable(false);
        }

        private void OnEnable()
        {
            _attributeSet.AddPostAttributeChangedCallback(HandleAttributeChanged);
        }

        private void OnDisable()
        {
            _attributeSet.RemovePostAttributeChangedCallback(HandleAttributeChanged);
        }

        private void Update()
        {
            if (_isMoving)
            {
                UpdateMove();
                return;
            }

            _bobTime += Time.deltaTime;
            float offset = Mathf.Sin((_bobTime / _bobPeriod) * Mathf.PI * 2.0f) * _bobHeight;
            transform.position = _restPosition + (Vector3.up * offset);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 겉모습을 홀로그램으로 바꾼다.
        /// </summary>
        public void BecomeHologram()
        {
            if ((_hologramMaterial == null) || (_renderers == null)) return;

            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.sharedMaterial = _hologramMaterial;
                }
            }
        }

        /// <summary>
        /// 총에 맞을 수 있는지 정한다.
        /// </summary>
        public void SetShootable(bool isShootable)
        {
            if (_hitCollider != null)
            {
                _hitCollider.enabled = isShootable;
            }
        }

        /// <summary>
        /// 지금 위치에서 target 까지 부드럽게 옮긴다. 도착하면 그 자리에서 다시 흔들린다.
        /// </summary>
        /// <param name="target">도착 위치 (월드 좌표)</param>
        /// <param name="duration">걸리는 시간(초)</param>
        /// <param name="onArrived">도착했을 때 한 번 실행할 동작 (선택)</param>
        public void MoveTo(Vector3 target, float duration, Action onArrived = null)
        {
            _isMoving = true;
            _moveStart = transform.position;
            _moveTarget = target;
            _moveDuration = Mathf.Max(duration, 0.01f);
            _moveElapsed = 0.0f;
            _onArrived = onArrived;
        }
        #endregion

        #region Private Methods
        private void UpdateMove()
        {
            _moveElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_moveElapsed / _moveDuration);
            transform.position = Vector3.Lerp(_moveStart, _moveTarget, Mathf.SmoothStep(0.0f, 1.0f, progress));

            if (progress < 1.0f) return;

            // 도착한 자리에서 흔들림이 0 부터 이어지게 한다
            _isMoving = false;
            _restPosition = _moveTarget;
            _bobTime = 0.0f;

            // 콜백 안에서 MoveTo 를 다시 부를 수 있으므로 먼저 비운다
            Action onArrived = _onArrived;
            _onArrived = null;
            onArrived?.Invoke();
        }

        private void HandleAttributeChanged(SAttributeChangeData data)
        {
            if (!string.Equals(data.AttributeName, HP_ATTRIBUTE, StringComparison.OrdinalIgnoreCase)) return;

            // HP 가 줄어든 것만 피격으로 본다 (아래에서 되돌릴 때의 증가는 무시)
            if (data.NewValue >= data.OldValue) return;

            // 틀린 벽에서 돌아온 뒤 다시 쏠 수 있도록 HP 를 처음 값으로 되돌린다
            _attributeSet.SetValue(HP_ATTRIBUTE, _initialHp);
            OnShot?.Invoke(this);
        }
        #endregion
    }
}
