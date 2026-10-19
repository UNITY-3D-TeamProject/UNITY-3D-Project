using System;
using UnityEngine;
using Attribute.Effect;
using AI.Boss.Arena;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A 어깨 포. 지정한 지점에 원형 예고를 띄운 뒤 반경 안 플레이어에게 피해를 주고 바깥으로 민다.
    /// 예고 시간은 최종 단계 전용 값이라 BossController.TelegraphScale 을 곱하지 않는다.
    /// BossKnockback 호출 측이므로 이번 프레임 이동에 합쳐지도록 실행 순서를 앞당긴다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BossShoulderCannon : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private BossController _controller;
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.0f;
        [Tooltip("임시값")]
        [SerializeField, Min(0.0f)] private float _radius = 2.5f;
        [SerializeField, Min(0.0f)] private float _knockbackDistance = 6.0f;
        [SerializeField, Min(0.0f)] private float _knockbackSeconds = 0.3f;
        [SerializeField] private SOAttributeEffect _hitEffect;
        #endregion

        #region Private Fields
        private readonly BossKnockback _knockback = new BossKnockback();
        private bool _isFiring;
        private Vector3 _point;
        private float _remaining;
        private int _circleHandle = -1;
        private Action _onDone;
        #endregion

        #region Properties
        public bool IsFiring => _isFiring;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_controller) _controller = GetComponentInParent<BossController>();
        }

        private void OnDisable()
        {
            Cancel();
            _knockback.Cancel();
        }

        private void Update()
        {
            _knockback.Tick(Time.deltaTime);

            if (!_isFiring) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0.0f) Explode();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 원형 예고를 띄우고 시간이 지나면 터진다. 터진 직후 onDone 을 부른다.
        /// 이미 발사 중이거나 꺼져 있어 쏠 수 없으면 바로 onDone 을 부른다.
        /// </summary>
        /// <param name="point">터질 월드 위치</param>
        /// <param name="onDone">터진 직후 부를 콜백</param>
        public void Fire(Vector3 point, Action onDone)
        {
            if (_isFiring || !isActiveAndEnabled)
            {
                onDone?.Invoke();
                return;
            }

            _isFiring = true;
            _point = point;
            _remaining = _telegraphSeconds;
            _onDone = onDone;

            if (_controller && _controller.Arena != null) _circleHandle = _controller.Arena.ShowCircle(point, _radius);
        }

        /// <summary>
        /// 발사를 취소한다. onDone 은 부르지 않는다.
        /// </summary>
        public void Cancel()
        {
            _isFiring = false;
            _onDone = null;
            HideCircle();
        }
        #endregion

        #region Private Methods
        private void Explode()
        {
            _isFiring = false;
            HideCircle();

            IBossSituation situation = _controller ? _controller.Situation : null;
            if (situation != null) HitPlayer(situation);

            Action onDone = _onDone;
            _onDone = null;
            onDone?.Invoke();
        }

        private void HitPlayer(IBossSituation situation)
        {
            Transform body = situation.PlayerBody;
            if (!body) return;

            Vector3 offset = body.position - _point;
            offset.y = 0.0f;
            if (offset.sqrMagnitude > (_radius * _radius)) return;

            if (_hitEffect) situation.TryApplyToPlayer(_hitEffect);

            if (!situation.PlayerMotor) return;

            // 중심에서 바깥으로 민다. 정확히 중심이면 포가 바라보는 방향으로 민다
            Vector3 direction = (offset.sqrMagnitude > 0.0001f) ? offset.normalized : transform.forward;
            direction.y = 0.0f;
            _knockback.Start(situation.PlayerMotor, direction.normalized * _knockbackDistance, _knockbackSeconds);
        }

        private void HideCircle()
        {
            if (_circleHandle < 0) return;

            if (_controller && _controller.Arena != null) _controller.Arena.HideCircle(_circleHandle);
            _circleHandle = -1;
        }
        #endregion
    }
}
