using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Movement;
using Core.ObjectPool;

namespace Projectile
{
    /// <summary>
    /// 총알의 직진 이동과 피격 처리를 담당한다.
    /// 매 프레임 SphereCollider 반지름으로 이동 구간을 SphereCast 해 피격을 감지하고,
    /// 맞힌 대상의 IEffectTarget 에 SOAttributeEffect 를 적용한 뒤 소멸한다.
    /// 풀에서 꺼낸 총알은 소멸 시 풀에 반납하고, 그 외(Instantiate)는 파괴한다.
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Hit")]
        [Tooltip("피격 대상으로 감지할 레이어")]
        [SerializeField] private LayerMask _hitLayers = Physics.DefaultRaycastLayers;
        [Tooltip("맞힌 대상에 적용할 기본 효과. Initialize 에서 효과를 넘기면 그 효과로 바뀐다.")]
        [SerializeField] private SOAttributeEffect _hitEffect;

        [Header("Movement")]
        [SerializeField] private float _speed = 20.0f;
        [SerializeField] private float _lifeSpan = 3.0f;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private SphereCollider _sphereCollider;
        private Vector3 _direction = Vector3.forward;
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

        private void OnEnable()
        {
            // 풀에서 재사용될 때 수명을 처음부터 다시 잰다
            _elapsedTime = 0.0f;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= _lifeSpan)
            {
                Despawn();
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
            Despawn();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 생성 직후 호출해 총알의 비행 정보를 설정한다. 피격 효과는 프리팹에 설정된 기본 효과를 쓴다.
        /// </summary>
        /// <param name="direction">비행 방향. zero 면 월드 z+ 방향을 사용한다.</param>
        /// <param name="cursor">발사자의 IEffectTarget. 효과의 ValueSource 가 Attribute 일 때 필요하다.</param>
        public void Initialize(Vector3 direction, IEffectTarget cursor)
        {
            _direction = (direction == Vector3.zero) ? Vector3.forward : direction.normalized;
            _cursor = cursor;
        }

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

            // TODO: 테스트용으로 우선 사용. 트리거에 처음 겹친 순간의 총알 중심을 맞은 지점으로 쓴다
            //       (탄도 방향 최대 약 0.33m 오차, 정밀한 표면 진입점 계산은 추후 검토)
            _hitEffect.Apply(other.gameObject, new SEffectContext(_cursor, hitPoint: transform.position, direction: _direction));
        }

        /// <summary>
        /// 총알을 소멸시킨다. 풀 매니저가 있으면 반납하고(풀 출신이 아니면 매니저가 파괴한다), 없으면 파괴한다.
        /// </summary>
        // 매니저가 없는 씬에서도 맵 기믹·적이 Instantiate 로 쓰므로 Destroy 로 대체한다
        private void Despawn()
        {
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Release(gameObject);
            else
                Destroy(gameObject);
        }
        #endregion
    }
}
