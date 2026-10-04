using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Movement;

namespace Projectile
{
    /// <summary>
    /// 총알의 직진 이동과 피격 처리를 담당한다.
    /// 매 프레임 SphereCollider 반지름으로 이동 구간을 SphereCast 해 피격을 감지하고,
    /// 맞힌 대상의 IEffectTarget 에 SOAttributeEffect 를 적용한 뒤 소멸한다.
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Hit")]
        [Tooltip("피격 대상으로 감지할 레이어")]
        [SerializeField] private LayerMask _hitLayers = Physics.DefaultRaycastLayers;

        [Header("Movement")]
        [SerializeField] private float _speed = 20.0f;
        [SerializeField] private float _lifeSpan = 3.0f;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private SphereCollider _sphereCollider;
        private Vector3 _direction = Vector3.forward;
        private SOAttributeEffect _hitEffect;
        private IEffectTarget _cursor;
        private float _elapsedTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();
            _sphereCollider = GetComponent<SphereCollider>();

            Debug.Assert(_motor != null, $"[{name}] TransformMotor가 연결되지 않았습니다.");
            Debug.Assert(_sphereCollider != null, $"[{name}] SphereCollider가 연결되지 않았습니다.");
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= _lifeSpan)
            {
                Destroy(gameObject);
                return;
            }

            float stepDistance = Mathf.Max(0.0f, _speed) * Time.deltaTime;
            _motor.MoveTo(transform.position + _direction * stepDistance, _speed);
        }

        /// <summary>
        /// 다른 콜라이더와 겹치면 호출된다.
        /// 피격 레이어에 속한 대상이면 효과를 적용하고 총알을 소멸시킨다.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // other의 레이어가 _hitLayers에 포함되는지 검사
            if ((_hitLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            ApplyHitEffect(other);
            Destroy(gameObject);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 생성 직후 호출해 총알의 비행 정보를 설정한다.
        /// </summary>
        /// <param name="direction">비행 방향. zero 면 월드 z+ 방향을 사용한다.</param>
        /// <param name="hitEffect">피격 대상에 적용할 효과. null 이면 효과 없이 소멸만 한다.</param>
        /// <param name="cursor">발사자의 IEffectTarget. 효과의 ValueSource 가 Attribute 일 때 필요하다.</param>
        public void Initialize(Vector3 direction, SOAttributeEffect hitEffect, IEffectTarget cursor = null)
        {
            _direction = (direction == Vector3.zero) ? Vector3.forward : direction.normalized;
            _hitEffect = hitEffect;
            _cursor = cursor;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 피격 대상에 효과를 적용한다.
        /// 대상이 없거나 효과의 대상 어트리뷰트를 갖지 않으면 적용하지 않는다.
        /// </summary>
        private void ApplyHitEffect(Collider other)
        {
            if (_hitEffect == null) return;

            _hitEffect.Apply(other.gameObject, _cursor);
        }
        #endregion
    }
}
